using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FluentFTP;
using HostChecker.Objects;
using Microsoft.CSharp.RuntimeBinder;


namespace HostChecker.Services
{
    class BackupChecker : HostClient
    {
        List<HostPath> paths;
        private ResultBackupItemBuilder ResultItemBuilder;

        internal List<ResultBackupItem> Check()
        {
            List<ResultBackupItem> results = new();
            var directories = client.GetListing();
            var sb = new StringBuilder();
            foreach (var dir in directories)
            {
                
                /*TODO algorithm
                 * 1. Check files from paths
                 * 2. Check last data file modification for today
                 * 3. If file does not exists, check last file
                 * 4. If file is not provided. Open issue: rather no backups or need to exclude that folder
                 */

                sb.Append(dir.FullName + "\n");
            }
            _logger.Info(sb.ToString());

            return results;
        }
        
        internal BackupChecker(FtpClient client, List<HostPath> paths) : base(client, "BackupChecker")
        {
            this.paths = paths;
            ResultItemBuilder = new(client.Host);
        }
    }
}
