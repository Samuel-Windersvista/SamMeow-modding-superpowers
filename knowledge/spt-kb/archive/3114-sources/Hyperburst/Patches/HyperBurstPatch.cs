using System.Reflection;
using EFT.Animations;
using System;
using SPT.Reflection.Patching;
using static EFT.Player;
using static EFT.Player.FirearmController;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Mass.HyperBurst.Patches
{
    ///<summary>Cache relations of related instances.</summary>
    public static class HbcExtensionCache
    {
        private static readonly ConditionalWeakTable<FirearmController, VariableFireRateControllerBase> _cacheFCtoHBC = new();
        ///<summary>Cache current coresponding variable fire-rate controller of WeaponSoundPlayer</summary>
        public static readonly ConditionalWeakTable<WeaponSoundPlayer, VariableFireRateControllerBase> _cacheWSPtoHBC = new();
        ///<summary>Get current coresponding variable fire-rate controller</summary>
        public static VariableFireRateControllerBase GetOrCreateServiceHBC(FirearmController instance)
        {
            if (instance == null)
            {
                return null;
            }
            if (_cacheFCtoHBC.TryGetValue(instance, out var service))
            {
                return service;
            }
            var newService = instance.ControllerGameObject?.GetComponent<VariableFireRateControllerBase>();
            _cacheFCtoHBC.Add(instance, newService);
            return newService;
        }
    }


    ///<summary>Hyper-burst can reduce recoil too much, need to artifically induce spread.</summary>
    public class ShotVectorPatch : ModulePatch
    {

        ///<summary>GetTargetMethod</summary>
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmController).GetMethod("AdjustShotVectors", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(FirearmController __instance, ref Vector3 direction)
        {
            VariableFireRateControllerBase hbc = HbcExtensionCache.GetOrCreateServiceHBC(__instance);

            if (hbc == null || !hbc.IsControllerEnabled())
            {
                return;
            }

            // Don't apply to first shot
            // This patch occurs earlier than ShootPatch, so ROFShotCount will start at 0
            bool applySpread = hbc.ShotCount >= 1 && hbc.ShouldApplySpreadModifier();

            if (!applySpread)
            {
                return;
            }

            float verticalSpread = hbc.GetSpread() * 0.00005f; // Scale down, value needs to be very small to avoid excessive spread
            verticalSpread = UnityEngine.Random.Range(verticalSpread * 0.45f, verticalSpread);
            float horizontalSpread = verticalSpread * 0.75f;
            horizontalSpread *= UnityEngine.Random.value < 0.5f ? -1f : 1f; // Randomly apply left or right spread

            direction = new Vector3(
                direction.x + horizontalSpread,
                direction.y + verticalSpread,
                direction.z
            );
        }
    }



    ///<summary>Actual firerate timing is handled in this Update() method. Firerate is used elsewhere for sound effects</summary>
    public class FireRatePatch : ModulePatch
    {
        ///<summary>GetTargetMethod</summary>
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2029).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(GClass2029 __instance)
        {
            VariableFireRateControllerBase hbc = HbcExtensionCache.GetOrCreateServiceHBC(__instance.FirearmController_0);

            if (hbc == null || !hbc.IsControllerEnabled())
            {
                return;
            }

            float fireRateMultiplier = hbc.GetFireRateMultiplier();
            // Console.WriteLine($"HyperBurstController: FireRatePatch called. ShotCount={hbc.ShotCount}, fireRateMultiplier={fireRateMultiplier}");

            //To avoid div by 0 error.
            int effectiveFireRate = Mathf.Max(1, (int)(__instance.Weapon_0.FireRate * fireRateMultiplier));

            //Float_5 is used to store the weapon's fire rate for lifetime of FC. When player switches weapon, this values would get reset.
            //So it's set here to allow for a dynamic fire rate.
            __instance.Float_5 = 60f / effectiveFireRate;
        }
    }

    ///<summary>Entry point used to determine the player has fired a shot</summary>
    public class ShootPatch : ModulePatch
    {
        ///<summary>GetTargetMethod</summary>
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ProceduralWeaponAnimation).GetMethod("Shoot", BindingFlags.Instance | BindingFlags.Public);

        }
        ///<summary>Prefix timing is too early for reliable ROF adjustment but is needed to adjust recoil strength</summary>
        [PatchPrefix]
        private static void PatchPrefix(ProceduralWeaponAnimation __instance, ref float str, out FirearmController __state, FirearmController ____firearmController)
        {
            __state = ____firearmController;
            if (__state == null)
            {
                return;
            }
            VariableFireRateControllerBase hbc = HbcExtensionCache.GetOrCreateServiceHBC(__state);
            if (hbc == null || !hbc.IsControllerEnabled())
            {
                return;
            }
            hbc.OnWeaponToFire(__state);
            if (!hbc.ShouldApplyRecoilModifier())
            {
                return;
            }

            str *= hbc.GetRecoilMultiplier();
        }

        [PatchPostfix]
        private static void PatchPostFix(ProceduralWeaponAnimation __instance, ref float str, FirearmController __state)
        {
            // Console.WriteLine($"ShootPatch.PatchPostFix():Recoil:{str}");
            if (__state == null)
            {
                // Console.WriteLine("ShootPatch.PatchPostFix():__state is null");
                return;

            }
            VariableFireRateControllerBase hbc = HbcExtensionCache.GetOrCreateServiceHBC(__state);
            if (hbc == null || !hbc.IsControllerEnabled())
            {
                return;
            }
            hbc.OnWeaponFired();
        }

    }

    ///<summary>Is called when weapon is equipped, used as entry point to determine if the weapon is hyper-burst-able</summary>
    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        ///<summary>GetTargetMethod</summary>
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance, FirearmController ____firearmController)
        {

            FirearmController firearmController = ____firearmController;
            if (firearmController == null)
            {
                return;
            }
            VariableFireRateControllerBase hbc = HbcExtensionCache.GetOrCreateServiceHBC(firearmController);
            if (hbc != null && hbc.IsControllerEnabled())
            {
                hbc.ResetController(firearmController);
                if (hbc.WeaponSoundPlayer == null)
                {
                    return;
                }
                if (!HbcExtensionCache._cacheWSPtoHBC.TryGetValue(hbc.WeaponSoundPlayer, out VariableFireRateControllerBase shbc))
                {
                    HbcExtensionCache._cacheWSPtoHBC.Add(hbc.WeaponSoundPlayer, hbc);
                }
                else
                {
                    if (shbc != hbc)
                    {
                        Console.WriteLine("UpdateWeaponVariablesPatch: WeaponSoundPlayer already has a different HyperBurstController associated with it.");
                    }
                }
            }

        }
    }
    ///<summary>GetTargetMethod</summary>
    public class FireBulletPatch : ModulePatch
    {
        ///<summary>GetTargetMethod</summary>
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponSoundPlayer).GetMethod("FireBullet", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(WeaponSoundPlayer __instance, ref bool multiShot, ref bool ____isFiring, ref float pitchMult)
        {

            if (HbcExtensionCache._cacheWSPtoHBC.TryGetValue(__instance, out VariableFireRateControllerBase hbc))
            {
                if (!hbc.IsControllerEnabled())
                {
                    return;
                }
                if (!hbc.ShouldApplySoundModifier())
                {
                    return;
                }
                if (hbc.PlaySeparateShotSound)
                {
                    multiShot = true;
                }
                if (hbc.ModifyShotSoundPitch)
                {
                    //pitchMult randoms from 0.97 to 1.03 when autofiring. Force setting to 1/FireRateMultiplier
                    pitchMult = hbc.GetFireRateMultiplier();
                }
                if (hbc.OverrideSoundEnqueue)
                {
                    ____isFiring = hbc.OnFireSoundPlayed();
                }
            }
        }
    }

    ///<summary>Updates weapon sound pitch when a variable fire-rate controller controls pitch.</summary>
    public class UpdatePitch : ModulePatch
    {
        ///<summary>Gets the WeaponSoundPlayer.UpdatePitch method patched by this module.</summary>
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponSoundPlayer).GetMethod("UpdatePitch", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(WeaponSoundPlayer __instance, ref GClass890 ____queue, ref float pitch, ref float ____pitch,ref float ____prevPitchMult)
        {
            if (HbcExtensionCache._cacheWSPtoHBC.TryGetValue(__instance, out VariableFireRateControllerBase hbc))
            {
                if (!hbc.IsControllerEnabled())
                {
                    return true;
                }
                if (!hbc.ShouldApplySoundModifier())
                {
                    return true;
                }
                if (hbc.ModifyShotSoundPitch)
                {
                    if (____queue == null)
                    {
                        return false;
                    }
                    pitch = Mathf.Clamp(pitch, 0.3333f, 3f);
                    if (Mathf.Abs(pitch - ____prevPitchMult) > Mathf.Epsilon)
                    {
                        ____pitch = pitch;
                        ____prevPitchMult = pitch;
                        ____queue.SetPitch(____pitch);
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
