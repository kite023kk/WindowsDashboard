using System.Collections.Generic;

namespace WindowsDashboard.Models;

public sealed class AppConfig
{
    public int ConfigVersion { get; set; }
    public bool DesktopMode { get; set; } = true;
    public bool WidgetMode { get; set; }
    public int OpacityPercent { get; set; } = 95;
    public bool AutoStart { get; set; }
    public string Theme { get; set; } = "Dark";
    public string FontColor { get; set; } = "#F6F7FA";
    public double WidgetLeft { get; set; } = -1;
    public double WidgetTop { get; set; } = -1;
    public double WidgetWidth { get; set; } = 360;
    public double WidgetHeight { get; set; } = 560;
    public bool WidgetShowClock { get; set; } = true;
    public bool WidgetShowQuick { get; set; } = true;
    public bool WidgetShowMonitor { get; set; } = true;
    public bool WidgetShowCalendar { get; set; } = true;
    public bool WidgetShowNotes { get; set; }
    public List<CardConfig> Cards { get; set; } = new();
    public List<CategoryConfig> Categories { get; set; } = new();
    public List<GitProject> GitProjects { get; set; } = new();
    public List<StickyNote> Notes { get; set; } = new();
    public List<string> ProjectFolders { get; set; } = new()
    {
        @"C:\Projects\C#",
        @"C:\Projects\Python",
        @"C:\Projects\Web",
        @"C:\Projects\Other"
    };
}

public sealed class CardConfig
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double LeftPercent { get; set; }
    public double TopPercent { get; set; }
    public double WidthPercent { get; set; }
    public double HeightPercent { get; set; }
    public string State { get; set; } = "Visible";
    public double Opacity { get; set; } = 1.0;
    public int ZIndex { get; set; } = 1;
}

public sealed class CategoryConfig
{
    public string Name { get; set; } = string.Empty;
    public List<AppEntryConfig> Apps { get; set; } = new();
}

public sealed class AppEntryConfig
{
    public string Name { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string ShortcutPath { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public int IconIndex { get; set; }
}

public sealed class GitProject
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Branch { get; set; } = "main";
    public string Status { get; set; } = "Synced";
}

public sealed class StickyNote
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = "#D5BD7A";
}
