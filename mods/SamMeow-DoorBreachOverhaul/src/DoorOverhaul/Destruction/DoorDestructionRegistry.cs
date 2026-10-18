using DoorBreach;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.NetworkPackets;
using DoorOverhaul.Destruction.Core;
using UnityEngine;

namespace DoorOverhaul.Destruction
{
    internal sealed class DoorEntry
    {
        public Door Door;
        public bool Eligible;
        public string Reason = string.Empty;
        public MaterialType Material;
        public MaterialProfile Profile;
        public LeafFrame Frame;
        public readonly List<BallisticCollider> Colliders = new List<BallisticCollider>();

        public readonly List<BallisticCollider> HookedColliders = new List<BallisticCollider>();

        public FractureState State;

        public DoorTakeover Takeover;
        public Transform LeafTransform => Takeover?.Leaf != null ? Takeover.Leaf : Door.transform;

        public EFT.IPlayer LastShooter;
        public int Hits;
        public int IgnoredHits;
        public string LastIgnoreReason = string.Empty;
    }

    internal static class DoorDestructionRegistry
    {
        public const string FikaGuid = "com.fika.core";

        private static readonly Dictionary<Door, DoorEntry> Entries = new Dictionary<Door, DoorEntry>();
        private static readonly Dictionary<BallisticCollider, DoorEntry> ColliderMap = new Dictionary<BallisticCollider, DoorEntry>();
        private static ExfiltrationDoor[] _exits = Array.Empty<ExfiltrationDoor>();

        public static IReadOnlyCollection<DoorEntry> All => Entries.Values;
        public static bool FikaPresent { get; private set; }
        public static string LocationId { get; private set; } = string.Empty;

        public static bool TryGet(Door door, out DoorEntry entry)
        {
            entry = null;
            return door != null && Entries.TryGetValue(door, out entry);
        }

        /// <summary>按已注册碰撞体查条目（唯一命中分派入口）。</summary>
        internal static bool TryGetByCollider(BallisticCollider collider, out DoorEntry entry)
        {
            entry = null;
            return collider != null && ColliderMap.TryGetValue(collider, out entry);
        }

        internal static void RegisterCollider(BallisticCollider collider, DoorEntry entry)
        {
            if (collider != null && entry != null)
            {
                ColliderMap[collider] = entry;
            }
        }

        internal static void UnregisterCollider(BallisticCollider collider)
        {
            if (collider != null)
            {
                ColliderMap.Remove(collider);
            }
        }

        public static void Build(GameWorld world)
        {
            Clear();
            FikaPresent = Chainloader.PluginInfos.ContainsKey(FikaGuid);
            LocationId = world.LocationId ?? string.Empty;
            _exits = UnityEngine.Object.FindObjectsOfType<ExfiltrationDoor>();

            List<Door> doors = FindDoors(world);
            foreach (Door door in doors)
            {
                Entries[door] = Evaluate(door);
            }

            MarkPossibleDoubleDoors();

            foreach (DoorEntry entry in Entries.Values.Where(e => e.Eligible))
            {
                foreach (BallisticCollider collider in entry.HookedColliders)
                {
                    RegisterCollider(collider, entry);
                }
            }
        }

        public static void Clear()
        {
            ColliderMap.Clear();
            Entries.Clear();
            _exits = Array.Empty<ExfiltrationDoor>();
        }

        public static MaterialProfile ProfileFor(MaterialType material)
        {
            switch (material)
            {
                case MaterialType.WoodThin:
                    MaterialProfile thin = MaterialProfile.WoodThin();
                    thin.ThroughPenetration = Settings.ThroughPenWoodThin.Value;
                    return thin;
                case MaterialType.WoodThick:
                    MaterialProfile thick = MaterialProfile.WoodThick();
                    thick.ThroughPenetration = Settings.ThroughPenWoodThick.Value;
                    return thick;
                case MaterialType.MetalThin:
                    MaterialProfile metal = MaterialProfile.MetalThin();
                    metal.ThroughPenetration = Settings.ThroughPenMetalThin.Value;
                    return metal;
                default:
                    return null;
            }
        }

        private static List<Door> FindDoors(GameWorld world)
        {
            var doors = new List<Door>();
            try
            {
                if (world.World != null)
                {
                    doors.AddRange(world.WorldInteractiveObjects().OfType<Door>());
                }
            }
            catch (Exception ex)
            {
                DoorBreachPlugin.Logger.LogWarning($"Destruction: could not read the world's doors ({ex.Message}); scanning the scene instead");
            }

            if (doors.Count == 0)
            {
                doors.AddRange(UnityEngine.Object.FindObjectsOfType<Door>());
            }

            return doors.Where(d => d != null).Distinct().ToList();
        }

