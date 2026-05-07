namespace LlTakeHome.Api.Models;

public sealed record WeatherForecastRecord(
    int Id,
    double Latitude,
    double Longitude,
    string? Timezone,
    double? Elevation,
    double? CurrentTemperatureCelsius,
    double? CurrentWindspeed,
    int? CurrentWindDirection,
    int? CurrentWeatherCode,
    DateTimeOffset? CurrentObservationTimeUtc,
    DateTimeOffset FetchedAtUtc);
