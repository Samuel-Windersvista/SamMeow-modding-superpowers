using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class RaidStateStoreTests
{
    [Fact]
    public void TryGet_is_false_before_any_publish()
    {
        var store = new RaidStateStore();

        Assert.False(store.TryGet(out var state));
        Assert.Null(state);
    }

    [Fact]
    public void Publish_makes_snapshot_visible()
    {
        var store = new RaidStateStore();
        var published = TestStates.PlayerState(sampledAtMs: 42);

        store.Publish(published);

        Assert.True(store.TryGet(out var state));
        Assert.Same(published, state);
    }

    [Fact]
    public void Publish_replaces_previous_snapshot()
    {
        var store = new RaidStateStore();
        var first = TestStates.PlayerState(sampledAtMs: 1);
        var second = TestStates.PlayerState(sampledAtMs: 2);

        store.Publish(first);
        store.Publish(second);

        Assert.True(store.TryGet(out var state));
        Assert.Same(second, state);
    }

    [Fact]
    public void Clear_marks_not_in_raid()
    {
        var store = new RaidStateStore();
        store.Publish(TestStates.PlayerState());

        store.Clear();

        Assert.False(store.TryGet(out var state));
        Assert.Null(state);
    }

    [Fact]
    public void Clear_then_publish_recovers()
    {
        var store = new RaidStateStore();
        store.Publish(TestStates.PlayerState(sampledAtMs: 1));
        store.Clear();

        var recovered = TestStates.PlayerState(sampledAtMs: 2);
        store.Publish(recovered);

        Assert.True(store.TryGet(out var state));
        Assert.Same(recovered, state);
    }

    [Fact]
    public void Concurrent_publish_read_and_clear_do_not_throw()
    {
        var store = new RaidStateStore();
        var states = Enumerable.Range(0, 8)
            .Select(i => TestStates.PlayerState(sampledAtMs: i))
            .ToArray();
        var failures = 0;

        Parallel.For(0, 2000, i =>
        {
            try
            {
                if (i % 3 == 0)
                {
                    store.Clear();
                }
                else
                {
                    store.Publish(states[i % states.Length]);
                }

                store.TryGet(out _);
            }
            catch
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.Equal(0, failures);
    }

    [Fact]
    public void Concurrent_publishers_leave_one_of_the_published_snapshots()
    {
        var store = new RaidStateStore();
        var states = Enumerable.Range(0, 16)
            .Select(i => TestStates.PlayerState(sampledAtMs: i))
            .ToArray();

        Parallel.For(0, states.Length, i => store.Publish(states[i]));

        Assert.True(store.TryGet(out var state));
        Assert.Contains(state, states);
    }
}
