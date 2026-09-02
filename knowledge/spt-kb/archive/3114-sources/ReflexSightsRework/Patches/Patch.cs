using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SamSWAT.ReflexSightsRework
{
    public class Patch : ModulePatch
    {
        public static float TotalErgonomics;
        public static float Overweight;
        private static Transform _currentWeapon;
        public static SightSwitch[] Instances;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("set_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, bool value)
        {
            if (__instance == null) return;

            if (_currentWeapon != __instance.WeaponRoot || Instances.Any(x => x.gameObject.activeSelf == false))
            {
                _currentWeapon = __instance.WeaponRoot;
                _currentWeapon.gameObject.AddComponent<SightSwitch>();
                Instances = _currentWeapon.GetComponentsInChildren<SightSwitch>();
            }

            if (Instances.Length == 0) return;

            var player = Singleton<GameWorld>.Instance.MainPlayer;

            TotalErgonomics = __instance.TotalErgonomics;
            Overweight = player.ProceduralWeaponAnimation.Overweight;

            foreach (var instance in Instances)
            {
                instance.enabled = value;
            }
        }
    }
}