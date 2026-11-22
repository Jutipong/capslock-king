using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Threading;
using CapsLockKing.Core;
using Wpf.Ui.Controls;

namespace CapsLockKing.Views;

/// <summary>แถวในหน้าตั้งค่า (ผูกกับ ItemsControl)</summary>
public sealed class MappingRow : INotifyPropertyChanged
{
    private string _key = string.Empty;
    private string _action = string.Empty;

    public string Key
    {
        get => _key;
        set
        {
            if (_key == value) return;
            _key = value;
            Raise(nameof(Key));
            Raise(nameof(KeyLabel));
        }
    }

    public string Action
    {
        get => _action;
        set
        {
            if (_action == value) return;
            _action = value;
            Raise(nameof(Action));
        }
    }

    public string KeyLabel => string.IsNullOrWhiteSpace(Key) ? "(เลือก)" : Key;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class MainWindow : FluentWindow
{
    private enum CaptureKind
    {
        Tap,
        HoldKey,
        Action,
    }

    private sealed record CaptureTarget(CaptureKind Kind, MappingRow? Row);

    private readonly AppHost _host;
    private readonly DispatcherTimer _captureTimer;
    private CaptureTarget? _capture;
    private string _tap = string.Empty;

    public ObservableCollection<MappingRow> Rows { get; } = [];

    public MainWindow(AppHost host)
    {
        _host = host;
        InitializeComponent();

        MappingList.ItemsSource = Rows;
        _captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _captureTimer.Tick += (_, _) => PollCapture();

        LoadFromDisk();
        UpdateTapLabel();
        FitToWorkArea();
    }

    /// <summary>กันหน้าต่างใหญ่เกินจอ (เช่น 125% scaling + จอ 1080p)</summary>
    private void FitToWorkArea()
    {
        var work = SystemParameters.WorkArea;
        var maxWidth = Math.Max(MinWidth, work.Width - 40);
        var maxHeight = Math.Max(MinHeight, work.Height - 40);
        if (Width > maxWidth)
        {
            Width = maxWidth;
        }
        if (Height > maxHeight)
        {
            Height = maxHeight;
        }
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
    }

    // ---------------------------------------------------------------- config

    private void LoadFromDisk()
    {
        var path = _host.ConfigPath;
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            SetStatus($"อ่านไฟล์ {path} ไม่ได้: {ex.Message}", ok: false);
            return;
        }

        try
        {
            ConfigLoader.Parse(text);
            SetStatus($"โหลดจาก {path}", ok: true);
        }
        catch (ConfigException ex)
        {
            SetStatus(ex.Message, ok: false);
        }

        PopulateFromJson(text);
    }

