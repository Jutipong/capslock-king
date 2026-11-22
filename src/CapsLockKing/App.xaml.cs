using System.IO;
using System.Threading;
using System.Windows;
using CapsLockKing.Core;

namespace CapsLockKing;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\CapsLockKingV2";
    private const string SignalName = @"Local\CapsLockKingV2.ShowSettings";

    private Mutex? _mutex;
    private EventWaitHandle? _signal;
    private AppHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var args = e.Args;
        var debug = args.Any(a => a is "--debug" or "-d");
        var baseDir = AppContext.BaseDirectory;
        Log.Init(debug, Path.Combine(baseDir, "capslockking.log"));

        if (args.Any(a => a is "--help" or "-h"))
        {
            System.Windows.MessageBox.Show(
                "CapsLock King v2\r\n\r\n" +
                "--debug            เขียน log ที่ capslockking.log\r\n" +
                "--config <path>    ใช้ไฟล์ config อื่น\r\n" +
                "--settings         เปิดหน้าตั้งค่าทันที\r\n\r\n" +
                "แก้ mapping ได้จาก tray icon → Settings…",
                "CapsLock King",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
            Log.Info($"unhandled: {ex.ExceptionObject}");

        // อินสแตนซ์ที่สองจะ Set event นี้ → เปิดหน้าตั้งค่า
        _signal = new EventWaitHandle(false, EventResetMode.AutoReset, SignalName);
        var listener = new Thread(() =>
        {
            while (_signal.WaitOne())
            {
                Dispatcher.Invoke(() => _host?.ShowSettings());
            }
        })
        {
            IsBackground = true,
            Name = "second-instance-listener",
        };
        listener.Start();

        var configPath = ValueOf(args, "--config") ?? Path.Combine(baseDir, "config.json");
        _host = new AppHost(configPath);

        try
        {
            _host.Start();
        }
        catch (Exception ex)
        {
            Log.Info(ex.ToString());
            System.Windows.MessageBox.Show(ex.Message, "CapsLock King", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        if (args.Any(a => a == "--settings"))
        {
            _host.ShowSettings();
        }
    }

    private static void SignalExistingInstance()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (EventWaitHandle.TryOpenExisting(SignalName, out var handle))
            {
                using (handle)
                {
                    handle.Set();
                }
                return;
            }
            Thread.Sleep(50);
        }
    }

    private static string? ValueOf(string[] args, string flag)
    {
        var index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        _signal?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
