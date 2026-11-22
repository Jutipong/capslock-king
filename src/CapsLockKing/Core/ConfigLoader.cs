using System.IO;
using System.Text.Json;

namespace CapsLockKing.Core;

/// <summary>อ่าน/ตรวจสอบ config.json — ไฟล์เดียวคือ source of truth</summary>
public static class ConfigLoader
{
    public const string TriggerKey = "caps_lock";

    private static string? _defaultJson;

    /// <summary>config ที่ฝังมากับ exe ใช้เมื่อไม่พบไฟล์ หรือไฟล์อ่านไม่ได้</summary>
    public static string DefaultJson
    {
        get
        {
            if (_defaultJson is not null)
            {
                return _defaultJson;
            }
            using var stream = typeof(ConfigLoader).Assembly
                .GetManifestResourceStream("CapsLockKing.config.json")
                ?? throw new InvalidOperationException("ไม่พบ config.json ที่ฝังใน exe");
            using var reader = new StreamReader(stream);
            _defaultJson = reader.ReadToEnd();
            return _defaultJson;
        }
    }

    public static CompiledConfig DefaultConfig() => Parse(DefaultJson);

    public static CompiledConfig Load(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new ConfigException($"อ่านไฟล์ {path} ไม่ได้: {ex.Message}");
        }
        return Parse(text);
    }

    public static CompiledConfig Parse(string text)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
        }
        catch (JsonException ex)
        {
            var line = (int?)(ex.LineNumber + 1) ?? null;
            throw new ConfigException(ex.Message, line);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new ConfigException("ไฟล์ config ต้องเป็น JSON object");
            }

            var version = ReadVersion(root);
            if (version != 1)
            {
                throw new ConfigException($"ไม่รองรับ version {version} (รองรับเฉพาะ 1)");
            }

            long? tapTimeout = null;
            if (root.TryGetProperty("tapTimeoutMs", out var timeoutElement))
            {
                if (timeoutElement.ValueKind != JsonValueKind.Number ||
                    !timeoutElement.TryGetInt64(out var ms))
                {
                    throw new ConfigException("tapTimeoutMs ต้องเป็นตัวเลข", FindLine(text, "tapTimeoutMs"));
                }
                tapTimeout = ms > 0 ? ms : null;
            }

            if (!root.TryGetProperty("remap", out var remap) || remap.ValueKind != JsonValueKind.Object)
            {
                throw new ConfigException("ไม่พบ \"remap\"", FindLine(text, "remap"));
            }

            JsonElement? trigger = null;
            foreach (var entry in remap.EnumerateObject())
            {
                if (KeyTable.Normalize(entry.Name) != TriggerKey)
                {
                    throw new ConfigException(
                        $"ไม่รู้จักปุ่มสั่งงาน \"{entry.Name}\" (รองรับเฉพาะ \"caps_lock\")",
                        FindLine(text, entry.Name));
                }
                if (trigger is not null)
                {
                    throw new ConfigException($"มี \"{entry.Name}\" ซ้ำใน remap", FindLine(text, entry.Name));
                }
                trigger = entry.Value;
            }

            if (trigger is null || trigger.Value.ValueKind != JsonValueKind.Object)
            {
                throw new ConfigException("ไม่พบ \"caps_lock\" ใน remap", FindLine(text, "caps_lock"));
            }

            Stroke? tap = null;
            if (trigger.Value.TryGetProperty("tap", out var tapElement))
            {
                if (tapElement.ValueKind != JsonValueKind.String)
                {
                    throw new ConfigException("tap ต้องเป็นข้อความ", FindLine(text, "tap"));
                }
                var spec = tapElement.GetString()!;
                tap = ParseStrokeOrThrow(spec, text);
            }

            var hold = new List<HoldMapping>();
            var seen = new HashSet<ushort>();
            if (trigger.Value.TryGetProperty("hold", out var holdElement))
            {
                if (holdElement.ValueKind != JsonValueKind.Object)
                {
                    throw new ConfigException("hold ต้องเป็น object", FindLine(text, "hold"));
                }

                foreach (var entry in holdElement.EnumerateObject())
                {
                    var def = KeyTable.Lookup(entry.Name)
                        ?? throw new ConfigException($"ไม่รู้จักคีย์ \"{entry.Name}\"", FindLine(text, entry.Name));

                    if (def.Name == TriggerKey)
                    {
                        throw new ConfigException("ใช้ caps_lock เป็นปุ่ม hold ไม่ได้", FindLine(text, entry.Name));
                    }
                    if (KeyTable.IsModifier(def.Name))
                    {
                        throw new ConfigException(
                            $"ใช้ \"{entry.Name}\" เป็นปุ่ม hold ไม่ได้ (เป็น modifier)",
                            FindLine(text, entry.Name));
                    }
                    if (!seen.Add(def.Scan))
                    {
                        throw new ConfigException($"คีย์ \"{entry.Name}\" ถูก map ซ้ำ", FindLine(text, entry.Name));
                    }

                    if (entry.Value.ValueKind != JsonValueKind.String)
                    {
                        throw new ConfigException(
                            $"action ของ \"{entry.Name}\" ต้องเป็นข้อความ",
                            FindLine(text, entry.Name));
                    }

                    var spec = entry.Value.GetString()!;
                    hold.Add(new HoldMapping(def.Scan, ParseStrokeOrThrow(spec, text, entry.Name)));
                }
            }

            return new CompiledConfig
            {
                TapTimeoutMs = tapTimeout,
                Tap = tap,
                Hold = hold,
            };
        }
    }

    private static Stroke ParseStrokeOrThrow(string spec, string text, string? forKey = null)
    {
        try
        {
            return ParseStroke(spec);
        }
        catch (FormatException ex)
        {
            var prefix = forKey is null ? "tap" : $"action ของคีย์ \"{forKey}\"";
            throw new ConfigException($"{prefix} \"{spec}\" ใช้ไม่ได้: {ex.Message}", FindLine(text, spec));
        }
    }

    /// <summary>แปลง "ctrl+shift+left" → Stroke</summary>
    public static Stroke ParseStroke(string spec)
    {
        var parts = spec.Split('+').Select(p => p.Trim()).ToArray();
        if (parts.Length == 0 || parts.Any(string.IsNullOrEmpty))
        {
            throw new FormatException("รูปแบบต้องเป็น ปุ่ม หรือ modifier+ปุ่ม เช่น \"ctrl+c\"");
        }

        var keys = new List<OutKey>();
        for (var i = 0; i < parts.Length; i++)
        {
            var def = KeyTable.Lookup(parts[i]) ?? throw new FormatException($"ไม่รู้จัก \"{parts[i]}\"");
            var isLast = i == parts.Length - 1;

            if (!isLast && !KeyTable.IsModifier(def.Name))
            {
                throw new FormatException($"\"{parts[i]}\" ไม่ใช่ modifier (ต้องเป็น ctrl/shift/alt/win)");
            }
            if (isLast && KeyTable.IsModifier(def.Name))
            {
                throw new FormatException($"\"{parts[i]}\" เป็น modifier ต้องมีปุ่มจริงต่อท้าย");
            }

            keys.Add(new OutKey(def.Vk, def.Extended));
        }

        return new Stroke(keys);
    }

    private static int ReadVersion(JsonElement root)
    {
        if (!root.TryGetProperty("version", out var element) || element.ValueKind != JsonValueKind.Number)
        {
            return 0;
        }
        return element.TryGetInt32(out var value) ? value : 0;
    }

    /// <summary>หาบรรทัดของข้อความใน JSON (best effort เพื่อบอกตำแหน่งที่ผิด)</summary>
    public static int? FindLine(string text, string? needle)
    {
        if (string.IsNullOrEmpty(needle))
        {
            return null;
        }
        var index = text.IndexOf($"\"{needle}\"", StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }
        var line = 1;
        for (var i = 0; i < index; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }
        return line;
    }
}
