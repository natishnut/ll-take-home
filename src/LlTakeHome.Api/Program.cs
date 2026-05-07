using LlTakeHome.Api.Data;
using LlTakeHome.Api.External;
using LlTakeHome.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DatabaseBootstrapper>();
builder.Services.AddScoped<IWeatherForecastStore, SqlWeatherForecastStore>();
builder.Services.AddScoped<WeatherForecastQueryService>();

builder.Services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>((_, client) =>
{
    var baseUrl = builder.Configuration["OpenMeteo:BaseUrl"] ?? "https://api.open-meteo.com/v1/";
    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>();
    await bootstrapper.EnsureSchemaAsync(CancellationToken.None);
}

app.MapGet("/", () => "LlTakeHome.Api");

app.MapGet("/api/weather-forecasts", async (WeatherForecastQueryService service, CancellationToken cancellationToken) =>
{
    var items = await service.ListCachedAsync(cancellationToken);
    return Results.Ok(items);
});

app.MapGet(
    "/api/weather-forecasts/location/{latitude:double}/{longitude:double}",
    async (double latitude, double longitude, WeatherForecastQueryService service, CancellationToken cancellationToken) =>
    {
        var item = await service.GetByLocationAsync(latitude, longitude, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    });

app.MapGet("/api/weather-forecasts/{id:int}", async (int id, WeatherForecastQueryService service, CancellationToken cancellationToken) =>
{
    var item = await service.GetByIdAsync(id, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

await app.RunAsync();
