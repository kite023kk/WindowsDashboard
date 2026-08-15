using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using WindowsDashboard.Win32;

namespace WindowsDashboard.Services;

public sealed class SystemMonitorService : IDisposable
{
    private readonly Timer _timer;
    private ulong _lastIdle;
    private ulong _lastKernel;
    private ulong _lastUser;
    private ulong _lastNetworkBytes;
    private DateTime _lastNetworkTime = DateTime.UtcNow;
    private bool _disposed;

    public event Action<SystemMonitorSnapshot>? Updated;

    public SystemMonitorService()
    {
        _timer = new Timer(_ => RaiseSnapshot(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
    }

    private void RaiseSnapshot()
    {
        if (_disposed)
        {
            return;
        }

        var snapshot = new SystemMonitorSnapshot
        {
            CpuPercent = ReadCpu(),
            MemoryPercent = ReadMemory(),
            CPercent = ReadDrive("C"),
            DPercent = ReadDrive("D"),
            GpuPercent = 0,
            GpuText = "N/A",
            NetworkMbps = ReadNetwork()
        };

        Updated?.Invoke(snapshot);
    }

    private double ReadCpu()
    {
        if (!NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return 0;
        }

        var idleTicks = idle.ToUInt64();
        var kernelTicks = kernel.ToUInt64();
        var userTicks = user.ToUInt64();

        if (_lastKernel == 0 && _lastUser == 0)
        {
            _lastIdle = idleTicks;
            _lastKernel = kernelTicks;
            _lastUser = userTicks;
            return 0;
        }

        var idleDelta = idleTicks - _lastIdle;
        var kernelDelta = kernelTicks - _lastKernel;
        var userDelta = userTicks - _lastUser;
        var total = kernelDelta + userDelta;

        _lastIdle = idleTicks;
        _lastKernel = kernelTicks;
        _lastUser = userTicks;

        if (total == 0)
        {
            return 0;
        }

        return Math.Clamp((total - idleDelta) * 100.0 / total, 0, 100);
    }

    private static double ReadMemory()
    {
        var status = new NativeMethods.MEMORYSTATUSEX
        {
            dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>()
        };

        if (!NativeMethods.GlobalMemoryStatusEx(ref status) || status.ullTotalPhys == 0)
        {
            return 0;
        }

        return Math.Clamp((status.ullTotalPhys - status.ullAvailPhys) * 100.0 / status.ullTotalPhys, 0, 100);
    }

    private static double ReadDrive(string letter)
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d =>
                d.IsReady &&
                string.Equals(d.Name.TrimEnd('\\'), letter + ":", StringComparison.OrdinalIgnoreCase));
            if (drive == null || drive.TotalSize == 0)
            {
                return 0;
            }

            return Math.Clamp((drive.TotalSize - drive.TotalFreeSpace) * 100.0 / drive.TotalSize, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    private double ReadNetwork()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(n => n.Speed)
                .FirstOrDefault();
            if (nic == null)
            {
                return 0;
            }

            var stats = nic.GetIPv4Statistics();
            var now = DateTime.UtcNow;
            var bytes = (ulong)Math.Max(0, stats.BytesReceived);
            var elapsed = (now - _lastNetworkTime).TotalSeconds;
            if (elapsed <= 0 || _lastNetworkBytes == 0)
            {
                _lastNetworkBytes = bytes;
                _lastNetworkTime = now;
                return 0;
            }

            var deltaBytes = bytes > _lastNetworkBytes ? bytes - _lastNetworkBytes : 0;
            var mbps = deltaBytes * 8.0 / elapsed / 1_000_000.0;
            _lastNetworkBytes = bytes;
            _lastNetworkTime = now;
            return Math.Max(0, mbps);
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}

public sealed class SystemMonitorSnapshot
{
    public double CpuPercent { get; set; }
    public double MemoryPercent { get; set; }
    public double CPercent { get; set; }
    public double DPercent { get; set; }
    public double GpuPercent { get; set; }
    public string GpuText { get; set; } = "N/A";
    public double NetworkMbps { get; set; }
}
