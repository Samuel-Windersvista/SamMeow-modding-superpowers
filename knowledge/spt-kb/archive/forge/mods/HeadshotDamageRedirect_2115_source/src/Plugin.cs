using BepInEx;
using BepInEx.Logging;
using EFT;
using EFT.Communications;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace HeadshotDamageRedirect
{
    [BepInPlugin("com.perfk.dmgRedirect", "Headshot Damage Redirection", "1.7.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private void Awake()
        {
            LogSource = Logger;
            LogSource.LogInfo("Headshot Damage Redirect plugin loaded!");

            Settings.Init(Config);
            new ApplyDamageInfoPatch().Enable();
        }
    }

    internal class ApplyDamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(Player).GetMethod("ApplyDamageInfo");

        [PatchPrefix]
        public static void PatchPrefix(ref EBodyPart bodyPartType, ref DamageInfoStruct damageInfo, ref Player __instance)
        {
            if (Settings.ModEnabled.Value == false && Settings.ChestEnabled.Value == false) return;

            // Target is not our player - don't do anything
            if (__instance == null || !__instance.IsYourPlayer || __instance.IsAI) return;

            // Scale damage based on our set damage %
            if (Settings.GlobalDamageReductionPercentage.Value != 100)
                damageInfo.Damage = damageInfo.Damage * Settings.GlobalDamageReductionPercentage.Value / 100;

            // Is the incoming damage coming to the head, and is the current player instance the main player?
            if ((bodyPartType == EBodyPart.Head && Settings.ModEnabled.Value == true) || (bodyPartType == EBodyPart.Chest && Settings.ChestEnabled.Value == true))
            {

                if (Settings.DebugEnabled.Value) Plugin.LogSource.LogInfo($"Redirecting {bodyPartType} damage...");

                float chance = Settings.ChanceToRedirect.Value;
                if (chance < 100 && !RandomBool(chance))
                {
                    if (Settings.DisplayMessage.Value || Settings.DebugEnabled.Value)
                        Plugin.LogSource.LogInfo($"Chance to redirect {bodyPartType} damage failed roll.");
                    return;
                }

                float originalPartDamage = damageInfo.Damage;

                // Did the user set a minimum damage threshold for the mod to activate? if so, check to see if the incoming damage meets that threshold.
                float minDmg = Settings.MinHeadDamageToRedirect.Value;
                if (minDmg > 0 && minDmg > originalPartDamage)
                {
                    if (Settings.DisplayMessage.Value || Settings.DebugEnabled.Value)
                        Plugin.LogSource.LogInfo($"Redirect {bodyPartType} damage less than min damage threshold.");
                    return;
                }

                // Reduce the incoming head damage by a ratio the user has set
                float newPartDamage = CalcDamageToOriginalPart(originalPartDamage, out float damageToRedirect);

                // Log health of each part 
                if (Settings.DebugEnabled.Value)
                    foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
                    {
                        Plugin.LogSource.LogInfo($"Before {bodyPart}  = {__instance.ActiveHealthController.GetBodyPartHealth(bodyPart).Current} hp");
                    }

                _sb.Clear();
                var parts = _partsToRedirect;
                getPartsToRedirect(parts);
                createDamageToEachPart(parts, damageToRedirect, damageInfo, __instance, _sb);

                // If the user set the max damage above 0, clamp the damage to what is set
                float maxDmg = Settings.MaxHeadDamageNumber.Value;
                if (maxDmg > 0)
                {
                    newPartDamage = Mathf.Clamp(newPartDamage, 0, maxDmg);
                }

                LogMessage(bodyPartType.ToString(), originalPartDamage, newPartDamage, damageToRedirect, _sb);

                // Update the damage to our reduced number.
                damageInfo.Damage = newPartDamage;

                // Log health of each part 
                if (Settings.DebugEnabled.Value)
                    foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
                    {
                        if (bodyPart == EBodyPart.Head)
                            Plugin.LogSource.LogInfo($"After {bodyPart}  = {__instance.ActiveHealthController.GetBodyPartHealth(bodyPart).Current - newPartDamage} hp");
                        else if (bodyPart == EBodyPart.Chest && Settings.ChestEnabled.Value == true)
                            Plugin.LogSource.LogInfo($"After {bodyPart}  = {__instance.ActiveHealthController.GetBodyPartHealth(bodyPart).Current - newPartDamage} hp");
                        else
                            Plugin.LogSource.LogInfo($"After {bodyPart}  = {__instance.ActiveHealthController.GetBodyPartHealth(bodyPart).Current} hp");
                    }

                // All Done!
            }
        }

        private static readonly StringBuilder _sb = new StringBuilder();

        private static void createDamageToEachPart(List<EBodyPart> parts, float totalDamage, DamageInfoStruct damageInfo, Player player, StringBuilder stringBuilder)
        {
            var healthController = player.ActiveHealthController;

            float perPart = totalDamage / parts.Count;
            string partsList = "";
            foreach (var part in parts)
            {
                DamageInfoStruct redirectedDamageInfo = CloneDamageInfo(damageInfo, perPart);
                healthController.ApplyDamage(part, perPart, redirectedDamageInfo);
                partsList = partsList + part.ToString() + " ";
            }

            stringBuilder.Append($"{partsList}");
        }

        private static void getPartsToRedirect(List<EBodyPart> parts)
        {
            int target = Settings.BodyPartsCountToRedirectTo.Value;
            parts.Clear();
            if (target == 1)
            {
                parts.Add(SelectRandomBodyPart());
                return;
            }

            int max = AllBodyParts.Count;
            if (target >= max)
            {
                parts.AddRange(AllBodyParts);
                return;
            }

            const int maxIterations = 100;
            for (int i = 0; i < maxIterations; i++)
            {
                EBodyPart random = SelectRandomBodyPart();
                if (!parts.Contains(random))
                {
                    parts.Add(random);
                }
                if (parts.Count == target)
                {
                    break;
                }
            }
        }

        private static readonly List<EBodyPart> _partsToRedirect = new List<EBodyPart>();

        private static void LogMessage(string bodyPart, float originalPartDamage, float newPartDamage, float damageToRedirect, StringBuilder stringBuilder)
        {
            string message = $"Redirected {bodyPart} damage ({originalPartDamage.ToString("0.#")}) = " +
                $"[{bodyPart}] ({newPartDamage.ToString("0.#")}) " +
                $"[{stringBuilder}] ({damageToRedirect.ToString("0.#")})";

            if (Settings.DisplayMessage.Value || Settings.DebugEnabled.Value)
            {
                NotificationManagerClass.DisplayMessageNotification(message,
                ENotificationDurationType.Long,
                ENotificationIconType.Alert);

                Plugin.LogSource.LogInfo(message);
            }
        }

        private static bool RandomBool(float v)
        {
            return UnityEngine.Random.Range(0f, 100f) < v;
        }

        private static DamageInfoStruct CloneDamageInfo(DamageInfoStruct oldDamageInfo, float newDamage)
        {
            return new DamageInfoStruct
            {
                Damage = newDamage,
                DamageType = oldDamageInfo.DamageType,
                PenetrationPower = oldDamageInfo.PenetrationPower,
                HitCollider = oldDamageInfo.HitCollider,
                Direction = oldDamageInfo.Direction,
                HitPoint = oldDamageInfo.HitPoint,
                MasterOrigin = oldDamageInfo.MasterOrigin,
                HitNormal = oldDamageInfo.HitNormal,
                HittedBallisticCollider = oldDamageInfo.HittedBallisticCollider,
                Player = oldDamageInfo.Player,
                Weapon = oldDamageInfo.Weapon,
                FireIndex = oldDamageInfo.FireIndex,
                ArmorDamage = oldDamageInfo.ArmorDamage,
                IsForwardHit = oldDamageInfo.IsForwardHit,
                HeavyBleedingDelta = oldDamageInfo.HeavyBleedingDelta,
                LightBleedingDelta = oldDamageInfo.LightBleedingDelta,
                BleedBlock = oldDamageInfo.BleedBlock,
                DeflectedBy = oldDamageInfo.DeflectedBy,
                BlockedBy = oldDamageInfo.BlockedBy,
                StaminaBurnRate = oldDamageInfo.StaminaBurnRate,
                DidBodyDamage = oldDamageInfo.DidBodyDamage,
                DidArmorDamage = oldDamageInfo.DidArmorDamage,
                SourceId = oldDamageInfo.SourceId,
                OverDamageFrom = oldDamageInfo.OverDamageFrom,
                BodyPartColliderType = oldDamageInfo.BodyPartColliderType,
            };
        }

        private static float CalcDamageToOriginalPart(float damageToHead, out float damageToRedirect)
        {

            // calc amount to redirect
            float ratio = Settings.RedirectPercentage.Value / 100f;
            damageToRedirect = damageToHead * ratio;

            float remainingDamage = damageToHead - damageToRedirect;
            remainingDamage *= Settings.HeadshotMultiplier.Value;
            return remainingDamage;
        }

        private static EBodyPart SelectRandomBodyPart()
        {
            AllBodyParts.Shuffle();

            for (int i = 0; i < AllBodyParts.Count; i++)
            {
                EBodyPart bodyPart = AllBodyParts[i];
                if (Settings.RedirectParts[bodyPart].Value == true)
                {
                    return bodyPart;
                }
            }

            // Nothing selected so use stomach
            return EBodyPart.Stomach;
        }

        public static readonly List<EBodyPart> AllBodyParts = new List<EBodyPart>
        {
            EBodyPart.Chest,
            EBodyPart.Stomach,
            EBodyPart.LeftArm,
            EBodyPart.RightArm,
            EBodyPart.LeftLeg,
            EBodyPart.RightLeg
        };

    }

    // Code used from https://stackoverflow.com/questions/273313/randomize-a-listt
    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static System.Random Local;

        public static System.Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new System.Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    // Code used from https://stackoverflow.com/questions/273313/randomize-a-listt
    internal static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}