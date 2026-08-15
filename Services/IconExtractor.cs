using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowsDashboard.Win32;

namespace WindowsDashboard.Services;

public static class IconExtractor
{
    public static ImageSource? ExtractIcon(string path, int iconIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var resolved = Environment.ExpandEnvironmentVariables(path);
        var target = resolved;

        if (resolved.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            var iconLocation = ShortcutResolver.ResolveIconLocation(resolved);
            if (iconLocation.HasValue)
            {
                target = Environment.ExpandEnvironmentVariables(iconLocation.Value.Path);
                iconIndex = iconLocation.Value.Index;
            }
            else
            {
                target = ShortcutResolver.ResolveTarget(resolved) ?? resolved;
            }
        }
        else if (resolved.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
        {
            target = resolved;
        }

        if (!File.Exists(target) && !Directory.Exists(target))
        {
            target = ResolveFromPath(path);
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        if (iconIndex != 0 || target.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            var indexed = ExtractIconByIndex(target, iconIndex);
            if (indexed != null)
            {
                return indexed;
            }
        }

        var info = new NativeMethods.SHFILEINFO();
        var flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON;
        if (!File.Exists(target) && !Directory.Exists(target))
        {
            flags |= NativeMethods.SHGFI_USEFILEATTRIBUTES;
        }

        var result = NativeMethods.SHGetFileInfo(target, 0, ref info, (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.SHFILEINFO>(), flags);
        if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
        {
            if (resolved.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractShortcutIcon(resolved);
            }

            return null;
        }

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                info.hIcon,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            NativeMethods.DestroyIcon(info.hIcon);
        }
    }

    private static ImageSource? ExtractShortcutIcon(string shortcutPath)
    {
        var info = new NativeMethods.SHFILEINFO();
        var flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON;
        var result = NativeMethods.SHGetFileInfo(shortcutPath, 0, ref info, (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.SHFILEINFO>(), flags);
        if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                info.hIcon,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            NativeMethods.DestroyIcon(info.hIcon);
        }
    }

    private static ImageSource? ExtractIconByIndex(string path, int iconIndex)
    {
        var count = NativeMethods.ExtractIconEx(path, iconIndex, out var large, out var small, 1);
        if (count == 0 || large == IntPtr.Zero)
        {
            if (small != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(small);
            }

            return null;
        }

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                large,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            NativeMethods.DestroyIcon(large);
            if (small != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(small);
            }
        }
    }

    public static ImageSource CreateDefaultIcon()
    {
        var drawing = new DrawingGroup();
        var background = new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(90, 100, 128)),
            null,
            new RectangleGeometry(new Rect(1, 1, 30, 30), 7, 7));

        var pen = new Pen(new SolidColorBrush(Color.FromRgb(238, 241, 248)), 2.2)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        var lines = new GeometryGroup();
        lines.Children.Add(new LineGeometry(new Point(8, 10), new Point(24, 10)));
        lines.Children.Add(new LineGeometry(new Point(8, 16), new Point(24, 16)));
        lines.Children.Add(new LineGeometry(new Point(8, 22), new Point(18, 22)));

        drawing.Children.Add(background);
        drawing.Children.Add(new GeometryDrawing(null, pen, lines));

        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static string ResolveFromPath(string name)
    {
        var candidates = new List<string> { name };
        if (!Path.HasExtension(name))
        {
            candidates.Add(name + ".exe");
            candidates.Add(name + ".cmd");
            candidates.Add(name + ".bat");
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var candidate in candidates)
            {
                var full = Path.Combine(dir.Trim('"'), candidate);
                if (File.Exists(full))
                {
                    return full;
                }
            }
        }

        return name;
    }
}
