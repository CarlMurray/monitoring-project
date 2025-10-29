namespace Models;

public class Policy
{
    public Guid Id { get; } = new Guid();
    public string Name { get; set; } = "";
    public string Input { get; set; } = "";
    public string Operation { get; set; } = "";
    public float Threshold { get; set; }
    public string? TimeWindow { get; set; } = "";
    private Log CurrentLog { get; set; }

    public Policy(string name, string input, float threshold, string operation)
    {
        Name = name;
        Input = input;
        Threshold = threshold;
        Operation = operation;
    }

    public bool Evaluate(Log log)
    {
        if (Operation == ">") return Convert.ToDouble(log.CpuUtilisation) > Convert.ToDouble(Threshold);
        if (Operation == "<") return Convert.ToDouble(log.CpuUtilisation) < Convert.ToDouble(Threshold);
        if (Operation == "=") return Convert.ToDouble(log.CpuUtilisation) == Convert.ToDouble(Threshold);
        if (Operation == ">=") return Convert.ToDouble(log.CpuUtilisation) >= Convert.ToDouble(Threshold);
        if (Operation == "<=") return Convert.ToDouble(log.CpuUtilisation) <= Convert.ToDouble(Threshold);
        return false;
    }

    public void CreateAlert()
    {
        Console.WriteLine($"ALERT: Policy has triggered.\nLog ID: {CurrentLog.Id}\nMetric: {CurrentLog.CpuUtilisation}\nThreshold: {Threshold}\nTimestamp: {CurrentLog.Timestamp}");
    }

    public void Execute(Log log)
    {
        CurrentLog = log;
        if (this.Evaluate(log))
        {
            CreateAlert();
        }
    }
}

public record Log(string Id, string Timestamp, string CpuUtilisation);