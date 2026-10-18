using System.Collections.Generic;
using EFT.Ballistics;
using EFT.Interactive;
using UnityEngine;
using NVector2 = System.Numerics.Vector2;

namespace DoorOverhaul.Destruction
{
    internal sealed class LeafFrame
    {
        public Vector3 Origin;
        public Vector3 WidthAxis;
        public Vector3 HeightAxis;
        public Vector3 NormalAxis;
        public float Width;
        public float Height;
        public float Thickness;
        public int GeometrySources;
        public string Source;

        public Collider SourceCollider;

        public NVector2 ToPanel(Transform door, Vector3 worldPoint)
        {
            Vector3 d = door.InverseTransformPoint(worldPoint) - Origin;
            return new NVector2(Vector3.Dot(d, WidthAxis), Vector3.Dot(d, HeightAxis));
        }

        public float Depth(Transform door, Vector3 worldPoint)
        {
            return Vector3.Dot(door.InverseTransformPoint(worldPoint) - Origin, NormalAxis);
        }

        public Vector3 ToWorld(Transform door, NVector2 panel, float depth = 0f)
        {
            return door.TransformPoint(Origin + WidthAxis * panel.X + HeightAxis * panel.Y + NormalAxis * depth);
        }

        public Vector3 NormalWorld(Transform door)
        {
            return door.TransformDirection(NormalAxis);
        }

        public static LeafFrame Build(Door door)
        {
            Transform t = door.transform;
            Vector3 axis = HingeAxisLocal(door);

            if (TryPrimaryBox(door, out Transform source, out Bounds box, out string sourceName, out Collider sourceCollider))
            {
                LeafFrame frame = FromBox(t, axis, source, box);
                if (frame != null)
                {
                    frame.Source = sourceName;
                    frame.GeometrySources = 1;
                    frame.SourceCollider = sourceCollider;
                    return frame;
                }
            }

            var points = new List<Vector3>();
            Transform largest = null;
            Bounds largestBounds = default;
            float largestVolume = -1f;
            foreach (MeshFilter filter in LeafMeshes(door))
            {
                Bounds b = filter.sharedMesh.bounds;
                AddCorners(points, t, filter.transform, b);
                float volume = Volume(filter.transform, b);
                if (volume > largestVolume)
                {
                    largestVolume = volume;
                    largest = filter.transform;
                    largestBounds = b;
                }
            }

            if (points.Count == 0)
            {
                return null;
            }

            LeafFrame fallback = FromBox(t, axis, largest, largestBounds, points);
            if (fallback != null)
            {
                fallback.Source = "render meshes";
                fallback.GeometrySources = points.Count / 8;
            }

            return fallback;
        }

        public static Collider PrimaryCollider(Door door)
        {
            return TryPrimaryBox(door, out _, out _, out _, out Collider collider) ? collider : null;
        }

        private static bool TryPrimaryBox(Door door, out Transform source, out Bounds box, out string name, out Collider sourceCollider)
        {
            source = null;
            box = default;
            name = null;
            sourceCollider = null;
            float best = -1f;
            foreach (BallisticCollider ballistic in door.GetComponentsInChildren<BallisticCollider>(false))
            {
                if (!ballistic.gameObject.activeInHierarchy || IsHandle(door, ballistic.transform))
                {
                    continue;
                }

                Collider collider = ballistic.GetComponent<Collider>();
                Bounds local;
                if (collider is BoxCollider boxCollider)
                {
                    local = new Bounds(boxCollider.center, boxCollider.size);
                }
                else if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
                {
                    local = meshCollider.sharedMesh.bounds;
                }
                else
                {
                    continue;
                }

                float volume = Volume(ballistic.transform, local);
                if (volume > best)
                {
                    best = volume;
                    source = ballistic.transform;
                    box = local;
                    name = "ballistic collider " + ballistic.name;
                    sourceCollider = collider;
                }
            }

            return source != null;
        }

