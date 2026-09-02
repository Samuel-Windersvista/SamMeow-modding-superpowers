using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace _DisableScavMode_egboggied.Patches;

public class InsuranceScreenPatch : ModulePatch {
    protected override MethodBase GetTargetMethod() {
        // PORT-NOTE: 4.1 中 MainMenuControllerClass → MainMenuShowOperation（已由其它已移植 mod 验证）。
        // 3.11 的 method_81（保险屏展示 + RaidMode 判定）在 4.1 拆分：展示在 method_51，判定在 CG_method_80
        // （OfflineRaidScreen 的 OnShowNextScreen → CG_method_80：RaidMode != Online 则跳过保险屏直达 method_52 确认屏）。
        // 在判定入口强制 RaidMode=Local 才能复现 3.11 的"屏蔽保险屏"语义，故目标改为 CG_method_80。
        return typeof(MainMenuShowOperation).GetMethod("CG_method_80", BindingFlags.Public | BindingFlags.Instance);
    }

    [PatchPrefix]
    public static bool Prefix(RaidSettings ___raidSettings_0) {
        // PORT-NOTE: 4.1 字段名 RaidSettings_0 → raidSettings_0（公开字段，已核对）
        if (!Plugin.InsuranceScreen.Value) ___raidSettings_0.RaidMode = ERaidMode.Local;

        return true;
    }
}
