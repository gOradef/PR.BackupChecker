using System;
using System.Collections.Generic;
using System.Text;
using HostChecker.Enums;

namespace HostChecker
{
    internal record ResultItem(string Host, BackupStatus Status,string BackupName, string Path);
}
