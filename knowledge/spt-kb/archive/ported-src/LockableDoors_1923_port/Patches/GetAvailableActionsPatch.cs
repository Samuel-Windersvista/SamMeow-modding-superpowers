using EFT;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using LockableDoors.Components;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace LockableDoors.Patches
{
    internal class GetAvailableActionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(
                typeof(EFT.InteractionContextHelper),
                nameof(EFT.InteractionContextHelper.GetAvailableActions),
                new Type[] { typeof(GamePlayerOwner), typeof(Door) }
            );
        }

        [PatchPostfix]
        static void PatchPostfix(GamePlayerOwner owner, object door, ref AvailableInteractionState __result)
        {
            if (door is not Door) return;
            if (door is KeycardDoor) return;
            Door doorTyped = door as Door;

            if (doorTyped.gameObject.TryGetComponent(out DoorLock doorLock))
            {
                doorLock.AddLockInteractionsToActionList(__result.Actions);
            }
            else
            {
                if (!LDSession.Instance.WorldDoors.ContainsKey(doorTyped.Id)) return;
                DoorLock.AddUninitializedLockInteractionsToActionList(__result.Actions, doorTyped);
            }
        }
    }
}
