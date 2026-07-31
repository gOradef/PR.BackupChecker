using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FluentFTP;
using Microsoft.CSharp.RuntimeBinder;


namespace HostChecker
{
    class BackupChecker
    {
        FtpClient client;
        private ResultItemBuilder ResultItemBuilder;
        private string[] ExcludedDirectoriesToCheck = [];
        private HostLogger _logger;

        private void IsValidHost()
        {
            try
            {
                _logger.Debug("Подключение к ftp...");
                client.Connect();
                _logger.Debug("Успешно подключено!");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка подключения к хосту {ex.Message}");
                throw new Exception($"Ошибка подключения к хосту");
            }
        }

        internal void SetWorkingDirectory(string path)
        {
            client.SetWorkingDirectory(path);
        }

        internal void SetExcludingDirectories(string[] excludedDirectories)
        {
            ExcludedDirectoriesToCheck = excludedDirectories;
        }

        internal List<ResultItem> Check()
        {
            List<ResultItem> results = new();
            var directories = client.GetListing();
            var sb = new StringBuilder();
            foreach (var dir in directories)
            {
                
                /*TODO algorithm
                 * 1. Ger dir path
                 * 2. Get files from that dir
                 * 3. Check extension of files
                 * 4. Check last data file modification for today
                 * 5. If file does not exists, check last file
                 * 6. If file is not provided. Open issue: rather no backups or need to exclude that folder
                 */

                sb.Append(dir.FullName + "\n");
            }
            _logger.Info(sb.ToString());

            return results;
        }
        
        internal BackupChecker(FtpClient client)
        {
            this.client = client;
            _logger = new(client.Host);
            ResultItemBuilder = new(client.Host);

            IsValidHost();
        }
    }
}
