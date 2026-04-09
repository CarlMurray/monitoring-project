using System.Text.Json;
using ClickHouse.Driver.ADO;
using ClickHouse.Driver.Utility;
using Confluent.Kafka;

var clickhouseDatabase = Environment.GetEnvironmentVariable("CLICKHOUSE_DB");
var clickhouseUser = Environment.GetEnvironmentVariable("CLICKHOUSE_USER");
var clickhousePassword = Environment.GetEnvironmentVariable("CLICKHOUSE_PASSWORD");
var clickhouseHost = Environment.GetEnvironmentVariable("CLICKHOUSE_HOST");
var clickhousePort = Environment.GetEnvironmentVariable("CLICKHOUSE_PORT");

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
        command.CommandText = $"INSERT INTO {clickhouseDatabase}.metrics (id, CpuUtilisation, Timestamp) VALUES ({id:String},{CpuUtilisation:String},(parseDateTimeBestEffort({Timestamp:String})))";
        command.ExecuteNonQuery();
    }
}

ClickHouseConnection InitialiseDatabase()
{
    var connectionString = $"Host={clickhouseHost};Port={clickhousePort};Protocol=http;Database={clickhouseDatabase};Username={clickhouseUser};Password={clickhousePassword}";
    var connection = new ClickHouseConnection(connectionString);
    connection.Open();
    using (var command = connection.CreateCommand())
    {
        // command.CommandText = "DROP DATABASE cooked_metrics";
        // command.ExecuteNonQuery();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS {clickhouseDatabase}";
        command.ExecuteNonQuery();
        command.CommandText = $"CREATE TABLE IF NOT EXISTS {clickhouseDatabase}.metrics (id String, CpuUtilisation String, Timestamp DateTime) ENGINE = MergeTree PRIMARY KEY (Timestamp, CpuUtilisation)";
        command.ExecuteNonQuery();
    }
    return connection;
}

IConsumer<Ignore, string> InitialiseKafkaConsumer()
{
    var config = new ConsumerConfig
    {
        BootstrapServers = $"{Environment.GetEnvironmentVariable("KAFKA_HOST")}:9092",
        GroupId = "1",
    };

    var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

    consumer.Subscribe([$"{Environment.GetEnvironmentVariable("KAFKA_TOPIC_LOGS")}"]);
    return consumer;
}

record Log(string Id, string Timestamp, string CpuUtilisation);
