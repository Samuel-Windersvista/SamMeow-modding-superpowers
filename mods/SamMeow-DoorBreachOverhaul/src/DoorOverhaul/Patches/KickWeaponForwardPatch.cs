using System.Reflection;
using DoorBreach;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DoorOverhaul.Patches
{
    internal class KickWeaponForwardPatch : ModulePatch
    {
        private const float BlendInSeconds = 0.12f;
        private const float BlendOutSeconds = 0.2f;

        private static Player _player;
        private static bool _recorded;
        private static Vector3 _positionOffset;
        private static Quaternion _rotationOffset;
        private static float _weight;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerBones), nameof(PlayerBones.ShiftWeaponRoot));
        }

        [PatchPostfix]
        private static void Postfix(PlayerBones __instance, EPointOfView pv)
        {
            Player player = __instance.Player;
            Transform root = __instance.Weapon_Root_Anim;
            Transform neck = __instance.Neck;
            if (pv != EPointOfView.FirstPerson || player == null || !player.IsYourPlayer || root == null || neck == null)
            {
                return;
            }

            if (player != _player)
            {
                _player = player;
                _recorded = false;
                _weight = 0f;
            }

            Vector3 look = player.LookDirection;
            if (look.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion view = Quaternion.LookRotation(look, Vector3.up);
            Vector3 origin = neck.position;
            bool kicking = Settings.KickWeaponForward.Value
                && KickWeaponReady.IsKicking(player)
                && player.HandsController is Player.FirearmController;

            _weight = Mathf.MoveTowards(_weight, kicking ? 1f : 0f,
                Time.deltaTime / (kicking ? BlendInSeconds : BlendOutSeconds));

            if (_weight <= 0f)
            {
                if (player.HandsController is Player.FirearmController
                    && !player.ProceduralWeaponAnimation.IsAiming
                    && player.CurrentState != null
                    && player.CurrentState.Name != EPlayerState.Sprint
                    && player.CurrentState.Name != EPlayerState.BreachDoor)
                {
                    Quaternion inverse = Quaternion.Inverse(view);
                    _positionOffset = inverse * (root.position - origin);
                    _rotationOffset = inverse * root.rotation;
                    _recorded = true;
                }

                return;
            }

            if (!_recorded)
            {
                return;
            }

            Vector3 position = origin + view * _positionOffset;
            Quaternion rotation = view * _rotationOffset;
            root.SetPositionAndRotation(
                Vector3.Lerp(root.position, position, _weight),
                Quaternion.Slerp(root.rotation, rotation, _weight));
        }

        public static void Clear()
        {
            _player = null;
            _recorded = false;
            _weight = 0f;
        }
    }
}
