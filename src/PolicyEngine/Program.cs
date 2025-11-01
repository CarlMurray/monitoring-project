using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Configuration;
using Models.Policies;

ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
IConfiguration configuration = configurationBuilder.AddUserSecrets<Program>().Build();
var connection = InitialiseDatabase();
PolicyEngine policyEngine = new(connection);

ClickHouseConnection InitialiseDatabase()
{
    var host = Environment.GetEnvironmentVariable("CH_HOST");
    var password = Environment.GetEnvironmentVariable("CH_PASSWORD");
    var connectionString = $"Host={host};Port=8443;Protocol=https;Database=default;Username=default;Password={password}";
    var connection = new ClickHouseConnection(connectionString);
    connection.Open();
    using (var command = connection.CreateCommand())
    {
        // command.CommandText = "DROP DATABASE cooked_metrics";
        // command.ExecuteNonQuery();
        command.CommandText = "CREATE DATABASE IF NOT EXISTS cooked_metrics";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE IF NOT EXISTS cooked_metrics.metrics (id String, CpuUtilisation String, Timestamp DateTime) ENGINE = MergeTree PRIMARY KEY (Timestamp, CpuUtilisation)";
        command.ExecuteNonQuery();
    }
    return connection;
}