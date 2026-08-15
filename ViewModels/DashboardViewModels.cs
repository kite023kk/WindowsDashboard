using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using WindowsDashboard.Models;

namespace WindowsDashboard.ViewModels;

public sealed class AppItemViewModel : INotifyPropertyChanged
{
    private ImageSource? _icon;

    public AppItemViewModel(AppEntryConfig entry, string category)
    {
        Entry = entry;
        Category = category;
    }

    public AppEntryConfig Entry { get; }
    public string Category { get; }

    public string Name => Entry.Name;
    public string TargetPath => Entry.TargetPath;
    public string ShortcutPath => Entry.ShortcutPath;
    public bool IsShortcut => !string.IsNullOrWhiteSpace(Entry.ShortcutPath);

    public ImageSource? Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value))
            {
                return;
            }

            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    public string DisplayPath
    {
        get
        {
            var path = Entry.TargetPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Entry.ShortcutPath;
            }

            return string.IsNullOrWhiteSpace(path) ? string.Empty : System.IO.Path.GetFileName(path);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class QuickToolViewModel
{
    public QuickToolViewModel(string name, string command)
    {
        Name = name;
        Command = command;
    }

    public string Name { get; }
    public string Command { get; }
    public ImageSource? Icon { get; set; }
}

public sealed class GitItemViewModel
{
    public GitItemViewModel(GitProject project)
    {
        Project = project;
    }

    public GitProject Project { get; }
    public string Name => Project.Name;
    public string Branch => Project.Branch;
    public string Path => Project.Path;
    public string StatusText => Project.Status.Equals("Synced", StringComparison.OrdinalIgnoreCase) ? "✓ 已同步" : "● 有未提交修改";
    public Brush StatusBrush => Project.Status.Equals("Synced", StringComparison.OrdinalIgnoreCase)
        ? Brushes.MediumSeaGreen
        : Brushes.Goldenrod;
}

public sealed class NoteItemViewModel : INotifyPropertyChanged
{
    private string _text;

    public NoteItemViewModel(StickyNote note)
    {
        Note = note;
        _text = note.Text;
    }

    public StickyNote Note { get; }
    public string Id => Note.Id;
    public string Color => Note.Color;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            Note.Text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class ProjectFolderViewModel
{
    public ProjectFolderViewModel(string path)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
    }

    public string Name { get; }
    public string Path { get; }
}

public sealed class MonitorItemViewModel : INotifyPropertyChanged
{
    private double _percent;
    private string _text;

    public MonitorItemViewModel(string name, Brush brush)
    {
        Name = name;
        Brush = brush;
        _text = "0%";
    }

    public string Name { get; }
    public Brush Brush { get; }

    public double Percent
    {
        get => _percent;
        set
        {
            if (Math.Abs(_percent - value) < 0.05)
            {
                return;
            }

            _percent = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Percent)));
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
