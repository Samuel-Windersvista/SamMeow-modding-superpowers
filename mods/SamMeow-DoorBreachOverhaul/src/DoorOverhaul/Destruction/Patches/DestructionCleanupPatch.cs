using System.Reflection;
using DoorOverhaul.Doors;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoorOverhaul.Destruction.Patches
{
    /// <summary>
    /// 战局结束清理：GameWorld.OnDestroy 后置清空破坏侧与交互侧全部运行态
    /// （视觉/碎片/命中/登记表；Peek/推门/踢枪/开门覆盖）。
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
            PeekDriver.Cancel();
            DoorPusher.Clear();
            DoorOverhaul.Patches.KickWeaponForwardPatch.Clear();
            DoorOpenOverrides.Clear();

            DestructionVisuals.Clear();
            DestructionFragments.Clear();
            DoorDamageIntake.Clear();
            DoorDestructionRegistry.Clear();
        }
    }
}
