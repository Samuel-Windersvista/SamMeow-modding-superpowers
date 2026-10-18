using System;
using System.Collections.Generic;
using System.Numerics;

namespace DoorOverhaul.Destruction.Core
{
    public static class SlabBuilder
    {
        private const float BorderEpsilon = 1e-4f;

        public static (List<Vector3> Positions, List<int> Triangles) Build(FractureLayout layout, IReadOnlyList<bool> visible, float halfThickness)
        {
            var positions = new List<Vector3>();
            var triangles = new List<int>();

            for (int c = 0; c < layout.Cells.Length; c++)
            {
                if (!visible[c])
                {
                    continue;
                }

                Vector2[] polygon = layout.Cells[c].Polygon;
                int n = polygon.Length;

                int front = positions.Count;
                foreach (Vector2 p in polygon)
                {
                    positions.Add(new Vector3(p, halfThickness));
                }

                int back = positions.Count;
                foreach (Vector2 p in polygon)
                {
                    positions.Add(new Vector3(p, -halfThickness));
                }

                for (int k = 1; k + 1 < n; k++)
                {
                    triangles.Add(front); triangles.Add(front + k); triangles.Add(front + k + 1);
                    triangles.Add(back); triangles.Add(back + k + 1); triangles.Add(back + k);
                }
            }

            foreach (Bond bond in layout.Bonds)
            {
                if (visible[bond.A] != visible[bond.B])
                {
                    Vector2 inside = layout.Cells[visible[bond.A] ? bond.A : bond.B].Centroid;
                    AddWall(positions, triangles, bond.P0, bond.P1, inside, halfThickness);
                }
            }

            for (int c = 0; c < layout.Cells.Length; c++)
            {
                if (!visible[c])
                {
                    continue;
                }

                Vector2[] polygon = layout.Cells[c].Polygon;
                for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                {
                    if (OnBorder(layout, polygon[j], polygon[i]))
                    {
                        AddWall(positions, triangles, polygon[j], polygon[i], layout.Cells[c].Centroid, halfThickness);
                    }
                }
            }

            return (positions, triangles);
        }

        private static void AddWall(List<Vector3> positions, List<int> triangles, Vector2 a, Vector2 b, Vector2 inside, float h)
        {
            Vector2 d = b - a;
            if (Vector2.Dot(new Vector2(d.Y, -d.X), inside - (a + b) * 0.5f) > 0f)
            {
                (a, b) = (b, a);
            }

            int first = positions.Count;
            positions.Add(new Vector3(a, -h));
            positions.Add(new Vector3(b, -h));
            positions.Add(new Vector3(b, h));
            positions.Add(new Vector3(a, h));
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
        }

        private static bool OnBorder(FractureLayout layout, Vector2 a, Vector2 b)
        {
            return (Math.Abs(a.X) < BorderEpsilon && Math.Abs(b.X) < BorderEpsilon)
                || (Math.Abs(a.X - layout.Width) < BorderEpsilon && Math.Abs(b.X - layout.Width) < BorderEpsilon)
                || (Math.Abs(a.Y) < BorderEpsilon && Math.Abs(b.Y) < BorderEpsilon)
                || (Math.Abs(a.Y - layout.Height) < BorderEpsilon && Math.Abs(b.Y - layout.Height) < BorderEpsilon);
        }
    }
}
