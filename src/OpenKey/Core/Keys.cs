namespace OpenKey.Core;

/// <summary>ปุ่มหนึ่งปุ่ม: ชื่อ, scancode (set 1) และ virtual-key</summary>
public sealed record KeyDef(string Name, ushort Scan, ushort Vk)
{
    /// <summary>ปุ่ม extended (ลูกศร, delete, win ฯลฯ) — ต้องส่ง KEYEVENTF_EXTENDEDKEY</summary>
    public bool Extended => (Scan & 0xE000) == 0xE000;
}

/// <summary>modifier ที่ต้องกดอยู่ตอน match binding</summary>
[Flags]
public enum ModMask
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Win = 8,
}

/// <summary>
/// ตารางชื่อปุ่ม ↔ scancode/VK
///
/// ฝั่ง input จับด้วย scancode เพื่ออ้าง "ตำแหน่งปุ่ม" บนคีย์บอร์ด QWERTY
/// (ไม่ขึ้นกับ layout ไทย/อังกฤษ) — ฝั่ง output ส่งด้วย VK เพื่อให้ shortcut ทำงานทุก layout
/// </summary>
public static class KeyTable
{
    public const ushort VkShift = 0x10;
    public const ushort VkControl = 0x11;
    public const ushort VkMenu = 0x12;
    public const ushort VkLWin = 0x5B;
    public const ushort VkRWin = 0x5C;

    public static readonly KeyDef[] Table =
    [
        new("escape", 0x01, 0x1B),
        new("1", 0x02, 0x31), new("2", 0x03, 0x32), new("3", 0x04, 0x33), new("4", 0x05, 0x34),
        new("5", 0x06, 0x35), new("6", 0x07, 0x36), new("7", 0x08, 0x37), new("8", 0x09, 0x38),
        new("9", 0x0A, 0x39), new("0", 0x0B, 0x30),
        new("-", 0x0C, 0xBD), new("=", 0x0D, 0xBB),
        new("backspace", 0x0E, 0x08), new("tab", 0x0F, 0x09),
        new("q", 0x10, 0x51), new("w", 0x11, 0x57), new("e", 0x12, 0x45), new("r", 0x13, 0x52),
        new("t", 0x14, 0x54), new("y", 0x15, 0x59), new("u", 0x16, 0x55), new("i", 0x17, 0x49),
        new("o", 0x18, 0x4F), new("p", 0x19, 0x50),
        new("[", 0x1A, 0xDB), new("]", 0x1B, 0xDD), new("enter", 0x1C, 0x0D),
        new("ctrl", 0x1D, VkControl),
        new("a", 0x1E, 0x41), new("s", 0x1F, 0x53), new("d", 0x20, 0x44), new("f", 0x21, 0x46),
        new("g", 0x22, 0x47), new("h", 0x23, 0x48), new("j", 0x24, 0x4A), new("k", 0x25, 0x4B),
        new("l", 0x26, 0x4C),
        new(";", 0x27, 0xBA), new("'", 0x28, 0xDE), new("`", 0x29, 0xC0),
        new("shift", 0x2A, VkShift), new("\\", 0x2B, 0xDC),
        new("z", 0x2C, 0x5A), new("x", 0x2D, 0x58), new("c", 0x2E, 0x43), new("v", 0x2F, 0x56),
        new("b", 0x30, 0x42), new("n", 0x31, 0x4E), new("m", 0x32, 0x4D),
        new(",", 0x33, 0xBC), new(".", 0x34, 0xBE), new("/", 0x35, 0xBF),
        new("alt", 0x38, VkMenu), new("space", 0x39, 0x20), new("caps_lock", 0x3A, 0x14),
        new("f1", 0x3B, 0x70), new("f2", 0x3C, 0x71), new("f3", 0x3D, 0x72), new("f4", 0x3E, 0x73),
        new("f5", 0x3F, 0x74), new("f6", 0x40, 0x75), new("f7", 0x41, 0x76), new("f8", 0x42, 0x77),
        new("f9", 0x43, 0x78), new("f10", 0x44, 0x79), new("f11", 0x57, 0x7A), new("f12", 0x58, 0x7B),
        new("print_screen", 0xE037, 0x2C), new("scroll_lock", 0x46, 0x91),
        new("home", 0xE047, 0x24), new("up", 0xE048, 0x26), new("page_up", 0xE049, 0x21),
        new("left", 0xE04B, 0x25), new("right", 0xE04D, 0x27), new("end", 0xE04F, 0x23),
        new("down", 0xE050, 0x28), new("page_down", 0xE051, 0x22),
        new("insert", 0xE052, 0x2D), new("delete", 0xE053, 0x2E),
        new("win", 0xE05B, VkLWin), new("apps", 0xE05D, 0x5D),
    ];

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["esc"] = "escape",
        ["caps"] = "caps_lock",
        ["capslock"] = "caps_lock",
        ["return"] = "enter",
        ["control"] = "ctrl",
        ["windows"] = "win",
        ["super"] = "win",
        ["meta"] = "win",
        ["lwin"] = "win",
        ["del"] = "delete",
        ["ins"] = "insert",
        ["pgup"] = "page_up",
        ["pageup"] = "page_up",
        ["pgdn"] = "page_down",
        ["pagedown"] = "page_down",
    };

    private static readonly Dictionary<string, KeyDef> ByName =
        Table.ToDictionary(k => k.Name, StringComparer.Ordinal);

    private static readonly Dictionary<ushort, KeyDef> ByScan = BuildByScan();

    private static Dictionary<ushort, KeyDef> BuildByScan()
    {
        var map = new Dictionary<ushort, KeyDef>();
        foreach (var def in Table)
        {
            map.TryAdd(def.Scan, def); // ปุ่มที่ใช้ scan ซ้ำ (เช่น win/lwin) เอาตัวแรก
        }
        return map;
    }

    public static readonly string[] Modifiers = ["ctrl", "shift", "alt", "win"];

    public static string Normalize(string name)
    {
        var lower = name.Trim().ToLowerInvariant();
        return Aliases.TryGetValue(lower, out var canonical) ? canonical : lower;
    }

    public static KeyDef? Lookup(string name) =>
        ByName.TryGetValue(Normalize(name), out var def) ? def : null;

    public static KeyDef? ByScanCode(ushort scan) =>
        ByScan.TryGetValue(scan, out var def) ? def : null;

    public static bool IsModifier(string name) =>
        Array.IndexOf(Modifiers, Normalize(name)) >= 0;

    /// <summary>แปลงชื่อ modifier เป็น bit (null = ไม่ใช่ modifier)</summary>
    public static ModMask? ModifierMask(string name) => Normalize(name) switch
    {
        "ctrl" => ModMask.Ctrl,
        "shift" => ModMask.Shift,
        "alt" => ModMask.Alt,
        "win" => ModMask.Win,
        _ => null,
    };

    public static IEnumerable<string> AllNames => Table.Select(k => k.Name);
}
