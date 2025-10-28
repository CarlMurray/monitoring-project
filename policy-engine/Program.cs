using ClickHouse.Driver.ADO;
using ClickHouse.Driver.Utility;
using Microsoft.Extensions.Configuration;
using Models;


ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
IConfiguration configuration = configurationBuilder.AddUserSecrets<Program>().Build();
var connection = InitialiseDatabase();
var policy = new Policy("Check CPU utilisation", "CpuUtilisation", 15, ">");
Console.WriteLine($"Policy created: {policy.Name}");
var policyExecutor = new PolicyExecutor(null, new List<Log>(), policy);

while (true)
{
    policyExecutor.FetchResults(connection);
    policyExecutor.AnalyseResults();
    Thread.Sleep(2000);
}

ClickHouseConnection InitialiseDatabase()
{
    var host = configuration["host"];
    var password = configuration["password"];
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

public class PolicyExecutor
{
    public string? LastAnalysedLogId { get; set; } = null;
    public Policy Policy { get; set; }
    public List<Log> Results { get; set; } = [];

    public PolicyExecutor(string? lastAnalysedLogId, List<Log> results, Policy policy)
    {
        LastAnalysedLogId = lastAnalysedLogId;
        Results = results;
        Policy = policy;
    }

    public void AnalyseResults()
    {
        foreach (var result in Results)
        {
            Policy.Execute(result);
            LastAnalysedLogId = result.Id;
        }
    }

    public void FetchResults(ClickHouseConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            if (String.IsNullOrEmpty(LastAnalysedLogId))
            {
                command.CommandText = "SELECT * FROM cooked_metrics.metrics LIMIT 5";
            }
            else
            {
                command.AddParameter("last_analysed_log", LastAnalysedLogId);
                command.CommandText = "SELECT * FROM cooked_metrics.metrics WHERE id != {last_analysed_log:String} AND Timestamp >= (SELECT Timestamp from cooked_metrics.metrics WHERE id = {last_analysed_log:String})";
            }
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                Log log = new Log(reader.GetValue(0).ToString(), reader.GetDateTime(2).ToString(), reader.GetValue(1).ToString());
                Results.Add(log);
            }

        }
    }
}