using System.ComponentModel;
using System.Text;
using System.Text.Json;

var client = new HttpClient();

Queue queue = new Queue();
queue.File.Close(); // ensure file is closed
while (true)
{
    string log = GenerateLog();
    Console.WriteLine(log);
    queue.WriteToQueue(log);
    var content = new StringContent(log, Encoding.UTF8, "application/json");
    try
    {
        await client.PostAsync("http://localhost:8081/logs", content);
    }
    catch (Exception e)
    {
        Console.WriteLine("There was an error with the post request: ");
        Console.WriteLine(e);
        Console.WriteLine("Writing log to queue...");
    }
}

string[] GetRawTopCommandOutput()
{
    System.Diagnostics.Process p = new System.Diagnostics.Process();
    p.StartInfo.FileName = "/bin/zsh";
    p.StartInfo.Arguments = "-c \"/usr/bin/top -n 0 -l 3 | /usr/bin/tail -n 13\"";
    p.StartInfo.UseShellExecute = false;
    p.StartInfo.CreateNoWindow = true;
    p.StartInfo.RedirectStandardOutput = true;
    p.Start();
    p.WaitForExit();
    return p.StandardOutput.ReadToEnd().Split("\n");
}

string GetTimestamp(string[] output)
{
    String timestampLine = output[2];
    string[] timeAndDate = timestampLine.Split(" ");
    var date = DateOnly.Parse(timeAndDate[0]);
    var time = TimeOnly.Parse(timeAndDate[1]);
    var datetime = new DateTime(date, time).ToString("u");
    return datetime;
}

string GetCpuUtilisation(string[] output)
{
    string cpuUsageLine = output[4];
    string cpuUsageIdle = cpuUsageLine.Split(",")[2].Replace(" idle", "").Trim().Replace("%", "");
    double cpuUsage = Math.Round(100 - Convert.ToDouble(cpuUsageIdle), 2);
    return cpuUsage.ToString();
}

string GenerateLog()
{
    var output = GetRawTopCommandOutput();
    string timestamp = GetTimestamp(output);
    string cpuUtilisation = GetCpuUtilisation(output);
    var log = new RawLog(timestamp, cpuUtilisation);
    var jsonLog = JsonSerializer.Serialize<RawLog>(log);
    return jsonLog;
}
record RawLog(string Timestamp, string CpuUtilisation);

public class Queue
{
    public FileStream File { get; }
    public string Path { get; } = "/Users/carlmurray/Desktop/Cmonitor/queue.log";
    public int MaxLines { get; } = 1000;
    public int LineCount { get => CountLines(); }

    public Queue()
    {
        Console.WriteLine("Creating log file...");
        File = System.IO.File.Create(Path);
    }

    public void WriteToQueue(string log)
    {
        Console.WriteLine("Appending log to file...");
        if (LineCount >= MaxLines)
        {
            var linesToWrite = System.IO.File.ReadAllLines(Path).Skip(1);
            linesToWrite.Append(log);
            System.IO.File.WriteAllLines(Path, linesToWrite);
        }
        else
        {
            System.IO.File.AppendAllLines(Path, new List<string>() { log });
        }
        Console.WriteLine("Text added to log file.");
    }

    public int CountLines()
    {
        var count = System.IO.File.ReadLines(Path).Count();
        return count;
    }
}