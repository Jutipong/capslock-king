using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using OpenKey.Core;
using Wpf.Ui.Controls;

namespace OpenKey.Views;

/// <summary>แถวในตาราง mapping ของ trigger</summary>
public sealed class MapRow : INotifyPropertyChanged
{
    private string _from = string.Empty;
    private string _to = string.Empty;

    public string From
    {
        get => _from;
        set
        {
            if (_from == value) return;
            _from = value;
            Raise(nameof(From));
            Raise(nameof(FromLabel));
        }
    }

    public string To
    {
        get => _to;
        set
        {
            if (_to == value) return;
            _to = value;
            Raise(nameof(To));
        }
    }

    public string FromLabel => string.IsNullOrWhiteSpace(From) ? "(เลือก)" : From;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>แถวในตาราง direct mapping</summary>
public sealed class DirectRow : INotifyPropertyChanged
{
    private string _from = string.Empty;
    private string _to = string.Empty;

    public string From
    {
        get => _from;
        set
        {
            if (_from == value) return;
            _from = value;
            Raise(nameof(From));
            Raise(nameof(FromLabel));
        }
    }

    public string To
    {
        get => _to;
        set
        {
            if (_to == value) return;
            _to = value;
            Raise(nameof(To));
        }
    }

    public string FromLabel => string.IsNullOrWhiteSpace(From) ? "(เลือก)" : From;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>trigger หนึ่งตัว + ตาราง map ของมัน</summary>
public sealed class TriggerRow : INotifyPropertyChanged
{
    private string _key = string.Empty;
    private string _tap = string.Empty;

    public string Key
    {
        get => _key;
        set
        {
            if (_key == value) return;
            _key = value;
            Raise(nameof(Key));
        }
    }

    public string Tap
    {
        get => _tap;
        set
        {
            if (_tap == value) return;
            _tap = value;
            Raise(nameof(Tap));
            Raise(nameof(TapLabel));
        }
    }

    public string TapLabel => string.IsNullOrWhiteSpace(Tap) ? "tap: (ไม่มี)" : $"tap: {Tap}";

