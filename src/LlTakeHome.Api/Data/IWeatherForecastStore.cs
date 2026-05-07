using LlTakeHome.Api.Models;

namespace LlTakeHome.Api.Data;

internal interface IWeatherForecastStore
{
    Task<IReadOnlyList<WeatherForecastRecord>> ListAsync(CancellationToken cancellationToken);

    Task<WeatherForecastRecord?> TryGetByIdAsync(int id, CancellationToken cancellationToken);

    Task<WeatherForecastRecord?> TryGetByCoordinatesAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken);

    Task<WeatherForecastRecord> InsertAsync(WeatherForecastRecord record, CancellationToken cancellationToken);
}
