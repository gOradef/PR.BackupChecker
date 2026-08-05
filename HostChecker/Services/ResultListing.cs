using System;
using System.Collections.Generic;
using System.Text;
using HostChecker.Enums;
using HostChecker.Objects;

namespace HostChecker.Services
{
    internal record ResultBackupItem(string Host, BackupStatus Status, string BackupName, string Path, DateTime ModifiedTime);
    internal record ResultPathItem(string Host, HostPath path);

    class ResultBackupItemBuilder(string Host) // for backup checker
    {
        private string Host = Host;

        internal ResultBackupItem Create(BackupStatus backupStatus, string BackupName, string Path, DateTime modifiedTime)
        {
            return new ResultBackupItem(Host, backupStatus, BackupName, Path, modifiedTime);
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
