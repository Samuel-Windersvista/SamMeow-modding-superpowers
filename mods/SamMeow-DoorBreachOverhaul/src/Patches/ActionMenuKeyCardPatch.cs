using System.Collections.Generic;
using System.Linq;
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
    /// 钥匙卡门交互菜单补丁：为 KeycardDoor 追加 "Plant Explosive"（与门补丁去重）。
    /// </summary>
    internal class ActionMenuKeyCardPatch : ModulePatch
    {
        // 4.1.6：显式选择 (GamePlayerOwner, KeycardDoor, bool isProxy) 重载。
        // KeycardDoor : Door，故 postfix 的 Door door 参数注入合法。
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions),
                new[] { typeof(GamePlayerOwner), typeof(KeycardDoor), typeof(bool) });


        // 检查动作是否已添加（门补丁的动作优先）
        public static bool IsActionAdded(List<InteractionAction> actions, string actionName)
        {
            return actions.Any(action => action.Name.ToLower() == actionName.ToLower());
        }

        [PatchPostfix]
        public static void Postfix(ref AvailableInteractionState __result, GamePlayerOwner owner, Door door)
        {
            if (__result == null ||
                __result.Actions == null ||
                IsActionAdded(__result.Actions, "Plant Explosive"))
                return;

            // 追加到现有动作之后
            __result.Actions.Add(new InteractionAction
            {
                Name = "Plant Explosive",
                Action = () =>
                {
                    ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player);
                    ExplosiveBreachComponent.RemoveItemFromPlayerInventory(owner.Player);

                    FikaBridge.SendPlantC4PacketPacket(owner.Player, door.Id, DoorBreachPlugin.explosiveTimerInSec);
                },
                Disabled = !door.IsBreachAngle(owner.Player.Position) ||
                           !ExplosiveBreachComponent.IsValidDoorState(door) ||
                           !ExplosiveBreachComponent.HasC4Explosives(owner.Player)
            });
        }
    }
}
