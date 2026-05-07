using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using LlTakeHome.Api.Models;

namespace LlTakeHome.Api.External;

internal sealed class OpenMeteoClient(HttpClient httpClient, ILogger<OpenMeteoClient> logger) : IOpenMeteoClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<WeatherForecastRecord?> GetForecastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        using var response = await httpClient.GetAsync(
            $"forecast?latitude={lat}&longitude={lon}&current_weather=true",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Open-Meteo returned {StatusCode} for latitude={Latitude} longitude={Longitude}.",
                (int)response.StatusCode,
                latitude,
                longitude);
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<OpenMeteoForecastResponse>(
            SerializerOptions,
            cancellationToken);

        if (dto is null)
        {
            logger.LogWarning(
                "Open-Meteo returned an empty body for latitude={Latitude} longitude={Longitude}.",
                latitude,
                longitude);
            return null;
        }

        var fetchedAt = DateTimeOffset.UtcNow;
        DateTimeOffset? observationUtc = null;
        if (dto.CurrentWeather?.Time is { } timeStr
            && DateTimeOffset.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            observationUtc = parsed;
        }

        var cw = dto.CurrentWeather;
        return new WeatherForecastRecord(
            0,
            latitude,
            longitude,
            dto.Timezone,
            dto.Elevation,
            cw?.Temperature,
            cw?.Windspeed,
            cw?.Winddirection,
            cw?.Weathercode,
            observationUtc,
            fetchedAt);
    }
}
