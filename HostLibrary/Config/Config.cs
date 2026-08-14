using System;
using System.Collections.Generic;
using System.Text;

namespace HostLibrary.Config {
    public record HostConfig(HostCreditionals Creditionals, List<HostPath> Paths);
    public record HostCreditionals(string Host, FtpCreditionals Ftp, SshCreditionals Ssh);
    public record FtpCreditionals(string User, string Password, string PathToRootBackupFolder);
    public record SshCreditionals(int Port, string User, string Password);
    public record HostPath(string Path, BackupExtension ExtentionBackupType, bool IsEnabled);
}
