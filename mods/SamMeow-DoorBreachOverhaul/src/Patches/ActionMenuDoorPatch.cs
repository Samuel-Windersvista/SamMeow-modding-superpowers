using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using DoorBreach.Fika;
using EFT.UI;

namespace DoorBreach.Patches
{
    /// <summary>
    /// 门交互菜单补丁：在 Door 的可用交互动作中追加 "Plant Explosive"（放置 C4）。
    /// </summary>
    internal class ActionMenuDoorPatch : ModulePatch
    {
        // 4.1.6：InteractionContextHelper.GetAvailableActions 有约 20 个重载，
        // 必须显式按签名选择 (GamePlayerOwner, Door)，否则 GetMethod(name) 抛 AmbiguousMatchException。
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions),
                new[] { typeof(GamePlayerOwner), typeof(Door) });


        [PatchPostfix]
        public static void Postfix(ref AvailableInteractionState __result, GamePlayerOwner owner, Door door)
        {
            if (__result == null ||
                __result.Actions == null)
                return;

            // 追加动作到现有动作列表
            InteractionAction breachC4 = new InteractionAction
            {
                Name = "Plant Explosive",
                Action = () =>
                {
                    ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player);
                    ExplosiveBreachComponent.RemoveItemFromPlayerInventory(owner.Player);

                    FikaBridge.SendPlantC4PacketPacket(owner.Player, door.Id, DoorBreachPlugin.explosiveTimerInSec);
                },

                // TODO: 生产环境中 IsBreachAngle 存在 null ref
                Disabled = !door.IsBreachAngle(owner.Player.Position) ||
                           !ExplosiveBreachComponent.IsValidDoorState(door) ||
                           !ExplosiveBreachComponent.HasC4Explosives(owner.Player)
            };

            __result.Actions.Add(breachC4);
        }
    }
}
