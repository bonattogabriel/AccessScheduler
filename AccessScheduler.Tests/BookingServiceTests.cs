using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AccessScheduler.Api.Data;
using AccessScheduler.Api.Services;
using AccessScheduler.Shared.DTOs;
using AccessScheduler.Shared.Models;
using Moq;
using System.Data;
using AccessScheduler.Shared.Exceptions;

namespace AccessScheduler.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly BookingService _service;
    private readonly Mock<ILogger<BookingService>> _loggerMock;

    public BookingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<BookingService>>();
        _service = new BookingService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_PersistsCorrectly()
    {
        var request = new BookingRequest
        {
            CustomerName = "João Silva",
            Document = "12345678901",
            Resource = "gate-1",
            Start = DateTime.Now.AddHours(2),
            End = DateTime.Now.AddHours(2.5),
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = -22.9068,
            Longitude = -43.1729
        };

        var result = await _service.CreateBookingAsync(request, "America/Sao_Paulo");

        Assert.NotNull(result);
        Assert.Equal(request.CustomerName, result.CustomerName);
        Assert.Equal(request.Document, result.Document);
        Assert.Equal(request.Resource, result.Resource);

        var savedBooking = await _context.Bookings.FindAsync(result.Id);
        Assert.NotNull(savedBooking);
        Assert.Equal(request.CustomerName, savedBooking.CustomerName);
    }

    [Fact]
    public async Task CreateBookingAsync_SimultaneousBookings_OnlyOnePersists()
    {
        var startTime = DateTime.Now.AddHours(2);
        var endTime = startTime.AddMinutes(30);

        var request1 = CreateValidBookingRequest("Cliente 1", startTime, endTime);
        var request2 = CreateValidBookingRequest("Cliente 2", startTime, endTime);

        var task1 = _service.CreateBookingAsync(request1, "America/Sao_Paulo");
        var task2 = _service.CreateBookingAsync(request2, "America/Sao_Paulo");

        var exceptions = new List<Exception>();
        var successfulResults = new List<BookingResponse>();

        try
        {
            var result1 = await task1;
            successfulResults.Add(result1);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        try
        {
            var result2 = await task2;
            successfulResults.Add(result2);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        Assert.Single(successfulResults);
        Assert.Single(exceptions); 
        Assert.IsType<ConcurrencyException>(exceptions.First());

        var bookingsCount = await _context.Bookings.CountAsync();
        Assert.Equal(1, bookingsCount);
    }

    [Fact]
    public async Task CreateBookingAsync_ConflictDetected_ReturnsSuggestions()
    {
        var existingStartTime = DateTime.Now.AddHours(2);
        var existingEndTime = existingStartTime.AddMinutes(30);

        var existingBooking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerName = "Cliente Existente",
            Document = "99999999999",
            Resource = "gate-1",
            StartUtc = existingStartTime.ToUniversalTime(),
            EndUtc = existingEndTime.ToUniversalTime(),
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = -22.9068,
            Longitude = -43.1729,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(existingBooking);
        await _context.SaveChangesAsync();

        var conflictingRequest = CreateValidBookingRequest("Novo Cliente", existingStartTime, existingEndTime);

        var exception = await Assert.ThrowsAsync<ConcurrencyException>(
            () => _service.CreateBookingAsync(conflictingRequest, "America/Sao_Paulo"));

        Assert.NotNull(exception.ConflictResponse);
        Assert.Equal("Conflito: já existe uma reserva neste intervalo.", exception.ConflictResponse.Message);
        Assert.NotNull(exception.ConflictResponse.ConflictWith);
        Assert.Equal(existingBooking.Id, exception.ConflictResponse.ConflictWith.Id);

        Assert.True(exception.ConflictResponse.AlternativeSlots.Count <= 3);
        Assert.True(exception.ConflictResponse.AlternativeSlots.Count > 0);
    }

    [Fact]
    public async Task GetFreeSlotsAsync_ValidDate_ReturnsAvailableSlots()
    {
        var testDate = DateTime.Today.AddDays(1);
        var resource = "gate-1";
        var duration = 30;

        var existingBooking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerName = "Cliente Teste",
            Document = "12345678901",
            Resource = resource,
            StartUtc = testDate.AddHours(10).ToUniversalTime(),
            EndUtc = testDate.AddHours(10.5).ToUniversalTime(),
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = -22.9068,
            Longitude = -43.1729,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(existingBooking);
        await _context.SaveChangesAsync();

        var freeSlots = await _service.GetFreeSlotsAsync(testDate, duration, resource, "America/Sao_Paulo");

        Assert.NotEmpty(freeSlots);

        var conflictingSlots = freeSlots.Where(slot =>
            slot.Start < testDate.AddHours(10.5) && slot.End > testDate.AddHours(10));

        Assert.Empty(conflictingSlots);

        var slotsBeforeBooking = freeSlots.Where(slot => slot.End <= testDate.AddHours(10));
        var slotsAfterBooking = freeSlots.Where(slot => slot.Start >= testDate.AddHours(10.5));

        Assert.NotEmpty(slotsBeforeBooking);
        Assert.NotEmpty(slotsAfterBooking);
    }

    [Fact]
    public async Task CancelBookingAsync_ExistingBooking_ReturnsTrue()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerName = "Cliente Teste",
            Document = "12345678901",
            Resource = "gate-1",
            StartUtc = DateTime.UtcNow.AddHours(2),
            EndUtc = DateTime.UtcNow.AddHours(2.5),
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = -22.9068,
            Longitude = -43.1729,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _service.CancelBookingAsync(booking.Id);

        Assert.True(result);

        var cancelledBooking = await _context.Bookings.FindAsync(booking.Id);
        Assert.Null(cancelledBooking);
    }

    [Fact]
    public async Task CancelBookingAsync_NonExistentBooking_ReturnsFalse()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _service.CancelBookingAsync(nonExistentId);

        Assert.False(result);
    }

    [Theory]
    [InlineData("", "Nome do cliente é obrigatório")]
    public async Task CreateBookingAsync_InvalidData_ThrowsArgumentException(string customerName, string expectedError)
    {
        var request = new BookingRequest
        {
            CustomerName = customerName,
            Document = "12345678901",
            Resource = "gate-1",
            Start = DateTime.Now.AddHours(2),
            End = DateTime.Now.AddHours(2.5),
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = -22.9068,
            Longitude = -43.1729
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateBookingAsync(request, "America/Sao_Paulo"));

        Assert.Contains(expectedError, exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_InvalidTimeRange_ThrowsArgumentException()
    {
        var request = new BookingRequest
        {
            CustomerName = "João Silva",
            Document = "12345678901",
            Resource = "gate-1",
            Start = DateTime.Now.AddHours(2),
            End = DateTime.Now.AddHours(1), // End before start
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = -22.9068,
            Longitude = -43.1729
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateBookingAsync(request, "America/Sao_Paulo"));

        Assert.Contains("Data de início deve ser anterior à data de fim", exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_PastBooking_ThrowsArgumentException()
    {
        var request = new BookingRequest
        {
            CustomerName = "João Silva",
            Document = "12345678901",
            Resource = "gate-1",
            Start = DateTime.Now.AddHours(-2),
            End = DateTime.Now.AddHours(-1),
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = -22.9068,
            Longitude = -43.1729
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateBookingAsync(request, "America/Sao_Paulo"));

        Assert.Contains("Não é possível agendar no passado", exception.Message);
    }

    private BookingRequest CreateValidBookingRequest(string customerName, DateTime start, DateTime end)
    {
        return new BookingRequest
        {
            CustomerName = customerName,
            Document = "12345678901",
            Resource = "gate-1",
            Start = start,
            End = end,
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = -22.9068,
            Longitude = -43.1729
        };
    }

    private byte[] CreateValidJpegBytes()
    {
        return new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0xFF, 0xD9
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
