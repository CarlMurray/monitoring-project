using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Configuration;
using Models.Policies;
using Models.DataPoints;

ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
IConfiguration configuration = configurationBuilder.AddUserSecrets<Program>().Build();
var connection = InitialiseDatabase();
PolicyEngine policyEngine = new(connection);
while (true)
{
    foreach (var policy in policyEngine.Policies)
    {
        Console.WriteLine(policy.Name, policy.Description);
    }
}

ClickHouseConnection InitialiseDatabase()
{
    var clickhouseHost = Environment.GetEnvironmentVariable("CLICKHOUSE_HOST");
    var clickhouseUser = Environment.GetEnvironmentVariable("CLICKHOUSE_USER");
    var clickhousePassword = Environment.GetEnvironmentVariable("CLICKHOUSE_PASSWORD");
    var clickhousePort = Environment.GetEnvironmentVariable("CLICKHOUSE_PORT");
    var clickhouseDb = Environment.GetEnvironmentVariable("CLICKHOUSE_DB");
    var connectionString = $"Host={clickhouseHost};Port={clickhousePort};Protocol=http;Database={clickhouseDb};Username={clickhouseUser};Password={clickhousePassword}";
    var connection = new ClickHouseConnection(connectionString);
    Console.WriteLine(connectionString);
    connection.Open();
    using (var command = connection.CreateCommand())
    {
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS {clickhouseDb}";
        command.ExecuteNonQuery();
        command.CommandText = $"CREATE TABLE IF NOT EXISTS {clickhouseDb}.metrics (id String, CpuUtilisation String, Timestamp DateTime) ENGINE = MergeTree PRIMARY KEY (Timestamp, CpuUtilisation)";
        command.ExecuteNonQuery();
        command.CommandText = $"CREATE TABLE IF NOT EXISTS {clickhouseDb}.policies (id String, state Boolean, name String, description String, conditions JSON) ENGINE = MergeTree PRIMARY KEY (id)";
        command.ExecuteNonQuery();
        PolicyEngine.CreateDummyPolicies(connection);
    }
    return connection;
}