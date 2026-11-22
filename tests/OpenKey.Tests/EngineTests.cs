using OpenKey.Core;

namespace OpenKey.Tests;

public class EngineTests
{
    private const ushort ScanCaps = 0x3A;
    private const ushort ScanSpace = 0x39;
    private const ushort ScanH = 0x23;
    private const ushort ScanQ = 0x10;
    private const ushort ScanC = 0x2E;
    private const ushort ScanAlt = 0x38;
    private const ushort ScanP = 0x19;
    private const uint VkCapital = 0x14;

    private const string CapsTrigger = """
        "caps_lock": { "tap": "escape", "map": { "h": "left", "c": "ctrl+c" } }
        """;

    private static HookEngine Engine(
        string triggers,
        long tapTimeoutMs = 0,
        string bindings = "[]") =>
        new(ConfigLoader.Compile(ConfigLoader.Parse($$"""
            {
              "version": 2,
              "settings": { "tapTimeoutMs": {{tapTimeoutMs}} },
              "triggers": { {{triggers}} },
              "bindings": {{bindings}}
            }
            """)));

    private static HookEngine CapsEngine(long tapTimeoutMs = 0) =>
        Engine(CapsTrigger, tapTimeoutMs);

    [Fact]
    public void Tap_alone_sends_tap_action()
    {
        var engine = CapsEngine();
        var down = engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);
        Assert.True(down.Swallow);
        Assert.Empty(down.Strokes);

