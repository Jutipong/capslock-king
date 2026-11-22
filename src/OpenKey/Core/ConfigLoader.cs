using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenKey.Core;

/// <summary>binding หนึ่งแถว (เก็บข้อความดิบไว้ให้ UI แก้ได้)</summary>
public sealed class BindingDocument
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;

    public BindingDocument() { }

    public BindingDocument(string from, string to)
    {
        From = from;
        To = to;
    }
}

/// <summary>trigger หนึ่งตัว = คีย์ที่กดค้างแล้วเปลี่ยนความหมายของคีย์อื่น</summary>
public sealed class TriggerDocument
{
    public string Key { get; set; } = string.Empty;
    /// <summary>action เมื่อแตะเดี่ยว (ว่าง = ไม่ส่งอะไร)</summary>
    public string Tap { get; set; } = string.Empty;
    public List<BindingDocument> Map { get; } = [];
}

/// <summary>แบบฟอร์มที่แก้ไขได้ — ใช้ทั้งใน UI และตัวอ่าน/เขียน JSON</summary>
public sealed class ConfigDocument
{
    public long TapTimeoutMs { get; set; }

    public List<TriggerDocument> Triggers { get; } = [];

    /// <summary>map ตรง ๆ ที่ไม่ต้องมี trigger (เช่น alt → win)</summary>
    public List<BindingDocument> Bindings { get; } = [];

    public TriggerDocument? FindTrigger(string key) =>
        Triggers.FirstOrDefault(t =>
            string.Equals(KeyTable.Normalize(t.Key), KeyTable.Normalize(key), StringComparison.Ordinal));
}

