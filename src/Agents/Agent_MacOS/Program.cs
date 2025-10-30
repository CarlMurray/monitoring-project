using System.Text;
using System.Text.Json;

var client = new HttpClient();

while (true)
{
    string log = GenerateLog();
    Console.WriteLine(log);
    var content = new StringContent(log, Encoding.UTF8, "application/json");
    try
    {
        await client.PostAsync("http://localhost:8081/logs", content);
    }
    catch (Exception e)
    {
        Console.WriteLine("There was an error with the post request: ");
        Console.WriteLine(e);
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
    var jsonLog = JsonSerializer.Serialize(new { timestamp = timestamp, cpuUtilisation = cpuUtilisation });
    return jsonLog;
}
