using CapsLockKing.Core;

namespace CapsLockKing.Tests;

public class ConfigTests
{
    [Fact]
    public void Shipped_default_config_parses()
    {
        var config = ConfigLoader.DefaultConfig();
        Assert.NotNull(config.Tap);
        Assert.Single(config.Tap!.Keys);
        Assert.Equal(0x1B, config.Tap.Keys[0].Vk);
        Assert.Equal(23, config.Hold.Count);
        Assert.Null(config.TapTimeoutMs);
    }

    [Fact]
    public void Hold_mapping_matches_key_table()
    {
        var config = ConfigLoader.DefaultConfig();

        var h = KeyTable.Lookup("h")!;
        var left = config.HoldAction(h.Scan);
        Assert.NotNull(left);
        Assert.Equal(0x25, left!.Keys[0].Vk);
        Assert.True(left.Keys[0].Extended);

        var c = KeyTable.Lookup("c")!;
        var copy = config.HoldAction(c.Scan)!;
        Assert.Equal(2, copy.Keys.Count);
        Assert.Equal(KeyTable.VkControl, copy.Keys[0].Vk);
        Assert.Equal(0x43, copy.Keys[1].Vk);
    }

    [Fact]
    public void Unknown_key_reports_line_and_name()
    {
        const string text = """
            {
              "version": 1,
              "remap": {
                "caps_lock": {
                  "hold": { "f44": "left" }
                }
              }
            }
            """;

        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Parse(text));
        Assert.Equal(5, ex.Line);
        Assert.Contains("f44", ex.Message);
    }

    [Fact]
    public void Unknown_action_reports_key_name()
    {
        const string text =
            """{ "version": 1, "remap": { "caps_lock": { "hold": { "h": "ctrll+left" } } } }""";

        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Parse(text));
        Assert.Contains("h", ex.Message);
        Assert.Contains("ctrll", ex.Message);
    }

    [Fact]
    public void Duplicate_hold_key_is_rejected()
    {
        const string text =
            """{ "version": 1, "remap": { "caps_lock": { "hold": { "h": "left", "H": "right" } } } }""";

        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Parse(text));
        Assert.Contains("ซ้ำ", ex.Message);
    }

    [Theory]
    [InlineData("ctrl+")]
    [InlineData("ctrl")]
    [InlineData("")]
    [InlineData("+")]
    public void Modifier_without_key_is_rejected(string spec) =>
        Assert.ThrowsAny<Exception>(() => ConfigLoader.ParseStroke(spec));

    [Theory]
    [InlineData("ctrl+c")]
    [InlineData("win+ctrl+left")]
    [InlineData("escape")]
    public void Valid_strokes_parse(string spec)
    {
        var stroke = ConfigLoader.ParseStroke(spec);
        Assert.NotEmpty(stroke.Keys);
    }

    [Fact]
    public void Tap_timeout_zero_means_always_tap()
    {
        const string always = """
            { "version": 1, "tapTimeoutMs": 0, "remap": { "caps_lock": { "tap": "escape" } } }
            """;
        Assert.Null(ConfigLoader.Parse(always).TapTimeoutMs);

        const string limited = """
            { "version": 1, "tapTimeoutMs": 200, "remap": { "caps_lock": { "tap": "escape" } } }
            """;
        Assert.Equal(200, ConfigLoader.Parse(limited).TapTimeoutMs);
    }

    [Fact]
    public void Unsupported_version_is_rejected()
    {
        const string text = """{ "version": 2, "remap": { "caps_lock": {} } }""";
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Parse(text));
        Assert.Contains("version", ex.Message);
    }

    [Fact]
    public void Unknown_trigger_key_is_rejected()
    {
        const string text = """{ "version": 1, "remap": { "alt": {} } }""";
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Parse(text));
        Assert.Contains("caps_lock", ex.Message);
    }
}
