namespace OpenKey.Core;

/// <summary>ปุ่มเดียวที่พร้อมส่ง (VK + extended flag)</summary>
public readonly record struct OutKey(ushort Vk, bool Extended);

/// <summary>ลำดับปุ่มที่กดต่อกันแล้วปล่อยย้อนกลับ เช่น "ctrl+c" = [ctrl, c]</summary>
public sealed class Stroke
{
    public IReadOnlyList<OutKey> Keys { get; }

    public Stroke(IReadOnlyList<OutKey> keys) => Keys = keys;

    public static Stroke Single(ushort vk, bool extended = false) => new([new OutKey(vk, extended)]);
}

/// <summary>คีย์ที่ใช้เป็น trigger ได้ + tap/hold ภายใน layer ของตัวเอง</summary>
public sealed class Trigger
{
    public required ushort Scan { get; init; }
    public required string Key { get; init; }
    public Stroke? Tap { get; init; }

    /// <summary>เรียงจาก modifier มาก → น้อย (ตัวที่เฉพาะเจาะจงกว่าชนะ)</summary>
    public required IReadOnlyList<(ushort Scan, ModMask Required, Stroke Action)> Map { get; init; }

    public Stroke? Find(ushort scan, ModMask mods)
    {
        foreach (var (mapScan, required, action) in Map)
        {
            if (mapScan == scan && (mods & required) == required)
            {
                return action;
            }
        }
        return null;
    }
}

public sealed class DirectBinding
{
    public required string From { get; init; }
    public required ushort Scan { get; init; }
    public required ModMask Required { get; init; }
    public required Stroke Action { get; init; }
}

/// <summary>ผลลัพธ์หลัง validate แล้ว — พร้อมใช้กับ hook ทันที</summary>
public sealed class CompiledConfig
{
    /// <summary>null = ปล่อย trigger แล้วส่ง tap ทุกครั้ง</summary>
    public long? TapTimeoutMs { get; init; }

    public IReadOnlyList<Trigger> Triggers { get; init; } = [];

    public IReadOnlyList<DirectBinding> Bindings { get; init; } = [];

    public Trigger? FindTrigger(ushort scan)
    {
        foreach (var trigger in Triggers)
        {
            if (trigger.Scan == scan)
            {
                return trigger;
            }
        }
        return null;
    }

    public Stroke? FindDirect(ushort scan, ModMask mods)
    {
        foreach (var binding in Bindings)
        {
            if (binding.Scan == scan && (mods & binding.Required) == binding.Required)
            {
                return binding.Action;
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
