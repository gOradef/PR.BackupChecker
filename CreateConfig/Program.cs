using FluentFTP;
using HostLibrary.Config;
using Renci.SshNet;
using System.Text.Json;


namespace CreateConfig
{
    internal class Program
    {
        private static List<HostConfig> _hostsConfig = [];

        private static void Main()
        {
            Console.WriteLine("=== IF YOU WANT TO CREATE NEW CONFIG, PRESS 'C' ===");

            if (Console.ReadKey(true).Key == ConsoleKey.C)
            {
                Func<HostCreditionals?> createNewHostCreds = () =>
                {
                    Console.Write("Enter ip address of host: ");
                    var host = Console.ReadLine();

                    // FTP
                    Console.WriteLine("--- Ftp config --- \n");

                    Console.Write("Enter ftp username: ");
                    var ftpUsername = Console.ReadLine();

                    Console.Write("Enter ftp password: ");
                    var ftpPassword = Console.ReadLine();

                    Console.Write("Enter path to root backup folder on host (by default '/'): ");
                    var ftpRootPath = Console.ReadLine();

                    // SSH
                    Console.WriteLine("--- Ssh config --- \n");

                    Console.Write("Enter ssh port (22 by default): ");
                    var SshPort = int.Parse(Console.ReadLine()!);

                    Console.Write("Enter ssh username (ftp value by default): ");
                    var SshUsername = Console.ReadLine();

                    Console.Write("Enter ssh password (ftp value by default): ");
                    var SshPassword = Console.ReadLine();



                    ftpRootPath ??= "/";
                    SshUsername ??= ftpUsername;
                    SshPassword ??= ftpPassword;

                    if (SshPort == null)
                    {
                        SshPort = 22;
                    }

                    Console.WriteLine("Check correctness of inserted data: ");
                    Console.WriteLine(
                        $"Host: {host} \n" +
                        $"FTP: User: {ftpUsername} \n" +
                        $"FTP: Password: {ftpUsername} \n" +
                        $"SSH: User: {SshUsername}" +
                        $"SSH: Password: {SshPassword}" +
                        $"SSH Port: {SshPort} \n" +
                        $"Root backup folder: {ftpRootPath}");
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
                        using var ftpClient = new FtpClient(host, ftpUsername, ftpPassword);
                        ftpClient.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                        ftpClient.Config.ValidateAnyCertificate = true;
                        
                        using var sshClient = new SshClient(host, SshPort, SshUsername, SshPassword);

                        try
                        {
                            ftpClient.Connect();
                            Console.WriteLine("Ftp works fine. \n");
                            sshClient.Connect();
                            Console.WriteLine("Ssh works fine. \n");
                            Console.WriteLine($"Sucsessfully able to connect the host. Adding {host} to config...");
                            return new HostCreditionals(host, new(ftpUsername, ftpPassword, ftpRootPath), new(SshPort, SshUsername, SshPassword));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Host {host} can not able to estalbish connection. Check creditionals");
                            Console.WriteLine($"Add host to config anyway? (y/n)");
                            var key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.Y)
                            {
                                return new HostCreditionals(host, new(ftpUsername, ftpPassword, ftpRootPath), new(SshPort, SshUsername, SshPassword));
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
                List<Task<List<ResultItem>>> resultsTasks = [];

                _hostsConfig = configs.Select(c => new HostConfig(c, new List<HostPath>())).ToList();

                foreach (var creds in configs)
                {
                    var task = Task.Run(() =>
                    {
                        try
                        {
                            using var client = new FtpClient(creds.Host, creds.Ftp.User, creds.Ftp.Password);
                            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                            client.Config.ValidateAnyCertificate = true;

                            HostPathsResolver pathsResolver = new(client);
                            pathsResolver.SetWorkingDirectory(creds.Ftp.PathToRootBackupFolder);
                            return pathsResolver.GetPaths();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            return new List<ResultItem>();
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

                string dateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"hosts.{dateTimeStamp}.json";
                File.WriteAllText(fileName, jsonString);
            }
        }
    }
}
