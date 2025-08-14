namespace AccessScheduler.Shared.DTOs;

public class ConflictResponse
{
    public string Message { get; set; } = string.Empty;
    public ConflictBooking ConflictWith { get; set; } = new();
    public List<AccessScheduler.Shared.DTOs.TimeSlot> AlternativeSlots { get; set; } = new();
}