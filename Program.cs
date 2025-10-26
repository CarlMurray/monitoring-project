using System.Text.Json;

while (true)
{
    string log = GenerateLog();
    Console.WriteLine(log);
    Thread.Sleep(2000);
    // TODO: Add logic to send data to Ingestion API
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
    var datetime = new DateTime(date, time, DateTimeKind.Utc).ToString();
    return datetime;
}

string GetCpuUtilisation(string[] output)
{
    string cpuUsageLine = output[4];
    string cpuUsage = cpuUsageLine.Split(",")[2].Replace(" idle", "");
    return cpuUsage;
}

string GenerateLog()
{
    var output = GetRawTopCommandOutput();
    string timestamp = GetTimestamp(output);
    string cpuUtilisation = GetCpuUtilisation(output);
    var jsonLog = JsonSerializer.Serialize(new { timestamp = timestamp, cpuUtilisation = cpuUtilisation });
    return jsonLog;
}
