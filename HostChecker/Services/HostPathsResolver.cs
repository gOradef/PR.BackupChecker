using FluentFTP;
using HostLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace HostChecker.Services
{
    /// <summary>
    /// Scans given host resursavelly across all directories presented on host. 
    /// If supported backup extensions is finded, then loads up path to them into hosts.json
    /// </summary>
    internal class HostPathsResolver : HostClient
    {
        ResultPathItemBuilder builder;

        internal HostPathsResolver(FtpClient client) : base(client, "HostPathsResolver")
        {
            builder = new(client.Host);
        }

        internal List<ResultPathItem> GetPaths()
        {
            _logger.Info("Started searching for backups paths");

            var result = new List<ResultPathItem>();
            var rootPath = client.GetWorkingDirectory();
            var directoriesToScan = new Stack<string>();
            directoriesToScan.Push(rootPath);

            while (directoriesToScan.Count > 0)
            {
                var currentDir = directoriesToScan.Pop();

                try
                {
                    var items = client.GetListing(currentDir);

                    // Check for backup files
                    BackupExtension? foundExtension = null;
                    foreach (var item in items.Where(el => el.Type == FtpObjectType.File))
                    {
                        var extension = Path.GetExtension(item.Name);
                        if (ExtensionMap.TryGetValue(extension, out var backupExt))
                        {
                            foundExtension = backupExt;
                            break;
                        }
                    }

                    if (foundExtension.HasValue)
                    {
                        result.Add(builder.Create(new (currentDir, foundExtension.Value, true)));
                    }

                    // Add subdirectories to stack (skip . and ..)
                    var subDirectories = items
                        .Where(item => item.Type == FtpObjectType.Directory && item.Name != "." && item.Name != "..")
                        .Select(item => item.FullName);

                    foreach (var subDir in subDirectories)
                    {
                        directoriesToScan.Push(subDir);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to scan directory {currentDir}: {ex.Message}");
                }
            }

            _logger.Info($"Found {result.Count} directories containing backup files");

            return result;
        }
    }
}