    public ObservableCollection<MapRow> Map { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class MainWindow : FluentWindow
{
    private enum CaptureKind
    {
        TriggerKey,
        TriggerTap,
        MapFrom,
        MapTo,
        DirectFrom,
        DirectTo,
    }

    private sealed record CaptureTarget(CaptureKind Kind, object? Item);

    private readonly AppHost _host;
    private readonly DispatcherTimer _captureTimer;
    private CaptureTarget? _capture;

    private ObservableCollection<TriggerRow> TriggerRows { get; } = [];
    private ObservableCollection<DirectRow> Directs { get; } = [];

    public MainWindow(AppHost host)
    {
        _host = host;
        InitializeComponent();

        TriggerList.ItemsSource = TriggerRows;
        DirectList.ItemsSource = Directs;

        _captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _captureTimer.Tick += (_, _) => PollCapture();

        LoadFromDisk();
        FitToWorkArea();
    }

    private TriggerRow? Selected => TriggerList.SelectedItem as TriggerRow;

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

        ConfigDocument document;
        try
        {
            document = ConfigLoader.Parse(text);
            ConfigLoader.Compile(document);
            SetStatus($"โหลดจาก {path}", ok: true);
        }
        catch (ConfigException ex)
        {
            SetStatus(ex.Message, ok: false);
            // ยังพยายามแสดงเท่าที่อ่านได้
            try
            {
                document = ConfigLoader.Parse(text);
            }
            catch
            {
                return;
            }
        }

        Populate(document);
    }

    private void Populate(ConfigDocument document)
    {
        TimeoutBox.Text = document.TapTimeoutMs.ToString();
        TriggerRows.Clear();
        foreach (var trigger in document.Triggers)
        {
            var row = new TriggerRow { Key = trigger.Key, Tap = trigger.Tap };
            foreach (var binding in trigger.Map)
            {
                row.Map.Add(new MapRow { From = binding.From, To = binding.To });
            }
            TriggerRows.Add(row);
        }

        Directs.Clear();
        foreach (var binding in document.Bindings)
        {
            Directs.Add(new DirectRow { From = binding.From, To = binding.To });
        }

        if (TriggerRows.Count > 0)
        {
            TriggerList.SelectedIndex = 0;
        }
        UpdateTriggerPanel();
    }

    private ConfigDocument BuildDocument()
    {
        if (!long.TryParse(TimeoutBox.Text.Trim(), out var timeout) || timeout < 0)
        {
            throw new ConfigException("tapTimeout ต้องเป็นจำนวนเต็มตั้งแต่ 0 ขึ้นไป");
        }

        var document = new ConfigDocument { TapTimeoutMs = timeout };

        foreach (var trigger in TriggerRows)
        {
            if (string.IsNullOrWhiteSpace(trigger.Key))
            {
                continue;
            }

            var doc = new TriggerDocument { Key = trigger.Key.Trim(), Tap = trigger.Tap.Trim() };
            foreach (var row in trigger.Map)
            {
                if (string.IsNullOrWhiteSpace(row.From))
                {
                    continue;
                }
                doc.Map.Add(new BindingDocument(row.From.Trim(), row.To.Trim()));
            }
            document.Triggers.Add(doc);
        }

        foreach (var row in Directs)
        {
            if (string.IsNullOrWhiteSpace(row.From))
            {
                continue;
            }
            document.Bindings.Add(new BindingDocument(row.From.Trim(), row.To.Trim()));
        }

        return document;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string json;
        ConfigDocument document;
        try
        {
            document = BuildDocument();
            ConfigLoader.Compile(document);
            json = ConfigLoader.ToJson(document);
            ConfigLoader.Compile(ConfigLoader.Parse(json));
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
        Populate(ConfigLoader.DefaultDocument());
        SetStatus("ใส่ค่าเริ่มต้นแล้ว — กด “บันทึก & ใช้” เพื่อยืนยัน", ok: true);
    }

    private void OpenJson_Click(object sender, RoutedEventArgs e)
    {
        _host.EnsureConfigFile();
        AppHost.OpenPath(_host.ConfigPath);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // ---------------------------------------------------------------- TriggerRows

    private void TriggerList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        UpdateTriggerPanel();

    private void UpdateTriggerPanel()
    {
        var selected = Selected;
        MapList.ItemsSource = selected?.Map;
        TapButton.Content = selected is null
            ? "(ไม่ทำอะไร)"
            : string.IsNullOrWhiteSpace(selected.Tap) ? "(ไม่ทำอะไร)" : selected.Tap;
        MapHeader.Text = selected is null
            ? "เลือก trigger ทางซ้าย"
            : $"คีย์ลัดของ {selected.Key}";
    }

    private void AddTrigger_Click(object sender, RoutedEventArgs e) =>
        BeginCapture(new CaptureTarget(CaptureKind.TriggerKey, null));

    private void DeleteTrigger_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { } row)
        {
            TriggerRows.Remove(row);
            UpdateTriggerPanel();
        }
    }

    private void CaptureTap_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { } row)
        {
            BeginCapture(new CaptureTarget(CaptureKind.TriggerTap, row));
        }
    }

