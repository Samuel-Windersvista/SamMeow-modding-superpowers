using System;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using EFT.UI;

namespace BackdoorBandit.Patches
{
    internal class ActionMenuDoorPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod() => typeof(EFT.InteractionContextHelper).GetMethod(nameof(EFT.InteractionContextHelper.GetAvailableActions), new Type[] { typeof(GamePlayerOwner), typeof(Door) });


        [PatchPostfix]
        public static void Postfix(ref AvailableInteractionState __result, GamePlayerOwner owner, Door door)
        {
            // Add an additional action after the original method executes
            if (__result != null && __result.Actions != null)
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