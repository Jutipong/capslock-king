using System.Text.Json;
using System.Text.Json.Nodes;

namespace CapsLockKing.Core;

/// <summary>สร้าง JSON จากค่าที่ผู้ใช้กรอกในหน้าตั้งค่า (คงลำดับแถวตามที่เห็นบนจอ)</summary>
public static class ConfigWriter
{
    public static string Build(string tap, int tapTimeoutMs, IEnumerable<(string Key, string Action)> rows)
    {
        if (tapTimeoutMs < 0)
        {
            throw new ConfigException("tapTimeoutMs ต้องเป็นจำนวนเต็มตั้งแต่ 0 ขึ้นไป");
        }

        var hold = new JsonObject();
        foreach (var (key, action) in rows)
        {
            var trimmedKey = key.Trim();
            if (trimmedKey.Length == 0)
            {
                continue;
            }
            if (hold.ContainsKey(trimmedKey))
            {
                throw new ConfigException($"คีย์ \"{trimmedKey}\" ถูก map ซ้ำ");
            }
            hold[trimmedKey] = action.Trim();
        }

        var caps = new JsonObject();
        if (tap.Trim().Length > 0)
        {
            caps["tap"] = tap.Trim();
        }
        caps["hold"] = hold;

        var remap = new JsonObject { [ConfigLoader.TriggerKey] = caps };

        var root = new JsonObject
        {
            ["version"] = 1,
            ["tapTimeoutMs"] = tapTimeoutMs,
            ["remap"] = remap,
        };

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
