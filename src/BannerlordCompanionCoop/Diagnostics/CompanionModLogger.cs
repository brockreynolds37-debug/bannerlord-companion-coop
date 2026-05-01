using System;
using System.IO;
using System.Text;

namespace BannerlordCompanionCoop.Diagnostics;

public static class CompanionModLogger
{
    private static readonly object Sync = new();
    private static string? _logFilePath;

    public static string LogFilePath
    {
        get
        {
            lock (Sync)
            {
                return _logFilePath ??= BuildLogFilePath(DateTime.Now);
            }
        }
    }

    public static void Info(string category, string message)
    {
        Write("INFO", category, message);
    }

    public static void Warn(string category, string message)
    {
        Write("WARN", category, message);
    }

    public static void Error(string category, string message)
    {
        Write("ERROR", category, message);
    }

    public static void Error(string category, string message, Exception exception)
    {
        if (exception is null)
        {
            Write("ERROR", category, message);
            return;
        }

        Write("ERROR", category, $"{message}{Environment.NewLine}{exception}");
    }

    private static void Write(string level, string category, string message)
    {
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            lock (Sync)
            {
                string logFilePath = _logFilePath ??= BuildLogFilePath(DateTime.Now);
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
                File.AppendAllText(
                    logFilePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{category}] {message}{Environment.NewLine}",
                    Encoding.UTF8);
            }
        }
        catch
        {
        }
    }

    private static string BuildLogFilePath(DateTime timestamp)
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(
            documentsPath,
            "Mount and Blade II Bannerlord",
            "Configs",
            "ModLogs",
            $"BannerlordCompanionCoop-{timestamp:yyyyMMdd}.log");
    }
}