    private void ClearTap_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { } row)
        {
            row.Tap = string.Empty;
            UpdateTriggerPanel();
        }
    }

    // ---------------------------------------------------------------- mappings

    private void AddMap_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } trigger)
        {
            SetStatus("เลือก trigger ก่อน", ok: false);
            return;
        }
        BeginCapture(new CaptureTarget(CaptureKind.MapFrom, trigger));
    }

    private void MapFrom_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MapRow row)
        {
            BeginCapture(new CaptureTarget(CaptureKind.MapFrom, row));
        }
    }

    private void MapTo_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MapRow row)
        {
            BeginCapture(new CaptureTarget(CaptureKind.MapTo, row));
        }
    }

    private void DeleteMap_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not MapRow row)
        {
            return;
        }
        if (Selected is { } trigger)
        {
            trigger.Map.Remove(row);
        }
    }

    private void AddDirect_Click(object sender, RoutedEventArgs e) =>
        BeginCapture(new CaptureTarget(CaptureKind.DirectFrom, null));

    private void DirectFrom_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DirectRow row)
        {
            BeginCapture(new CaptureTarget(CaptureKind.DirectFrom, row));
        }
    }

    private void DirectTo_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DirectRow row)
        {
            BeginCapture(new CaptureTarget(CaptureKind.DirectTo, row));
        }
    }

    private void DeleteDirect_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DirectRow row)
        {
            Directs.Remove(row);
        }
    }

    // ---------------------------------------------------------------- capture

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
            CaptureKind.TriggerKey => ("กดคีย์ที่ต้องการใช้เป็น trigger",
                "คีย์นี้จะถูกใช้เป็นคีย์ค้าง (แตะเดี่ยวจะพิมพ์ตัวมันเอง)"),
            CaptureKind.TriggerTap => ("กดคีย์สำหรับ “แตะเดี่ยว”",
                "กดปุ่มบนคีย์บอร์ดได้เลย (Esc ก็ใช้ได้)"),
            CaptureKind.MapFrom => ("กดคีย์ที่ใช้คู่กับ trigger",
                "กด modifier ค้างได้ เช่น Shift+H"),
            CaptureKind.MapTo => ("กด combo ที่ต้องการส่งออก",
                "เช่น Ctrl+C, Left, Win+Ctrl+Right"),
            CaptureKind.DirectFrom => ("กดคีย์ต้นทาง",
                "กด modifier ค้างได้ เช่น Alt หรือ Ctrl+Alt+P"),
            _ => ("กด combo ที่ต้องการส่งออก",
                "เช่น Win, Ctrl+Shift+Escape"),
        };
    }

    private void PollCapture()
    {
        if (_capture is not { } target)
        {
            return;
        }

        var captured = _host.Hook.TakeCaptured();
        if (captured is not { } key)
        {
            return;
        }

        var def = Core.KeyTable.ByScanCode(key.Scan);
        if (def is null)
        {
            SetStatus($"ปุ่มนี้ยังไม่รองรับ (scancode 0x{key.Scan:X4}) — ลองปุ่มอื่น", ok: false);
            return;
        }

        switch (target.Kind)
        {
            case CaptureKind.TriggerKey:
            {
                var row = new TriggerRow { Key = def.Name, Tap = def.Name };
                TriggerRows.Add(row);
                TriggerList.SelectedItem = row;
                UpdateTriggerPanel();
                EndCapture();
                SetStatus($"เพิ่ม trigger “{def.Name}” แล้ว", ok: true);
                break;
            }

            case CaptureKind.TriggerTap when target.Item is TriggerRow trigger:
                trigger.Tap = key.ActionSpec();
                UpdateTriggerPanel();
                EndCapture();
                break;

            case CaptureKind.MapFrom when target.Item is TriggerRow owner:
            {
                var row = new MapRow { From = key.ActionSpec() };
                owner.Map.Add(row);
                if (string.IsNullOrWhiteSpace(row.To))
                {
                    _capture = new CaptureTarget(CaptureKind.MapTo, row);
                    CaptureHint.Text = "กด combo ที่ต้องการส่งออกต่อได้เลย";
                }
                else
                {
                    EndCapture();
                }
                break;
            }

            case CaptureKind.MapFrom when target.Item is MapRow row:
                row.From = key.ActionSpec();
                EndCapture();
                break;

            case CaptureKind.MapTo when target.Item is MapRow row:
                row.To = key.ActionSpec();
                EndCapture();
                break;

            case CaptureKind.DirectFrom:
            {
                var row = target.Item as DirectRow ?? new DirectRow();
                if (target.Item is null)
                {
                    Directs.Add(row);
                }
                row.From = key.ActionSpec();
                if (string.IsNullOrWhiteSpace(row.To))
                {
                    _capture = new CaptureTarget(CaptureKind.DirectTo, row);
                    CaptureHint.Text = "กด combo ที่ต้องการส่งออกต่อได้เลย";
                }
                else
                {
                    EndCapture();
                }
                break;
            }

            case CaptureKind.DirectTo when target.Item is DirectRow row:
                row.To = key.ActionSpec();
                EndCapture();
                break;

            default:
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

    private void SetStatus(string text, bool ok)
    {
        StatusText.Text = text;
        StatusText.Foreground = (System.Windows.Media.Brush)FindResource(
            ok ? "PageOkBrush" : "PageErrorBrush");
    }
}
