using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class LogSummaryStoreTests
{
    private static readonly long T0 =
        new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc).Ticks;

    private static void Observe(LogSummaryStore store, long ticks, string level, string text)
    {
        store.Observe(ticks, level, "src", text);
    }

    /// <summary>组键的序数排序——断言集合内容，与专门的组序断言解耦。</summary>
    private static string[] Keys(LogSummaryPage page)
    {
        return page.Groups.Select(group => group.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    /// <summary>把序号转成纯字母 token（归一化会剥离数字，故测试数据必须用字母区分）。</summary>
    private static string Alpha(int index)
    {
        var chars = new char[3];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('a' + index % 26);
            index /= 26;
        }

        return new string(chars);
    }

    // ---------- 聚合与字段 ----------

    [Fact]
    public void Identical_errors_merge_into_one_group_with_full_count()
    {
        var store = new LogSummaryStore();
        Observe(store, T0, "warning", "Failed to load asset 12");
        Observe(store, T0 + 1, "warning", "Failed to load asset 345");
        Observe(store, T0 + 2, "warning", "Failed to load asset 9");

        var page = store.Snapshot(0);

        Assert.Single(page.Groups);
        Assert.Equal(3, page.Groups[0].Count);
        Assert.Equal("Failed to load asset <n>", page.Groups[0].Key);
    }

    [Fact]
    public void Different_errors_stay_in_separate_groups()
    {
        var store = new LogSummaryStore();
        Observe(store, T0, "warning", "Failed to load asset 12");
        Observe(store, T0, "warning", "Failed to unload asset 12");

        var page = store.Snapshot(0);

        Assert.Equal(2, page.Groups.Length);
        Assert.All(page.Groups, group => Assert.Equal(1, group.Count));
    }

    [Fact]
    public void Group_carries_level_source_count_and_first_last_timestamps()
    {
        var store = new LogSummaryStore();
        store.Observe(T0 + 10, "warning", "Unity", "boom 1");
        store.Observe(T0 + 30, "warning", "Unity", "boom 2");

        var group = store.Snapshot(0).Groups[0];

        Assert.Equal("boom <n>", group.Key);
        Assert.Equal("warning", group.Level);
        Assert.Equal("Unity", group.Source);
        Assert.Equal(2, group.Count);
        Assert.Equal(T0 + 10, group.FirstTimestampUtcTicks);
        Assert.Equal(T0 + 30, group.LastTimestampUtcTicks);
    }

    [Fact]
    public void Sample_text_is_the_first_observed_original_text()
    {
        var store = new LogSummaryStore();
        Observe(store, T0, "warning", "Failed to load asset 12");
        Observe(store, T0 + 1, "warning", "Failed to load asset 345");

        Assert.Equal("Failed to load asset 12", store.Snapshot(0).Groups[0].SampleText);
    }

    [Fact]
    public void First_and_last_timestamps_are_min_and_max_not_arrival_order()
    {
        // 多线程采集下到达顺序不保证单调，first/last 必须是极值。
        var store = new LogSummaryStore();
        Observe(store, T0 + 300, "warning", "boom");
        Observe(store, T0 + 100, "warning", "boom");
        Observe(store, T0 + 200, "warning", "boom");

        var group = store.Snapshot(0).Groups[0];

        Assert.Equal(T0 + 100, group.FirstTimestampUtcTicks);
        Assert.Equal(T0 + 300, group.LastTimestampUtcTicks);
    }

    [Fact]
    public void Level_upgrades_to_the_most_severe_observed()
    {
        var store = new LogSummaryStore();
        Observe(store, T0, "warning", "boom");
        Observe(store, T0 + 1, "error", "boom");
        Observe(store, T0 + 2, "warning", "boom");

        Assert.Equal("error", store.Snapshot(0).Groups[0].Level);
    }

    [Fact]
    public void Source_stays_the_first_observed_one()
    {
        var store = new LogSummaryStore();
        store.Observe(T0, "warning", "Unity", "boom");
        store.Observe(T0 + 1, "warning", "Assembly-CSharp", "boom");

        Assert.Equal("Unity", store.Snapshot(0).Groups[0].Source);
    }

    [Fact]
    public void Count_reflects_every_observed_entry_not_just_a_bounded_window()
    {
        // 红线：聚合计数必须覆盖全部采集条目（刷屏 6477 条要显示 6477），
        // 不受 /logs/recent 环形缓冲容量影响。
        var store = new LogSummaryStore();
        for (var i = 0; i < 5_000; i++)
        {
            Observe(store, T0 + i, "error", "Failed to load asset 12");
        }

        var group = store.Snapshot(0).Groups[0];

        Assert.Equal(5_000, group.Count);
        Assert.Equal(T0, group.FirstTimestampUtcTicks);
        Assert.Equal(T0 + 4_999, group.LastTimestampUtcTicks);
    }

    [Fact]
    public void Null_level_source_and_text_do_not_throw()
    {
        var store = new LogSummaryStore();

        store.Observe(T0, null, null, null);

        var group = store.Snapshot(0).Groups[0];
        Assert.Equal(string.Empty, group.Key);
        Assert.Equal(string.Empty, group.Level);
        Assert.Equal(string.Empty, group.Source);
        Assert.Equal(string.Empty, group.SampleText);
    }

    // ---------- 淘汰 / overflowDropped ----------

    [Fact]
    public void Empty_store_returns_no_groups_and_zero_overflow()
    {
        var page = new LogSummaryStore().Snapshot(0);

        Assert.Empty(page.Groups);
        Assert.Equal(0, page.OverflowDropped);
    }

    [Fact]
    public void Default_max_groups_is_500()
    {
        Assert.Equal(500, LogSummaryStore.DefaultMaxGroups);
    }

    [Fact]
    public void Max_groups_is_clamped_to_at_least_one()
    {
        // 行为断言（无 store 级 MaxGroups 属性）：cap 钳到 1 后第二个新键即触发淘汰。
        var store = new LogSummaryStore(maxGroups: 0);
        Observe(store, T0, "warning", "alpha");
        Observe(store, T0 + 1, "warning", "bravo");

        var page = store.Snapshot(0);

        Assert.Single(page.Groups);
        Assert.Equal("bravo", page.Groups[0].Key);
        Assert.Equal(1, page.OverflowDropped);

        var negative = new LogSummaryStore(maxGroups: -7);
        Observe(negative, T0, "warning", "alpha");
        Observe(negative, T0 + 1, "warning", "bravo");

        Assert.Single(negative.Snapshot(0).Groups);
    }

    [Fact]
    public void Overflow_evicts_the_least_recently_updated_group_and_counts_it()
    {
        var store = new LogSummaryStore(maxGroups: 2);
        Observe(store, T0 + 1, "warning", "alpha");
        Observe(store, T0 + 2, "warning", "bravo");
        Observe(store, T0 + 3, "warning", "charlie");

        var page = store.Snapshot(0);

        Assert.Equal(1, page.OverflowDropped);
        Assert.Equal(new[] { "bravo", "charlie" }, Keys(page));

        // 再次溢出：累计计数，且被淘汰的是当时最久未更新的 "charlie"。
        Observe(store, T0 + 4, "warning", "alpha");

        var second = store.Snapshot(0);
        Assert.Equal(2, second.OverflowDropped);
        Assert.Equal(new[] { "alpha", "charlie" }, Keys(second));
    }

    [Fact]
    public void Updating_an_existing_group_refreshes_its_last_timestamp_against_eviction()
    {
        var store = new LogSummaryStore(maxGroups: 2);
        Observe(store, T0 + 1, "warning", "alpha");
        Observe(store, T0 + 2, "warning", "bravo");
        Observe(store, T0 + 3, "warning", "alpha"); // alpha 变为最新
        Observe(store, T0 + 4, "warning", "charlie"); // 淘汰 bravo（最久未更新）

        var page = store.Snapshot(0);

        Assert.Equal(1, page.OverflowDropped);
        Assert.Equal(new[] { "alpha", "charlie" }, Keys(page));
    }

    [Fact]
    public void Overflow_tie_break_is_deterministic_smallest_key()
    {
        var store = new LogSummaryStore(maxGroups: 2);
        Observe(store, T0 + 5, "warning", "bravo");
        Observe(store, T0 + 5, "warning", "charlie");
        Observe(store, T0 + 6, "warning", "alpha"); // bravo/charlie 同为最旧 → 淘汰 key 序最小者

        var page = store.Snapshot(0);

        Assert.Equal(1, page.OverflowDropped);
        Assert.Equal(new[] { "alpha", "charlie" }, Keys(page));
    }

    [Fact]
    public void Overflow_does_not_drop_groups_before_the_cap_is_reached()
    {
        var store = new LogSummaryStore(maxGroups: 3);
        Observe(store, T0, "warning", "alpha");
        Observe(store, T0, "warning", "bravo");
        Observe(store, T0, "warning", "charlie");

        var page = store.Snapshot(0);

        Assert.Equal(0, page.OverflowDropped);
        Assert.Equal(3, page.Groups.Length);
    }

    // ---------- since 过滤 ----------

    [Fact]
    public void Since_returns_only_groups_updated_after_the_cursor()
    {
        var store = new LogSummaryStore();
        Observe(store, T0 + 10, "warning", "alpha");
        Observe(store, T0 + 20, "warning", "bravo");

        var page = store.Snapshot(T0 + 15);

        Assert.Single(page.Groups);
        Assert.Equal("bravo", page.Groups[0].Key);
    }

    [Fact]
    public void Since_is_exclusive_on_last_timestamp()
    {
        var store = new LogSummaryStore();
        Observe(store, T0 + 10, "warning", "alpha");

        Assert.Empty(store.Snapshot(T0 + 10).Groups);
        Assert.Single(store.Snapshot(T0 + 9).Groups);
    }

    [Fact]
    public void Since_uses_last_timestamp_of_updated_groups()
    {
        var store = new LogSummaryStore();
        Observe(store, T0 + 10, "warning", "alpha");
        Observe(store, T0 + 50, "warning", "alpha");

        Assert.Single(store.Snapshot(T0 + 20).Groups);
        Assert.Equal(2, store.Snapshot(T0 + 20).Groups[0].Count);
    }

    [Fact]
    public void Negative_since_returns_everything()
    {
        var store = new LogSummaryStore();
        Observe(store, T0, "warning", "alpha");

        Assert.Single(store.Snapshot(-5).Groups);
    }

    // ---------- 顺序与读隔离 ----------

    [Fact]
    public void Groups_are_ordered_by_count_then_last_timestamp_then_key()
    {
        var store = new LogSummaryStore();
        Observe(store, T0 + 1, "warning", "alpha");
        Observe(store, T0 + 2, "warning", "bravo");
        Observe(store, T0 + 3, "warning", "bravo");
        Observe(store, T0 + 4, "warning", "charlie");
        Observe(store, T0 + 5, "warning", "charlie");
        Observe(store, T0 + 6, "warning", "charlie");

        var keys = store.Snapshot(0).Groups.Select(g => g.Key).ToArray();

        Assert.Equal(new[] { "charlie", "bravo", "alpha" }, keys);
    }

    [Fact]
    public void Snapshot_is_a_copy_and_is_not_mutated_by_later_observes()
    {
        var store = new LogSummaryStore();
        Observe(store, T0, "warning", "alpha");

        var page = store.Snapshot(0);
        Observe(store, T0 + 1, "warning", "alpha");

        Assert.Equal(1, page.Groups[0].Count);
        Assert.Equal(2, store.Snapshot(0).Groups[0].Count);
    }

    // ---------- 并发 ----------

    [Fact]
    public async Task Concurrent_observes_are_lossless()
    {
        const int threadCount = 8;
        const int perThread = 500;
        var store = new LogSummaryStore();

        var tasks = new Task[threadCount];
        for (var t = 0; t < threadCount; t++)
        {
            var id = t;
            tasks[t] = Task.Run(() =>
            {
                for (var i = 0; i < perThread; i++)
                {
                    store.Observe(T0 + i, "warning", "src", "Failed to load asset " + i);
                }
            });
        }

        await Task.WhenAll(tasks);

        var page = store.Snapshot(0);

        Assert.Single(page.Groups);
        Assert.Equal(threadCount * perThread, page.Groups[0].Count);
        Assert.Equal(0, page.OverflowDropped);
    }

    [Fact]
    public async Task Concurrent_observes_of_distinct_texts_respect_the_cap()
    {
        const int maxGroups = 50;
        var store = new LogSummaryStore(maxGroups: maxGroups);

        var tasks = new Task[4];
        for (var t = 0; t < tasks.Length; t++)
        {
            var id = t;
            tasks[t] = Task.Run(() =>
            {
                for (var i = 0; i < 200; i++)
                {
                    // 归一化会剥离数字，故用字母 token 保证 800 个键两两不同。
                    store.Observe(T0 + i, "warning", "src", $"error {(char)('a' + id)} {Alpha(i)}");
                }
            });
        }

        await Task.WhenAll(tasks);

        var page = store.Snapshot(0);

        Assert.Equal(maxGroups, page.Groups.Length);
        Assert.Equal(4 * 200 - maxGroups, page.OverflowDropped);
    }
}
