using AccessScheduler.Shared.DTOs;
using AccessScheduler.Shared.Validators;

namespace AccessScheduler.Tests;

public class BookingValidatorTests
{
    [Fact]
    public void ValidateBookingRequest_ValidRequest_ReturnsTrue()
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

        var (isValid, errorMessage) = BookingValidator.ValidateBookingRequest(request);

        Assert.True(isValid);
        Assert.Empty(errorMessage);
    }

    [Theory]
    [InlineData(-91.0, -43.1729, false, "Latitude deve estar entre -90 e 90")]
    [InlineData(91.0, -43.1729, false, "Latitude deve estar entre -90 e 90")]
    [InlineData(-22.9068, -181.0, false, "Longitude deve estar entre -180 e 180")]
    [InlineData(-22.9068, 181.0, false, "Longitude deve estar entre -180 e 180")]
    [InlineData(-90.0, -180.0, true, "")]
    [InlineData(90.0, 180.0, true, "")]
    public void ValidateBookingRequest_CoordinateValidation(double latitude, double longitude, bool expectedValid, string expectedError)
    {
        var request = new BookingRequest
        {
            CustomerName = "João Silva",
            Document = "12345678901",
            Resource = "gate-1",
            Start = DateTime.Now.AddHours(2),
            End = DateTime.Now.AddHours(2.5),
            RetratoBase64 = Convert.ToBase64String(CreateValidJpegBytes()),
            Latitude = latitude,
            Longitude = longitude
        };

        var (isValid, errorMessage) = BookingValidator.ValidateBookingRequest(request);

        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
            Assert.Contains(expectedError, errorMessage);
    }

    [Fact]
    public void ValidateBookingRequest_OversizedImage_ReturnsFalse()
    {
        var largeImageBytes = new byte[1024 * 1024 + 1];
        largeImageBytes[0] = 0xFF;
        largeImageBytes[1] = 0xD8;
        largeImageBytes[2] = 0xFF;

        var request = new BookingRequest
        {
            CustomerName = "João Silva",
            Document = "12345678901",
            Resource = "gate-1",
            Start = DateTime.Now.AddHours(2),
            End = DateTime.Now.AddHours(2.5),
            RetratoBase64 = Convert.ToBase64String(largeImageBytes),
            Latitude = -22.9068,
            Longitude = -43.1729
        };

        var (isValid, errorMessage) = BookingValidator.ValidateBookingRequest(request);

        Assert.False(isValid);
        Assert.Contains("Imagem deve ter no máximo 1MB", errorMessage);
    }

    [Fact]
    public void ValidateBookingRequest_InvalidImageFormat_ReturnsFalse()
    {
        var invalidImageBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        var request = new BookingRequest
        {
            CustomerName = "João Silva",
            Document = "12345678901",
            Resource = "gate-1",
            Start = DateTime.Now.AddHours(2),
            End = DateTime.Now.AddHours(2.5),
            RetratoBase64 = Convert.ToBase64String(invalidImageBytes),
            Latitude = -22.9068,
            Longitude = -43.1729
        };

        var (isValid, errorMessage) = BookingValidator.ValidateBookingRequest(request);

        Assert.False(isValid);
        Assert.Contains("Formato de imagem não suportado", errorMessage);
    }

    private byte[] CreateValidJpegBytes()
    {
        return new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0xFF, 0xD9
        };
    }
}
