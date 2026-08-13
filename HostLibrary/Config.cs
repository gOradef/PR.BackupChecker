using System;
using System.Collections.Generic;
using System.Text;

namespace HostLibrary {
    public record HostConfig(HostCreditionals Creditionals, List<HostPath> Paths);
    public record HostCreditionals(string Host, string User, string Password, string PathToRootBackupFolder);
    public record HostPath(string Path, BackupExtension ExtentionBackupType, bool IsEnabled);
}
