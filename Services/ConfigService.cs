using System;
using System.IO;
using System.Text.Json;
using WindowsDashboard.Models;

namespace WindowsDashboard.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _configPath;

    public ConfigService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsDashboard");
        Directory.CreateDirectory(root);
        _configPath = Path.Combine(root, "config.json");
    }

    public string ConfigPath => _configPath;

    public AppConfig Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (config != null)
                {
                    EnsureDefaults(config);
                    return config;
                }
            }
        }
        catch
        {
            // Corrupt config should not prevent the app from starting.
        }

        var defaults = CreateDefaults();
        Save(defaults);
        return defaults;
    }

    public void Save(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch
        {
            // Ignore transient write failures; next save will retry.
        }
    }

    public static AppConfig CreateDefaults()
    {
        return new AppConfig
        {
            ConfigVersion = 2,
            Cards =
            {
                Card("calendar", "日历", 19.5, 2.6, 16.1, 33),
                Card("ai", "AI", 19.5, 37.6, 16.1, 24),
                Card("dev", "开发", 36.7, 1.9, 19.2, 36),
                Card("other", "其他", 36.7, 39.8, 19.2, 27.5),
                Card("quick", "快捷工具", 57.2, 1.6, 21.5, 22.5),
                Card("git", "Git", 57.2, 25.6, 21.5, 56),
                Card("time", "时间", 80.1, 1.9, 17.0, 17.5),
                Card("monitor", "系统监控", 80.1, 21.3, 17.0, 28.5),
                Card("notes", "便签", 80.1, 51.7, 17.0, 24.5),
                Card("project", "Project", 80.1, 78.2, 17.0, 17.5)
            },
            Categories =
            {
                Category("开发", new[]
                {
                    AppEntry("Visual Studio", @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"),
                    AppEntry("Visual Studio Code", @"C:\Users\86183\AppData\Local\Programs\Microsoft VS Code\Code.exe"),
                    AppEntry("PyCharm", @"C:\Program Files\JetBrains\PyCharm\bin\pycharm64.exe"),
                    AppEntry("Navicat", @"C:\Program Files\PremiumSoft\Navicat Premium 16\navicat.exe"),
                    AppEntry("Obsidian", @"C:\Users\86183\AppData\Local\Obsidian\Obsidian.exe"),
                    AppEntry("Git", @"C:\Program Files\Git\git-bash.exe")
                }),
                Category("AI", new[]
                {
                    AppEntry("Codex", "codex"),
                    AppEntry("Claude", "claude"),
                    AppEntry("ChatGPT", "start https://chatgpt.com/")
                }),
                Category("工具", new[]
                {
                    AppEntry("CMD", "cmd.exe"),
                    AppEntry("PowerShell", "powershell.exe"),
                    AppEntry("Explorer", "explorer.exe")
                }),
                Category("游戏", new[]
                {
                    AppEntry("Steam", @"C:\Program Files (x86)\Steam\steam.exe")
                }),
                Category("其他", new[]
                {
                    AppEntry("微信", @"C:\Program Files\Tencent\WeChat\WeChat.exe"),
                    AppEntry("QQ", @"C:\Program Files\Tencent\QQ\QQ.exe"),
                    AppEntry("Chrome", @"C:\Program Files\Google\Chrome\Application\chrome.exe")
                })
            },
            GitProjects =
            {
                new GitProject { Name = "dashboard-ui", Path = @"C:\Projects\dashboard-ui", Branch = "main", Status = "Synced" },
                new GitProject { Name = "blog", Path = @"C:\Projects\blog", Branch = "dev", Status = "Modified" },
                new GitProject { Name = "toolkit", Path = @"C:\Projects\toolkit", Branch = "master", Status = "Synced" }
            },
            Notes =
            {
                new StickyNote { Id = Guid.NewGuid().ToString("N"), Text = "周会 10:00\n整理本周计划" }
            }
        };
    }

    private static CardConfig Card(string id, string title, double left, double top, double width, double height)
    {
        return new CardConfig { Id = id, Title = title, LeftPercent = left, TopPercent = top, WidthPercent = width, HeightPercent = height };
    }

    private static CategoryConfig Category(string name, IEnumerable<AppEntryConfig> apps)
    {
        return new CategoryConfig { Name = name, Apps = apps.ToList() };
    }

    private static AppEntryConfig AppEntry(string name, string target)
    {
        return new AppEntryConfig { Name = name, TargetPath = target, IconPath = target };
    }

    private static void EnsureDefaults(AppConfig config)
    {
        if (config.ConfigVersion < 2)
        {
            if (config.OpacityPercent < 85)
            {
                config.OpacityPercent = 95;
            }

            config.ConfigVersion = 2;
        }

        if (config.Cards.Count == 0)
        {
            config.Cards = CreateDefaults().Cards;
        }

        if (config.Categories.Count == 0)
        {
            config.Categories = CreateDefaults().Categories;
        }

        if (config.ProjectFolders.Count == 0)
        {
            config.ProjectFolders = new List<string>
            {
                @"C:\Projects\C#",
                @"C:\Projects\Python",
                @"C:\Projects\Web",
                @"C:\Projects\Other"
            };
        }
    }
}
