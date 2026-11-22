namespace OpenKey.Core;

/// <summary>ผลลัพธ์ของหนึ่ง event</summary>
public sealed record FilterResult(bool Swallow, IReadOnlyList<Stroke> Strokes)
{
    public static readonly FilterResult Pass = new(false, []);
    public static readonly FilterResult SwallowOnly = new(true, []);
    public static FilterResult SwallowWith(params Stroke[] strokes) => new(true, strokes);
    public static FilterResult PassWith(params Stroke[] strokes) => new(false, strokes);
}

/// <summary>
/// ตรรกะการ map ทั้งหมด (pure — ทดสอบได้โดยไม่ต้องมีคีย์บอร์ดจริง)
///
/// โมเดล: คีย์ใดก็ได้เป็น trigger → กดค้างแล้วเปลี่ยนความหมายของคีย์อื่น
/// - แตะ trigger เดี่ยว            → tap
/// - trigger + ปุ่มที่ map ไว้     → action ของปุ่มนั้น
/// - trigger + ปุ่มที่ไม่ได้ map   → ปล่อย tap แล้วส่งปุ่มนั้นต่อ
/// - ไม่มี trigger กดอยู่          → ดู direct bindings
/// </summary>
public sealed class HookEngine
{
    private sealed class ActiveTrigger
    {
        public required Trigger Trigger { get; init; }
        public bool Used { get; set; }
        public long DownAt { get; init; }
    }

    private readonly CompiledConfig _config;
    private readonly List<ActiveTrigger> _active = [];
    private readonly List<ushort> _swallowed = [];

    public HookEngine(CompiledConfig config) => _config = config;

    public FilterResult OnEvent(uint vk, ushort scan, bool down, ModMask mods, long nowMs)
    {
        var trigger = _config.FindTrigger(scan);

        if (trigger is not null)
        {
            if (down)
            {
                _active.Add(new ActiveTrigger { Trigger = trigger, DownAt = nowMs });
                return FilterResult.SwallowOnly;
            }

            return ReleaseTrigger(trigger, nowMs);
        }

        if (down)
        {
            return HandleKeyDown(scan, mods);
        }

        // keyup ของคีย์ที่เคยถูกกลืน (เช่น ปล่อย trigger ก่อนปล่อย h)
        var index = _swallowed.IndexOf(scan);
        if (index >= 0)
        {
            _swallowed.RemoveAt(index);
            return FilterResult.SwallowOnly;
        }

        return FilterResult.Pass;
    }

    private FilterResult ReleaseTrigger(Trigger trigger, long nowMs)
    {
        var index = _active.FindLastIndex(t => t.Trigger == trigger);
        var active = index >= 0 ? _active[index] : null;
        if (index >= 0)
        {
            _active.RemoveAt(index);
        }

        if (active is null || active.Used || trigger.Tap is null)
        {
            return FilterResult.SwallowOnly;
        }

        if (_config.TapTimeoutMs is { } limit && nowMs - active.DownAt > limit)
        {
            return FilterResult.SwallowOnly;
        }

        return FilterResult.SwallowWith(trigger.Tap);
    }

    private FilterResult HandleKeyDown(ushort scan, ModMask mods)
    {
        // trigger ที่กดค้างอยู่ (ตัวล่าสุดชนะ)
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var action = _active[i].Trigger.Find(scan, mods);
            if (action is null)
            {
                continue;
            }

            _active[i].Used = true;
            if (!_swallowed.Contains(scan))
            {
                _swallowed.Add(scan);
            }
            return FilterResult.SwallowWith(action);
        }

        if (_active.Count > 0)
        {
            // ปุ่มที่ไม่ได้ map: ปล่อย tap ของ trigger ตัวบนสุด (ครั้งเดียว) แล้วให้ปุ่มนี้ผ่าน
            var top = _active[^1];
            if (top.Used || top.Trigger.Tap is null)
            {
                top.Used = true;
                return FilterResult.Pass;
            }

            top.Used = true;
            return FilterResult.PassWith(top.Trigger.Tap);
        }

        var direct = _config.FindDirect(scan, mods);
        if (direct is not null)
        {
            if (!_swallowed.Contains(scan))
            {
                _swallowed.Add(scan);
            }
            return FilterResult.SwallowWith(direct);
        }

        return FilterResult.Pass;
    }
}
