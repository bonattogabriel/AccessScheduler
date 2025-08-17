using Microsoft.JSInterop;

namespace AccessScheduler.Blazor.Services;

public class GeolocationService
{
    private readonly IJSRuntime _jsRuntime;

    public GeolocationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<GeolocationPosition?> GetCurrentPositionAsync()
    {
        try
        {
            var position = await _jsRuntime.InvokeAsync<GeolocationPosition?>("getCurrentPosition");
            return position;
        }
        catch (JSException jsEx)
        {
            Console.WriteLine($"Erro JavaScript ao obter localização: {jsEx.Message}");
            return GetDefaultPosition();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro geral ao obter localização: {ex.Message}");
            return GetDefaultPosition();
        }
    }

    private GeolocationPosition GetDefaultPosition()
    {
        return new GeolocationPosition
        {
            Latitude = -22.9068,
            Longitude = -43.1729
        };
    }

    public async Task<GeolocationPosition> GetCurrentPositionWithFallbackAsync()
    {
        var position = await GetCurrentPositionAsync();
        return position ?? GetDefaultPosition();
    }
}
