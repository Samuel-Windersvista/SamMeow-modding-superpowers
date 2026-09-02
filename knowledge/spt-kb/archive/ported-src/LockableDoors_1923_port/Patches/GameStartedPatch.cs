using EFT;
using EFT.Interactive;
using LockableDoors.Components;
using LockableDoors.Helpers;
using LockableDoors.Models;
using SPT.Reflection.Patching;
using System.Reflection;

namespace LockableDoors.Patches
{
    internal class GameStartedPatch : ModulePatch
    {
        // EFT "Interactive" layer is fixed at 16; 4.1 removed the LayerMaskClass.InteractiveLayer constant.
        private const int InteractiveLayer = 16;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        static void PatchPrefix()
        {
            ServerDataPack pack = LockableDoors.Helpers.Utils.ServerRoute<ServerDataPack>(Plugin.DataToClientURL, ServerDataPack.GetRequestPack());
            foreach (string id in pack.LockedDoorIds)
            {
                Door door = LDSession.GetDoor(id);

                // Operatable and layer checks yoinked from Door Randomizer, thanks Drakia!
                // We don't support non-operatable doors
                if (!door.Operatable || !door.enabled) continue;

                // We don't support doors that aren't on the "Interactive" layer
                if (door.gameObject.layer != InteractiveLayer) continue;

                if (door.DoorState == EDoorState.Open)
                {
                    door.DoorState = EDoorState.Shut;
                    door.OnEnable();
                }

                if (door.DoorState != EDoorState.Shut) continue;

                DoorLock doorLock = door.gameObject.AddComponent<DoorLock>();
                doorLock.Lock(false);

                if (Settings.VisualizerEnabled.Value)
                {
                    doorLock.EnabledVisualizer();
                }
            }
        }
    }
}
