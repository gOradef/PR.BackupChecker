using System;
using System.Collections.Generic;
using System.Text;
using HostChecker.Enums;

namespace HostChecker
{
    internal record ResultItem(string Host, BackupStatus Status, string BackupName, string Path);

    class ResultItemBuilder(string Host)
    {
        private string Host = Host;

        internal ResultItem Create(BackupStatus backupStatus, string BackupName, string Path)
        {
            return new ResultItem(Host, backupStatus, BackupName, Path);
        }
    }
}
