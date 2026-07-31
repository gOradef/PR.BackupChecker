namespace HostChecker;
using Microsoft.Extensions.Logging;

public class HostLogger
{
    private readonly string _prefix;
    private ILogger _logger;

    public HostLogger(string hostPrefix)
    {
        _prefix = $"[{hostPrefix}] ";
    }

    public void Info(string message) => Console.WriteLine($"[INFO] {_prefix}: " + message);
    public void Debug(string message) => Console.WriteLine($"[DEBUG] {_prefix}: " + message);
    public void Error(string message) => Console.WriteLine($"[ERROR] {_prefix}: " + message);
    public void Fatal(string message) => Console.WriteLine($"[FATAL] {_prefix}: " + message);
}