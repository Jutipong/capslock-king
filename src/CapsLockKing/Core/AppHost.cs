using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace CapsLockKing.Core;

/// <summary>ศูนย์กลางของแอป: config, hook, tray, การเปิดหน้าตั้งค่า</summary>
public sealed class AppHost : IDisposable
{
    private readonly KeyboardHook _hook = new();
    private TrayIcon? _tray;
    private Views.MainWindow? _window;

    public string ConfigPath { get; }
    public CompiledConfig Config { get; private set; }

    public KeyboardHook Hook => _hook;

    public AppHost(string configPath)
    {
        ConfigPath = configPath;
        Config = ConfigLoader.DefaultConfig();
    }

    public void Start()
    {
        Config = LoadInitial();
        ClearCapsLockToggle();
        _hook.Start(Config);
        _tray = new TrayIcon(HandleCommand);
        Log.Info($"พร้อมใช้งาน — config: {ConfigPath} ({Config.Hold.Count} mapping)");
    }

    public CompiledConfig LoadInitial()
    {
        if (!File.Exists(ConfigPath))
        {
            Log.Info($"ไม่พบ {ConfigPath} → ใช้ค่าเริ่มต้นที่ฝังใน exe");
            return ConfigLoader.DefaultConfig();
        }

        try
        {
            return ConfigLoader.Load(ConfigPath);
        }
        catch (ConfigException ex)
        {
            Log.Info(ex.Message);
            System.Windows.MessageBox.Show(
                $"{ex.Message}\n\nใช้ค่าเริ่มต้นที่ฝังมากับ exe แทน",
                "CapsLock King — config ผิดพลาด",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return ConfigLoader.DefaultConfig();
        }
    }

    /// <summary>อ่าน config ใหม่แล้วสลับของเดิม (ถ้าอ่านไม่ผ่าน = คงของเดิมไว้)</summary>
    public bool ReloadFromDisk(bool report)
    {
        try
        {
            Config = ConfigLoader.Load(ConfigPath);
            _hook.SetConfig(Config);
            Log.Info($"reload สำเร็จ: {Config.Hold.Count} mapping");
            if (report)
            {
                System.Windows.MessageBox.Show(
                    $"โหลด config ใหม่แล้ว ({Config.Hold.Count} mapping)",
                    "CapsLock King",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            return true;
        }
        catch (ConfigException ex)
        {
            Log.Info($"reload ไม่สำเร็จ: {ex.Message}");
            if (report)
            {
                System.Windows.MessageBox.Show(
                    $"{ex.Message}\n\nยังใช้ mapping เดิมอยู่",
                    "CapsLock King — config ผิดพลาด",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            return false;
        }
    }

    public void ShowSettings()
    {
        if (_window is null)
        {
            _window = new Views.MainWindow(this);
            _window.Closed += (_, _) => _window = null;
        }
        _window.Show();
        _window.Activate();
        _window.Focus();
    }

    private void HandleCommand(TrayCommand command)
    {
        switch (command)
        {
            case TrayCommand.Settings:
                ShowSettings();
                break;
            case TrayCommand.Reload:
                ReloadFromDisk(report: true);
                break;
            case TrayCommand.OpenConfig:
                EnsureConfigFile();
                OpenPath(ConfigPath);
                break;
            case TrayCommand.OpenFolder:
                OpenPath(Path.GetDirectoryName(ConfigPath) ?? ".");
                break;
            case TrayCommand.Exit:
                Log.Info("exit จาก tray");
                Dispose();
                System.Windows.Application.Current.Shutdown();
                break;
        }
    }

    public void EnsureConfigFile()
    {
        if (File.Exists(ConfigPath))
        {
            return;
        }
        try
        {
            File.WriteAllText(ConfigPath, ConfigLoader.DefaultJson);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"สร้าง {ConfigPath} ไม่ได้: {ex.Message}",
                "CapsLock King",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public static void OpenPath(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "CapsLock King", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>ถ้า CapsLock ถูก toggle ค้างไว้ตอนเปิดโปรแกรม ให้ปิด LED ไปเลย</summary>
    private static void ClearCapsLockToggle()
    {
        if ((GetAsyncKeyState(0x14) & 1) == 0)
        {
            return;
        }
        Sender.Send(Stroke.Single(0x14));
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public void Dispose()
    {
        _hook.Dispose();
        _tray?.Dispose();
        _tray = null;
    }
}
