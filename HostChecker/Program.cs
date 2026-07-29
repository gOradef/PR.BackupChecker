using FluentFTP;
using HostChecker.Enums;
using System.ComponentModel.Design;

namespace HostChecker
{
    internal class Program
    {
        private record Creditionals(string host, string user, string password, string pathToBackupFolder);
        private static List<Creditionals> hostsToCheck;
        private static List<ResultItem> results;

        public static Dictionary<Folders, string> FolderPaths = new();

        private static void SetupHosts()
        {
            var host4410 = new Creditionals(
                host: "192.168.44.10",
                user: @"premjer\postmaster",
                password: "5gdPK6away",
                pathToBackupFolder: "/Backups");
            var host222 = new Creditionals(
                host: "192.168.2.22",
                user: "premier",
                password: "5gdPK6away",
                pathToBackupFolder: "/");
            
            //TODO
            //var host 221;
            //var host 224;

            if (hostsToCheck is not null && hostsToCheck.Count == 0)
            {
                hostsToCheck.Add(host4410);
                hostsToCheck.Add(host222);
            }
        }
        private static void RunCheck()
        {
            foreach (var host in hostsToCheck)
            {
                using var client = new FtpClient(host.host, host.user, host.password);
                try
                {
                    Console.WriteLine($"Подключение к FTP ({host.host})...");
                    client.Connect();

                    Console.WriteLine("Успешно подключено!");
                    client.SetWorkingDirectory(host.pathToBackupFolder);


                    var directories = client.GetListing();

                    foreach (var dir in directories)
                    {
                        Console.Write(dir.FullName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка подключения: {ex.Message}");
                }
            }
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
