using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace WindowsDashboard.Services;

public static class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowsDashboard";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string value &&
                   !string.IsNullOrWhiteSpace(value);
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null)
            {
                return;
            }

            if (enabled)
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exe))
                {
                    return;
                }

                key.SetValue(ValueName, $"\"{exe}\" --autostart");
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }
        }
        catch
        {
            // Registry access can be blocked in restricted environments.
        }
    }
}