        private static LeafFrame FromBox(Transform door, Vector3 axis, Transform source, Bounds box, List<Vector3> extentPoints = null)
        {
            var corners = new List<Vector3>(8);
            AddCorners(corners, door, source, box);
            Vector3 c000 = corners[0];
            Vector3[] edges = { corners[1] - c000, corners[2] - c000, corners[4] - c000 };

            int heightEdge = 0;
            for (int i = 1; i < 3; i++)
            {
                if (Mathf.Abs(Vector3.Dot(edges[i].normalized, axis)) > Mathf.Abs(Vector3.Dot(edges[heightEdge].normalized, axis)))
                {
                    heightEdge = i;
                }
            }

            int a = (heightEdge + 1) % 3;
            int b = (heightEdge + 2) % 3;
            Vector3 widthEdge = edges[a].sqrMagnitude >= edges[b].sqrMagnitude ? edges[a] : edges[b];
            Vector3 width = Vector3.ProjectOnPlane(widthEdge, axis);
            if (width.sqrMagnitude < 1e-6f)
            {
                return null;
            }

            width.Normalize();
            Vector3 centre = Vector3.zero;
            foreach (Vector3 p in corners)
            {
                centre += p;
            }

            centre /= corners.Count;
            if (Vector3.Dot(Vector3.ProjectOnPlane(centre, axis), width) < 0f)
            {
                width = -width;
            }

            Vector3 normal = Vector3.Cross(axis, width).normalized;

            float minW = float.MaxValue, maxW = float.MinValue;
            float minH = float.MaxValue, maxH = float.MinValue;
            float minN = float.MaxValue, maxN = float.MinValue;
            foreach (Vector3 p in extentPoints ?? corners)
            {
                float w = Vector3.Dot(p, width);
                float h = Vector3.Dot(p, axis);
                float n = Vector3.Dot(p, normal);
                minW = Mathf.Min(minW, w); maxW = Mathf.Max(maxW, w);
                minH = Mathf.Min(minH, h); maxH = Mathf.Max(maxH, h);
                minN = Mathf.Min(minN, n); maxN = Mathf.Max(maxN, n);
            }

            return new LeafFrame
            {
                WidthAxis = width,
                HeightAxis = axis,
                NormalAxis = normal,
                Origin = width * minW + axis * minH + normal * ((minN + maxN) * 0.5f),
                Width = maxW - minW,
                Height = maxH - minH,
                Thickness = maxN - minN
            };
        }

        private static void AddCorners(List<Vector3> points, Transform door, Transform source, Bounds b)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                points.Add(door.InverseTransformPoint(source.TransformPoint(corner)));
            }
        }

        private static float Volume(Transform source, Bounds b)
        {
            Vector3 s = Vector3.Scale(b.size, source.lossyScale);
            return Mathf.Abs(s.x * s.y * s.z);
        }

        public static IEnumerable<MeshFilter> LeafMeshes(Door door)
        {
            foreach (MeshFilter filter in door.transform.GetComponentsInChildren<MeshFilter>(false))
            {
                if (filter.sharedMesh != null && filter.gameObject.activeInHierarchy && !IsHandle(door, filter.transform)
                    && filter.GetComponentInParent<WindowBreaker>() == null)
                {
                    yield return filter;
                }
            }
        }

        public static Vector3 HingeAxisLocal(Door door)
        {
            Vector3 world = WorldInteractiveObject.GetRotationAxis(door.DoorAxis, door.transform.parent);
            if (world.sqrMagnitude < 1e-6f)
            {
                world = door.transform.up;
            }

            Vector3 local = door.transform.InverseTransformDirection(world).normalized;

            if (Vector3.Dot(door.transform.TransformDirection(local), Vector3.up) < 0f)
            {
                local = -local;
            }

            return local;
        }

        private static bool IsHandle(Door door, Transform candidate)
        {
            return (door._handle != null && candidate.IsChildOf(door._handle.transform))
                || (door.LockHandle != null && candidate.IsChildOf(door.LockHandle.transform));
        }
    }
}
