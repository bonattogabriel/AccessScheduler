using Microsoft.EntityFrameworkCore;
using AccessScheduler.Api.Data;
using AccessScheduler.Shared.Models;
using AccessScheduler.Shared.DTOs;
using AccessScheduler.Shared.Extensions;
using AccessScheduler.Shared.Validators;
using AccessScheduler.API.Interfaces;
using System.Data;
using AccessScheduler.Shared.Exceptions;

namespace AccessScheduler.Api.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingService> _logger;

    public BookingService(AppDbContext context, ILogger<BookingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null)
            return false;

        _context.Bookings.Remove(booking);
        var result = await _context.SaveChangesAsync();

        if (result > 0)
            _logger.LogInformation("Booking cancelled successfully: {BookingId}", bookingId);

        return result > 0;
    }

    public async Task<BookingResponse> CreateBookingAsync(BookingRequest request, string timeZoneId)
    {
        var (isValid, errorMessage) = BookingValidator.ValidateBookingRequest(request);
        if (!isValid)
            throw new ArgumentException(errorMessage);

        var startUtc = request.Start.ConvertToUtc(timeZoneId);
        var endUtc = request.End.ConvertToUtc(timeZoneId);

        var conflictingBooking = await _context.Bookings
            .Where(b => b.Resource == request.Resource &&
                       b.StartUtc < endUtc &&
                       b.EndUtc > startUtc)
            .FirstOrDefaultAsync();

        if (conflictingBooking != null)
        {
            var alternativeSlots = await GetAlternativeSlotsAsync(request.Resource, startUtc, endUtc);

            var conflictResponse = new ConflictResponse
            {
                Message = "Conflito: já existe uma reserva neste intervalo.",
                ConflictWith = new ConflictBooking
                {
                    Id = conflictingBooking.Id,
                    Start = conflictingBooking.StartUtc.ConvertFromUtc(timeZoneId),
                    End = conflictingBooking.EndUtc.ConvertFromUtc(timeZoneId),
                    Resource = conflictingBooking.Resource
                },
                AlternativeSlots = alternativeSlots
            };

            throw new ConcurrencyException("Booking conflict", conflictResponse);
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            Document = request.Document,
            Resource = request.Resource,
            StartUtc = startUtc,
            EndUtc = endUtc,
            RetratoBase64 = request.RetratoBase64,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking created successfully: {BookingId}", booking.Id);

            return new BookingResponse
            {
                Id = booking.Id,
                CustomerName = booking.CustomerName,
                Document = booking.Document,
                Resource = booking.Resource,
                Start = booking.StartUtc.ConvertFromUtc(timeZoneId),
                End = booking.EndUtc.ConvertFromUtc(timeZoneId),
                Latitude = booking.Latitude,
                Longitude = booking.Longitude,
                CreatedAt = booking.CreatedAt
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning("Concurrency conflict during booking creation: {Error}", ex.Message);

            var alternativeSlots = await GetAlternativeSlotsAsync(request.Resource, startUtc, endUtc);
            var conflictResponse = new ConflictResponse
            {
                Message = "Conflito: já existe uma reserva neste intervalo.",
                AlternativeSlots = alternativeSlots
            };

            throw new ConcurrencyException("Concurrency conflict", conflictResponse);
        }
    }

    public async Task<List<FreeSlot>> GetFreeSlotsAsync(DateTime date, int durationMinutes, string resource, string timeZoneId)
    {
        var startOfDay = date.Date == DateTime.Now.Date ? date.ConvertToUtc(timeZoneId) : date.Date.ConvertToUtc(timeZoneId);
        var endOfDay = date.Date.AddDays(1).ConvertToUtc(timeZoneId);

        var bookings = await _context.Bookings
            .Where(b => b.Resource == resource &&
                       b.StartUtc < endOfDay &&
                       b.EndUtc > startOfDay)
            .OrderBy(b => b.StartUtc)
            .Select(b => new { b.StartUtc, b.EndUtc })
            .ToListAsync();

        var freeSlots = new List<FreeSlot>();

        var workingStartUtc = date.Date.AddHours(8).ConvertToUtc(timeZoneId);
        var workingEndUtc = date.Date.AddHours(18).ConvertToUtc(timeZoneId);

        var minimumTimeUtc = date.Date == DateTime.Now.Date
            ? DateTimeExtensions.MaxDateTime(workingStartUtc, DateTime.UtcNow)
            : workingStartUtc;

        var currentTime = DateTimeExtensions.MaxDateTime(minimumTimeUtc, startOfDay);
        var endTime = DateTimeExtensions.MinDateTime(workingEndUtc, endOfDay);

        foreach (var booking in bookings)
        {
            if (booking.StartUtc > currentTime)
            {
                var availableEnd = DateTimeExtensions.MinDateTime(booking.StartUtc, endTime);
                if ((availableEnd - currentTime).TotalMinutes >= durationMinutes)
                {
                    GenerateSlotsInRange(currentTime, availableEnd, durationMinutes, resource, timeZoneId, freeSlots, date.Date == DateTime.Now.Date);
                }
            }
            currentTime = DateTimeExtensions.MaxDateTime(currentTime, booking.EndUtc);
        }

        if (currentTime < endTime && (endTime - currentTime).TotalMinutes >= durationMinutes)
        {
            GenerateSlotsInRange(currentTime, endTime, durationMinutes, resource, timeZoneId, freeSlots, date.Date == DateTime.Now.Date);
        }

        return freeSlots;
    }

    private void GenerateSlotsInRange(DateTime startUtc, DateTime endUtc, int durationMinutes,
            string resource, string timeZoneId, List<FreeSlot> freeSlots, bool isToday)
    {
        var startLocal = startUtc.ConvertFromUtc(timeZoneId);
        var endLocal = endUtc.ConvertFromUtc(timeZoneId);

        DateTime slotStart;
        if (isToday)
        {
            var now = DateTime.Now;
            var minutes = now.Minute <= 30 ? 30 : 60;
            slotStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddMinutes(minutes);

            if (slotStart < startLocal)
            {
                slotStart = GetNextSlotTime(startLocal);
            }
        }
        else
        {
            slotStart = GetNextSlotTime(startLocal);
        }

        while (slotStart.AddMinutes(durationMinutes) <= endLocal)
        {
            if (!isToday || slotStart > DateTime.Now)
            {
                freeSlots.Add(new FreeSlot
                {
                    Start = slotStart,
                    End = slotStart.AddMinutes(durationMinutes),
                    Resource = resource
                });
            }

            slotStart = slotStart.AddMinutes(30);
        }
    }

    private DateTime GetNextSlotTime(DateTime time)
    {
        var minutes = time.Minute;

        if (minutes == 0 || minutes == 30)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, minutes, 0);
        }
        else if (minutes < 30)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, 30, 0);
        }
        else
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0).AddHours(1);
        }
    }

    public async Task<List<TimeSlot>> GetAlternativeSlotsAsync(string resource, DateTime startUtc, DateTime endUtc)
    {
        var duration = (int)(endUtc - startUtc).TotalMinutes;
        var searchDate = startUtc.Date;
        var timeZoneId = "America/Sao_Paulo";

        var freeSlots = await GetFreeSlotsAsync(searchDate, duration, resource, timeZoneId);

        return freeSlots
            .Select(slot => new TimeSlot { Start = slot.Start, End = slot.End })
            .OrderBy(slot => Math.Abs((slot.Start - startUtc.ConvertFromUtc(timeZoneId)).Ticks))
            .Take(3)
            .ToList();
    }
}