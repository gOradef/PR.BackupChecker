using DiskAvalailbleSpace;
using HostLibrary.Config;
using Renci.SshNet;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DiskAvalailbleSpace
{
    internal class Program
    {
        class VolumeInfo
        {
            public string Filesystem { get; set; }
            public string Size { get; set; }
            public string Used { get; set; }
            public string Available { get; set; }
            public string UsePercent { get; set; }
            public string MountPoint { get; set; }
            public string VolumeType { get; set; }
        }

        class HostResult
        {
            public string Host { get; set; }
            public bool Success { get; set; }
            public List<VolumeInfo> Volumes { get; set; }
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
                        var volumeList = new List<VolumeInfo>();

                        foreach (string line in lines)
                        {
                            string trimmedLine = line.Trim();

                            // Skip header line
                            if (trimmedLine.StartsWith("Filesystem") || trimmedLine.StartsWith("rootfs"))
                                continue;

                            // Skip temporary filesystems
                            if (trimmedLine.Contains("tmpfs") ||
                                trimmedLine.Contains("devtmpfs") ||
                                trimmedLine.Contains("none") ||
                                trimmedLine.Contains("rootfs") ||
                                trimmedLine.Contains("/dev/loop") ||
                                trimmedLine.Contains("/dev/root"))
                                continue;

                            string[] parts = Regex.Split(trimmedLine, @"\s+");

                            if (parts.Length >= 6)
                            {
                                string filesystem = parts[0];
                                string size = parts[1];
                                string used = parts[2];
                                string available = parts[3];
                                string usePercent = parts[4];
                                string mountPoint = parts[5];

                                // Determine volume type
                                string volumeType = "Other";
                                if (filesystem.Contains("/dev/mapper/"))
                                {
                                    volumeType = "LVM";
                                }
                                else if (filesystem.Contains("/dev/md"))
                                {
                                    volumeType = "Software RAID";
                                }
                                else if (filesystem.Contains("/dev/sd") || filesystem.Contains("/dev/hd"))
                                {
                                    volumeType = "Physical Disk";
                                }

                                // Only add if it's a significant volume (size > 1GB or important mount points)
                                long sizeBytes = ParseSizeToBytes(size);
                                if (sizeBytes > 1024 * 1024 * 1024 || // > 1GB
                                    mountPoint == "/" ||
                                    mountPoint.StartsWith("/Volume") ||
                                    mountPoint.StartsWith("/volume") ||
                                    mountPoint.StartsWith("/mnt/pools") ||
                                    mountPoint.StartsWith("/nfs"))
                                {
                                    volumeList.Add(new VolumeInfo
                                    {
                                        Filesystem = filesystem,
                                        Size = size,
                                        Used = used,
                                        Available = available,
                                        UsePercent = usePercent,
                                        MountPoint = mountPoint,
                                        VolumeType = volumeType
                                    });
                                }
                            }
                        }

                        // Calculate total storage
                        long totalAvailableBytes = 0;
                        long totalSizeBytes = 0;
                        long totalUsedBytes = 0;

                        foreach (var vol in volumeList)
                        {
                            totalAvailableBytes += ParseSizeToBytes(vol.Available);
                            totalSizeBytes += ParseSizeToBytes(vol.Size);
                            totalUsedBytes += ParseSizeToBytes(vol.Used);
                        }

                        int totalUsagePercent = totalSizeBytes > 0 ? (int)((totalUsedBytes * 100) / totalSizeBytes) : 0;

                        return new HostResult
                        {
                            Host = hostConfig.Creditionals.Host,
                            Success = true,
                            Volumes = volumeList,
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
                            Volumes = new List<VolumeInfo>(),
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
                        volumes = item.Volumes.Select(v => new
                        {
                            filesystem = v.Filesystem,
                            size = v.Size,
                            used = v.Used,
                            available = v.Available,
                            use_percent = v.UsePercent,
                            mount_point = v.MountPoint,
                            type = v.VolumeType
                        }),
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

            // Handle sizes like "1.7G", "64T", "372G"
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