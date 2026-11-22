using System.Diagnostics;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace OpenKey.Core;

/// <summary>ศูนย์กลางของแอป: config, hook, tray, การเปิดหน้าตั้งค่า</summary>
public sealed class AppHost : IDisposable
{
    private readonly KeyboardHook _hook = new();
    private TrayIcon? _tray;
    private Views.MainWindow? _window;

    public string ConfigPath { get; }
    public ConfigDocument Document { get; private set; }
    public CompiledConfig Config { get; private set; }

    public KeyboardHook Hook => _hook;

    public AppHost(string configPath)
    {
        ConfigPath = configPath;
        Document = ConfigLoader.DefaultDocument();
        Config = ConfigLoader.Compile(Document);
    }

    public void Start()
    {
        Document = LoadInitial();
        ClearCapsLockToggle();
        _hook.Start(Config);
        _tray = new TrayIcon(HandleCommand);
        Log.Info($"พร้อมใช้งาน — config: {ConfigPath} ({Config.Triggers.Count} trigger, {Config.Bindings.Count} binding)");
    }

    public ConfigDocument LoadInitial()
    {
        if (!File.Exists(ConfigPath))
        {
            Log.Info($"ไม่พบ {ConfigPath} → ใช้ค่าเริ่มต้นที่ฝังใน exe");
            return ConfigLoader.DefaultDocument();
        }

        try
        {
            var document = ConfigLoader.Load(ConfigPath);
            Config = ConfigLoader.Compile(document);
            return document;
        }
        catch (ConfigException ex)
        {
            Log.Info(ex.Message);
            MessageBox.Show(
                $"{ex.Message}\n\nใช้ค่าเริ่มต้นที่ฝังมากับ exe แทน",
                "OpenKey — config ผิดพลาด",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            var fallback = ConfigLoader.DefaultDocument();
            Config = ConfigLoader.Compile(fallback);
            return fallback;
        }
    }

    /// <summary>อ่าน config ใหม่แล้วสลับของเดิม (ถ้าอ่านไม่ผ่าน = คงของเดิมไว้)</summary>
    public bool ReloadFromDisk(bool report)
    {
        try
        {
            var document = ConfigLoader.Load(ConfigPath);
            var compiled = ConfigLoader.Compile(document);
            Document = document;
            Config = compiled;
            _hook.SetConfig(compiled);
            Log.Info($"reload สำเร็จ: {compiled.Triggers.Count} trigger, {compiled.Bindings.Count} binding");
            if (report)
            {
                MessageBox.Show(
                    $"โหลด config ใหม่แล้ว ({compiled.Triggers.Count} trigger, {compiled.Bindings.Count} binding)",
                    "OpenKey",
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
                MessageBox.Show(
                    $"{ex.Message}\n\nยังใช้ mapping เดิมอยู่",
                    "OpenKey — config ผิดพลาด",
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
                Application.Current.Shutdown();
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
            MessageBox.Show(
                $"สร้าง {ConfigPath} ไม่ได้: {ex.Message}",
                "OpenKey",
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
            MessageBox.Show(ex.Message, "OpenKey", MessageBoxButton.OK, MessageBoxImage.Error);
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
