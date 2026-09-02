using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace hideoutcat.bepinex
{
    internal class PatchAvailableHideoutActions : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // 3.11: GetActionsClass.GetAvailableHideoutActions(HideoutPlayerOwner, GInterface150)
            // 4.1:  GetActionsClass was removed; the method moved to EFT.InteractionContextHelper and
            //       now takes EFT.IInteractive, returning EFT.UI.AvailableInteractionState
            return AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableHideoutActions));
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref AvailableInteractionState __result, HideoutPlayerOwner owner, IInteractive interactive)
        {
            HideoutCat cat = interactive as HideoutCat;
            if (cat == null)
                return true;

            __result = GetCatAvailableActions(cat, owner);

            return false;
        }

        public static AvailableInteractionState GetCatAvailableActions(HideoutCat cat, HideoutPlayerOwner owner)
        {
            AvailableInteractionState actionsReturnClass = new AvailableInteractionState
            {
                Actions = new List<InteractionAction>()
            };

            actionsReturnClass.Actions.Add(new InteractionAction
            {
                Name = "Pet",
                Action = new Action(delegate
                {
                    cat.Pet();
                    owner.Player.SetInteractInHands(EInteraction.ContainerOpenDefault);
                    owner.InteractionsChangedHandler();
                }),
                Disabled = !cat.IsPettable()
            });

            actionsReturnClass.Actions.Add(new InteractionAction
            {
                Name = "Wake up",
                Action = new Action(delegate
                {
                    cat.WakeUp();
                    owner.InteractionsChangedHandler();
                }),
                Disabled = !cat.IsSleeping()
            });

            return actionsReturnClass;
        }
    }
}
