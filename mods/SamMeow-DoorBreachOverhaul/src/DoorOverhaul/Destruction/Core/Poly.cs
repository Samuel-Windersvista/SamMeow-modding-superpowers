using System;
using System.Collections.Generic;
using System.Numerics;

namespace DoorOverhaul.Destruction.Core
{
    public static class Poly
    {
        public const float Epsilon = 1e-6f;

        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            float twice = 0f;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                twice += Cross(polygon[j], polygon[i]);
            }

            return twice * 0.5f;
        }

        public static Vector2 Centroid(IReadOnlyList<Vector2> polygon)
        {
            float twiceArea = 0f;
            Vector2 sum = Vector2.Zero;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                float cross = Cross(polygon[j], polygon[i]);
                twiceArea += cross;
                sum += (polygon[j] + polygon[i]) * cross;
            }

            if (Math.Abs(twiceArea) < Epsilon)
            {
                Vector2 mean = Vector2.Zero;
                foreach (Vector2 p in polygon)
                {
                    mean += p;
                }

                return polygon.Count > 0 ? mean / polygon.Count : Vector2.Zero;
            }

            return sum / (3f * twiceArea);
        }

        public static List<Vector2> ClipHalfPlane(List<Vector2> polygon, Vector2 normal, float offset)
        {
            var result = new List<Vector2>(polygon.Count + 2);
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 current = polygon[i];
                Vector2 next = polygon[(i + 1) % polygon.Count];
                float dc = Vector2.Dot(normal, current) - offset;
                float dn = Vector2.Dot(normal, next) - offset;

                if (dc <= 0f)
                {
                    result.Add(current);
                }

                if ((dc < 0f && dn > 0f) || (dc > 0f && dn < 0f))
                {
                    float t = dc / (dc - dn);
                    result.Add(current + (next - current) * t);
                }
            }

            return result;
        }

        public static bool ContainsConvex(IReadOnlyList<Vector2> polygonCcw, Vector2 point, float tolerance = 1e-5f)
        {
            for (int i = 0; i < polygonCcw.Count; i++)
            {
                Vector2 a = polygonCcw[i];
                Vector2 b = polygonCcw[(i + 1) % polygonCcw.Count];
                if (Cross(b - a, point - a) < -tolerance)
                {
                    return false;
                }
            }

            return true;
        }

        public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.LengthSquared();
            float t = lengthSq > 0f ? Math.Clamp(Vector2.Dot(point - a, ab) / lengthSq, 0f, 1f) : 0f;
            return Vector2.Distance(point, a + ab * t);
        }

        public static List<BaryPoint> ClipConvex(List<BaryPoint> subject, IReadOnlyList<Vector2> clipCcw)
        {
            List<BaryPoint> output = subject;
            for (int e = 0; e < clipCcw.Count && output.Count > 0; e++)
            {
                Vector2 c0 = clipCcw[e];
                Vector2 c1 = clipCcw[(e + 1) % clipCcw.Count];
                Vector2 edge = c1 - c0;

                List<BaryPoint> input = output;
                output = new List<BaryPoint>(input.Count + 2);
                for (int i = 0; i < input.Count; i++)
                {
                    BaryPoint current = input[i];
                    BaryPoint next = input[(i + 1) % input.Count];
                    float sc = Cross(edge, current.Position - c0);
                    float sn = Cross(edge, next.Position - c0);

                    if (sc >= 0f)
                    {
                        output.Add(current);
                    }

                    if ((sc > 0f && sn < 0f) || (sc < 0f && sn > 0f))
                    {
                        float t = sc / (sc - sn);
                        output.Add(BaryPoint.Lerp(current, next, t));
                    }
                }
            }

            return output;
        }
    }

    public struct BaryPoint
    {
        public Vector2 Position;
        public Vector3 Weights;

        public BaryPoint(Vector2 position, Vector3 weights)
        {
            Position = position;
            Weights = weights;
        }

        public static BaryPoint Lerp(BaryPoint a, BaryPoint b, float t)
        {
            return new BaryPoint(a.Position + (b.Position - a.Position) * t, a.Weights + (b.Weights - a.Weights) * t);
        }
    }
}
