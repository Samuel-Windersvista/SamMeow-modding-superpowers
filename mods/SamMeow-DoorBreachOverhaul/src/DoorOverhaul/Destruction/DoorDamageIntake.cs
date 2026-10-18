using DoorBreach;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using DoorOverhaul.Destruction.Core;
using UnityEngine;
using NVector2 = System.Numerics.Vector2;

namespace DoorOverhaul.Destruction
{
    internal sealed class ImpactRecord
    {
        public DoorEntry Entry;
        public Vector3 Point;
        public Vector3 Direction;
        public NVector2 Panel;
        public float Depth;
        public float Penetration;
        public float Damage;
        public float DiameterMm;
        public float MassGram;
        public int Projectiles = 1;
        public float Distance;
        public EDamageType DamageType;
        public string Shooter = string.Empty;
        public int FireIndex;
        public bool Forward;
        public bool ExitSeen;
        public float ReceivedAt;
        public float Time;
        public ImpactKernel Kernel;
        public string Outcome = string.Empty;
    }

    internal static class DoorDamageIntake
    {
        private const float DuplicateWindow = 0.25f;
        private const float DuplicateLateral = 0.02f;
        private const float SamePointSqr = 0.003f * 0.003f;
        private const float SwingWaitLimit = 3f;
        private const float RecentSeconds = 20f;
        private const int RecentCap = 300;

        private static readonly List<ImpactRecord> Pending = new List<ImpactRecord>();
        private static readonly List<ImpactRecord> Waiting = new List<ImpactRecord>();
        private static readonly Dictionary<string, AmmoTemplate> AmmoCache = new Dictionary<string, AmmoTemplate>();

        public static readonly List<ImpactRecord> Recent = new List<ImpactRecord>();
        public static int Processed { get; private set; }
        public static int Duplicates { get; private set; }
        public static int Ignored { get; private set; }

        private static bool _hitErrorLogged;

        public static void OnHit(DoorEntry entry, DamageInfo info)
        {
            if (!Settings.DestructionEnabled.Value)
            {
                return;
            }

            try
            {
                Record(entry, info);
            }
            catch (System.Exception ex)
            {
                if (!_hitErrorLogged)
                {
                    _hitErrorLogged = true;
                    DoorBreachPlugin.Logger.LogError($"Destruction: recording a door hit failed (further errors suppressed): {ex}");
                }
            }
        }

        private static void Record(DoorEntry entry, DamageInfo info)
        {
            entry.LastShooter = info.Player?.iPlayer ?? entry.LastShooter;
            RecordHit(entry, info);
        }

        private static void RecordHit(DoorEntry entry, DamageInfo info)
        {
            Pending.Add(new ImpactRecord
            {
                Entry = entry,
                Point = info.HitPoint,
                Direction = info.Direction,
                Penetration = info.PenetrationPower,
                Damage = info.Damage,
                Distance = info.MasterOrigin == Vector3.zero ? 0f : Vector3.Distance(info.HitPoint, info.MasterOrigin),
                DamageType = info.DamageType,
                Shooter = info.Player?.iPlayer?.ProfileId ?? string.Empty,
                FireIndex = info.FireIndex,
                Forward = info.IsForwardHit,
                ReceivedAt = UnityEngine.Time.time
            }.WithAmmo(info.SourceId));
        }

