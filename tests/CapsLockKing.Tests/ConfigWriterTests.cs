using CapsLockKing.Core;

namespace CapsLockKing.Tests;

public class ConfigWriterTests
{
    [Fact]
    public void Gui_output_is_valid_config()
    {
        var json = ConfigWriter.Build("escape", 150, [("h", "left"), ("c", "ctrl+c")]);
        var config = ConfigLoader.Parse(json);

        Assert.Equal(150, config.TapTimeoutMs);
        Assert.Equal(2, config.Hold.Count);
        Assert.Equal(KeyTable.Lookup("h")!.Scan, config.Hold[0].Scan);
        Assert.Equal(KeyTable.Lookup("c")!.Scan, config.Hold[1].Scan);
    }

    [Fact]
    public void Empty_rows_are_skipped_and_empty_tap_means_none()
    {
        var json = ConfigWriter.Build("", 0, [("   ", "left"), ("h", "left")]);
        var config = ConfigLoader.Parse(json);

        Assert.Null(config.Tap);
        Assert.Single(config.Hold);
    }

    [Fact]
    public void Duplicate_keys_are_rejected()
    {
        var ex = Assert.Throws<ConfigException>(() =>
            ConfigWriter.Build("", 0, [("h", "left"), (" h ", "right")]));
        Assert.Contains("ซ้ำ", ex.Message);
    }

    [Fact]
    public void Bad_action_is_rejected_by_validation()
    {
        var json = ConfigWriter.Build("", 0, [("h", "ctrll+x")]);
        Assert.Throws<ConfigException>(() => ConfigLoader.Parse(json));
    }

    [Fact]
    public void Negative_timeout_is_rejected()
    {
        Assert.Throws<ConfigException>(() => ConfigWriter.Build("escape", -1, []));
    }

    [Fact]
    public void Default_config_round_trips_through_writer()
    {
        var defaults = ConfigLoader.DefaultConfig();
        var rows = defaults.Hold
            .Select(mapping => (KeyTable.ByScanCode(mapping.Scan)!.Name, "left"))
            .ToArray();

        var json = ConfigWriter.Build("escape", 0, rows);
        var parsed = ConfigLoader.Parse(json);
        Assert.Equal(defaults.Hold.Count, parsed.Hold.Count);
    }
}
