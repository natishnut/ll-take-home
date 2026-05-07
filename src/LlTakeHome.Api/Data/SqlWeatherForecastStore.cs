using System.Data;
using LlTakeHome.Api.Models;
using Microsoft.Data.SqlClient;

namespace LlTakeHome.Api.Data;

internal sealed class SqlWeatherForecastStore(IConfiguration configuration) : IWeatherForecastStore
{
    private SqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        return new SqlConnection(connectionString);
    }

    public async Task<IReadOnlyList<WeatherForecastRecord>> ListAsync(CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT Id, Latitude, Longitude, Timezone, Elevation, CurrentTemperatureCelsius, CurrentWindspeed,
                   CurrentWindDirection, CurrentWeatherCode, CurrentObservationTimeUtc, FetchedAtUtc
            FROM dbo.WeatherForecasts
            ORDER BY Id;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var results = new List<WeatherForecastRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task<WeatherForecastRecord?> TryGetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT Id, Latitude, Longitude, Timezone, Elevation, CurrentTemperatureCelsius, CurrentWindspeed,
                   CurrentWindDirection, CurrentWeatherCode, CurrentObservationTimeUtc, FetchedAtUtc
            FROM dbo.WeatherForecasts
            WHERE Id = @id;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@id", id));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<WeatherForecastRecord?> TryGetByCoordinatesAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT Id, Latitude, Longitude, Timezone, Elevation, CurrentTemperatureCelsius, CurrentWindspeed,
                   CurrentWindDirection, CurrentWeatherCode, CurrentObservationTimeUtc, FetchedAtUtc
            FROM dbo.WeatherForecasts
            WHERE Latitude = @latitude AND Longitude = @longitude;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@latitude", SqlDbType.Decimal) { Precision = 9, Scale = 6, Value = latitude });
        command.Parameters.Add(new SqlParameter("@longitude", SqlDbType.Decimal) { Precision = 9, Scale = 6, Value = longitude });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<WeatherForecastRecord> InsertAsync(WeatherForecastRecord record, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO dbo.WeatherForecasts (
                Latitude, Longitude, Timezone, Elevation, CurrentTemperatureCelsius, CurrentWindspeed,
                CurrentWindDirection, CurrentWeatherCode, CurrentObservationTimeUtc, FetchedAtUtc)
            OUTPUT INSERTED.Id, INSERTED.Latitude, INSERTED.Longitude, INSERTED.Timezone, INSERTED.Elevation,
                   INSERTED.CurrentTemperatureCelsius, INSERTED.CurrentWindspeed, INSERTED.CurrentWindDirection,
                   INSERTED.CurrentWeatherCode, INSERTED.CurrentObservationTimeUtc, INSERTED.FetchedAtUtc
            VALUES (
                @latitude, @longitude, @timezone, @elevation, @currentTemperatureCelsius, @currentWindspeed,
                @currentWindDirection, @currentWeatherCode, @currentObservationTimeUtc, @fetchedAtUtc);
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var lat = (decimal)record.Latitude;
        var lon = (decimal)record.Longitude;
        command.Parameters.Add(new SqlParameter("@latitude", SqlDbType.Decimal) { Precision = 9, Scale = 6, Value = lat });
        command.Parameters.Add(new SqlParameter("@longitude", SqlDbType.Decimal) { Precision = 9, Scale = 6, Value = lon });
        command.Parameters.Add(new SqlParameter("@timezone", SqlDbType.NVarChar, 128) { Value = record.Timezone ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@elevation", SqlDbType.Float) { Value = record.Elevation ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@currentTemperatureCelsius", SqlDbType.Float)
        {
            Value = record.CurrentTemperatureCelsius ?? (object)DBNull.Value,
        });
        command.Parameters.Add(new SqlParameter("@currentWindspeed", SqlDbType.Float)
        {
            Value = record.CurrentWindspeed ?? (object)DBNull.Value,
        });
        command.Parameters.Add(new SqlParameter("@currentWindDirection", SqlDbType.Int)
        {
            Value = record.CurrentWindDirection ?? (object)DBNull.Value,
        });
        command.Parameters.Add(new SqlParameter("@currentWeatherCode", SqlDbType.Int)
        {
            Value = record.CurrentWeatherCode ?? (object)DBNull.Value,
        });
        command.Parameters.Add(new SqlParameter("@currentObservationTimeUtc", SqlDbType.DateTime2)
        {
            Value = record.CurrentObservationTimeUtc.HasValue
                ? record.CurrentObservationTimeUtc.Value.UtcDateTime
                : DBNull.Value,
        });
        command.Parameters.Add(new SqlParameter("@fetchedAtUtc", SqlDbType.DateTime2) { Value = record.FetchedAtUtc.UtcDateTime });

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("INSERT did not return a row.");
            }

            return Map(reader);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            var existing = await TryGetByCoordinatesAsync(lat, lon, cancellationToken);
            return existing
                ?? throw new InvalidOperationException("Duplicate key but row could not be reloaded.", ex);
        }
    }

    private static WeatherForecastRecord Map(SqlDataReader reader)
    {
        var id = reader.GetInt32(0);
        var latitude = (double)reader.GetDecimal(1);
        var longitude = (double)reader.GetDecimal(2);
        var timezone = reader.IsDBNull(3) ? null : reader.GetString(3);
        double? elevation = reader.IsDBNull(4) ? null : reader.GetDouble(4);
        double? temp = reader.IsDBNull(5) ? null : reader.GetDouble(5);
        double? wind = reader.IsDBNull(6) ? null : reader.GetDouble(6);
        int? windDir = reader.IsDBNull(7) ? null : reader.GetInt32(7);
        int? code = reader.IsDBNull(8) ? null : reader.GetInt32(8);
        DateTimeOffset? obs = null;
        if (!reader.IsDBNull(9))
        {
            var dt = DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc);
            obs = new DateTimeOffset(dt);
        }

        var fetched = DateTime.SpecifyKind(reader.GetDateTime(10), DateTimeKind.Utc);

        return new WeatherForecastRecord(
            id,
            latitude,
            longitude,
            timezone,
            elevation,
            temp,
            wind,
            windDir,
            code,
            obs,
            new DateTimeOffset(fetched));
    }
}
