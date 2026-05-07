using LlTakeHome.Api.Data;
using LlTakeHome.Api.External;
using LlTakeHome.Api.Models;

namespace LlTakeHome.Api.Services;

internal sealed class WeatherForecastQueryService(
    IWeatherForecastStore store,
    IOpenMeteoClient remoteClient,
    ILogger<WeatherForecastQueryService> logger)
{
    public Task<IReadOnlyList<WeatherForecastRecord>> ListCachedAsync(CancellationToken cancellationToken) =>
        store.ListAsync(cancellationToken);

    public async Task<WeatherForecastRecord?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await store.TryGetByIdAsync(id, cancellationToken);

    public async Task<WeatherForecastRecord?> GetByLocationAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        latitude = NormalizeCoordinate(latitude);
        longitude = NormalizeCoordinate(longitude);
        var latDec = ToDecimalCoordinate(latitude);
        var lonDec = ToDecimalCoordinate(longitude);

        var cached = await store.TryGetByCoordinatesAsync(latDec, lonDec, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug(
                "Forecast for ({Latitude}, {Longitude}) served from database cache.",
                latitude,
                longitude);
            return cached;
        }

        var remote = await remoteClient.GetForecastAsync(latitude, longitude, cancellationToken);
        if (remote is null)
        {
            return null;
        }

        var normalizedRemote = remote with
        {
            Latitude = latitude,
            Longitude = longitude,
        };

        return await store.InsertAsync(normalizedRemote, cancellationToken);
    }

    private static double NormalizeCoordinate(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static decimal ToDecimalCoordinate(double value) =>
        Math.Round((decimal)value, 6, MidpointRounding.AwayFromZero);
}
