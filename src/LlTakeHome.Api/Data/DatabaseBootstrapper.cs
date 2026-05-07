using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace LlTakeHome.Api.Data;

internal sealed class DatabaseBootstrapper(IConfiguration configuration, ILogger<DatabaseBootstrapper> logger)
{
    private static readonly Regex ValidDatabaseName = new(
        "^[A-Za-z0-9_]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private const string CreateTableSql =
        """
        IF NOT EXISTS (
            SELECT 1
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE t.name = N'WeatherForecasts' AND s.name = N'dbo')
        BEGIN
            CREATE TABLE dbo.WeatherForecasts (
                Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WeatherForecasts PRIMARY KEY,
                Latitude DECIMAL(9,6) NOT NULL,
                Longitude DECIMAL(9,6) NOT NULL,
                Timezone NVARCHAR(128) NULL,
                Elevation FLOAT NULL,
                CurrentTemperatureCelsius FLOAT NULL,
                CurrentWindspeed FLOAT NULL,
                CurrentWindDirection INT NULL,
                CurrentWeatherCode INT NULL,
                CurrentObservationTimeUtc DATETIME2(7) NULL,
                FetchedAtUtc DATETIME2(7) NOT NULL CONSTRAINT DF_WeatherForecasts_FetchedAtUtc DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT UQ_WeatherForecasts_LatLon UNIQUE (Latitude, Longitude)
            );
        END
        """;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = string.IsNullOrWhiteSpace(builder.InitialCatalog)
            ? "LlTakeHome"
            : builder.InitialCatalog;

        if (databaseName.Length is <= 0 or > 128 || !ValidDatabaseName.IsMatch(databaseName))
        {
            throw new InvalidOperationException(
                $"Database name '{databaseName}' must be 1–128 characters and contain only letters, digits, or underscore.");
        }

        var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };

        await using (var masterConnection = new SqlConnection(masterBuilder.ConnectionString))
        {
            await masterConnection.OpenAsync(cancellationToken);
            await using var command = masterConnection.CreateCommand();
            var bracketed = BracketQuote(databaseName);
            command.CommandText = $"IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NULL CREATE DATABASE {bracketed};";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        builder.InitialCatalog = databaseName;
        await using var catalogConnection = new SqlConnection(builder.ConnectionString);
        await catalogConnection.OpenAsync(cancellationToken);

        await using var ddl = catalogConnection.CreateCommand();
        ddl.CommandText = CreateTableSql;
        await ddl.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Database schema verified (database {Database}, table dbo.WeatherForecasts).", databaseName);
    }

    private static string BracketQuote(string name) => "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]";
}
