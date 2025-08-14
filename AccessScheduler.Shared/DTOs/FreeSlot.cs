namespace AccessScheduler.Shared.DTOs;

public class FreeSlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Resource { get; set; } = string.Empty;
}