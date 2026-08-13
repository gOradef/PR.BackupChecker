using FluentFTP;
using HostLibrary;
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
        private string HostName; // Сохраняем имя хоста для использования в ошибках

        internal List<ResultBackupItem> Check()
        {
            List<ResultBackupItem> results = new();
            
            // Проверяем, есть ли пути для проверки
            var enabledPaths = Paths.Where(el => el.IsEnabled == true).ToList();
            
            if (!enabledPaths.Any())
            {
                // Если нет включенных путей, возвращаем пустой список
                // В основном методе это будет обработано как NO_BACKUPS
#if DEBUG
                _logger.Debug($"No enabled paths found for host {client.Host}");
#endif
                return results;
            }

            foreach (var path in enabledPaths)
            {
                try
                {
                    // Проверяем, существует ли директория
                    if (!client.DirectoryExists(path.Path))
                    {
#if DEBUG
                        _logger.Debug($"Directory {path.Path} does not exist on host {client.Host}");
#endif
                        // Добавляем запись о том, что директория не существует
                        results.Add(ResultItemBuilder.Create(
                            BackupStatus.BAD,
                            "DIRECTORY_NOT_FOUND",
                            path.Path,
                            DateTime.MinValue
                        ));
                        continue;
                    }

                    var files = client.GetListing(path.Path);
                    
                    // Проверяем, есть ли файлы в директории
                    if (files == null || !files.Any())
                    {
#if DEBUG
                        _logger.Debug($"Directory {path.Path} is empty on host {client.Host}");
#endif
                        // Добавляем запись о пустой директории
                        results.Add(ResultItemBuilder.Create(
                            BackupStatus.BAD,
                            "EMPTY_DIRECTORY",
                            path.Path,
                            DateTime.MinValue
                        ));
                        continue;
                    }

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

                    // Проверяем, найдены ли файлы с нужными расширениями
                    if (!result.Any())
                    {
#if DEBUG
                        _logger.Debug($"No backup files with valid extensions found in {path.Path} on host {client.Host}");
#endif
                        // Добавляем запись о том, что бекапов с нужными расширениями нет
                        results.Add(ResultItemBuilder.Create(
                            BackupStatus.BAD,
                            "NO_VALID_BACKUP_FILES",
                            path.Path,
                            DateTime.MinValue
                        ));
                        continue;
                    }

                    foreach (var item in result)
                    {
                        var modifiedTime = client.GetModifiedTime(item.File.FullName);
                        bool isBackupActual = modifiedTime > DateTime.Now - TimeSpan.FromDays(1);
                        
                        results.Add(ResultItemBuilder.Create(
                            isBackupActual ? BackupStatus.OK : BackupStatus.BAD,
                            item.File.Name, 
                            item.File.FullName, 
                            item.File.Modified
                        ));
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    _logger.Debug($"Error checking path {path.Path} on host {client.Host}: {ex.Message}");
#endif
                    // Добавляем запись об ошибке при проверке конкретного пути
                    results.Add(ResultItemBuilder.Create(
                        BackupStatus.BAD,
                        "PATH_CHECK_ERROR",
                        $"{path.Path}: {ex.Message}",
                        DateTime.MinValue
                    ));
                }
            }

#if DEBUG
            foreach (var el in results
                .OrderBy(el => el.Status)
                .ThenBy(el => el.BackupName)
                .ThenBy(el => el.ModifiedTime))
            {
                _logger.Debug($"{el.Status}: {el.ModifiedTime}\t{el.BackupName}\t{el.Path}");
            }
#endif
            return results;
        }
        
        internal BackupChecker(FtpClient client, List<HostPath> paths) : base(client, "BackupChecker")
        {
            this.Paths = paths;
            this.HostName = client.Host;
            ResultItemBuilder = new(client.Host);
        }
    }
}