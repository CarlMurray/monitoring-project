namespace Models.DataPoints;

using System;
using System.Data;

public interface INormalisedDataPoint
{
    public Guid Id { get; init; }
    public string Source { get; init; }
    public DateTime Timestamp { get; init; }
}

public class MetricDataPoint : INormalisedDataPoint
{
    public Guid Id { get; init; }
    public string Source { get; init; }
    public DateTime Timestamp { get; init; }
    public string Name { get; init; }
    public string Value { get; init; }

    public MetricDataPoint(string source, DateTime timestamp, string name, string value)
    {
        Id = Guid.NewGuid();
        Source = source;
        Timestamp = timestamp;
        Name = name;
        Value = value;
    }
}

public class EventDataPoint : INormalisedDataPoint
{
    public Guid Id { get; init; }
    public string Source { get; init; }
    public DateTime Timestamp { get; init; }
    public string Message { get; init; }

    public EventDataPoint(string source, DateTime timestamp, string message)
    {
        Id = Guid.NewGuid();
        Source = source;
        Timestamp = timestamp;
        Message = message;
    }
}