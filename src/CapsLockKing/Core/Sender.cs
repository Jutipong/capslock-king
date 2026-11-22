using System.Runtime.InteropServices;

namespace CapsLockKing.Core;

/// <summary>ส่งคีย์ออกด้วย SendInput</summary>
public static class Sender
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfExtendedKey = 0x0001;
    private const uint KeyeventfKeyUp = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// กดทุกปุ่มตามลำดับแล้วปล่อยย้อนกลับใน batch เดียว
    ///
    /// ไม่ต้อง press modifier ที่ผู้ใช้กดค้างซ้ำ — modifier เหล่านั้นยัง down อยู่ในระบบ
    /// (เราแค่ปล่อย event ผ่าน hook ไปตามปกติ) แอปปลายทางจึงเห็น Shift/Ctrl/Alt เอง
    /// </summary>
    public static void Send(Stroke stroke)
    {
        if (stroke.Keys.Count == 0)
        {
            return;
        }

        var inputs = new INPUT[stroke.Keys.Count * 2];
        var index = 0;
        foreach (var key in stroke.Keys)
        {
            inputs[index++] = MakeInput(key, up: false);
        }
        for (var i = stroke.Keys.Count - 1; i >= 0; i--)
        {
            inputs[index++] = MakeInput(stroke.Keys[i], up: true);
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeInput(OutKey key, bool up)
    {
        var flags = 0u;
        if (key.Extended)
        {
            flags |= KeyeventfExtendedKey;
        }
        if (up)
        {
            flags |= KeyeventfKeyUp;
        }

        return new INPUT
        {
            type = InputKeyboard,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = key.Vk,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };
    }
}
