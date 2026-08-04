using System;
using System.Collections.Generic;
using System.Text;

using HostChecker.Enums;

namespace HostChecker.Objects
{
    internal record HostConfig(HostCreditionals Creditionals, List<HostPath> Paths);
    internal record HostCreditionals(string Host, string User, string Password, string PathToRootBackupFolder);
    internal record HostPath(string Path, BackupExtension ExtentionBackupType, bool IsEnabled = true);
}
