using AccessScheduler.Shared.DTOs;

namespace AccessScheduler.API.Interfaces;

public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(BookingRequest request, string timeZoneId);
    Task<bool> CancelBookingAsync(Guid bookingId);
    Task<List<FreeSlot>> GetFreeSlotsAsync(DateTime date, int durationMinutes, string resource, string timeZoneId);
    Task<List<TimeSlot>> GetAlternativeSlotsAsync(string resource, DateTime startUtc, DateTime endUtc);
}