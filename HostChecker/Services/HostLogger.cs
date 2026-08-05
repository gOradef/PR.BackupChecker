namespace HostChecker.Services;
using Microsoft.Extensions.Logging;

public class HostLogger
{
    private readonly string _host;
    private readonly string _service;
    private ILogger _logger;

    public HostLogger(string hostPrefix, string servicePrefix)
    {
        _host = hostPrefix;
        _service = servicePrefix;
    }

    public void Info(string message) => Console.WriteLine($"[INFO] {_service}({_host}): " + message);
#if DEBUG
    public void Debug(string message)
    {
        Console.WriteLine($"[DEBUG] {_service}({_host}): " + message);
    }
#endif
    public void Error(string message) => Console.WriteLine($"[ERROR] {_service}({_host}): " + message);
    public void Fatal(string message) => Console.WriteLine($"[FATAL] {_service}({_host}): " + message);
}