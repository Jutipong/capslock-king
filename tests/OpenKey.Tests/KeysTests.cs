using OpenKey.Core;

namespace OpenKey.Tests;

public class KeysTests
{
    [Theory]
    [InlineData("ESC", "escape")]
    [InlineData("CapsLock", "caps_lock")]
    [InlineData(" pgdn ", "page_down")]
    [InlineData("Return", "enter")]
    public void Aliases_map_to_canonical_names(string input, string expected) =>
        Assert.Equal(expected, KeyTable.Normalize(input));

    [Fact]
    public void Lookup_known_key_returns_scan_and_vk()
    {
        var h = KeyTable.Lookup("h");
        Assert.NotNull(h);
        Assert.Equal(0x23, h!.Scan);
        Assert.Equal(0x48, h.Vk);
    }

    [Theory]
    [InlineData("f44")]
    [InlineData("")]
    [InlineData("ไม่ใช่ปุ่ม")]
    public void Lookup_unknown_key_returns_null(string name) =>
        Assert.Null(KeyTable.Lookup(name));

    [Fact]
    public void Table_has_no_duplicate_names()
    {
        var names = KeyTable.Table.Select(k => k.Name).ToArray();
        Assert.Equal(names.Length, names.Distinct().Count());
    }

    [Fact]
    public void Scan_lookup_finds_physical_keys()
    {
        Assert.Equal("h", KeyTable.ByScanCode(0x23)!.Name);
        Assert.Equal("delete", KeyTable.ByScanCode(0xE053)!.Name);
        Assert.Null(KeyTable.ByScanCode(0xFFFF));
    }

    [Fact]
    public void Extended_keys_are_flagged()
    {
        Assert.True(KeyTable.Lookup("left")!.Extended);
        Assert.False(KeyTable.Lookup("h")!.Extended);
    }

    [Theory]
    [InlineData("ctrl", ModMask.Ctrl)]
    [InlineData("SHIFT", ModMask.Shift)]
    [InlineData("alt", ModMask.Alt)]
    [InlineData("win", ModMask.Win)]
    public void Modifier_mask_mapping(string name, ModMask expected) =>
        Assert.Equal(expected, KeyTable.ModifierMask(name));

    [Fact]
    public void Non_modifier_has_no_mask() => Assert.Null(KeyTable.ModifierMask("h"));
}
