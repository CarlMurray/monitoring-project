using System.Text.Json;
using ClickHouse.Driver.ADO;
using ClickHouse.Driver.Utility;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
IConfiguration configuration = configurationBuilder.AddUserSecrets<Program>().Build();

var consumer = InitialiseKafkaConsumer();
var connection = InitialiseDatabase();

while (true)
{
    var consumeResult = consumer.Consume();
    Console.WriteLine(consumeResult.Message.Value);
    Log log = JsonSerializer.Deserialize<Log>(consumeResult.Message.Value)!;
    using (var command = connection.CreateCommand())
    {
        command.AddParameter("id", log.Id);
        command.AddParameter("CpuUtilisation", log.CpuUtilisation);
        command.AddParameter("Timestamp", log.Timestamp);
        command.CommandText = "INSERT INTO cooked_metrics.metrics (id, CpuUtilisation, Timestamp) VALUES ({id:String},{CpuUtilisation:String},(parseDateTimeBestEffort({Timestamp:String})))";
        command.ExecuteNonQuery();
    }
}

ClickHouseConnection InitialiseDatabase()
{
    var host = configuration["clickHouseHost"];
    var password = configuration["clickHousePassword"];
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

IConsumer<Ignore, string> InitialiseKafkaConsumer()
{
    var config = new ConsumerConfig
    {
        BootstrapServers = "localhost:9092",
        GroupId = "1",
    };

    var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

    consumer.Subscribe(["test-topic"]);
    return consumer;
}

record Log(string Id, string Timestamp, string CpuUtilisation);
