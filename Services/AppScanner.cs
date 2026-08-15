using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsDashboard.Models;

namespace WindowsDashboard.Services;

public static class AppScanner
{
    public static List<AppEntryConfig> ScanDesktopShortcuts()
    {
        var entries = new List<AppEntryConfig>();
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs")
        };

        foreach (var folder in folders.Where(Directory.Exists))
        {
            var links = Directory.Exists(folder)
                ? Directory.EnumerateFiles(folder, "*.lnk", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();
            foreach (var link in links)
            {
                var target = ShortcutResolver.ResolveTarget(link);
                entries.Add(new AppEntryConfig
                {
                    Name = Path.GetFileNameWithoutExtension(link),
                    TargetPath = target ?? link,
                    ShortcutPath = link,
                    IconPath = link,
                    IconIndex = 0,
                    WorkingDirectory = target != null ? Path.GetDirectoryName(target) ?? string.Empty : string.Empty
                });
            }

            var urls = Directory.Exists(folder)
                ? Directory.EnumerateFiles(folder, "*.url", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();
            foreach (var url in urls)
            {
                var targetUrl = ReadUrlTarget(url);
                entries.Add(new AppEntryConfig
                {
                    Name = Path.GetFileNameWithoutExtension(url),
                    TargetPath = targetUrl ?? url,
                    ShortcutPath = url,
                    IconPath = url
                });
            }
        }

        return entries
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(e => e.Name)
            .ToList();
    }

    public static void MergeDesktopShortcuts(AppConfig config)
    {
        var other = config.Categories.FirstOrDefault(c => c.Name == "其他");
        if (other == null)
        {
            other = new CategoryConfig { Name = "其他" };
            config.Categories.Add(other);
        }

        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var app in config.Categories.SelectMany(c => c.Apps))
        {
            if (!string.IsNullOrWhiteSpace(app.ShortcutPath) &&
                app.ShortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                var icon = ShortcutResolver.ResolveIconLocation(app.ShortcutPath);
                if (icon.HasValue &&
                    (string.IsNullOrWhiteSpace(app.IconPath) ||
                     string.Equals(app.IconPath, app.TargetPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(app.IconPath, app.ShortcutPath, StringComparison.OrdinalIgnoreCase)))
                {
                    app.IconPath = app.ShortcutPath;
                    app.IconIndex = 0;
                }
            }

            if (string.IsNullOrWhiteSpace(app.IconPath))
            {
                if (!string.IsNullOrWhiteSpace(app.ShortcutPath))
                {
                    app.IconPath = app.ShortcutPath;
                }
                else if (!string.IsNullOrWhiteSpace(app.TargetPath))
                {
                    app.IconPath = app.TargetPath;
                }
            }

            if (!string.IsNullOrWhiteSpace(app.TargetPath))
            {
                known.Add(app.TargetPath);
            }

            if (!string.IsNullOrWhiteSpace(app.ShortcutPath))
            {
                known.Add(app.ShortcutPath);
            }
        }

        foreach (var shortcut in ScanDesktopShortcuts())
        {
            var key = !string.IsNullOrWhiteSpace(shortcut.TargetPath) ? shortcut.TargetPath : shortcut.ShortcutPath;
            if (known.Add(key))
            {
                other.Apps.Add(shortcut);
            }
        }
    }

    public static string? ResolveExecutable(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        var expanded = Environment.ExpandEnvironmentVariables(target);
        if (expanded.IndexOf(Path.DirectorySeparatorChar) >= 0 || expanded.IndexOf('/') >= 0)
        {
            return expanded;
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim('"'), expanded);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return expanded;
    }

    private static string? ReadUrlTarget(string urlPath)
    {
        try
        {
            foreach (var line in File.ReadAllLines(urlPath))
            {
                if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                {
                    return line["URL=".Length..].Trim();
                }
            }
        }
        catch
        {
            // Ignore unreadable shortcut.
        }

        return null;
    }
}
