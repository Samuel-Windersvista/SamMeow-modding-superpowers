using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using EFT.UI;

namespace BackdoorBandit.Patches
{
    internal class ActionMenuKeyCardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(EFT.InteractionContextHelper).GetMethod(nameof(EFT.InteractionContextHelper.GetAvailableActions), new Type[] { typeof(GamePlayerOwner), typeof(KeycardDoor), typeof(bool) });


        // Check if an action is already added. Hopefully door's action takes precedence
        public static bool IsActionAdded(List<InteractionAction> actions, string actionName)
        {
            return actions.Any(action => action.Name.ToLower() == actionName.ToLower());
        }

        [PatchPostfix]
        public static void Postfix(ref AvailableInteractionState __result, GamePlayerOwner owner, Door door)
        {
            if (__result != null && __result.Actions != null && !IsActionAdded(__result.Actions, "Plant Explosive"))
            {
                __result.Actions.Add(new InteractionAction
                {
                    Name = "Plant Explosive",
                    Action = new Action(() =>
                    {
                        BackdoorBandit.ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player);
                    }),
                    Disabled = (!door.IsBreachAngle(owner.Player.Position) || !BackdoorBandit.ExplosiveBreachComponent.IsValidDoorState(door) ||
                                !BackdoorBandit.ExplosiveBreachComponent.hasC4Explosives(owner.Player))
                });
            }
        }
    }
}
