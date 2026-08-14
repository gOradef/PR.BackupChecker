using FluentFTP;
using HostLibrary.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace HostLibrary.Classes
{
    /// <summary>
    /// Logger and client for using host by ftp client
    /// </summary>
    public class HostClient
    {
        protected HostLogger _logger;
        protected FtpClient client;
  
        protected internal static readonly Dictionary<string, BackupExtension> ExtensionMap =
    new(StringComparer.OrdinalIgnoreCase)
{
    { ".asxcdertgbnj134234fgrty", BackupExtension.dotasxcdertgbnj134234fgrty },
    { ".347ujhwqmsjkth480qekmcx", BackupExtension.dot347ujhwqmsjkth480qekmcx },
    { ".tib", BackupExtension.TIB }
};
        public void SetWorkingDirectory(string path)
        {
            client.SetWorkingDirectory(path);
        }

        private void IsHostValid()
        {
            try
            {
                _logger.Info("Подключение к ftp...");
                client.Connect();
                _logger.Info("Успешно подключено!");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка подключения к хосту {ex.Message}");
                throw new Exception($"Ошибка подключения к хосту");
            }
        }

        public HostClient(FtpClient client, string serviceName)
        {
            this.client = client;
            _logger = new(client.Host, serviceName);

            IsHostValid();
        }
    }
}
