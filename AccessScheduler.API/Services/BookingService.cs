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

    public async Task<List<FreeSlot>> GetFreeSlotsAsync(DateTime date, int durationMinutes, string resource, string timeZoneId)
    {
        var startOfDay = date.Date.ConvertToUtc(timeZoneId);
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

        var currentTime = DateTimeExtensions.MaxDateTime(workingStartUtc, startOfDay);
        var endTime = DateTimeExtensions.MinDateTime(workingEndUtc, endOfDay);

        foreach (var booking in bookings)
        {
            if (booking.StartUtc > currentTime)
            {
                var availableEnd = DateTimeExtensions.MinDateTime(booking.StartUtc, endTime);
                if ((availableEnd - currentTime).TotalMinutes >= durationMinutes)
                {
                    GenerateSlotsInRange(currentTime, availableEnd, durationMinutes, resource, timeZoneId, freeSlots);
                }
            }
            currentTime = DateTimeExtensions.MaxDateTime(currentTime, booking.EndUtc);
        }

        if (currentTime < endTime && (endTime - currentTime).TotalMinutes >= durationMinutes)
        {
            GenerateSlotsInRange(currentTime, endTime, durationMinutes, resource, timeZoneId, freeSlots);
        }

        return freeSlots;
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

    private void GenerateSlotsInRange(DateTime startUtc, DateTime endUtc, int durationMinutes,
        string resource, string timeZoneId, List<FreeSlot> freeSlots)
    {
        var current = startUtc;

        while (current.AddMinutes(durationMinutes) <= endUtc)
        {
            freeSlots.Add(new FreeSlot
            {
                Start = current.ConvertFromUtc(timeZoneId),
                End = current.AddMinutes(durationMinutes).ConvertFromUtc(timeZoneId),
                Resource = resource
            });

            current = current.AddMinutes(30);
        }
    }
}