using FluentFTP;
using HostChecker.Enums;
using System.ComponentModel.Design;
using Microsoft.Extensions.Logging;


namespace HostChecker
{
    internal class Program
    {
        private record Creditionals(string host, string user, string password, string pathToBackupFolder, string[]? excludedDirectoriesToCheck = null);
        private static List<Creditionals> _hostsToCheck = [];
        private static List<ResultItem> results;
        private static ILoggerFactory _factory;

        // public static Dictionary<Folders, string> FolderPaths = new();

        private static void SetupHosts()
        {
            var host4410 = new Creditionals(
                host: "192.168.44.10",
                user: "postmaster",
                password: "5gdPK6away",
                pathToBackupFolder: "/Backups");
            var host221 = new Creditionals(
                host: "192.168.2.21",
                user: "postmaster",
                password: "5gdPK6away",
                pathToBackupFolder: "/");
            var host222 = new Creditionals(
                host: "192.168.2.22",
                user: "premier",
                password: "5gdPK6away",
                pathToBackupFolder: "/");
            var host223 = new Creditionals(
                host: "192.168.2.23",
                user: "postmaster",
                password: "5gdPK6away",
                pathToBackupFolder: "/");

            if (_hostsToCheck.Count == 0)
            {
                _hostsToCheck = new List<Creditionals>() { host4410, host221, host222, host223}; //TODO 21 and 23 does not connectiong
            }
        }
        private static void RunCheck()
        {
            List<Task<List<ResultItem>>> resultsTasks = [];
            
            foreach (var hostCreds in _hostsToCheck)
            {
                var hostResult = Task.Run( List<ResultItem> () =>
                {
                    List<ResultItem> results = new();
                    using var client = new FtpClient(hostCreds.host, hostCreds.user, hostCreds.password);
                    client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                    client.Config.ValidateAnyCertificate = true;

                    try
                    {
                        BackupChecker checker = new(client);
                        checker.SetWorkingDirectory(hostCreds.pathToBackupFolder);
                        if (hostCreds.excludedDirectoriesToCheck != null)
                        {
                            checker.SetExcludingDirectories(hostCreds.excludedDirectoriesToCheck);
                        }

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
            
        }
        private static void SendResultsToZabbix()
        {

        }
        //private static void SetupFolderPaths()
        //{
        //    // .asxcdertgbnj134234fgrty
        //    FolderPaths[Folders.C_Backup] = "1C_Backup";
        //    FolderPaths[Folders.C_Buh_83] = "1C_Buh_8.3";
        //    FolderPaths[Folders.C_BUH_8_Maxim] = "1C_BUH_8_Maxim";
        //    FolderPaths[Folders.C_Zup] = "1C_Zup";
        //    FolderPaths[Folders.C_ZUP_8_Maxim] = "1C_ZUP_8_Maxim";
        //    FolderPaths[Folders.Arsan_1C] = "Arsan_1C";

        //    // .asxcdertgbnj134234fgrty
        //    // /Backups/TS/Base/... BUT(!) exclude 1C_Bases
        //    FolderPaths[Folders.TS] = "TS/Base";


        //    // ?
        //    FolderPaths[Folders.Acronis] = "Acronis";
        //    FolderPaths[Folders.Kerio] = "Kerio";

        //    // .347ujhwqmsjkth480qekmcx
        //    FolderPaths[Folders.mobile_prkzn] = "mobile_prkzn";

        //    // .tbz
        //    FolderPaths[Folders.Ideco] = "Ideco";
        //    FolderPaths[Folders.Ideco_TS] = "Ideco_TS";

        //    // .bak
        //    FolderPaths[Folders.infobase] = "infobase";
        //}
        static void Main(string[] args)
        {
            SetupHosts();
            RunCheck();
            SendResultsToZabbix();
        }
    }
}
