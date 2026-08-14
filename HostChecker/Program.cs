using FluentFTP;
using FluentFTP.Model.Functions;
using HostChecker.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using HostLibrary.Config;

namespace HostChecker
{
    internal class Program
    {
        private static List<HostConfig> _hostsConfig = [];
        private static readonly List<Task<List<ResultItem>>> BackupsResultTasks = [];

        private static void UseExistedConfig() {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                _hostsConfig = JsonSerializer.Deserialize<List<HostConfig>>(File.ReadAllText("hosts.json"))!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can not deserialize hosts.json. Check correctness of the file. {ex.Message}");
                throw new Exception("Error with deserialization of the file.");
            }
        }
        
        

        private static void LoadHostsConfig(string[] args)
        {
            if (args.Length == 0 && File.Exists("hosts.json"))
                UseExistedConfig();
            else if (args.Length == 0 && !File.Exists("hosts.json"))
            {
                throw new Exception("No hosts.json provided. Run file 'CreateConfig' to create config first");
            }
        }
        
        private static void RunCheckBackups()
        {
            foreach (var hostConfig in _hostsConfig)
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        using var client = new FtpClient(hostConfig.Creditionals.Host, 
                                                         hostConfig.Creditionals.Ftp.User, 
                                                         hostConfig.Creditionals.Ftp.Password);
                        client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                        client.Config.ValidateAnyCertificate = true;
                        client.Config.SanitizeTraversal = false;

                        BackupChecker checker = new(client, hostConfig.Paths);
                        checker.SetWorkingDirectory(hostConfig.Creditionals.Ftp.PathToRootBackupFolder);
                        return checker.Check();
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Console.WriteLine($"Exception for host {hostConfig.Creditionals.Host}: {ex.Message}");
#endif
                        return new List<ResultItem>(); // Возвращаем пустой список при ошибке
                    }
                });
                BackupsResultTasks.Add(task);
            }
            
            Task.WaitAll(BackupsResultTasks.ToArray());
        }
        
        private static void SendResultsToZabbix()
        {
            var allBackups = new List<object>();

            for (int i = 0; i < BackupsResultTasks.Count; i++)
            {
                var hostBackupsTask = BackupsResultTasks[i];
                var hostConfig = _hostsConfig[i];
                var hostName = hostConfig.Creditionals.Host;

                if (hostBackupsTask.IsFaulted)
                {
                    allBackups.Add(new
                    {
                        host_name = hostName,
                        backup_name = "ERROR",
                        backup_path = hostBackupsTask.Exception?.InnerException?.Message ?? "Unknown error",
                        backup_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                    continue;
                }

                var hostBackups = hostBackupsTask.Result;

                if (hostBackups == null || hostBackups.Count == 0)
                {
                    allBackups.Add(new
                    {
                        host_name = hostName,
                        backup_name = "INVALID_HOST",
                        backup_path = "/none",
                        backup_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                    continue;
                }

                foreach (var backup in hostBackups)
                {
                    string backupName = System.IO.Path.GetFileName(backup.Path);
                    if (string.IsNullOrEmpty(backupName))
                    {
                        backupName = "unknown_backup";
                    }

                    allBackups.Add(new
                    {
                        host_name = hostName,
                        backup_name = backupName,
                        backup_path = backup.Path,
                        backup_time = backup.ModifiedTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            if (allBackups.Count == 0)
            {
                allBackups.Add(new
                {
                    host_name = "NO_DATA",
                    backup_name = "NO_DATA",
                    backup_path = "/none",
                    backup_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            var lldData = new { data = allBackups };

            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = false
            };
            string jsonString = JsonSerializer.Serialize(lldData, jsonOptions);

            Console.WriteLine(jsonString);
        }
        
        static void Main(string[] args)
        {
            LoadHostsConfig(args);
            RunCheckBackups();
            SendResultsToZabbix();
        }
    }
}