using AccessScheduler.Shared.DTOs;
using System.Text.RegularExpressions;

namespace AccessScheduler.Shared.Validators;

public static class BookingValidator
{
    private const int MaxImageSizeBytes = 1024 * 1024;
    private static readonly Regex Base64Regex = new(@"^[A-Za-z0-9+/]*={0,2}$");

    public static (bool IsValid, string ErrorMessage) ValidateBookingRequest(BookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return (false, "Nome do cliente é obrigatório");

        if (string.IsNullOrWhiteSpace(request.Document))
            return (false, "Documento é obrigatório");

        if (string.IsNullOrWhiteSpace(request.Resource))
            return (false, "Recurso é obrigatório");

        if (request.Start >= request.End)
            return (false, "Data de início deve ser anterior à data de fim");

        if (request.Start < DateTime.Now.AddMinutes(-5))
            return (false, "Não é possível agendar no passado");

        if (!IsValidLatitude(request.Latitude))
            return (false, "Latitude deve estar entre -90 e 90");

        if (!IsValidLongitude(request.Longitude))
            return (false, "Longitude deve estar entre -180 e 180");

        var (isValidImage, imageError) = ValidateBase64Image(request.RetratoBase64);
        if (!isValidImage)
            return (false, imageError);

        return (true, string.Empty);
    }

    private static bool IsValidLatitude(double latitude) => latitude >= -90 && latitude <= 90;
    private static bool IsValidLongitude(double longitude) => longitude >= -180 && longitude <= 180;

    private static (bool IsValid, string ErrorMessage) ValidateBase64Image(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return (false, "Retrato é obrigatório");

        var base64Data = base64;
        if (base64.StartsWith("data:image"))
        {
            var commaIndex = base64.IndexOf(',');
            if (commaIndex > 0)
                base64Data = base64.Substring(commaIndex + 1);
        }

        if (!Base64Regex.IsMatch(base64Data))
            return (false, "Formato de imagem inválido");

        try
        {
            var bytes = Convert.FromBase64String(base64Data);
            if (bytes.Length > MaxImageSizeBytes)
                return (false, "Imagem deve ter no máximo 1MB");

            if (!IsValidImageFormat(bytes))
                return (false, "Formato de imagem não suportado. Use JPEG ou PNG");

            return (true, string.Empty);
        }
        catch
        {
            return (false, "Formato de imagem inválido");
        }
    }

    private static bool IsValidImageFormat(byte[] bytes)
    {
        if (bytes.Length < 4) return false;

        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return true;

        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return true;

        return false;
    }
}