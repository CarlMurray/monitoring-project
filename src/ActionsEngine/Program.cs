using Confluent.Kafka;
using System.Collections.Generic;

var config = new ConsumerConfig
{
    BootstrapServers = $"{Environment.GetEnvironmentVariable("KAFKA_HOST")}:9092",
    GroupId = "1",
};

using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
{
    consumer.Subscribe(Environment.GetEnvironmentVariable("KAFKA_TOPIC_ALERTS"));
    while (true)
    {
        var message = consumer.Consume();
        Console.WriteLine("MESSAGE READ BY ACTIONS ENGINE:");
        Console.WriteLine(message.Message.Value);
    }
}