        public static void Flush()
        {
            if (Pending.Count == 0 && Waiting.Count == 0)
            {
                PruneRecent();
                return;
            }

            var batch = new List<ImpactRecord>(Waiting.Count + Pending.Count);
            batch.AddRange(Waiting);
            batch.AddRange(Pending);
            Waiting.Clear();
            Pending.Clear();

            var touched = new HashSet<DoorEntry>();
            var push = new Dictionary<DoorEntry, Vector3>();
            foreach (ImpactRecord hit in batch)
            {
                if (IsDuplicate(hit))
                {
                    Duplicates++;
                    continue;
                }

                Door door = hit.Entry.Door;
                if (door == null || hit.Entry.Frame == null)
                {
                    continue;
                }

                if (door.DoorState == EDoorState.Interacting || door.DoorState == EDoorState.Breaching)
                {
                    if (UnityEngine.Time.time - hit.ReceivedAt < SwingWaitLimit)
                    {
                        Waiting.Add(hit);
                        continue;
                    }

                    hit.Outcome = "dropped: door kept swinging";
                    Ignore(hit);
                    continue;
                }

                hit.Time = UnityEngine.Time.time;
                Transform leaf = hit.Entry.LeafTransform;
                hit.Panel = hit.Entry.Frame.ToPanel(leaf, hit.Point);
                hit.Depth = hit.Entry.Frame.Depth(leaf, hit.Point);

                if (hit.DamageType == EDamageType.GrenadeFragment)
                {
                    hit.Outcome = "grenade: not yet";
                    Ignore(hit);
                    continue;
                }

                FractureState state = hit.Entry.State ?? (hit.Entry.State = CreateState(hit.Entry));
                if (state.Leaf == LeafStatus.Detached && hit.Entry.Takeover == null)
                {
                    hit.Outcome = "door already detached";
                    Ignore(hit);
                    continue;
                }

                state.Profile.HoleSizeScale = Settings.HoleSize.Value;
                state.HingesBreakable = Settings.HingeCollapse.Value && Settings.ShowHoles.Value;
                hit.Kernel = ImpactModel.Evaluate(new ImpactInput
                {
                    Source = hit.DamageType == EDamageType.GrenadeFragment ? ImpactSource.GrenadeFragment : ImpactSource.Bullet,
                    PenetrationPower = hit.Penetration,
                    Damage = hit.Damage,
                    DiameterMm = hit.DiameterMm,
                    MassGram = hit.MassGram,
                    ProjectileCount = hit.Projectiles,
                    Distance = hit.Distance
                }, state.Profile, state.Layout.CellWidth);

                state.Damage(hit.Panel, hit.Kernel);
                hit.Outcome = hit.Kernel.Through ? "punch-through" : hit.Kernel.Breaching ? "breaching" : "surface";
                hit.Entry.Hits++;
                Processed++;
                touched.Add(hit.Entry);
                Vector3 direction = hit.Direction.sqrMagnitude > 1e-6f ? hit.Direction.normalized : Vector3.zero;
                push[hit.Entry] = (push.TryGetValue(hit.Entry, out Vector3 sum) ? sum : Vector3.zero) + direction * hit.Kernel.Impulse;
                Remember(hit);
            }

            foreach (DoorEntry entry in touched)
            {
                ResolveDoor(entry, push.TryGetValue(entry, out Vector3 momentum) ? momentum : Vector3.zero);
            }

            PruneRecent();
        }

        private static void ResolveDoor(DoorEntry entry, Vector3 momentum)
        {
            FractureState state = entry.State;
            state.LatchEngaged = entry.Door.DoorState == EDoorState.Shut && state.Leaf == LeafStatus.Attached;
            DestructionResult result = state.Resolve();
            if (result.RemovedCells.Count > 0 || result.Groups.Count > 0 || result.BecameHanging || result.LeafDetached)
            {
                DestructionVisuals.OnResolved(entry, result, momentum);
                DoorGlass.OnCellsGone(entry, result, momentum);
            }
        }

        public static void ApplyBlast(DoorEntry entry, NVector2 panel, ImpactKernel kernel, Vector3 momentum)
        {
            if (entry.Frame == null || entry.Door == null)
            {
                return;
            }

            FractureState state = entry.State ?? (entry.State = CreateState(entry));
            if (state.Leaf == LeafStatus.Detached && entry.Takeover == null)
            {
                return;
            }

            state.HingesBreakable = Settings.HingeCollapse.Value && Settings.ShowHoles.Value;
            state.Damage(panel, kernel);
            Processed++;
            ResolveDoor(entry, momentum);
        }

        public static void Clear()
        {
            Pending.Clear();
            Waiting.Clear();
            Recent.Clear();
            Processed = 0;
            Duplicates = 0;
            Ignored = 0;
        }

