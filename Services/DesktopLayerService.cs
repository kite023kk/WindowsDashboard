using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using WindowsDashboard.Win32;

namespace WindowsDashboard.Services;

/// <summary>
/// Attaches the dashboard window into the Windows desktop window hierarchy.
/// The window becomes a child of the WorkerW that also hosts SHELLDLL_DefView,
/// and is kept below the desktop icon list so normal windows can cover it.
/// </summary>
public sealed class DesktopLayerService : IDisposable
{
    private readonly Window _window;
    private readonly HwndSource? _source;
    private readonly System.Windows.Threading.DispatcherTimer _watcher;
    private IntPtr _progman;
    private IntPtr _workerW;
    private IntPtr _shellDefView;
    private IntPtr _attachedHwnd;
    private bool _attached;
    private bool _useCustomBounds;
    private int _boundsX;
    private int _boundsY;
    private int _boundsWidth;
    private int _boundsHeight;
    private bool _disposed;

    public DesktopLayerService(Window window)
    {
        _window = window;
        _source = (HwndSource?)PresentationSource.FromVisual(window);
        _watcher = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _watcher.Tick += (_, _) => EnsureConnected();
    }

    public bool IsAttached => _attached;

    public void Start()
    {
        _watcher.Start();
        EnsureConnected();
    }

    public void Stop()
    {
        _watcher.Stop();
        Detach();
    }

    public void SetBounds(int x, int y, int width, int height)
    {
        _useCustomBounds = true;
        _boundsX = x;
        _boundsY = y;
        _boundsWidth = Math.Max(1, width);
        _boundsHeight = Math.Max(1, height);

        if (_attached && _attachedHwnd != IntPtr.Zero)
        {
            NativeMethods.MoveWindow(_attachedHwnd, _boundsX, _boundsY, _boundsWidth, _boundsHeight, true);
        }
    }

    public void SetFullScreen()
    {
        _useCustomBounds = false;

        if (_attached && _attachedHwnd != IntPtr.Zero && _workerW != IntPtr.Zero)
        {
            NativeMethods.GetWindowRect(_workerW, out var rect);
            NativeMethods.MoveWindow(
                _attachedHwnd,
                0,
                0,
                Math.Max(1, rect.Right - rect.Left),
                Math.Max(1, rect.Bottom - rect.Top),
                true);
        }
    }

    public void EnsureConnected()
    {
        if (_disposed)
        {
            return;
        }

        var hwnd = GetWindowHandle();
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var parent = FindDesktopLayer();

        if (_attached && _attachedHwnd == hwnd && parent == _workerW && NativeMethods.IsWindow(parent))
        {
            return;
        }

        Attach(hwnd, parent);
    }

    private void Attach(IntPtr hwnd, IntPtr parent)
    {
        if (parent == IntPtr.Zero)
        {
            return;
        }

        var style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
        style &= ~NativeMethods.WS_POPUP;
        style |= NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_CLIPSIBLINGS;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_STYLE, new IntPtr(style));

        var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

        NativeMethods.SetParent(hwnd, parent);

        if (_useCustomBounds)
        {
            NativeMethods.MoveWindow(hwnd, _boundsX, _boundsY, _boundsWidth, _boundsHeight, true);
        }
        else
        {
            NativeMethods.GetWindowRect(parent, out var rect);
            NativeMethods.MoveWindow(
                hwnd,
                0,
                0,
                Math.Max(1, rect.Right - rect.Left),
                Math.Max(1, rect.Bottom - rect.Top),
                true);
        }

        var insertAfter = _shellDefView != IntPtr.Zero ? _shellDefView : NativeMethods.HWND_BOTTOM;
        NativeMethods.SetWindowPos(
            hwnd,
            insertAfter,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

        _workerW = parent;
        _attachedHwnd = hwnd;
        _attached = true;
    }

    public void Detach()
    {
        if (!_attached)
        {
            return;
        }

        var hwnd = GetWindowHandle();
        if (hwnd != IntPtr.Zero)
        {
            var style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
            style &= ~NativeMethods.WS_CHILD;
            style |= NativeMethods.WS_POPUP;
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_STYLE, new IntPtr(style));
            NativeMethods.SetParent(hwnd, IntPtr.Zero);
            NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HWND_NOTOPMOST,
                0,
                0,
                (int)_window.Width,
                (int)_window.Height,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_SHOWWINDOW);
        }

        _attached = false;
        _attachedHwnd = IntPtr.Zero;
    }

    private IntPtr FindDesktopLayer()
    {
        _progman = NativeMethods.FindWindow("Progman", null);
        _shellDefView = IntPtr.Zero;
        _workerW = IntPtr.Zero;

        if (_progman != IntPtr.Zero)
        {
            _shellDefView = NativeMethods.FindWindowEx(_progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        }

        if (_shellDefView == IntPtr.Zero)
        {
            FindWorkerWWithDefView();
        }

        if (_shellDefView == IntPtr.Zero && _progman != IntPtr.Zero)
        {
            // Desktop icons may be hidden. Progman is still the safest desktop-layer parent.
            return _progman;
        }

        return _workerW != IntPtr.Zero ? _workerW : _progman;
    }

    private void FindWorkerWWithDefView()
    {
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            var className = GetClassName(hWnd);
            if (!string.Equals(className, "WorkerW", StringComparison.Ordinal))
            {
                return true;
            }

            IntPtr found = IntPtr.Zero;
            NativeMethods.EnumChildWindows(hWnd, (child, _) =>
            {
                if (string.Equals(GetClassName(child), "SHELLDLL_DefView", StringComparison.Ordinal))
                {
                    found = child;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero)
            {
                _workerW = hWnd;
                _shellDefView = found;
                return false;
            }

            return true;
        }, IntPtr.Zero);
    }

    private static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private IntPtr GetWindowHandle()
    {
        if (_source != null)
        {
            return _source.Handle;
        }

        return new WindowInteropHelper(_window).Handle;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}
