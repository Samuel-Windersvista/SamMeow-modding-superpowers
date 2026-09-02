using BepInEx;
using BepInEx.Logging;
using VContainer;
using VContainer.Unity;
using Logger = SamSWAT.HeliCrash.ArysReloaded.Utils.Logger;

namespace SamSWAT.HeliCrash.ArysReloaded;

public class ApplicationLifetimeScope : LifetimeScope
{
    private BaseUnityPlugin _plugin;
    private ManualLogSource _logSource;

    public void Initialize(BaseUnityPlugin plugin, ManualLogSource logSource)
    {
        _plugin = plugin;
        _logSource = logSource;
    }

    protected override void Awake()
    {
        autoRun = false;

        base.Awake();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_logSource);
        builder.Register<LocalizationService>(Lifetime.Singleton);
        builder.Register<ConfigurationService>(Lifetime.Singleton).WithParameter(_plugin.Config);
        builder.Register<Logger>(Lifetime.Singleton);
        builder.Register<GetLocalePatch>(Lifetime.Singleton);
        builder.Register<RaidLifetimeScopeController>(Lifetime.Singleton);
        builder.Register<RaidLoadScreenPatch>(Lifetime.Singleton);
        builder.Register<HeliCrashLocationService>(Lifetime.Singleton);
        builder.Register<LootContainerFactory>(Lifetime.Singleton);
        builder.RegisterBuildCallback(container =>
        {
            container.Resolve<GetLocalePatch>().Enable();
            container.Resolve<RaidLifetimeScopeController>().Initialize(this);
            container.Resolve<RaidLoadScreenPatch>().Enable();
        });
    }
}
