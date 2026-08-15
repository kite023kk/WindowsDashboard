using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsDashboard.Services;

public static class ShortcutResolver
{
    private const int STGM_READ = 0x00000000;

    public static string? ResolveTarget(string shortcutPath)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath) ||
            !shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(shortcutPath))
        {
            return null;
        }

        try
        {
            var link = (IShellLinkW)new CShellLink();
            var persist = (IPersistFile)link;
            persist.Load(shortcutPath, STGM_READ);

            var buffer = new StringBuilder(1024);
            link.GetPath(buffer, buffer.Capacity, IntPtr.Zero, 0);
            var target = buffer.ToString();
            return string.IsNullOrWhiteSpace(target) ? null : target;
        }
        catch
        {
            return null;
        }
    }

    public static (string Path, int Index)? ResolveIconLocation(string shortcutPath)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath) ||
            !shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(shortcutPath))
        {
            return null;
        }

        try
        {
            var link = (IShellLinkW)new CShellLink();
            var persist = (IPersistFile)link;
            persist.Load(shortcutPath, STGM_READ);

            var buffer = new StringBuilder(1024);
            link.GetIconLocation(buffer, buffer.Capacity, out var index);
            var iconPath = buffer.ToString();
            return string.IsNullOrWhiteSpace(iconPath) ? null : (iconPath, index);
        }
        catch
        {
            return null;
        }
    }
}

[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
internal class CShellLink
{
}

[ComImport]
[Guid("000214F9-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellLinkW
{
    void GetPath(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
        int cch,
        IntPtr pfd,
        uint fFlags);

    void GetIDList(out IntPtr ppidl);
    void SetIDList(IntPtr pidl);
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
    void GetHotkey(out short pwHotkey);
    void SetHotkey(short wHotkey);
    void GetShowCmd(out int piShowCmd);
    void SetShowCmd(int iShowCmd);
    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
    void Resolve(IntPtr hwnd, uint fFlags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

[ComImport]
[Guid("0000010B-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPersistFile
{
    void GetClassID(out Guid pClassID);
    [PreserveSig]
    int IsDirty();
    void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
    void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
    void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
    void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
}
