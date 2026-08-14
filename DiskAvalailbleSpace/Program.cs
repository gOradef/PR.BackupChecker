using DiskAvalailbleSpace;
using HostLibrary.Config;
using Renci.SshNet;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DiskAvalailbleSpace
{
    internal class Program
    {
        class HostResult
        {
            public string Host { get; set; }
            public bool Success { get; set; }
            public long TotalSizeBytes { get; set; }
            public long TotalUsedBytes { get; set; }
            public long TotalAvailableBytes { get; set; }
            public int TotalUsagePercent { get; set; }
            public string Error { get; set; }
        }

        static async Task Main(string[] args)
        {
            try
            {
                if (!File.Exists("hosts.json"))
                {
                    var errorResponse = new { error = "No hosts.json provided in workdir" };
                    Console.WriteLine(JsonSerializer.Serialize(errorResponse));
                    return;
                }

                var hostsConfig = JsonSerializer.Deserialize<List<HostConfig>>(
                    File.ReadAllText("hosts.json"))!;

                var tasks = hostsConfig.Select(hostConfig => Task.Run(() =>
                {
                    try
                    {
                        using var client = new SshClient(
                            hostConfig.Creditionals.Host,
                            hostConfig.Creditionals.Ssh.Port,
                            hostConfig.Creditionals.Ssh.User,
                            hostConfig.Creditionals.Ssh.Password);

                        client.Connect();

                        // Get disk information
                        var sshCommand = client.CreateCommand("df -h");
                        var result = sshCommand.Execute();

                        // Parse the output
                        string[] lines = result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        var diskInfoList = new List<Dictionary<string, string>>();

                        foreach (string line in lines)
                        {
                            string trimmedLine = line.Trim();

                            // Check for /dev/mapper (LVM) or /dev/md (software RAID)
                            if (trimmedLine.Contains("/dev/mapper") || trimmedLine.Contains("/dev/md"))
                            {
                                string[] parts = Regex.Split(trimmedLine, @"\s+");

                                if (parts.Length >= 6)
                                {
                                    string filesystem = parts[0];
                                    string size = parts[1];
                                    string used = parts[2];
                                    string available = parts[3];
                                    string usePercent = parts[4];
                                    string mountPoint = parts[5];

                                    // Skip if mount point is already added
                                    if (!diskInfoList.Any(d => d["MountPoint"] == mountPoint))
                                    {
                                        diskInfoList.Add(new Dictionary<string, string>
                                        {
                                            ["Filesystem"] = filesystem,
                                            ["MountPoint"] = mountPoint,
                                            ["Available"] = available,
                                            ["Used"] = used,
                                            ["Size"] = size,
                                            ["UsePercent"] = usePercent
                                        });
                                    }
                                }
                            }
                        }

                        // Calculate total storage (include LVM and RAID only)
                        long totalAvailableBytes = 0;
                        long totalSizeBytes = 0;
                        long totalUsedBytes = 0;

                        foreach (var disk in diskInfoList)
                        {
                            totalAvailableBytes += ParseSizeToBytes(disk["Available"]);
                            totalSizeBytes += ParseSizeToBytes(disk["Size"]);
                            totalUsedBytes += ParseSizeToBytes(disk["Used"]);
                        }

                        int totalUsagePercent = totalSizeBytes > 0 ? (int)((totalUsedBytes * 100) / totalSizeBytes) : 0;

                        return new HostResult
                        {
                            Host = hostConfig.Creditionals.Host,
                            Success = true,
                            TotalSizeBytes = totalSizeBytes,
                            TotalUsedBytes = totalUsedBytes,
                            TotalAvailableBytes = totalAvailableBytes,
                            TotalUsagePercent = totalUsagePercent,
                            Error = null
                        };
                    }
                    catch (Exception ex)
                    {
                        return new HostResult
                        {
                            Host = hostConfig.Creditionals.Host,
                            Success = false,
                            TotalSizeBytes = 0,
                            TotalUsedBytes = 0,
                            TotalAvailableBytes = 0,
                            TotalUsagePercent = 0,
                            Error = ex.Message
                        };
                    }
                }));

                var results = await Task.WhenAll(tasks);

                // Create JSON response for Zabbix
                var response = new
                {
                    data = results.Select(item => new
                    {
                        host = item.Host,
                        status = item.Success ? "ONLINE" : "OFFLINE",
                        total_size_bytes = item.TotalSizeBytes,
                        total_used_bytes = item.TotalUsedBytes,
                        total_available_bytes = item.TotalAvailableBytes,
                        usage_percent = item.TotalUsagePercent,
                        error = item.Error
                    }).ToArray()
                };

                string jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                Console.WriteLine(jsonResponse);
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = ex.Message };
                Console.WriteLine(JsonSerializer.Serialize(errorResponse));
            }
        }

        // Helper method to parse size strings to bytes
        static long ParseSizeToBytes(string size)
        {
            if (string.IsNullOrEmpty(size) || size == "N/A" || size == "0B") return 0;

            size = size.Trim();
            size = size.Replace(",", ".");

            if (size.Length < 2) return 0;

            char unit = size[^1];
            string numberPart = size[..^1].Trim();

            if (!double.TryParse(numberPart, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
                return 0;

            return unit switch
            {
                'T' => (long)(value * 1024L * 1024L * 1024L * 1024L),
                'G' => (long)(value * 1024L * 1024L * 1024L),
                'M' => (long)(value * 1024L * 1024L),
                'K' => (long)(value * 1024L),
                'B' => (long)value,
                _ => (long)value
            };
        }
    }
}