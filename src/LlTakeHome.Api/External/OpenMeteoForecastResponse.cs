using System.Text.Json.Serialization;

namespace LlTakeHome.Api.External;

internal sealed class OpenMeteoForecastResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("elevation")]
    public double? Elevation { get; init; }

    [JsonPropertyName("current_weather")]
    public OpenMeteoCurrentWeather? CurrentWeather { get; init; }
}

internal sealed class OpenMeteoCurrentWeather
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    [JsonPropertyName("windspeed")]
    public double Windspeed { get; init; }

    [JsonPropertyName("winddirection")]
    public int Winddirection { get; init; }

    [JsonPropertyName("weathercode")]
    public int Weathercode { get; init; }

    [JsonPropertyName("time")]
    public string? Time { get; init; }
}
