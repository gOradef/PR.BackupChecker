namespace HostChecker.Services;
using Microsoft.Extensions.Logging;

public class HostLogger
{
    private readonly string _host;
    private readonly string _service;
    private ILogger _logger;

    private delegate void Log(string tag, string message);
    private Log _log = new((tag, message) => {});

    public HostLogger(string hostPrefix, string servicePrefix)
    {
        _host = hostPrefix;
        _service = servicePrefix;

#if DEBUG
        _log += (tag, message) =>
        {
            Console.WriteLine($"[{tag}] {_service}({_host}): " + message);
        };
#endif
    }

    public void Info(string message) => _log("INFO", message);

    public void Debug(string message) => _log("DEBUG", message);
    public void Error(string message) => _log("ERROR", message);

    public void Fatal(string message) => _log("FATAL", message);
}