using System.Net.Http.Json;
using System.Text.Json;
using AccessScheduler.Shared.DTOs
;

namespace AccessScheduler.Blazor.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BookingResponse?> CreateBookingAsync(BookingRequest request, string timeZone)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Client-TimeZone", timeZone);

            var response = await _httpClient.PostAsJsonAsync("book", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BookingResponse>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var conflictContent = await response.Content.ReadAsStringAsync();
                var conflictResponse = JsonSerializer.Deserialize<ConflictResponse>(conflictContent,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                throw new BookingConflictException("Conflito de reserva", conflictResponse);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new ApiException($"Erro na API: {response.StatusCode} - {errorContent}");
        }
        catch (BookingConflictException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar reserva");
            throw new ApiException("Erro ao comunicar com a API", ex);
        }
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"book/{bookingId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar reserva {BookingId}", bookingId);
            return false;
        }
    }

    public async Task<List<FreeSlot>> GetFreeSlotsAsync(DateTime date, int duration, string resource, string timeZone)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Client-TimeZone", timeZone);

            var dateStr = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"free-slots?date={dateStr}&duration={duration}&resource={resource}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<FreeSlot>>() ?? new();
            }

            return new List<FreeSlot>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar horários livres");
            return new List<FreeSlot>();
        }
    }
}