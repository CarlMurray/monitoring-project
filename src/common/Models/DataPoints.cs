using System;

namespace Models.DataPoints;

public interface INormalisedDataPoint
{
    public Guid Id { get; set; }
    public double Value { get; set; }
}