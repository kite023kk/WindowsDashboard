using System;
using System.Diagnostics;
using System.IO;
using WindowsDashboard.Models;

namespace WindowsDashboard.Services;

public static class ProcessLauncher
{
    public static void Launch(AppEntryConfig app)
    {
        if (app == null)
        {
            return;
        }

        var target = app.TargetPath;
        if (string.IsNullOrWhiteSpace(target))
        {
            target = app.ShortcutPath;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        target = Environment.ExpandEnvironmentVariables(target);

        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            target.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
        {
            Start(target, app.Arguments);
            return;
        }

        if (Directory.Exists(target))
        {
            OpenPath(target);
            return;
        }

        var resolved = AppScanner.ResolveExecutable(target) ?? target;
        Start(resolved, app.Arguments, app.WorkingDirectory);
    }

    public static void OpenPath(string path, string? mode = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (mode == "code")
        {
            Start(ResolveCode(), $"\"{path}\"");
            return;
        }

        if (mode == "terminal")
        {
            Start("cmd.exe", $"/k cd /d \"{path}\"", path);
            return;
        }

        Start("explorer.exe", $"\"{path}\"");
    }

    public static void OpenFileLocation(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        path = Environment.ExpandEnvironmentVariables(path);
        if (File.Exists(path))
        {
            Start("explorer.exe", $"/select,\"{path}\"");
        }
        else if (Directory.Exists(path))
        {
            Start("explorer.exe", $"\"{path}\"");
        }
    }

    private static void Start(string fileName, string arguments, string? workingDirectory = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory))
            {
                psi.WorkingDirectory = workingDirectory;
            }

            Process.Start(psi);
        }
        catch
        {
            // A missing executable should not crash the dashboard.
        }
    }

    private static string ResolveCode()
    {
        var candidates = new[]
        {
            @"C:\Users\86183\AppData\Local\Programs\Microsoft VS Code\Code.exe",
            @"C:\Program Files\Microsoft VS Code\Code.exe",
            "code"
        };

        foreach (var candidate in candidates)
        {
            var resolved = AppScanner.ResolveExecutable(candidate);
            if (resolved != null && File.Exists(resolved))
            {
                return resolved;
            }
        }

        return "code";
    }
}