    private void PopulateFromJson(string text)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return; // ไฟล์เสีย — เก็บของเดิมในหน้าจอไว้ให้แก้
        }

        using (document)
        {
            var root = document.RootElement;
            var timeout = 0L;
            if (root.TryGetProperty("tapTimeoutMs", out var timeoutElement) &&
                timeoutElement.ValueKind == JsonValueKind.Number)
            {
                timeoutElement.TryGetInt64(out timeout);
            }
            TimeoutBox.Text = timeout.ToString();

            _tap = string.Empty;
            Rows.Clear();

            if (root.TryGetProperty("remap", out var remap) &&
                remap.TryGetProperty(ConfigLoader.TriggerKey, out var caps))
            {
                if (caps.TryGetProperty("tap", out var tapElement) &&
                    tapElement.ValueKind == JsonValueKind.String)
                {
                    _tap = tapElement.GetString() ?? string.Empty;
                }

                if (caps.TryGetProperty("hold", out var hold) && hold.ValueKind == JsonValueKind.Object)
                {
                    foreach (var entry in hold.EnumerateObject())
                    {
                        Rows.Add(new MappingRow
                        {
                            Key = entry.Name,
                            Action = entry.Value.ValueKind == JsonValueKind.String
                                ? entry.Value.GetString() ?? string.Empty
                                : string.Empty,
                        });
                    }
                }
            }

            UpdateTapLabel();
        }
    }

    private string BuildJson()
    {
        if (!int.TryParse(TimeoutBox.Text.Trim(), out var timeout))
        {
            throw new ConfigException("tapTimeoutMs ต้องเป็นจำนวนเต็มตั้งแต่ 0 ขึ้นไป");
        }

        return ConfigWriter.Build(
            _tap,
            timeout,
            Rows.Select(row => (row.Key, row.Action)));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string json;
        try
        {
            json = BuildJson();
            ConfigLoader.Parse(json);
        }
        catch (ConfigException ex)
        {
            SetStatus(ex.Message, ok: false);
            return;
        }

        var path = _host.ConfigPath;
        try
        {
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            SetStatus($"บันทึกไฟล์ไม่ได้: {ex.Message}", ok: false);
            return;
        }

        _host.ReloadFromDisk(report: false);
        Log.Info($"บันทึก config จาก GUI: {path}");
        SetStatus($"บันทึกและใช้งานแล้ว ({path})", ok: true);
    }

    private void Reload_Click(object sender, RoutedEventArgs e) => LoadFromDisk();

    private void Defaults_Click(object sender, RoutedEventArgs e)
    {
        PopulateFromJson(ConfigLoader.DefaultJson);
        SetStatus("ใส่ค่าเริ่มต้นแล้ว — กด “บันทึก & ใช้” เพื่อยืนยัน", ok: true);
    }

    private void OpenJson_Click(object sender, RoutedEventArgs e)
    {
        _host.EnsureConfigFile();
        AppHost.OpenPath(_host.ConfigPath);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // ---------------------------------------------------------------- capture

    private void CaptureTap_Click(object sender, RoutedEventArgs e) =>
        BeginCapture(new CaptureTarget(CaptureKind.Tap, null));

    private void ClearTap_Click(object sender, RoutedEventArgs e)
    {
        _tap = string.Empty;
        UpdateTapLabel();
    }

    private void CaptureKey_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MappingRow row)
        {
            BeginCapture(new CaptureTarget(CaptureKind.HoldKey, row));
        }
    }

    private void CaptureCombo_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MappingRow row)
        {
            BeginCapture(new CaptureTarget(CaptureKind.Action, row));
        }
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not MappingRow row)
        {
            return;
        }
        Rows.Remove(row);
        if (_capture?.Row == row)
        {
            EndCapture();
        }
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        var row = new MappingRow();
        Rows.Add(row);
        MappingList.UpdateLayout();
        if (MappingList.ItemContainerGenerator.ContainerFromItem(row) is FrameworkElement container)
        {
            container.BringIntoView();
        }
        BeginCapture(new CaptureTarget(CaptureKind.HoldKey, row));
    }

    private void BeginCapture(CaptureTarget target)
    {
        _capture = target;
        _host.Hook.SetCapture(true);
        CaptureOverlay.Visibility = Visibility.Visible;
        UpdateCaptureText();
        _captureTimer.Start();
    }

    private void UpdateCaptureText()
    {
        if (_capture is null)
        {
            return;
        }

        (CaptureTitle.Text, CaptureHint.Text) = _capture.Kind switch
        {
            CaptureKind.Tap => ("กดปุ่มสำหรับ CapsLock เดี่ยว",
                "กดปุ่มบนคีย์บอร์ดได้เลย (Esc ก็ใช้ได้)"),
            CaptureKind.HoldKey => ("กดปุ่มที่ต้องการใช้คู่กับ CapsLock",
                "กดปุ่มบนคีย์บอร์ดได้เลย — ระบบจะถาม combo ต่อถ้ายังว่างอยู่"),
            _ => ("กด combo ที่ต้องการ",
                "เช่น Ctrl+Shift+Left หรือ Ctrl+C"),
        };
    }

    private void PollCapture()
    {
        if (_capture is null)
        {
            return;
        }

        var captured = _host.Hook.TakeCaptured();
        if (captured is not { } key)
        {
            return;
        }

        var def = KeyTable.ByScanCode(key.Scan);
        if (def is null)
        {
            SetStatus($"ปุ่มนี้ยังไม่รองรับ (scancode 0x{key.Scan:X4}) — ลองปุ่มอื่น", ok: false);
            return;
        }

        switch (_capture.Kind)
        {
            case CaptureKind.Tap:
                _tap = def.Name;
                UpdateTapLabel();
                EndCapture();
                break;

            case CaptureKind.HoldKey when _capture.Row is { } row:
                row.Key = def.Name;
                if (string.IsNullOrWhiteSpace(row.Action))
                {
                    // ให้จับ combo ต่อทันที — ผู้ใช้กดต่อได้เลย
                    _capture = new CaptureTarget(CaptureKind.Action, row);
                    CaptureHint.Text = "กด combo ที่ต้องการต่อ (เช่น Ctrl+C)";
                    SetStatus("ได้ปุ่มแล้ว — กด combo ที่ต้องการต่อได้เลย", ok: true);
                }
                else
                {
                    EndCapture();
                }
                break;

            case CaptureKind.Action when _capture.Row is { } row:
                row.Action = key.ActionSpec();
                EndCapture();
                break;
        }
    }

    private void CancelCapture_Click(object sender, RoutedEventArgs e) => EndCapture();

    private void EndCapture()
    {
        _captureTimer.Stop();
        _capture = null;
        _host.Hook.SetCapture(false);
        CaptureOverlay.Visibility = Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        EndCapture();
        base.OnClosed(e);
    }

    // ---------------------------------------------------------------- helpers

    private void UpdateTapLabel()
    {
        TapButton.Content = _tap.Length == 0 ? "(ไม่ทำอะไร)" : _tap;
    }

    private void SetStatus(string text, bool ok)
    {
        StatusText.Text = text;
        StatusText.Foreground = (System.Windows.Media.Brush)FindResource(
            ok ? "PageOkBrush" : "PageErrorBrush");
    }
}
