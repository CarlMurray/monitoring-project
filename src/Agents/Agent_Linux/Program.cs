using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

/*
This agent runs on Linux systems which have the mpstat command with sysstat v12.6.1
*/
var client = new HttpClient();
int interval = 1; //seconds

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

JsonDocument GetRawMpstatCommandOutput()
{
    System.Diagnostics.Process p = new System.Diagnostics.Process();
    p.StartInfo.FileName = "/bin/sh";
    p.StartInfo.Arguments = $"-c \"mpstat 1 {interval} -o JSON\"";
    p.StartInfo.UseShellExecute = false;
    p.StartInfo.CreateNoWindow = true;
    p.StartInfo.RedirectStandardOutput = true;
    p.Start();
    p.WaitForExit();
    var result = p.StandardOutput.ReadToEnd();
    var json = JsonSerializer.Deserialize<JsonDocument>(result);
    return json;
}

string GetTimestamp(JsonDocument jsonStats)
{
    string date = jsonStats.RootElement.GetProperty("sysstat").GetProperty("hosts")[0].GetProperty("date").ToString();
    string timestamp = jsonStats.RootElement.GetProperty("sysstat").GetProperty("hosts")[0].GetProperty("statistics")[0].GetProperty("timestamp").ToString();
    var dateObj = DateOnly.Parse(date);
    var timeObj = TimeOnly.Parse(timestamp);
    var datetime = new DateTime(dateObj, timeObj).ToString("u");
    return datetime;
}

string GetCpuUtilisation(JsonDocument jsonStats)
{
    string idleCpu = jsonStats.RootElement.GetProperty("sysstat").GetProperty("hosts")[0].GetProperty("statistics")[0].GetProperty("cpu-load")[0].GetProperty("idle").ToString();
    double cpuUsage = Math.Round(100 - Convert.ToDouble(idleCpu), 2);
    return cpuUsage.ToString();
}

string GetHostname(JsonDocument jsonStats)
{
    string hostname = jsonStats.RootElement.GetProperty("sysstat").GetProperty("hosts")[0].GetProperty("nodename").ToString();
    return hostname;
}

string GenerateLog()
{
    var output = GetRawMpstatCommandOutput();
    string timestamp = GetTimestamp(output);
    string cpuUtilisation = GetCpuUtilisation(output);
    string hostname = GetHostname(output);
    var jsonLog = JsonSerializer.Serialize(new { hostname = hostname, timestamp = timestamp, cpuUtilisation = cpuUtilisation });
    return jsonLog;
}