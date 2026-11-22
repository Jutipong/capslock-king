using OpenKey.Core;

namespace OpenKey.Tests;

public class ConfigTests
{
    [Fact]
    public void Shipped_default_config_parses()
    {
        var document = ConfigLoader.DefaultDocument();
        Assert.Single(document.Triggers);
        Assert.Equal("caps_lock", document.Triggers[0].Key);
        Assert.Equal("escape", document.Triggers[0].Tap);
        Assert.Equal(23, document.Triggers[0].Map.Count);
        Assert.Empty(document.Bindings);

        var config = ConfigLoader.Compile(document);
        Assert.Null(config.TapTimeoutMs);
        Assert.Single(config.Triggers);
    }

    [Fact]
    public void V1_config_is_migrated_to_triggers()
    {
        const string v1 = """
            {
              "version": 1,
              "tapTimeoutMs": 150,
              "remap": {
                "caps_lock": {
                  "tap": "escape",
                  "hold": { "h": "left", "c": "ctrl+c" }
                }
              }
            }
            """;

        var document = ConfigLoader.Parse(v1);
        Assert.Equal(150, document.TapTimeoutMs);
        var trigger = Assert.Single(document.Triggers);
        Assert.Equal("caps_lock", trigger.Key);
        Assert.Equal("escape", trigger.Tap);
        Assert.Equal(2, trigger.Map.Count);

        var config = ConfigLoader.Compile(document);
        Assert.Equal(150, config.TapTimeoutMs);
        Assert.NotNull(config.FindTrigger(0x3A));
    }

    [Fact]
    public void Multiple_triggers_and_direct_bindings_compile()
    {
        const string text = """
            {
              "version": 2,
              "triggers": {
                "caps_lock": { "tap": "escape", "map": { "h": "left" } },
                "space": { "tap": "space", "map": { "j": "down" } }
              },
              "bindings": [
                { "from": "alt", "to": "win" },
                { "from": "ctrl+alt+p", "to": "print_screen" }
              ]
            }
            """;

        var config = ConfigLoader.Compile(ConfigLoader.Parse(text));
        Assert.Equal(2, config.Triggers.Count);
        Assert.Equal(2, config.Bindings.Count);
        Assert.NotNull(config.FindDirect(0x38, ModMask.Alt));
        Assert.NotNull(config.FindDirect(0x19, ModMask.Ctrl | ModMask.Alt)); // P
    }

    [Fact]
    public void Unknown_key_reports_line_and_name()
    {
        const string text = """
            {
              "version": 2,
              "triggers": {
                "caps_lock": {
                  "map": { "f44": "left" }
                }
              }
            }
            """;

        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Parse(text));
        Assert.Equal(5, ex.Line);
        Assert.Contains("f44", ex.Message);
    }

    [Fact]
    public void Unknown_action_reports_spec()
    {
        const string text = """
            { "version": 2, "triggers": { "caps_lock": { "map": { "h": "ctrll+left" } } } }
            """;

        var document = ConfigLoader.Parse(text);
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Compile(document));
        Assert.Contains("ctrll", ex.Message);
    }

    [Fact]
    public void Duplicate_map_key_is_rejected()
    {
        const string text = """
            { "version": 2, "triggers": { "caps_lock": { "map": { "h": "left", "H": "right" } } } }
            """;

        var document = ConfigLoader.Parse(text);
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Compile(document));
        Assert.Contains("ซ้ำ", ex.Message);
    }

    [Fact]
    public void Modifier_cannot_be_trigger()
    {
        const string text = """{ "version": 2, "triggers": { "alt": { "map": { "h": "left" } } } }""";
        var document = ConfigLoader.Parse(text);
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Compile(document));
        Assert.Contains("modifier", ex.Message);
    }

    [Fact]
    public void Binding_on_trigger_key_is_rejected()
    {
        const string text = """
            { "version": 2, "triggers": { "caps_lock": {} }, "bindings": [ { "from": "caps_lock", "to": "escape" } ] }
            """;
        var document = ConfigLoader.Parse(text);
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Compile(document));
        Assert.Contains("trigger", ex.Message);
    }

    [Theory]
    [InlineData("ctrl+")]
    [InlineData("")]
    [InlineData("+")]
    public void Invalid_combos_are_rejected(string spec) =>
        Assert.Throws<ConfigException>(() => ConfigLoader.ParseStroke(spec));

    [Theory]
    [InlineData("ctrl+c")]
    [InlineData("win+ctrl+left")]
    [InlineData("escape")]
    [InlineData("ctrl")]
    [InlineData("alt")]
    public void Valid_combos_parse(string spec)
    {
        var stroke = ConfigLoader.ParseStroke(spec);
        Assert.NotEmpty(stroke.Keys);
    }

    [Fact]
    public void Json_round_trip_produces_valid_config()
    {
        var document = ConfigLoader.DefaultDocument();
        document.Bindings.Add(new BindingDocument("alt", "win"));

        var json = ConfigLoader.ToJson(document);
        var parsed = ConfigLoader.Parse(json);
        var config = ConfigLoader.Compile(parsed);

        Assert.Single(config.Triggers);
        Assert.Single(config.Bindings);
        Assert.Contains("\"version\": 2", json);
    }

    [Fact]
    public void Duplicate_trigger_is_rejected_by_writer()
    {
        var document = new ConfigDocument();
        document.Triggers.Add(new TriggerDocument { Key = "space", Tap = "space" });
        document.Triggers.Add(new TriggerDocument { Key = "space", Tap = "space" });

        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.ToJson(document));
        Assert.Contains("ซ้ำ", ex.Message);
    }

    [Fact]
    public void Unsupported_version_is_rejected()
    {
        const string text = """{ "version": 9, "triggers": {} }""";
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Parse(text));
        Assert.Contains("version", ex.Message);
    }

    [Fact]
    public void Specific_modifiers_win_over_generic()
    {
        const string text = """
            {
              "version": 2,
              "triggers": {
                "caps_lock": {
                  "map": { "h": "left", "shift+h": "home" }
                }
              }
            }
            """;

        var config = ConfigLoader.Compile(ConfigLoader.Parse(text));
        var trigger = config.FindTrigger(0x3A)!;
        Assert.Equal(0x24, trigger.Find(0x23, ModMask.Shift)!.Keys[0].Vk);  // VK_HOME
        Assert.Equal(0x25, trigger.Find(0x23, ModMask.None)!.Keys[0].Vk);   // VK_LEFT
        Assert.NotEqual(trigger.Find(0x23, ModMask.None), trigger.Find(0x23, ModMask.Shift));
    }
}
