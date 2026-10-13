using System.Reflection;
using InescapableTarkovsSoftcore;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class ModLoadHookTests
{
    [Fact]
    public void Loader_ImplementsIOnLoad()
    {
        Assert.True(typeof(IOnLoad).IsAssignableFrom(typeof(ModLoadHook)));
    }

    [Fact]
    public void Loader_IsInjectable_AtPreloadPlusOne()
    {
        var attribute = typeof(ModLoadHook).GetCustomAttribute<Injectable>();

        Assert.NotNull(attribute);
        Assert.Equal(OnLoadOrder.Preload + 1, attribute!.TypePriority);
    }

    [Fact]
    public void Loader_LogLine_IsFixedItsFormat()
    {
        Assert.Equal("[ITS] Inescapable Tarkov's Softcore v0.1.0 loaded", ModLoadHook.LoadLogLine);
    }
}
