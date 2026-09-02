using EFT;
using EFT.Interactive;
using HarmonyLib;
using LockableDoors.Common;
using LockableDoors.Components;
using LockableDoors.Fika;
using LockableDoors.Helpers;
using LockableDoors.Models;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LockableDoors.Patches
{
    internal class GameEndedPatch : ModulePatch
    {
        private static Type _targetClassType;

        protected override MethodBase GetTargetMethod()
        {
            _targetClassType = PatchConstants.EftTypes.Single(targetClass =>
                !targetClass.IsInterface &&
                !targetClass.IsNested &&
                targetClass.GetMethods().Any(method => method.Name == "LocalRaidEnded") &&
                targetClass.GetMethods().Any(method => method.Name == "LocalRaidStarted")
            );

            return AccessTools.Method(_targetClassType.GetTypeInfo(), "LocalRaidEnded");
        }

        [PatchPostfix]
        static void Postfix(LocalRaidSettings settings, object results, ref object[] lostInsuredItems, object transferItems)
        {
            if (!FikaBridge.IAmHost()) return;

            if (Settings.UnlockAllDoorsOnRaidEnd.Value)
            {
                ConsoleCommands.UnlockAllDoors();
            }

            List<string> lockedDoorIds = LDSession.Instance.DoorsWithLocks.Where(door => door.DoorState == EDoorState.Locked)
                                                                              .Select(door => door.Id)
                                                                              .ToList();

            ServerDataPack pack = new ServerDataPack(FikaBridge.GetRaidId(), LDSession.Instance.GameWorld.LocationId.ToLower(), lockedDoorIds);
            Utils.ServerRoute(Plugin.DataToServerURL, pack);
        }
    }
}
