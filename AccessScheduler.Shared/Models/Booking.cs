using System.ComponentModel.DataAnnotations;

namespace AccessScheduler.Shared.Models;

public class Booking
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string RetratoBase64 { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
