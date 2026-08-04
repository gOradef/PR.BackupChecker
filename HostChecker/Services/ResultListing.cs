using System;
using System.Collections.Generic;
using System.Text;
using HostChecker.Enums;
using HostChecker.Objects;

namespace HostChecker.Services
{
    internal record ResultBackupItem(string Host, BackupStatus Status, string BackupName, string Path);
    internal record ResultPathItem(string Host, HostPath path);

    class ResultBackupItemBuilder(string Host) // for backup checker
    {
        private string Host = Host;

        internal ResultBackupItem Create(BackupStatus backupStatus, string BackupName, string Path)
        {
            return new ResultBackupItem(Host, backupStatus, BackupName, Path);
        }
    }
    class ResultPathItemBuilder(string Host) // for paths resolver
    {
        private string Host = Host;

        internal ResultPathItem Create(HostPath path)
        {
            return new(Host, path);
        }
    }
}
