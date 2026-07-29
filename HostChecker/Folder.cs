using System;
using System.Collections.Generic;
using System.Text;
using HostChecker.Enums;


namespace HostChecker
{
    internal struct Folder
    {
        Folders Name;
        string Path;
        string backupFileName;
        CheckBackupType checkType;

        public Folder()
        {

        }
    }
}
