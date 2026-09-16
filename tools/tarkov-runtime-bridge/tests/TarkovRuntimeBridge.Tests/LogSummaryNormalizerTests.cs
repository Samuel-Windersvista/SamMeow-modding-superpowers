using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class LogSummaryNormalizerTests
{
    // ---------- 合并：易变 token 被剥离后同键 ----------

    [Theory]
    [InlineData("KeyNotFoundException: 5ac66d9b5acfc4001633997a", "KeyNotFoundException: 5aa7e276e5b5b000171d0647")]
    [InlineData("Failed to load asset 12", "Failed to load asset 345")]
    [InlineData("Index 5 out of range of length 9", "Index 12 out of range of length 40")]
    [InlineData("at Foo.Bar() [0x00012] in <abc>:0", "at Foo.Bar() [0x0ff9a] in <abc>:0")]
    public void Volatile_tokens_collapse_to_same_key(string first, string second)
    {
        Assert.Equal(
            LogSummaryNormalizer.Normalize(first),
            LogSummaryNormalizer.Normalize(second));
    }

    [Fact]
    public void Different_guids_collapse_to_same_key()
    {
        Assert.Equal(
            LogSummaryNormalizer.Normalize(
                "Profile 8f3a1b2c-4d5e-6f70-8192-a3b4c5d6e7f8 not found"),
            LogSummaryNormalizer.Normalize(
                "Profile 11111111-2222-3333-4444-555555555555 not found"));
    }

    [Fact]
    public void Whitespace_runs_collapse_to_same_key()
    {
        Assert.Equal(
            LogSummaryNormalizer.Normalize("Failed\tto   load"),
            LogSummaryNormalizer.Normalize("Failed to load"));
    }

    // ---------- 不合并：不同错误保持不同键（保守策略）----------

    [Theory]
    [InlineData("KeyNotFoundException: item", "NullReferenceException: item")]
    [InlineData("Failed to load asset 12", "Failed to unload asset 12")]
    [InlineData("Error at Map 1", "Error at Maps 1")]
    [InlineData("Error x", "error x")]
    public void Different_errors_do_not_merge(string first, string second)
    {
        Assert.NotEqual(
            LogSummaryNormalizer.Normalize(first),
            LogSummaryNormalizer.Normalize(second));
    }

    [Fact]
    public void Non_24_char_hex_tokens_are_not_treated_as_item_ids()
    {
        // 保守：只有恰好 24 位 hex（EFT tpl id 形态）才被替换，25 位不是 id。
        var twentyFour = LogSummaryNormalizer.Normalize("tpl 5ac66d9b5acfc4001633997a");
        var twentyFive = LogSummaryNormalizer.Normalize("tpl 5ac66d9b5acfc4001633997aa");

        Assert.NotEqual(twentyFour, twentyFive);
    }

    [Fact]
    public void Item_id_followed_by_a_word_character_still_collapses()
    {
        // 真实样本（2026-09-16）：24hex id 紧贴字面量 s。右边界必须是「其后不得再有 hex 字符」
        // 而非 \b（\b 在该位置不成立 → 漏配 → 同一错误的不同实例无法合并）。
        var first = LogSummaryNormalizer.Normalize(
            "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1");
        var second = LogSummaryNormalizer.Normalize(
            "Fixed item: 6aa409923c427c1424103c2as undefined StackObjectsCount value, now set to 1");

        Assert.Equal(first, second);
        Assert.Equal(
            "Fixed item: <id>s undefined StackObjectsCount value, now set to <n>",
            first);
    }

    [Theory]
    [InlineData("6aa409923c427c1424103c2ba")]
    [InlineData("6aa409923c427c1424103c2b12345678")]
    public void Longer_hex_runs_are_not_item_ids(string token)
    {
        // 右边界放宽后仍不得吞掉 25 / 32 位 hex 串（其后仍是 hex 字符 → 不匹配）。
        var key = LogSummaryNormalizer.Normalize("Fixed item: " + token + " undefined");

        Assert.Contains(token, key);
    }

    [Fact]
    public void Hex_token_embedded_in_a_longer_word_is_left_alone()
    {
        // 词边界约束：嵌在更长标识符里的 24-hex 片段不是独立 id。
        Assert.Contains(
            "5ac66d9b5acfc4001633997a",
            LogSummaryNormalizer.Normalize("path_5ac66d9b5acfc4001633997a_bak"));
    }

    // ---------- 形状 ----------

    [Fact]
    public void Null_and_empty_normalize_to_empty_string()
    {
        Assert.Equal(string.Empty, LogSummaryNormalizer.Normalize(null));
        Assert.Equal(string.Empty, LogSummaryNormalizer.Normalize(string.Empty));
    }

    [Fact]
    public void Whitespace_only_normalizes_to_empty_string()
    {
        Assert.Equal(string.Empty, LogSummaryNormalizer.Normalize("   \t\r\n "));
    }

    [Fact]
    public void Placeholders_are_stable_and_visible()
    {
        var key = LogSummaryNormalizer.Normalize(
            "Item 5ac66d9b5acfc4001633997a at 0x1a4 count 42 " +
            "id 8f3a1b2c-4d5e-6f70-8192-a3b4c5d6e7f8");

        Assert.Equal("Item <id> at <hex> count <n> id <guid>", key);
    }

    [Fact]
    public void Normalization_is_idempotent()
    {
        var once = LogSummaryNormalizer.Normalize("Failed 12 at 0x1a4");
        var twice = LogSummaryNormalizer.Normalize(once);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Text_without_volatile_tokens_is_unchanged_apart_from_trim()
    {
        Assert.Equal(
            "Object reference not set to an instance of an object",
            LogSummaryNormalizer.Normalize("  Object reference not set to an instance of an object  "));
    }
}
