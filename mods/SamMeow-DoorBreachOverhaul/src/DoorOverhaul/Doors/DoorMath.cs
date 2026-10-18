using EFT.Interactive;
using UnityEngine;

namespace DoorOverhaul.Doors
{
    internal static class DoorMath
    {
        public static bool CanSwing(WorldInteractiveObject door)
        {
            return door is Door && !(door is SlidingDoor) && !(door is DoorSwitch) && Mathf.Abs(Travel(door)) > 1f;
        }

        public static float Travel(WorldInteractiveObject door)
        {
            return Mathf.DeltaAngle(door.CloseAngle, door.OpenAngle);
        }

        public static float OpenFraction(WorldInteractiveObject door)
        {
            float travel = Travel(door);
            if (Mathf.Abs(travel) < 0.01f)
            {
                return door.DoorState == EDoorState.Open ? 1f : 0f;
            }

            return Mathf.Clamp01(Mathf.DeltaAngle(door.CloseAngle, door.CurrentAngle) / travel);
        }

        public static float FullyOpenFraction(WorldInteractiveObject door)
        {
            float travel = Travel(door);
            if (Mathf.Abs(travel) < 0.01f)
            {
                return 1f;
            }

            AnimationCurve curve = door.ProgressCurve;
            float end = curve != null && curve.length > 0 ? curve[curve.length - 1].value : 1f;
            return Mathf.Clamp(Mathf.DeltaAngle(door.CloseAngle, door.OpenAngle * end) / travel, 0.05f, 1f);
        }

        public static float AngleAt(WorldInteractiveObject door, float fraction)
        {
            return door.CloseAngle + Travel(door) * Mathf.Clamp01(fraction);
        }

        public static Bounds GetBounds(WorldInteractiveObject door)
        {
            Collider collider = door.Collider;
            if (collider != null)
            {
                return collider.bounds;
            }

            return new Bounds(door.transform.position, new Vector3(1.2f, 2.2f, 1.2f));
        }

        public static Vector3 HandlePosition(WorldInteractiveObject door)
        {
            if (door._handle != null)
            {
                return door._handle.transform.position;
            }

            if (door.LockHandle != null)
            {
                return door.LockHandle.transform.position;
            }

            return GetBounds(door).center;
        }
    }
}
