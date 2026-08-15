using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace WindowsDashboard;

public partial class App : Application
{
    private Mutex? _instanceMutex;
    private bool _ownsInstance;
    private EventWaitHandle? _activateEvent;
    private DispatcherTimer? _activateTimer;
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(true, @"Local\WindowsDashboard.SingleInstance", out _ownsInstance);
        if (!_ownsInstance)
        {
            try
            {
                using var activate = EventWaitHandle.OpenExisting(@"Local\WindowsDashboard.Activate");
                activate.Set();
            }
            catch
            {
                // The primary instance may not be ready yet; ignore.
            }

            Shutdown();
            return;
        }

        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\WindowsDashboard.Activate");
        _activateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _activateTimer.Tick += (_, _) =>
        {
            if (_activateEvent?.WaitOne(0) == true)
            {
                _window?.ShowFromSecondInstance();
            }
        };
        _activateTimer.Start();

        _window = new MainWindow();
        _window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _window?.ShutdownServices();
        _activateTimer?.Stop();
        _activateEvent?.Dispose();
        if (_ownsInstance)
        {
            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
        }

        base.OnExit(e);
    }
}
