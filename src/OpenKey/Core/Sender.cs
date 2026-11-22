using System.Runtime.InteropServices;

namespace OpenKey.Core;

/// <summary>ส่งคีย์ออกด้วย SendInput</summary>
public static class Sender
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfExtendedKey = 0x0001;
    private const uint KeyeventfKeyUp = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    /// <summary>
    /// ต้องมีทั้ง mouse/hardware ใน union ไม่งั้น sizeof(INPUT) จะเล็กกว่าที่ Windows คาด
    /// (x64 = 40 bytes) แล้ว SendInput จะ fail ด้วย ERROR_INVALID_PARAMETER
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    internal static int InputSize => Marshal.SizeOf<INPUT>();

    /// <summary>ขนาดที่ถูกต้องของ INPUT ตามสถาปัตยกรรม (x64 = 40, x86 = 28)</summary>
    internal static int ExpectedInputSize => IntPtr.Size == 8 ? 40 : 28;

    static Sender()
    {
        if (InputSize != ExpectedInputSize)
        {
            throw new InvalidOperationException(
                $"INPUT struct ผิดขนาด: {InputSize} (ต้องเป็น {ExpectedInputSize}) — SendInput จะไม่ทำงาน");
        }
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

        var sent = SendInput((uint)inputs.Length, inputs, InputSize);
        if (sent != inputs.Length)
        {
            Log.Info($"[send] SendInput ส่งได้ {sent}/{inputs.Length} (error {Marshal.GetLastWin32Error()})");
        }
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
