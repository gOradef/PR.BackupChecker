using FluentFTP;
using HostChecker.Enums;
using HostChecker.Objects;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;


namespace HostChecker.Services
{
    class BackupChecker : HostClient
    {
        private List<HostPath> Paths;
        private ResultBackupItemBuilder ResultItemBuilder;

        internal List<ResultBackupItem> Check()
        {
            List<ResultBackupItem> results = new();
            foreach (var path in Paths.Where(el => el.IsEnabled == true))
            {
                var files = client.GetListing(path.Path);
                var result = files
                    .Where(el => el.Type == FtpObjectType.File)
                    .Select(file => new
                    {
                        File = file,
                        FoundExtension = ExtensionMap.TryGetValue(Path.GetExtension(file.Name), out var backupExt) ? backupExt : (BackupExtension?)null
                    })
                    .Where(x => x.FoundExtension.HasValue)
                    .OrderByDescending(x => x.File.Modified)
                    .Take(1)
                    .ToList();

                foreach (var item in result)
                {
                    var modifiedTime = client.GetModifiedTime(item.File.FullName);
                    //_logger.Debug(item.File.FullName + "\t" + modifiedTime);
                    bool isBackupActual = modifiedTime > DateTime.Now - TimeSpan.FromDays(2);
                    results.Add(ResultItemBuilder.Create(isBackupActual ? BackupStatus.OK : BackupStatus.BAD,
                        item.File.Name, item.File.FullName, item.File.Modified));
                }
                /*TODO algorithm
                 * 1. Check files from paths
                 * 2. Check last data file modification for today
                 * 3. If file does not exists, check last file
                 * 4. If file is not provided. Open issue: rather no backups or need to exclude that folder
                 */
            }
#if DEBUG
            foreach (var el in results.OrderBy(el => el.Status))
            {
                _logger.Debug($"{el.Status}: {el.ModifiedTime}\t{el.BackupName}\t{el.Path}");
            }
#endif
            return results;
        }
        
        internal BackupChecker(FtpClient client, List<HostPath> paths) : base(client, "BackupChecker")
        {
            this.Paths = paths;
            ResultItemBuilder = new(client.Host);
        }
    }
}
