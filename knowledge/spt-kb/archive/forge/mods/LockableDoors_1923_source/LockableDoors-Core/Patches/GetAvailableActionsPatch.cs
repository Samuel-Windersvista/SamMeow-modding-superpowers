using EFT;
using EFT.Interactive;
using HarmonyLib;
using LockableDoors.Components;
using SPT.Reflection.Patching;
using System.Reflection;

namespace LockableDoors.Patches
{
    internal class GetAvailableActionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(GetActionsClass), method => method.Name == nameof(GetActionsClass.GetAvailableActions) && method.GetParameters()[0].Name == "owner");
        }

        [PatchPostfix]
        static void PatchPostfix(GamePlayerOwner owner, object interactive, ref ActionsReturnClass __result)
        {
            if (interactive is not Door) return;
            if (interactive is KeycardDoor) return;
            Door door = interactive as Door;

            if (door.gameObject.TryGetComponent(out DoorLock doorLock))
            {
                doorLock.AddLockInteractionsToActionList(__result.Actions);
            }
            else
            {
                if (!LDSession.Instance.WorldDoors.ContainsKey(door.Id)) return;
                DoorLock.AddUninitializedLockInteractionsToActionList(__result.Actions, door);
            }
        }
    }
}
