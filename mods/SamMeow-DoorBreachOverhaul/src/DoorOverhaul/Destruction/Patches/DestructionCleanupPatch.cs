using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoorOverhaul.Destruction.Patches
{
    /// <summary>
    /// 战局结束清理：GameWorld.OnDestroy 后置清空破坏侧全部运行态（视觉/碎片/命中/登记表）。
    /// 仅含破坏侧；交互侧清理留工单 04。
    /// </summary>
    internal class DestructionCleanupPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnDestroy));
        }

        [PatchPostfix]
        private static void Postfix()
        {
            DestructionVisuals.Clear();
            DestructionFragments.Clear();
            DoorDamageIntake.Clear();
            DoorDestructionRegistry.Clear();
        }
    }
}
