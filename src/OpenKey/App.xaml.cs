using System.IO;
using System.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using OpenKey.Core;

namespace OpenKey;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\OpenKey";
    private const string SignalName = @"Local\OpenKey.ShowSettings";

    private Mutex? _mutex;
    private EventWaitHandle? _signal;
    private AppHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var args = e.Args;
        var debug = args.Any(a => a is "--debug" or "-d");
        var baseDir = AppContext.BaseDirectory;
        Log.Init(debug, Path.Combine(baseDir, "openkey.log"));

        if (args.Any(a => a is "--help" or "-h"))
        {
            MessageBox.Show(
                "OpenKey — key remapper for Windows\r\n\r\n" +
                "--debug            เขียน log ที่ openkey.log\r\n" +
                "--config <path>    ใช้ไฟล์ config อื่น\r\n" +
                "--settings         เปิดหน้าตั้งค่าทันที\r\n\r\n" +
                "แก้ mapping ได้จาก tray icon → Settings…",
                "OpenKey",
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
            MessageBox.Show(ex.Message, "OpenKey", MessageBoxButton.OK, MessageBoxImage.Error);
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
