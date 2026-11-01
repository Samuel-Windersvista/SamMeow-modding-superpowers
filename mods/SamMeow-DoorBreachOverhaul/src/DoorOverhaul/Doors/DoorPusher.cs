using System.Collections.Generic;
using DoorBreach;
using EFT;
using EFT.Interactive;
using DoorOverhaul.Destruction;
using UnityEngine;

namespace DoorOverhaul.Doors
{
    internal static class DoorPusher
    {
        private const float ScanInterval = 0.2f;
        private const float ScanRadius = 2.2f;
        private const float Lookahead = 0.22f;
        private const float MinPushSpeed = 1.1f;
        private const float MaxDegreesPerSecond = 220f;
        private const float SqueakCooldown = 1.5f;

        private static readonly Collider[] Hits = new Collider[64];
        private static readonly List<Door> Nearby = new List<Door>();
        private static readonly Dictionary<Door, LeafFrame> Frames = new Dictionary<Door, LeafFrame>();
        private static readonly Dictionary<Door, float> LastPush = new Dictionary<Door, float>();
        private static float _nextScan;
        private static int _mask;

        public static void Tick(Player player)
        {
            if (!Settings.BodyPush.Value || player == null || !player.HealthController.IsAlive)
            {
                return;
            }

            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + ScanInterval;
                Rescan(player);
            }

            if (Nearby.Count == 0)
            {
                return;
            }

            Vector3 wish = WishVelocity(player);
            if (wish.sqrMagnitude < 0.04f)
            {
                return;
            }

            foreach (Door door in Nearby)
            {
                TryPush(player, door, wish);
            }
        }

        public static void Clear()
        {
            Nearby.Clear();
            Frames.Clear();
            LastPush.Clear();
        }

        private static void Rescan(Player player)
        {
            if (_mask == 0)
            {
                _mask = LayerMask.GetMask("DoorLowPolyCollider", "Interactive", "LowPolyCollider", "HighPolyCollider");
            }

            Nearby.Clear();
            int count = Physics.OverlapSphereNonAlloc(player.Position + Vector3.up, ScanRadius, Hits, _mask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Door door = Hits[i] != null ? Hits[i].GetComponentInParent<Door>() : null;
                if (door != null && !Nearby.Contains(door))
                {
                    Nearby.Add(door);
                }
            }
        }

        private static Vector3 WishVelocity(Player player)
        {
            Vector2 input = player.MovementContext.MovementDirection;
            if (input.sqrMagnitude < 0.01f)
            {
                return Vector3.zero;
            }

            Vector3 direction = player.Transform.Original.TransformDirection(new Vector3(input.x, 0f, input.y));
            direction.y = 0f;
            Vector3 actual = player.Velocity;
            actual.y = 0f;
            float speed = Mathf.Max(actual.magnitude, MinPushSpeed * Mathf.Clamp01(input.magnitude));
            return direction.normalized * speed;
        }

        private static void TryPush(Player player, Door door, Vector3 wish)
        {
            // Unity 假 null：门可能在 0.2s 扫描缓存窗口内被 C4 / 破坏管线销毁。
            if (door == null || door.DoorState != EDoorState.Open || !door.Operatable || door.NoInteractionsAllowed || !DoorMath.CanSwing(door)
                || (PeekDriver.IsActive && PeekDriver.Door == door))
            {
                return;
            }

            float full = DoorMath.FullyOpenFraction(door);
            float fraction = DoorMath.OpenFraction(door);
            if (fraction >= full - 0.002f)
            {
                return;
            }

            LeafFrame frame = FrameFor(door);
            if (frame == null)
            {
                return;
            }

            Transform t = door.transform;
            Vector3 chest = player.Position + Vector3.up * 1.1f;
            float height = frame.ToPanel(t, chest).Y;
            if (height < -0.3f || height > frame.Height + 0.3f)
            {
                return;
            }

            height = Mathf.Clamp(height, 0.1f, frame.Height - 0.1f);
            Vector3 a = frame.ToWorld(t, new System.Numerics.Vector2(0f, height));
            Vector3 b = frame.ToWorld(t, new System.Numerics.Vector2(frame.Width, height));
            Vector3 ab = b - a;
            Vector3 ap = chest - a;
            ab.y = 0f;
            ap.y = 0f;
            float along = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude) : 0f;
            Vector3 contact = a + (b - a) * along;

            Vector3 toChest = chest - contact;
            toChest.y = 0f;
            float radius = player.MovementContext.CharacterController.radius;
            float gap = toChest.magnitude - frame.Thickness * 0.5f - radius;
            if (gap > Lookahead)
            {
                return;
            }

            Vector3 axis = WorldInteractiveObject.GetRotationAxis(door.DoorAxis, t.parent);
            Vector3 lever = contact - t.position;
            Vector3 motion = Quaternion.AngleAxis(Mathf.Sign(DoorMath.Travel(door)), axis) * lever - lever;
            motion.y = 0f;
            if (motion.sqrMagnitude < 1e-10f)
            {
                return;
            }

            motion.Normalize();
            float push = Vector3.Dot(wish, motion);

            if (push < 0.15f || Vector3.Dot(contact - chest, motion) <= 0f)
            {
                return;
            }

            Vector3 flatLever = lever;
            flatLever.y = 0f;
            float arm = Mathf.Max(0.15f, flatLever.magnitude);
            float degrees = push / arm * Mathf.Rad2Deg * Settings.BodyPushStrength.Value * Time.deltaTime;

            float overlap = Mathf.Max(0f, 0.04f - gap);
            degrees += overlap / arm * Mathf.Rad2Deg;
            degrees = Mathf.Min(degrees, MaxDegreesPerSecond * Time.deltaTime + overlap / arm * Mathf.Rad2Deg);

            float travel = Mathf.Abs(DoorMath.Travel(door));
            float next = Mathf.Min(full, fraction + degrees / travel);
            if (next <= fraction)
            {
                return;
            }

            door.CurrentAngle = DoorMath.AngleAt(door, next);
            Squeak(door);
        }

        private static void Squeak(Door door)
        {
            float now = Time.time;
            bool fresh = !LastPush.TryGetValue(door, out float last) || now - last > SqueakCooldown;
            LastPush[door] = now;
            if (!fresh || door.SqueakSound == null || door.SqueakSound.Length == 0)
            {
                return;
            }

            AudioClip clip = door.SqueakSound[Random.Range(0, door.SqueakSound.Length)];
            if (clip != null)
            {
                door.PlaySoundAtPoint(door.GetSoundPos(), clip, BetterAudio.AudioSourceGroupType.InteractiveObjects, door._squeakSoundRolloff, 0.5f);
            }
        }

        private static LeafFrame FrameFor(Door door)
        {
            if (DoorDestructionRegistry.TryGet(door, out DoorEntry entry) && entry.Frame != null)
            {
                return entry.Frame;
            }

            if (!Frames.TryGetValue(door, out LeafFrame frame))
            {
                frame = LeafFrame.Build(door);
                Frames[door] = frame;
            }

            return frame;
        }
    }
}
