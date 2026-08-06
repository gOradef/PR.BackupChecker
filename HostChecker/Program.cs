using FluentFTP;
using FluentFTP.Model.Functions;
using HostChecker.Enums;
using HostChecker.Objects;
using HostChecker.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;


namespace HostChecker
{
    internal class Program
    {
        private static List<HostConfig> _hostsConfig = [];

        //private static List<HostConfig> CreateNewConfig() { }
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
        private static void CreateNewConfig()
        {
            Console.WriteLine("=== CONFIG IS NOT PRESENTED IN WORKDIR (hosts.json) ===");
            Console.WriteLine("=== IF YOU WANT TO CREATE NEW CONFIG, PRESS 'C' ===");

            if (Console.ReadKey(true).Key == ConsoleKey.C)
            {
                Func<HostCreditionals?> createNewHostCreds = () =>
                {
                    Console.Write("Enter ip address of host: ");
                    var host = Console.ReadLine();
                    Console.Write("Enter username: ");
                    var user = Console.ReadLine();
                    Console.Write("Enter password: ");
                    var password = Console.ReadLine();
                    Console.Write("Enter path to root backup folder on host (by default '/'): ");
                    var rootPath = Console.ReadLine();
                    if (rootPath == null)
                    {
                        rootPath = "/";
                    }

                    Console.WriteLine("Check correctness of inserted data: ");
                    Console.WriteLine(
                        $"Host: {host} \n" +
                        $"User: {user} \n" +
                        $"Password: {password} \n" +
                        $"Root backup folder: {rootPath}");
                    Console.WriteLine("Press 'n' to reassign the host \n " +
                        "Press 'y' to procceed. \n");


                    var insertedKey = Console.ReadKey(true).Key;

                    while (insertedKey is not (ConsoleKey.Y or ConsoleKey.N))
                    {
                        Console.WriteLine("Please enter (y or n)");
                        insertedKey = Console.ReadKey(true).Key;
                    }

                    if (insertedKey == ConsoleKey.N)
                    {
                        return null;
                    }
                    else if (insertedKey == ConsoleKey.Y)
                    {
                        Console.WriteLine($"Testing connection {host}...");
                        using var client = new FtpClient(host, user, password);
                        client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                        client.Config.ValidateAnyCertificate = true;

                        try
                        {
                            client.Connect();
                            Console.WriteLine($"Sucsessfully able to connect the host. Adding {host} to config...");
                            return new HostCreditionals(host, user, password, rootPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Host {host} can not able to estalbish connection. Check creditionals");
                            Console.WriteLine($"Add host to config anyway? (y/n)");
                            var key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.Y)
                            {
                                return new HostCreditionals(host, user, password, rootPath);
                            }
                            if (key == ConsoleKey.N)
                            {
                                return null;
                            }
                        }
                    }
                    return null;
                };
                List<HostCreditionals> configs = new();
                while (true)
                {
                    var hostcreds = createNewHostCreds();
                    if (hostcreds != null)
                    {
                        configs.Add(hostcreds);
                    }
                    string[] hosts = new string[configs.Count];

                    for (int i = 0; i < configs.Count; i++)
                    {
                        hosts[i] = configs[i].Host;
                    }

                    Console.WriteLine($"Enter 's' to save current config (contains: {string.Join(", ", hosts)}). Or other key to continue");

                    if (Console.ReadKey(true).Key == ConsoleKey.S)
                    {
                        break;
                    }
                }
                Console.WriteLine("=== Started scanning hosts on backup pattern... ===");
                List<Task<List<ResultPathItem>>> resultsTasks = [];

                _hostsConfig = configs.Select(c => new HostConfig(c, new List<HostPath>())).ToList();

                foreach (var creds in configs)
                {
                    var task = Task.Run(() =>
                    {
                        try
                        {
                            using var client = new FtpClient(creds.Host, creds.User, creds.Password);
                            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                            client.Config.ValidateAnyCertificate = true;

                            HostPathsResolver pathsResolver = new(client);
                            pathsResolver.SetWorkingDirectory(creds.PathToRootBackupFolder);
                            return pathsResolver.GetPaths();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            return new List<ResultPathItem>();
                        }
                    });
                    resultsTasks.Add(task);
                }

                Task.WaitAll(resultsTasks.ToArray());

                foreach (var elHost in resultsTasks)
                {
                    if (elHost.Result.Count > 0)
                    {
                        var hostName = elHost.Result[0].Host;
                        var hostCfg = _hostsConfig.FirstOrDefault(el => el.Creditionals.Host == hostName);

                        if (hostCfg != null)
                        {
                            var index = _hostsConfig.IndexOf(hostCfg);
                            _hostsConfig[index] = hostCfg with { Paths = elHost.Result.Select(a => a.path).ToList() };
                        }
                    }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(_hostsConfig, options);

                File.WriteAllText("hosts.json", jsonString);
            }
        }

        private static void SetupHosts()
        {
            if (File.Exists("hosts.json"))
                UseExistedConfig();
            else
                CreateNewConfig();
        }
        private static void RunCheckBackups()
        {
            List<Task<List<ResultBackupItem>>> resultsTasks = [];
            
            foreach (var hostConfig in _hostsConfig)
            {
                var hostResult = Task.Run( List<ResultBackupItem> () =>
                {
                    List<ResultBackupItem> results = new();
                    using var client = new FtpClient(hostConfig.Creditionals.Host, hostConfig.Creditionals.User, hostConfig.Creditionals.Password);
                    client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                    client.Config.ValidateAnyCertificate = true;
                    client.Config.SanitizeTraversal = false;

                    try
                    {
                        BackupChecker checker = new(client, hostConfig.Paths);
                        checker.SetWorkingDirectory(hostConfig.Creditionals.PathToRootBackupFolder);
                        return checker.Check();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return [];
                    }
                    
                });
                resultsTasks.Add(hostResult);

            }
            Task.WaitAll(resultsTasks);
            
            var sb = new StringBuilder();
            foreach (var hostBackups in resultsTasks.Select(el => el.Result))
            {
                var badBackups = hostBackups.Where(a => a.Status == BackupStatus.BAD).OrderBy(a => a.ModifiedTime);
                sb.Append($"=== Bad backups of {hostBackups.FirstOrDefault()?.Host} ({badBackups.Count()}/{hostBackups.Count}) ===\n");
                foreach(var backup in badBackups)
                {
                    sb.AppendLine($"Last update: {backup.ModifiedTime} \t {backup.Path}");
                }
                Console.WriteLine(sb.ToString());
                sb.Clear();
            }
        }
        private static void SendResultsToZabbix()
        {
            
        }
        static void Main(string[] args)
        {
            SetupHosts();
            RunCheckBackups();
            SendResultsToZabbix();
        }
    }
}
