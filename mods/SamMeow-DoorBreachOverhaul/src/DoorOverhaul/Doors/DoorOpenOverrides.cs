using System.Collections.Generic;
using EFT.Interactive;
using UnityEngine;

namespace DoorOverhaul.Doors
{
    internal sealed class OpenOverride
    {
        public float Fraction = 1f;
        public float Slowdown = 1f;
        public bool Quiet;
        public float ExpiresAt;
    }

    internal static class DoorOpenOverrides
    {
        private const float ArmTimeout = 3f;
        private const float QuietTimeout = 6f;

        private static readonly Dictionary<WorldInteractiveObject, OpenOverride> Armed = new Dictionary<WorldInteractiveObject, OpenOverride>();
        private static readonly Dictionary<WorldInteractiveObject, float> QuietUntil = new Dictionary<WorldInteractiveObject, float>();

        public static void Arm(WorldInteractiveObject door, float fraction, float slowdown, bool quiet)
        {
            Armed[door] = new OpenOverride
            {
                Fraction = Mathf.Clamp01(fraction),
                Slowdown = Mathf.Clamp(slowdown, 0.2f, 5f),
                Quiet = quiet,
                ExpiresAt = Time.time + ArmTimeout
            };
        }

        public static void Disarm(WorldInteractiveObject door)
        {
            Armed.Remove(door);
        }

        public static bool TryConsume(WorldInteractiveObject door, out OpenOverride result)
        {
            if (Armed.TryGetValue(door, out result))
            {
                Armed.Remove(door);
                if (Time.time <= result.ExpiresAt)
                {
                    if (result.Quiet)
                    {
                        QuietUntil[door] = Time.time + QuietTimeout;
                    }

                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool ConsumeQuiet(WorldInteractiveObject door)
        {
            if (QuietUntil.TryGetValue(door, out float until))
            {
                QuietUntil.Remove(door);
                return Time.time <= until;
            }

            return false;
        }

        public static void Clear()
        {
            Armed.Clear();
            QuietUntil.Clear();
        }
    }
}
