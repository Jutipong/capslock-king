using CapsLockKing.Core;

namespace CapsLockKing.Tests;

public class EngineTests
{
    private const uint VkCapital = 0x14;
    private const ushort ScanCaps = 0x3A;
    private const ushort ScanH = 0x23;
    private const ushort ScanQ = 0x10;
    private const ushort ScanC = 0x2E;

    private static HookEngine Engine(long? tapTimeoutMs = null)
    {
        var timeout = tapTimeoutMs is null ? string.Empty : $", \"tapTimeoutMs\": {tapTimeoutMs}";
        var json = $$"""
            { "version": 1{{timeout}}, "remap": { "caps_lock": {
                "tap": "escape",
                "hold": { "h": "left", "c": "ctrl+c" }
            } } }
            """;
        return new HookEngine(ConfigLoader.Parse(json));
    }

    [Fact]
    public void Tap_caps_alone_sends_escape()
    {
        var engine = Engine();
        Assert.True(engine.OnEvent(VkCapital, ScanCaps, true, 0).Swallow);
        Assert.Empty(engine.OnEvent(VkCapital, ScanCaps, true, 0).Strokes);

        var up = engine.OnEvent(VkCapital, ScanCaps, false, 50);
        Assert.True(up.Swallow);
        var stroke = Assert.Single(up.Strokes);
        Assert.Equal(0x1B, stroke.Keys[0].Vk);
    }

    [Fact]
    public void Tap_timeout_suppresses_escape_when_held_too_long()
    {
        var engine = Engine(tapTimeoutMs: 200);
        engine.OnEvent(VkCapital, ScanCaps, true, 0);

        var up = engine.OnEvent(VkCapital, ScanCaps, false, 400);
        Assert.True(up.Swallow);
        Assert.Empty(up.Strokes);
    }

    [Fact]
    public void Tap_within_timeout_still_sends_escape()
    {
        var engine = Engine(tapTimeoutMs: 200);
        engine.OnEvent(VkCapital, ScanCaps, true, 0);

        var up = engine.OnEvent(VkCapital, ScanCaps, false, 100);
        Assert.Single(up.Strokes);
    }

    [Fact]
    public void Caps_plus_mapped_key_sends_action_and_swallows_both()
    {
        var engine = Engine();
        engine.OnEvent(VkCapital, ScanCaps, true, 0);

        var down = engine.OnEvent(0, ScanH, true, 0);
        Assert.True(down.Swallow);
        Assert.Equal(0x25, Assert.Single(down.Strokes).Keys[0].Vk);

        var up = engine.OnEvent(0, ScanH, false, 0);
        Assert.True(up.Swallow, "keyup ของคีย์ที่กลืนแล้วต้องถูกกลืนด้วย");
        Assert.Empty(up.Strokes);

        var capsUp = engine.OnEvent(VkCapital, ScanCaps, false, 0);
        Assert.True(capsUp.Swallow);
        Assert.Empty(capsUp.Strokes);
    }

    [Fact]
    public void Caps_plus_unmapped_key_sends_escape_then_passes_through()
    {
        var engine = Engine();
        engine.OnEvent(VkCapital, ScanCaps, true, 0);

        var down = engine.OnEvent(0, ScanQ, true, 0);
        Assert.False(down.Swallow, "คีย์ที่ไม่ได้ map ต้องผ่านไป");
        Assert.Equal(0x1B, Assert.Single(down.Strokes).Keys[0].Vk);

        var up = engine.OnEvent(0, ScanQ, false, 0);
        Assert.False(up.Swallow);
        Assert.Empty(up.Strokes);

        Assert.Empty(engine.OnEvent(VkCapital, ScanCaps, false, 0).Strokes);
    }

    [Fact]
    public void Holding_mapped_key_repeats_action_on_autorepeat()
    {
        var engine = Engine();
        engine.OnEvent(VkCapital, ScanCaps, true, 0);

        Assert.Single(engine.OnEvent(0, ScanH, true, 0).Strokes);
        Assert.Single(engine.OnEvent(0, ScanH, true, 10).Strokes);
    }

    [Fact]
    public void Keyup_after_caps_release_is_still_swallowed()
    {
        var engine = Engine();
        engine.OnEvent(VkCapital, ScanCaps, true, 0);
        engine.OnEvent(0, ScanH, true, 0);
        engine.OnEvent(VkCapital, ScanCaps, false, 0);

        Assert.True(engine.OnEvent(0, ScanH, false, 0).Swallow);
    }

    [Fact]
    public void Keys_without_caps_pass_through_untouched()
    {
        var engine = Engine();
        var result = engine.OnEvent(0, ScanH, true, 0);
        Assert.False(result.Swallow);
        Assert.Empty(result.Strokes);
    }

    [Fact]
    public void Multi_key_action_presses_modifier_first()
    {
        var engine = Engine();
        engine.OnEvent(VkCapital, ScanCaps, true, 0);

        var result = engine.OnEvent(0, ScanC, true, 0);
        var keys = Assert.Single(result.Strokes).Keys;
        Assert.Equal(2, keys.Count);
        Assert.Equal(KeyTable.VkControl, keys[0].Vk);
        Assert.Equal(0x43, keys[1].Vk);
    }

    [Fact]
    public void Caps_without_tap_configured_sends_nothing()
    {
        var engine = new HookEngine(ConfigLoader.Parse(
            """{ "version": 1, "remap": { "caps_lock": { "hold": { "h": "left" } } } }"""));
        engine.OnEvent(VkCapital, ScanCaps, true, 0);
        Assert.Empty(engine.OnEvent(VkCapital, ScanCaps, false, 10).Strokes);
    }
}
