using DoorBreach;
using System;
using EFT;
using EFT.Interactive;
using DoorOverhaul.Destruction.Core;
using UnityEngine;
using NVector2 = System.Numerics.Vector2;

namespace DoorOverhaul.Destruction
{
    internal static class DoorBlast
    {
        private const float StrengthToDamage = 6f;

        private const float RangeShare = 0.6f;

        private static readonly int SightMask = LayerMask.GetMask("HighPolyCollider", "LowPolyCollider", "Terrain");

        public static void Explode(IExplosiveItem item, Vector3 position)
        {
            if (!Settings.DestructionEnabled.Value || DoorDestructionRegistry.FikaPresent || item == null || item.IsDummy)
            {
                return;
            }

            float strength = item.GetStrength;
            float range = item.MaxExplosionDistance * RangeShare;
            if (strength <= 0f || range <= 0f)
            {
                return;
            }

            foreach (DoorEntry entry in DoorDestructionRegistry.All)
            {
                if (!entry.Eligible || entry.Frame == null || entry.Door == null)
                {
                    continue;
                }

                try
                {
                    Apply(entry, position, strength * StrengthToDamage, range);
                }
                catch (Exception ex)
                {
                    DoorBreachPlugin.Logger.LogError($"[EDI-Blast] {entry.Door.Id}: {ex}");
                }
            }
        }

        private static bool Apply(DoorEntry entry, Vector3 position, float strength, float range)
        {
            Transform leaf = entry.LeafTransform;
            LeafFrame frame = entry.Frame;

            Vector3 centre = frame.ToWorld(leaf, new NVector2(frame.Width * 0.5f, frame.Height * 0.5f));
            if ((centre - position).sqrMagnitude > (range + frame.Height) * (range + frame.Height))
            {
                return false;
            }

            NVector2 panel = frame.ToPanel(leaf, position);
            panel = new NVector2(Mathf.Clamp(panel.X, 0f, frame.Width), Mathf.Clamp(panel.Y, 0f, frame.Height));
            float depth = Mathf.Clamp(frame.Depth(leaf, position), -frame.Thickness * 0.5f, frame.Thickness * 0.5f);
            Vector3 target = frame.ToWorld(leaf, panel, depth);
            float distance = Vector3.Distance(position, target);
            if (distance >= range || !InSight(entry, position, target))
            {
                return false;
            }

            ImpactKernel kernel = ImpactModel.Blast(strength, distance, range, entry.State?.Layout.CellWidth ?? Settings.CellSizeCm.Value / 100f);
            if (kernel.Amount <= 1f)
            {
                return false;
            }

            Vector3 away = (target - position).sqrMagnitude > 1e-6f ? (target - position).normalized : frame.NormalWorld(leaf);
            DoorDamageIntake.ApplyBlast(entry, panel, kernel, away * kernel.Impulse);
            return true;
        }

        private static bool InSight(DoorEntry entry, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.05f)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(from, delta / length, length - 0.02f, SightMask, QueryTriggerInteraction.Ignore);
            Transform door = entry.Door.transform;
            Transform leaf = entry.LeafTransform;
            foreach (RaycastHit hit in hits)
            {
                Transform other = hit.collider.transform;
                if (!other.IsChildOf(door) && !other.IsChildOf(leaf))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
