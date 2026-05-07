using LlTakeHome.Api.Models;

namespace LlTakeHome.Api.External;

internal interface IOpenMeteoClient
{
    Task<WeatherForecastRecord?> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken);
}
