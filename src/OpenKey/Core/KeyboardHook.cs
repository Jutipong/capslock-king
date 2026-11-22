using System.Runtime.InteropServices;

namespace OpenKey.Core;

/// <summary>คีย์ที่จับได้ระหว่างโหมด capture ของ GUI</summary>
public readonly record struct CapturedKey(ushort Scan, bool Ctrl, bool Shift, bool Alt, bool Win)
{
    /// <summary>แปลงเป็นข้อความใน config เช่น "ctrl+shift+left"</summary>
    public string ActionSpec()
    {
        var parts = new List<string>();
        if (Ctrl) parts.Add("ctrl");
        if (Shift) parts.Add("shift");
        if (Alt) parts.Add("alt");
        if (Win) parts.Add("win");
        parts.Add(KeyTable.ByScanCode(Scan)?.Name
            ?? throw new InvalidOperationException("ปุ่มนี้ยังไม่รองรับ"));
        return string.Join('+', parts);
    }
}

/// <summary>
/// Low-level keyboard hook (WH_KEYBOARD_LL) — ติดตั้งบน thread ปัจจุบัน
/// ต้องเป็น thread ที่มี message loop (UI thread ของ WPF ใช้ได้)
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const uint LlkhfExtended = 0x01;
    private const uint LlkhfInjected = 0x10;
    private const uint VkCapital = 0x14;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    // delegate ต้องมี reference ค้างไว้ ไม่งั้น GC เก็บแล้ว hook crash
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _handle;

    private readonly object _gate = new();
    private HookEngine? _engine;

    // สถานะ capture (อ่านจาก hook callback / เขียนจาก UI thread)
    private volatile bool _capture;
    private long _captured; // bit63 = valid

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Start(CompiledConfig config)
    {
        SetConfig(config);
        if (_handle != IntPtr.Zero)
        {
            return;
        }

        _handle = SetWindowsHookEx(WhKeyboardLl, _proc, GetModuleHandle(null), 0);
        if (_handle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"ติดตั้ง keyboard hook ไม่สำเร็จ (error {Marshal.GetLastWin32Error()})");
        }
        Log.Info($"[hook] installed handle=0x{_handle:X}");
    }

    public void SetConfig(CompiledConfig config)
    {
        lock (_gate)
        {
            _engine = new HookEngine(config);
        }
    }

    /// <summary>true = ปล่อยคีย์ดิบทั้งหมด (ใช้ตอน GUI กำลังจับคีย์)</summary>
    public void SetCapture(bool on)
    {
        _capture = on;
        Log.Info($"[hook] capture={on}");
        if (on)
        {
            Interlocked.Exchange(ref _captured, 0);
        }
    }

    /// <summary>ดึงคีย์ที่จับได้ล่าสุด (null = ยังไม่มี)</summary>
    public CapturedKey? TakeCaptured()
    {
        var packed = Interlocked.Exchange(ref _captured, 0);
        if ((packed & (1L << 63)) == 0)
        {
            return null;
        }

        var flags = (int)((packed >> 32) & 0xF);
        return new CapturedKey(
            Scan: (ushort)((packed >> 16) & 0xFFFF),
            Ctrl: (flags & 1) != 0,
            Shift: (flags & 2) != 0,
            Alt: (flags & 4) != 0,
            Win: (flags & 8) != 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(_handle, nCode, wParam, lParam);
        }

        var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        var down = (int)wParam == WmKeyDown || (int)wParam == WmSysKeyDown;
        var scan = (ushort)info.scanCode;
        if ((info.flags & LlkhfExtended) != 0)
        {
            scan |= 0xE000;
        }

        // กัน recursion จาก SendInput ของเราเอง
        if ((info.flags & LlkhfInjected) != 0)
        {
            return CallNextHookEx(_handle, nCode, wParam, lParam);
        }

        if (_capture)
        {
            // CapsLock ยังต้องกลืนไว้ ไม่งั้น LED/สถานะ toggle จะเพี้ยนตอนจับคีย์
            if (info.vkCode == VkCapital)
            {
                return (IntPtr)1;
            }
            if (down)
            {
                RecordCapture(info.vkCode, scan);
            }
            return CallNextHookEx(_handle, nCode, wParam, lParam);
        }

        FilterResult result;
        lock (_gate)
        {
            result = _engine?.OnEvent(info.vkCode, scan, down, CurrentModifiers(), Environment.TickCount64)
                ?? FilterResult.Pass;
        }

        foreach (var stroke in result.Strokes)
        {
            Sender.Send(stroke);
        }

        return result.Swallow ? (IntPtr)1 : CallNextHookEx(_handle, nCode, wParam, lParam);
    }

    private static ModMask CurrentModifiers()
    {
        var mods = ModMask.None;
        if (Held(0xA2) || Held(0xA3)) mods |= ModMask.Ctrl;
        if (Held(0xA0) || Held(0xA1)) mods |= ModMask.Shift;
        if (Held(0xA4) || Held(0xA5)) mods |= ModMask.Alt;
        if (Held(0x5B) || Held(0x5C)) mods |= ModMask.Win;
        return mods;
    }

    private static bool Held(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private static bool IsModifierVk(uint vk) =>
        vk is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or (>= 0xA0 and <= 0xA5);

    private void RecordCapture(uint vk, ushort scan)
    {
        if (IsModifierVk(vk) || vk == VkCapital)
        {
            return; // รอปุ่มจริง ไม่นับ modifier/CapsLock
        }

        var mods = CurrentModifiers();
        var flags = 0L;
        if (mods.HasFlag(ModMask.Ctrl)) flags |= 1;
        if (mods.HasFlag(ModMask.Shift)) flags |= 2;
        if (mods.HasFlag(ModMask.Alt)) flags |= 4;
        if (mods.HasFlag(ModMask.Win)) flags |= 8;

        var packed = (1L << 63) | (vk & 0xFFFF) | ((long)scan << 16) | (flags << 32);
        Interlocked.Exchange(ref _captured, packed);
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
