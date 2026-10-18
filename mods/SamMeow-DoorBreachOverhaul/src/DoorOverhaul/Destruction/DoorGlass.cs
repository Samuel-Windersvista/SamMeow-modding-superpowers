using DoorBreach;
using System;
using System.Collections.Generic;
using EFT.Ballistics;
using EFT.Interactive;
using DoorOverhaul.Destruction.Core;
using UnityEngine;
using NVector2 = System.Numerics.Vector2;

namespace DoorOverhaul.Destruction
{
    internal static class DoorGlass
    {
        private const float Margin = 0.03f;

        public static void OnCellsGone(DoorEntry entry, DestructionResult result, Vector3 momentum)
        {
            if (entry.Door == null || entry.Frame == null)
            {
                return;
            }

            WindowBreaker[] windows = entry.Door.GetComponentsInChildren<WindowBreaker>(true);
            if (windows.Length == 0)
            {
                return;
            }

            var gone = new List<int>(result.RemovedCells);
            foreach (DetachedGroup group in result.Groups)
            {
                if (!group.IsLeafBody)
                {
                    gone.AddRange(group.Cells);
                }
            }

            if (gone.Count == 0)
            {
                return;
            }

            Transform leaf = entry.Door.transform;
            LeafFrame frame = entry.Frame;
            FractureLayout layout = entry.State.Layout;
            foreach (WindowBreaker window in windows)
            {
                if (window == null || window.IsDamaged || !window.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!PanelRect(window, frame, leaf, out NVector2 min, out NVector2 max))
                {
                    continue;
                }

                foreach (int cell in gone)
                {
                    NVector2 c = layout.Cells[cell].Centroid;
                    if (c.X >= min.X - Margin && c.X <= max.X + Margin && c.Y >= min.Y - Margin && c.Y <= max.Y + Margin)
                    {
                        Shatter(window, frame.ToWorld(leaf, c), momentum.sqrMagnitude > 1e-8f ? momentum.normalized : frame.NormalWorld(leaf), entry.Door.Id);
                        break;
                    }
                }
            }
        }

        private static void Shatter(WindowBreaker window, Vector3 point, Vector3 direction, string door)
        {
            try
            {
                var info = new DamageInfo { HitPoint = point, Direction = direction, DamageType = EFT.EDamageType.Bullet };
                window.MakeHit(in info, instantFall: true);
                DestructionFragments.Burst(MaterialType.Glass, point, -direction, 0.8f);
            }
            catch (Exception ex)
            {
                DoorBreachPlugin.Logger.LogWarning($"[EDI-Glass] {door}: shattering a window failed: {ex.Message}");
            }
        }

        private static bool PanelRect(WindowBreaker window, LeafFrame frame, Transform leaf, out NVector2 min, out NVector2 max)
        {
            min = new NVector2(float.MaxValue, float.MaxValue);
            max = new NVector2(float.MinValue, float.MinValue);
            bool any = false;
            foreach (Renderer renderer in window.GetComponentsInChildren<Renderer>(true))
            {
                Grow(renderer.bounds, frame, leaf, ref min, ref max);
                any = true;
            }

            if (!any)
            {
                foreach (Collider collider in window.GetComponentsInChildren<Collider>(true))
                {
                    Grow(collider.bounds, frame, leaf, ref min, ref max);
                    any = true;
                }
            }

            return any;
        }

        private static void Grow(Bounds bounds, LeafFrame frame, Transform leaf, ref NVector2 min, ref NVector2 max)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                NVector2 p = frame.ToPanel(leaf, corner);
                min = NVector2.Min(min, p);
                max = NVector2.Max(max, p);
            }
        }
    }
}