        private static DoorEntry Evaluate(Door door)
        {
            var entry = new DoorEntry { Door = door };

            foreach (BallisticCollider collider in door.GetComponentsInChildren<BallisticCollider>(true))
            {
                if (collider.HitType == EHitType.Window || collider.HitType == EHitType.Lamp
                    || collider.TypeOfMaterial == MaterialType.Glass || collider.TypeOfMaterial == MaterialType.GlassShattered)
                {
                    continue;
                }

                entry.HookedColliders.Add(collider);
                if (collider.gameObject.activeInHierarchy)
                {
                    entry.Colliders.Add(collider);
                }
            }

            entry.Material = DominantMaterial(entry.Colliders);
            entry.Profile = ProfileFor(entry.Material);
            entry.Frame = LeafFrame.Build(door);
            entry.Reason = ExclusionReason(door, entry);
            entry.Eligible = entry.Reason.Length == 0;
            return entry;
        }

        private static string ExclusionReason(Door door, DoorEntry entry)
        {
            if (FikaPresent)
            {
                return "Fika installed (destruction not synced yet)";
            }

            if (door is KeycardDoor)
            {
                return "keycard door";
            }

            if (door is SlidingDoor)
            {
                return "sliding door";
            }

            if (door is DoorSwitch)
            {
                return "door switch";
            }

            if (door.TriggersMap != null && door.TriggersMap.Length > 0)
            {
                return "fires map/quest triggers";
            }

            if (IsExtractionDoor(door))
            {
                return "extraction door";
            }

            if (!door.Operatable)
            {
                return "not operatable";
            }

            if (entry.Colliders.Count == 0)
            {
                return "no ballistic collider";
            }

            if (entry.Profile == null)
            {
                return $"material {entry.Material} stays vanilla";
            }

            if (entry.Frame == null || entry.Frame.Width < 0.3f || entry.Frame.Height < 1.0f || entry.Frame.Thickness > 0.25f)
            {
                return entry.Frame == null ? "no leaf geometry" : $"unexpected leaf shape {entry.Frame.Width:0.00}x{entry.Frame.Height:0.00}x{entry.Frame.Thickness:0.000}";
            }

            return string.Empty;
        }

        private static bool IsExtractionDoor(Door door)
        {
            if (door.GetComponentInParent<ExfiltrationDoor>() != null)
            {
                return true;
            }

            foreach (ExfiltrationDoor exit in _exits)
            {
                if (exit == null)
                {
                    continue;
                }

                if (exit._doorTransforms == null)
                {
                    continue;
                }

                foreach (ExfiltrationDoor.DoorTransform moving in exit._doorTransforms)
                {
                    if (moving?.Transform != null && (door.transform.IsChildOf(moving.Transform) || moving.Transform.IsChildOf(door.transform)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void MarkPossibleDoubleDoors()
        {
            List<DoorEntry> candidates = Entries.Values.Where(e => e.Eligible && e.Frame != null).ToList();
            for (int i = 0; i < candidates.Count; i++)
            {
                for (int j = i + 1; j < candidates.Count; j++)
                {
                    DoorEntry a = candidates[i];
                    DoorEntry b = candidates[j];
                    bool sameFrame = a.Door.transform.parent != null && a.Door.transform.parent == b.Door.transform.parent;
                    bool meeting = Vector3.Distance(LatchEdge(a), LatchEdge(b)) < 0.25f;
                    if (sameFrame || meeting)
                    {
                        a.Eligible = b.Eligible = false;
                        a.Reason = b.Reason = "possible double door (until surveyed)";
                    }
                }
            }
        }

        private static Vector3 LatchEdge(DoorEntry entry)
        {
            return entry.Frame.ToWorld(entry.Door.transform, new System.Numerics.Vector2(entry.Frame.Width, entry.Frame.Height * 0.5f));
        }

        private static MaterialType DominantMaterial(List<BallisticCollider> colliders)
        {
            MaterialType best = MaterialType.None;
            float bestVolume = -1f;
            foreach (IGrouping<MaterialType, BallisticCollider> group in colliders.GroupBy(c => c.TypeOfMaterial))
            {
                float volume = group.Sum(c => Volume(c));
                if (volume > bestVolume)
                {
                    bestVolume = volume;
                    best = group.Key;
                }
            }

            return best;
        }

        private static float Volume(BallisticCollider ballistic)
        {
            Collider collider = ballistic.GetComponent<Collider>();
            if (collider == null)
            {
                return 0f;
            }

            Vector3 size = collider.bounds.size;
            return Mathf.Max(size.x, 0.01f) * Mathf.Max(size.y, 0.01f) * Mathf.Max(size.z, 0.01f);
        }
    }
}
