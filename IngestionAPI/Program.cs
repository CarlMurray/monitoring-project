using System.Text.Json;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var config = new ProducerConfig
{
    BootstrapServers = "localhost:9092"
};

var producer = new ProducerBuilder<Null, string>(config).Build();
app.MapPost("/logs", (RawLog rawLog) =>
{
    var log = new RawLog(rawLog.Timestamp, rawLog.CpuUtilisation);


    var result = producer.ProduceAsync("test-topic", new Message<Null, string> { Value = JsonSerializer.Serialize(log) });

}).WithOpenApi();

app.Run();

record RawLog(string Timestamp, string CpuUtilisation)
{
    public string Id { get; } = Guid.NewGuid().ToString();
}