        var up = engine.OnEvent(VkCapital, ScanCaps, false, ModMask.None, 50);
        Assert.True(up.Swallow);
        Assert.Equal(0x1B, Assert.Single(up.Strokes).Keys[0].Vk);
    }

    [Fact]
    public void Tap_timeout_suppresses_tap_when_held_too_long()
    {
        var engine = CapsEngine(tapTimeoutMs: 200);
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);

        var up = engine.OnEvent(VkCapital, ScanCaps, false, ModMask.None, 400);
        Assert.Empty(up.Strokes);
    }

    [Fact]
    public void Tap_within_timeout_still_sends_tap()
    {
        var engine = CapsEngine(tapTimeoutMs: 200);
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);
        Assert.Single(engine.OnEvent(VkCapital, ScanCaps, false, ModMask.None, 100).Strokes);
    }

    [Fact]
    public void Trigger_plus_mapped_key_sends_action_and_swallows_both()
    {
        var engine = CapsEngine();
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);

        var down = engine.OnEvent(0, ScanH, true, ModMask.None, 0);
        Assert.True(down.Swallow);
        Assert.Equal(0x25, Assert.Single(down.Strokes).Keys[0].Vk); // VK_LEFT

        var up = engine.OnEvent(0, ScanH, false, ModMask.None, 0);
        Assert.True(up.Swallow);
        Assert.Empty(up.Strokes);

        var capsUp = engine.OnEvent(VkCapital, ScanCaps, false, ModMask.None, 0);
        Assert.True(capsUp.Swallow);
        Assert.Empty(capsUp.Strokes);
    }

    [Fact]
    public void Trigger_plus_unmapped_key_sends_tap_then_passes_through()
    {
        var engine = CapsEngine();
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);

        var down = engine.OnEvent(0, ScanQ, true, ModMask.None, 0);
        Assert.False(down.Swallow);
        Assert.Equal(0x1B, Assert.Single(down.Strokes).Keys[0].Vk);

        var up = engine.OnEvent(0, ScanQ, false, ModMask.None, 0);
        Assert.False(up.Swallow);
        Assert.Empty(up.Strokes);

        Assert.Empty(engine.OnEvent(VkCapital, ScanCaps, false, ModMask.None, 0).Strokes);
    }

    [Fact]
    public void Holding_mapped_key_repeats_action()
    {
        var engine = CapsEngine();
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);
        Assert.Single(engine.OnEvent(0, ScanH, true, ModMask.None, 0).Strokes);
        Assert.Single(engine.OnEvent(0, ScanH, true, ModMask.None, 10).Strokes);
    }

    [Fact]
    public void Keyup_after_trigger_release_is_still_swallowed()
    {
        var engine = CapsEngine();
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);
        engine.OnEvent(0, ScanH, true, ModMask.None, 0);
        engine.OnEvent(VkCapital, ScanCaps, false, ModMask.None, 0);

        Assert.True(engine.OnEvent(0, ScanH, false, ModMask.None, 0).Swallow);
    }

    [Fact]
    public void Keys_without_trigger_pass_through()
    {
        var engine = CapsEngine();
        var result = engine.OnEvent(0, ScanH, true, ModMask.None, 0);
        Assert.False(result.Swallow);
        Assert.Empty(result.Strokes);
    }

    [Fact]
    public void Multi_key_action_presses_modifier_first()
    {
        var engine = CapsEngine();
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);

        var keys = Assert.Single(engine.OnEvent(0, ScanC, true, ModMask.None, 0).Strokes).Keys;
        Assert.Equal(2, keys.Count);
        Assert.Equal(KeyTable.VkControl, keys[0].Vk);
        Assert.Equal(0x43, keys[1].Vk);
    }

    [Fact]
    public void Trigger_without_tap_sends_nothing()
    {
        var engine = Engine("""
            "caps_lock": { "map": { "h": "left" } }
            """);
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);
        Assert.Empty(engine.OnEvent(VkCapital, ScanCaps, false, ModMask.None, 10).Strokes);
    }

    [Fact]
    public void Any_key_can_be_a_trigger_and_nests()
    {
        var engine = Engine("""
            "caps_lock": { "tap": "escape", "map": { "h": "left" } },
            "space": { "tap": "space", "map": { "h": "home" } }
            """);

        // caps + space + h → layer ของ space ชนะ
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);
        var spaceDown = engine.OnEvent(0, ScanSpace, true, ModMask.None, 0);
        Assert.True(spaceDown.Swallow);
        Assert.Empty(spaceDown.Strokes);

        Assert.Equal(0x24, Assert.Single(engine.OnEvent(0, ScanH, true, ModMask.None, 0).Strokes).Keys[0].Vk);
        Assert.Empty(engine.OnEvent(0, ScanH, false, ModMask.None, 0).Strokes);

        // ปล่อย space ที่ถูกใช้ไปแล้ว → ไม่มี tap
        Assert.Empty(engine.OnEvent(0, ScanSpace, false, ModMask.None, 0).Strokes);

        // กลับไป layer caps
        Assert.Equal(0x25, Assert.Single(engine.OnEvent(0, ScanH, true, ModMask.None, 0).Strokes).Keys[0].Vk);
    }

    [Fact]
    public void Unused_nested_trigger_sends_its_tap()
    {
        var engine = Engine("""
            "caps_lock": { "tap": "escape", "map": { "h": "left" } },
            "space": { "tap": "space", "map": { "h": "home" } }
            """);

        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);
        engine.OnEvent(0, ScanSpace, true, ModMask.None, 0);
        var spaceUp = engine.OnEvent(0, ScanSpace, false, ModMask.None, 30);
        Assert.Equal(0x20, Assert.Single(spaceUp.Strokes).Keys[0].Vk);
    }

    [Fact]
    public void Direct_binding_remaps_a_key()
    {
        var engine = Engine("", bindings: """[ { "from": "alt", "to": "win" } ]""");

        var down = engine.OnEvent(0x12, ScanAlt, true, ModMask.None, 0);
        Assert.True(down.Swallow);
        Assert.Equal(KeyTable.VkLWin, Assert.Single(down.Strokes).Keys[0].Vk);

        Assert.True(engine.OnEvent(0x12, ScanAlt, false, ModMask.None, 0).Swallow);
    }

    [Fact]
    public void Direct_binding_respects_required_modifiers()
    {
        var engine = Engine("", bindings: """[ { "from": "ctrl+alt+p", "to": "print_screen" } ]""");

        var wrong = engine.OnEvent(0x50, ScanP, true, ModMask.Alt, 0);
        Assert.False(wrong.Swallow);

        var right = engine.OnEvent(0x50, ScanP, true, ModMask.Ctrl | ModMask.Alt, 0);
        Assert.True(right.Swallow);
        Assert.Equal(0x2C, Assert.Single(right.Strokes).Keys[0].Vk);
    }

    [Fact]
    public void Generic_mapping_still_matches_when_extra_modifiers_are_held()
    {
        var engine = CapsEngine();
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);
        var result = engine.OnEvent(0, ScanH, true, ModMask.Shift, 0);
        Assert.Equal(0x25, Assert.Single(result.Strokes).Keys[0].Vk);
    }

    [Fact]
    public void Specific_mapping_wins_over_generic()
    {
        var engine = Engine("""
            "caps_lock": { "map": { "h": "left", "shift+h": "home" } }
            """);
        engine.OnEvent(VkCapital, ScanCaps, true, ModMask.None, 0);

        Assert.Equal(0x24, Assert.Single(engine.OnEvent(0, ScanH, true, ModMask.Shift, 0).Strokes).Keys[0].Vk);
        Assert.Equal(0x25, Assert.Single(engine.OnEvent(0, ScanH, true, ModMask.None, 0).Strokes).Keys[0].Vk);
    }
}