        private static bool IsDuplicate(ImpactRecord hit)
        {
            for (int i = Recent.Count - 1; i >= 0; i--)
            {
                ImpactRecord earlier = Recent[i];
                if (UnityEngine.Time.time - earlier.Time > DuplicateWindow)
                {
                    break;
                }

                if (earlier.Entry != hit.Entry || earlier.Shooter != hit.Shooter || earlier.FireIndex != hit.FireIndex)
                {
                    continue;
                }

                if (hit.Projectiles > 1 && hit.Forward == earlier.Forward && (hit.Point - earlier.Point).sqrMagnitude > SamePointSqr)
                {
                    continue;
                }

                Vector3 direction = earlier.Direction.sqrMagnitude > 1e-6f ? earlier.Direction.normalized : Vector3.forward;
                Vector3 offset = Vector3.ProjectOnPlane(hit.Point - earlier.Point, direction);
                if (offset.magnitude < DuplicateLateral)
                {
                    if (hit.Forward != earlier.Forward)
                    {
                        earlier.ExitSeen = true;
                    }

                    return true;
                }
            }

            return false;
        }

        private static FractureState CreateState(DoorEntry entry)
        {
            LeafFrame frame = entry.Frame;
            string key = string.IsNullOrEmpty(entry.Door.Id) ? entry.Door.transform.position.ToString("F2") : entry.Door.Id;
            FractureLayout layout = FractureLayout.Generate(new LayoutSettings
            {
                Width = frame.Width,
                Height = frame.Height,
                CellSize = Settings.CellSizeCm.Value / 100f,
                Seed = StableHash.Fnv1a(key)
            });

            float hingeX = Mathf.Min(0.03f, frame.Width * 0.05f);
            NVector2 handle = frame.ToPanel(entry.Door.transform, Doors.DoorMath.HandlePosition(entry.Door));
            float latchY = handle.Y > 0.2f && handle.Y < frame.Height - 0.2f ? handle.Y : frame.Height * 0.45f;

            float hingeInset = Mathf.Clamp(0.2f, frame.Height * 0.08f, frame.Height * 0.2f);

            var anchors = new[]
            {
                new AnchorSpec { Kind = AnchorKind.Hinge, Position = new NVector2(hingeX, hingeInset), Radius = 0.1f },
                new AnchorSpec { Kind = AnchorKind.Hinge, Position = new NVector2(hingeX, frame.Height - hingeInset), Radius = 0.1f },
                new AnchorSpec { Kind = AnchorKind.Latch, Position = new NVector2(frame.Width - 0.04f, latchY), Radius = 0.1f }
            };

            var state = new FractureState(layout, entry.Profile, Mathf.Clamp(frame.Thickness, 0.02f, 0.1f), anchors);
            return state;
        }

        private static ImpactRecord WithAmmo(this ImpactRecord record, string sourceId)
        {
            record.DiameterMm = 5.56f;
            record.MassGram = 4f;
            if (string.IsNullOrEmpty(sourceId))
            {
                return record;
            }

            if (!AmmoCache.TryGetValue(sourceId, out AmmoTemplate ammo))
            {
                if (Singleton<ItemFactory>.Instantiated
                    && Singleton<ItemFactory>.Instance.ItemTemplates.TryGetValue(sourceId, out ItemTemplate template))
                {
                    ammo = template as AmmoTemplate;
                }

                AmmoCache[sourceId] = ammo;
            }

            if (ammo != null)
            {
                record.DiameterMm = ammo.BulletDiameterMilimeters > 0f ? ammo.BulletDiameterMilimeters : record.DiameterMm;
                record.MassGram = ammo.BulletMassGram > 0f ? ammo.BulletMassGram : record.MassGram;
                record.Projectiles = Mathf.Max(1, ammo.ProjectileCount);
            }

            return record;
        }

        private static void Ignore(ImpactRecord hit)
        {
            hit.Time = UnityEngine.Time.time;
            hit.Entry.IgnoredHits++;
            hit.Entry.LastIgnoreReason = hit.Outcome;
            Ignored++;
            Remember(hit);
        }

        private static void Remember(ImpactRecord hit)
        {
            Recent.Add(hit);
            if (Recent.Count > RecentCap)
            {
                Recent.RemoveRange(0, Recent.Count - RecentCap);
            }
        }

        private static void PruneRecent()
        {
            int stale = 0;
            while (stale < Recent.Count && UnityEngine.Time.time - Recent[stale].Time > RecentSeconds)
            {
                stale++;
            }

            if (stale > 0)
            {
                Recent.RemoveRange(0, stale);
            }
        }
    }
}
