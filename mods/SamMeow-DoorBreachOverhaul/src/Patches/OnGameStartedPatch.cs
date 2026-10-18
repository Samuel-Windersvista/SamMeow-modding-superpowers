using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using DoorOverhaul.Destruction;

namespace DoorBreach.Patches;

/// <summary>
/// 开局补丁：GameWorld.OnGameStarted 后置时铺场（门/容器/后备箱）并挂载 C4 组件。
/// 同点并入 DO 破坏系统装配：清空滞留命中 → 构建可破坏门登记表。
/// </summary>
public class OnGameStartedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    private static void PatchPostfix(GameWorld __instance)
    {
        if (Application.isBatchMode)
            return;

        // 战局启动重读 F12 配置（快照语义：战局中途的修改在下一个战局生效）。
        DoorBreachPlugin.RefreshConfigSnapshot();

        DoorDamageIntake.Clear();
        if (Settings.DestructionEnabled.Value)
            DoorDestructionRegistry.Build(__instance);

        DoorBreachComponent.Enable();
        ExplosiveBreachComponent.Enable();
    }
}
