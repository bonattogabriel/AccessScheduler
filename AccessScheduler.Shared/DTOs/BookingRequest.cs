using System.ComponentModel.DataAnnotations;

namespace AccessScheduler.Shared.DTOs;

public class BookingRequest
{
    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Document { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Resource { get; set; } = string.Empty;

    [Required]
    public DateTime Start { get; set; }

    [Required]
    public DateTime End { get; set; }

    [Required]
    public string RetratoBase64 { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }
}