/// <summary>อ่าน/ตรวจสอบ config.json — ไฟล์เดียวคือ source of truth</summary>
public static class ConfigLoader
{
    private const int CurrentVersion = 2;

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
                .GetManifestResourceStream("OpenKey.config.json")
                ?? throw new InvalidOperationException("ไม่พบ config.json ที่ฝังใน exe");
            using var reader = new StreamReader(stream);
            _defaultJson = reader.ReadToEnd();
            return _defaultJson;
        }
    }

    public static ConfigDocument DefaultDocument() => Parse(DefaultJson);

    public static ConfigDocument Load(string path)
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

    // ---------------------------------------------------------------- reading

    public static ConfigDocument Parse(string text)
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
            throw new ConfigException(ex.Message, (int?)(ex.LineNumber + 1) ?? null);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new ConfigException("ไฟล์ config ต้องเป็น JSON object");
            }

            var version = ReadInt(root, "version") ?? 0;
            return version switch
            {
                1 => ParseV1(root, text),
                2 => ParseV2(root, text),
                _ => throw new ConfigException(
                    $"ไม่รองรับ version {version} (รองรับเฉพาะ 1 และ 2)", FindLine(text, "version")),
            };
        }
    }

    private static ConfigDocument ParseV2(JsonElement root, string text)
    {
        var document = new ConfigDocument();

        if (root.TryGetProperty("settings", out var settings) &&
            settings.ValueKind == JsonValueKind.Object)
        {
            document.TapTimeoutMs = ReadInt(settings, "tapTimeoutMs") ?? 0;
        }

        if (root.TryGetProperty("triggers", out var triggers))
        {
            if (triggers.ValueKind != JsonValueKind.Object)
            {
                throw new ConfigException("triggers ต้องเป็น object", FindLine(text, "triggers"));
            }

            foreach (var entry in triggers.EnumerateObject())
            {
                if (entry.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new ConfigException(
                        $"trigger \"{entry.Name}\" ต้องเป็น object", FindLine(text, entry.Name));
                }

                if (KeyTable.Lookup(entry.Name) is null)
                {
                    throw new ConfigException($"ไม่รู้จักคีย์ \"{entry.Name}\"", FindLine(text, entry.Name));
                }

                var trigger = new TriggerDocument { Key = entry.Name };

                if (entry.Value.TryGetProperty("tap", out var tap))
                {
                    trigger.Tap = tap.ValueKind == JsonValueKind.String ? tap.GetString() ?? "" : "";
                }

                if (entry.Value.TryGetProperty("map", out var map))
                {
                    if (map.ValueKind != JsonValueKind.Object)
                    {
                        throw new ConfigException(
                            $"map ของ \"{entry.Name}\" ต้องเป็น object", FindLine(text, entry.Name));
                    }
                    foreach (var binding in map.EnumerateObject())
                    {
                        var to = binding.Value.ValueKind == JsonValueKind.String
                            ? binding.Value.GetString() ?? ""
                            : throw new ConfigException(
                                $"action ของ \"{binding.Name}\" ต้องเป็นข้อความ", FindLine(text, binding.Name));
                        try
                        {
                            ParseFrom(binding.Name);
                        }
                        catch (ConfigException ex)
                        {
                            throw WithLine(ex, text, binding.Name);
                        }
                        trigger.Map.Add(new BindingDocument(binding.Name, to));
                    }
                }

                document.Triggers.Add(trigger);
            }
        }

        if (root.TryGetProperty("bindings", out var bindings))
        {
            if (bindings.ValueKind != JsonValueKind.Array)
            {
                throw new ConfigException("bindings ต้องเป็น array", FindLine(text, "bindings"));
            }

            foreach (var entry in bindings.EnumerateArray())
            {
                var from = ReadString(entry, "from")
                    ?? throw new ConfigException("binding ต้องมี \"from\"", FindLine(text, "from"));
                var to = ReadString(entry, "to")
                    ?? throw new ConfigException($"binding \"{from}\" ต้องมี \"to\"", FindLine(text, from));
                document.Bindings.Add(new BindingDocument(from, to));
            }
        }

        return document;
    }

    /// <summary>แปลงไฟล์ v1 (CapsLock King) ให้เป็นโมเดล v2 อัตโนมัติ</summary>
    private static ConfigDocument ParseV1(JsonElement root, string text)
    {
        var document = new ConfigDocument
        {
            TapTimeoutMs = ReadInt(root, "tapTimeoutMs") ?? 0,
        };

        if (!root.TryGetProperty("remap", out var remap) || remap.ValueKind != JsonValueKind.Object)
        {
            throw new ConfigException("ไม่พบ \"remap\"", FindLine(text, "remap"));
        }

        foreach (var entry in remap.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var trigger = new TriggerDocument { Key = entry.Name };

            if (entry.Value.TryGetProperty("tap", out var tap) && tap.ValueKind == JsonValueKind.String)
            {
                trigger.Tap = tap.GetString() ?? "";
            }

            if (entry.Value.TryGetProperty("hold", out var hold) && hold.ValueKind == JsonValueKind.Object)
            {
                foreach (var binding in hold.EnumerateObject())
                {
                    var to = binding.Value.ValueKind == JsonValueKind.String
                        ? binding.Value.GetString() ?? ""
                        : "";
                    trigger.Map.Add(new BindingDocument(binding.Name, to));
                }
            }

            document.Triggers.Add(trigger);
        }

        return document;
    }

    // ---------------------------------------------------------------- writing

    /// <summary>สร้าง JSON v2 จากเอกสาร (คงลำดับตามที่เห็นบนจอ)</summary>
    public static string ToJson(ConfigDocument document)
    {
        var triggers = new JsonObject();
        foreach (var trigger in document.Triggers)
        {
            var key = trigger.Key.Trim();
            if (key.Length == 0)
            {
                continue;
            }

            var entry = new JsonObject();
            if (trigger.Tap.Trim().Length > 0)
            {
                entry["tap"] = trigger.Tap.Trim();
            }

            var map = new JsonObject();
            foreach (var binding in trigger.Map)
            {
                var from = binding.From.Trim();
                if (from.Length == 0)
                {
                    continue;
                }
                if (map.ContainsKey(from))
                {
                    throw new ConfigException($"คีย์ \"{from}\" ซ้ำใน trigger \"{key}\"");
                }
                map[from] = binding.To.Trim();
            }
            entry["map"] = map;

            if (triggers.ContainsKey(key))
            {
                throw new ConfigException($"trigger \"{key}\" ซ้ำ");
            }
            triggers[key] = entry;
        }

        var bindings = new JsonArray();
        foreach (var binding in document.Bindings)
        {
            var from = binding.From.Trim();
            if (from.Length == 0)
            {
                continue;
            }
            bindings.Add(new JsonObject
            {
                ["from"] = from,
                ["to"] = binding.To.Trim(),
            });
        }

        var root = new JsonObject
        {
            ["version"] = CurrentVersion,
            ["settings"] = new JsonObject { ["tapTimeoutMs"] = document.TapTimeoutMs },
            ["triggers"] = triggers,
            ["bindings"] = bindings,
        };

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    // ---------------------------------------------------------------- compile

    public static CompiledConfig Compile(ConfigDocument document)
    {
        if (document.TapTimeoutMs < 0)
        {
            throw new ConfigException("tapTimeoutMs ต้องเป็นจำนวนเต็มตั้งแต่ 0 ขึ้นไป");
        }

        var triggers = new List<Trigger>();
        var triggerScans = new HashSet<ushort>();

        foreach (var triggerDoc in document.Triggers)
        {
            if (triggerDoc.Key.Trim().Length == 0)
            {
                continue;
            }

            var triggerKey = KeyTable.Lookup(triggerDoc.Key)
                ?? throw new ConfigException($"ไม่รู้จักคีย์ \"{triggerDoc.Key}\"");
            if (KeyTable.IsModifier(triggerKey.Name))
            {
                throw new ConfigException(
                    $"ใช้ \"{triggerDoc.Key}\" เป็น trigger ไม่ได้ (เป็น modifier)");
            }
            if (!triggerScans.Add(triggerKey.Scan))
            {
                throw new ConfigException($"trigger \"{triggerDoc.Key}\" ซ้ำ");
            }

            var map = new List<(ushort Scan, ModMask Required, Stroke Action)>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var binding in triggerDoc.Map)
            {
                if (binding.From.Trim().Length == 0)
                {
                    continue;
                }

                var (key, mods) = ParseFrom(binding.From);
                if (KeyTable.IsModifier(key.Name))
                {
                    throw new ConfigException(
                        $"ใช้ \"{binding.From}\" เป็นปุ่มใน trigger ไม่ได้ (เป็น modifier)");
                }
                if (key.Scan == triggerKey.Scan)
                {
                    throw new ConfigException(
                        $"trigger \"{triggerDoc.Key}\" map ตัวเองไม่ได้");
                }
                if (!seen.Add($"{key.Scan}:{mods}"))
                {
                    throw new ConfigException($"คีย์ \"{binding.From}\" ซ้ำใน trigger \"{triggerDoc.Key}\"");
                }

                map.Add((key.Scan, mods, ParseStroke(binding.To)));
            }

            // modifier มากกว่าชนะ (shift+h ต้องชนะ h)
            map.Sort((a, b) =>
                System.Numerics.BitOperations.PopCount((uint)b.Required)
                    .CompareTo(System.Numerics.BitOperations.PopCount((uint)a.Required)));

            triggers.Add(new Trigger
            {
                Scan = triggerKey.Scan,
                Key = triggerKey.Name,
                Tap = triggerDoc.Tap.Trim().Length == 0 ? null : ParseStroke(triggerDoc.Tap),
                Map = map,
            });
        }

        var direct = new List<DirectBinding>();
        var seenDirect = new HashSet<string>(StringComparer.Ordinal);

        foreach (var binding in document.Bindings)
        {
            if (binding.From.Trim().Length == 0)
            {
                continue;
            }

            var (key, mods) = ParseFrom(binding.From);
            if (triggerScans.Contains(key.Scan))
            {
                throw new ConfigException(
                    $"\"{binding.From}\" เป็น trigger อยู่แล้ว — ตั้งที่ trigger แทน");
            }
            if (!seenDirect.Add($"{key.Scan}:{mods}"))
            {
                throw new ConfigException($"\"{binding.From}\" ซ้ำใน bindings");
            }

            direct.Add(new DirectBinding
            {
                From = binding.From.Trim(),
                Scan = key.Scan,
                Required = mods,
                Action = ParseStroke(binding.To),
            });
        }

        direct.Sort((a, b) =>
            System.Numerics.BitOperations.PopCount((uint)b.Required)
                .CompareTo(System.Numerics.BitOperations.PopCount((uint)a.Required)));

        return new CompiledConfig
        {
            TapTimeoutMs = document.TapTimeoutMs > 0 ? document.TapTimeoutMs : null,
            Triggers = triggers,
            Bindings = direct,
        };
    }

    // ---------------------------------------------------------------- parsing helpers

    /// <summary>แปลง "ctrl+shift+left" → stroke ที่จะส่งออก (ปุ่มสุดท้ายต้องไม่ใช่ modifier)</summary>
    public static Stroke ParseStroke(string spec) => ParseCombo(spec).Stroke;

    /// <summary>
    /// แปลง "from" ต้นทาง: "h", "shift+h", "alt", "ctrl+alt"
    /// (ปุ่มสุดท้ายเป็น modifier ได้ เพราะหมายถึงปุ่ม modifier ตัวนั้นจริง ๆ)
    /// </summary>
    public static (KeyDef Key, ModMask Modifiers) ParseFrom(string spec)
    {
        var (key, mods, _) = ParseCombo(spec);
        return (key, mods);
    }

    private static (KeyDef Key, ModMask Modifiers, Stroke Stroke) ParseCombo(string spec)
    {
        var parts = spec.Split('+').Select(p => p.Trim()).ToArray();
        if (parts.Length == 0 || parts.Any(string.IsNullOrEmpty))
        {
            throw new ConfigException($"\"{spec}\" ใช้ไม่ได้: รูปแบบต้องเป็น ปุ่ม หรือ modifier+ปุ่ม");
        }

        var mods = ModMask.None;
        var keys = new List<OutKey>();

        for (var i = 0; i < parts.Length; i++)
        {
            var def = KeyTable.Lookup(parts[i])
                ?? throw new ConfigException($"\"{spec}\" ใช้ไม่ได้: ไม่รู้จัก \"{parts[i]}\"");
            var isLast = i == parts.Length - 1;

            if (isLast)
            {
                // ปุ่มสุดท้ายเป็น modifier ได้ (เช่น alt → win)
                keys.Add(new OutKey(def.Vk, def.Extended));
            }
            else
            {
                var mask = KeyTable.ModifierMask(def.Name)
                    ?? throw new ConfigException(
                        $"\"{spec}\" ใช้ไม่ได้: \"{parts[i]}\" ไม่ใช่ modifier (ctrl/shift/alt/win)");
                mods |= mask;
                keys.Add(new OutKey(def.Vk, def.Extended));
            }
        }

        var key = KeyTable.Lookup(parts[^1])!;
        return (key, mods, new Stroke(keys));
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }
        return value.TryGetInt32(out var result) ? result : null;
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

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

    /// <summary>เติมเลขบรรทัดให้ error ที่ยังไม่มี (ใช้ตอนอ่านจากข้อความ)</summary>
    private static ConfigException WithLine(ConfigException ex, string text, string needle)
    {
        if (ex.Line is not null)
        {
            return ex;
        }
        const string prefix = "config.json: ";
        var message = ex.Message.StartsWith(prefix, StringComparison.Ordinal)
            ? ex.Message[prefix.Length..]
            : ex.Message;
        return new ConfigException(message, FindLine(text, needle));
    }
}
