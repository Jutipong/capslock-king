namespace CapsLockKing.Core;

/// <summary>ปุ่มเดียวที่พร้อมส่ง (VK + extended flag)</summary>
public readonly record struct OutKey(ushort Vk, bool Extended);

/// <summary>ลำดับปุ่มที่กดต่อกันแล้วปล่อยย้อนกลับ เช่น "ctrl+c" = [ctrl, c]</summary>
public sealed class Stroke
{
    public IReadOnlyList<OutKey> Keys { get; }

    public Stroke(IReadOnlyList<OutKey> keys) => Keys = keys;

    public static Stroke Single(ushort vk, bool extended = false) => new([new OutKey(vk, extended)]);
}

public readonly record struct HoldMapping(ushort Scan, Stroke Action);

/// <summary>ผลลัพธ์หลัง validate แล้ว — พร้อมใช้กับ hook ทันที</summary>
public sealed class CompiledConfig
{
    /// <summary>null = กด CapsLock เดี่ยวแล้วส่ง tap ทุกครั้ง (พฤติกรรมเดิมของ AHK)</summary>
    public long? TapTimeoutMs { get; init; }

    /// <summary>null = ไม่ส่งอะไรเมื่อแตะ</summary>
    public Stroke? Tap { get; init; }

    public IReadOnlyList<HoldMapping> Hold { get; init; } = [];

    public Stroke? HoldAction(ushort scan)
    {
        foreach (var mapping in Hold)
        {
            if (mapping.Scan == scan)
            {
                return mapping.Action;
            }
        }
        return null;
    }
}

public sealed class ConfigException : Exception
{
    public int? Line { get; }

    public ConfigException(string message, int? line = null) : base(Format(message, line))
    {
        Line = line;
    }

    private static string Format(string message, int? line) =>
        line is null ? $"config.json: {message}" : $"config.json บรรทัด {line}: {message}";
}
