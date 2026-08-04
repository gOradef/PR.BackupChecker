using FluentFTP;
using System;
using System.Collections.Generic;
using System.Text;

namespace HostChecker.Services
{
    /// <summary>
    /// Logger and client for using host by ftp client
    /// </summary>
    internal class HostClient
    {
        protected HostLogger _logger;
        protected FtpClient client;

        protected internal void SetWorkingDirectory(string path)
        {
            client.SetWorkingDirectory(path);
        }

        private void IsHostValid()
        {
            try
            {
                _logger.Debug("Подключение к ftp...");
                client.Connect();
                _logger.Debug("Успешно подключено!");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка подключения к хосту {ex.Message}");
                throw new Exception($"Ошибка подключения к хосту");
            }
        }

        internal HostClient(FtpClient client, string serviceName)
        {
            this.client = client;
            _logger = new(client.Host, serviceName);

            IsHostValid();
        }
    }
}
