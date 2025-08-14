namespace AccessScheduler.Shared.DTOs;

public class ConflictBooking
{
    public Guid Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Resource { get; set; } = string.Empty;
}