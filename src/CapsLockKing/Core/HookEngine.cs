namespace CapsLockKing.Core;

/// <summary>ผลลัพธ์ของหนึ่ง event</summary>
public sealed record FilterResult(bool Swallow, IReadOnlyList<Stroke> Strokes)
{
    public static readonly FilterResult Pass = new(false, []);
    public static readonly FilterResult SwallowOnly = new(true, []);
    public static FilterResult SwallowWith(params Stroke[] strokes) => new(true, strokes);
    public static FilterResult PassWith(params Stroke[] strokes) => new(false, strokes);
}

/// <summary>
/// ตรรกะ tap/hold/passthrough ทั้งหมด (pure — ทดสอบได้โดยไม่ต้องมีคีย์บอร์ดจริง)
///
/// - แตะ CapsLock เดี่ยว           → tap (ค่าเริ่มต้น escape)
/// - CapsLock + คีย์ที่ map ไว้     → ส่ง action และกลืนคีย์นั้นทั้ง down/up
/// - CapsLock + คีย์ที่ไม่ได้ map  → ปล่อย tap แล้วปล่อยคีย์นั้นผ่านไป
/// - modifier ที่ผู้ใช้กดค้าง      → ไม่ต้องทำอะไร ระบบยังเห็นว่ากดค้างอยู่แล้ว
/// </summary>
public sealed class HookEngine
{
    private const uint VkCapital = 0x14;

    private readonly CompiledConfig _config;
    private readonly List<ushort> _swallowed = [];
    private bool _capsDown;
    private bool _capsUsed;
    private long? _capsDownAt;

    public HookEngine(CompiledConfig config) => _config = config;

    public FilterResult OnEvent(uint vk, ushort scan, bool down, long nowMs)
    {
        if (vk == VkCapital)
        {
            return HandleCaps(down, nowMs);
        }

        if (_capsDown)
        {
            var action = _config.HoldAction(scan);
            if (action is not null)
            {
                _capsUsed = true;
                if (!_swallowed.Contains(scan))
                {
                    _swallowed.Add(scan);
                }
                return down ? FilterResult.SwallowWith(action) : FilterResult.SwallowOnly;
            }

            if (down)
            {
                var strokes = _capsUsed || _config.Tap is null ? [] : new[] { _config.Tap };
                _capsUsed = true;
                return new FilterResult(false, strokes);
            }
        }

        // keyup ของคีย์ที่เคยถูกกลืน (เช่น ปล่อย CapsLock ก่อนปล่อย h)
        var index = _swallowed.IndexOf(scan);
        if (index >= 0)
        {
            _swallowed.RemoveAt(index);
            return FilterResult.SwallowOnly;
        }

        return FilterResult.Pass;
    }

    private FilterResult HandleCaps(bool down, long nowMs)
    {
        if (down)
        {
            _capsDown = true;
            _capsUsed = false;
            _capsDownAt = nowMs;
            return FilterResult.SwallowOnly;
        }

        var tapped = !_capsUsed;
        var held = _capsDownAt;
        _capsDown = false;
        _capsUsed = false;
        _capsDownAt = null;

        if (!tapped || _config.Tap is null)
        {
            return FilterResult.SwallowOnly;
        }

        if (_config.TapTimeoutMs is { } limit && held is { } at && nowMs - at > limit)
        {
            return FilterResult.SwallowOnly;
        }

        return FilterResult.SwallowWith(_config.Tap);
    }
}
