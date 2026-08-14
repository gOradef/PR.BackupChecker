using System;
using System.Collections.Generic;
using System.Text;
using HostLibrary.Config;

namespace HostChecker.Services
{
    internal record ResultItem(string Host, BackupStatus Status, string BackupName, string Path, DateTime ModifiedTime);

    internal class ResultBackupItemBuilder(string Host)
    {
        private string Host = Host;

        internal ResultItem Create(BackupStatus backupStatus, string BackupName, string Path, DateTime modifiedTime)
        {
            return new ResultItem(Host, backupStatus, BackupName, Path, modifiedTime);
        }
    }
    
}
