using System;
using System.Collections.Generic;
using System.Numerics;

namespace DoorOverhaul.Destruction.Core
{
    public sealed class TrianglePiece
    {
        public int Triangle;
        public Vector3[] Weights;
    }

    public static class LeafPartition
    {
        private const float MinProjectedArea = 1e-7f;
        private const float MinPieceArea = 1e-7f;
        private const int MaxSplitDepth = 12;

        public static List<TrianglePiece>[] Build(FractureLayout layout, IReadOnlyList<Vector2> panelPositions, IReadOnlyList<int> triangles)
        {
            var pieces = new List<TrianglePiece>[layout.Cells.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = new List<TrianglePiece>();
            }

            float chop = Math.Min(layout.CellWidth, layout.CellHeight);

            Vector2 p00 = Vector2.Zero, p10 = new Vector2(layout.Width, 0f);
            Vector2 p11 = new Vector2(layout.Width, layout.Height), p01 = new Vector2(0f, layout.Height);
            var outline = new[] { (p00, p10), (p10, p11), (p11, p01), (p01, p00) };

            for (int t = 0; t + 2 < triangles.Count; t += 3)
            {
                Vector2 a = panelPositions[triangles[t]];
                Vector2 b = panelPositions[triangles[t + 1]];
                Vector2 c = panelPositions[triangles[t + 2]];
                int triangle = t / 3;
                var ba = new BaryPoint(a, Vector3.UnitX);
                var bb = new BaryPoint(b, Vector3.UnitY);
                var bc = new BaryPoint(c, Vector3.UnitZ);

                if (Math.Abs(Poly.Cross(b - a, c - a)) * 0.5f < MinProjectedArea)
                {
                    Chop(layout, pieces, triangle, ba, bb, bc, chop, 0);
                    continue;
                }

                var inside = new List<BaryPoint> { ba, bb, bc };
                foreach ((Vector2 e0, Vector2 e1) in outline)
                {
                    List<BaryPoint> outside = ClipHalf(inside, e1, e0);
                    if (outside.Count >= 3 && Math.Abs(LeafPartition.Area(outside)) >= MinPieceArea)
                    {
                        for (int k = 1; k + 1 < outside.Count; k++)
                        {
                            Chop(layout, pieces, triangle, outside[0], outside[k], outside[k + 1], chop, 0);
                        }
                    }

                    inside = ClipHalf(inside, e0, e1);
                    if (inside.Count < 3)
                    {
                        break;
                    }
                }

                if (inside.Count < 3)
                {
                    continue;
                }

                bool clockwise = Poly.Cross(b - a, c - a) < 0f;

                Vector2 min = Vector2.Min(a, Vector2.Min(b, c));
                Vector2 max = Vector2.Max(a, Vector2.Max(b, c));
                foreach (int id in layout.CellsOverlapping(min, max))
                {
                    List<BaryPoint> clipped = Poly.ClipConvex(inside, layout.Cells[id].Polygon);
                    if (clipped.Count < 3)
                    {
                        continue;
                    }

                    var piece = new TrianglePiece { Triangle = triangle, Weights = new Vector3[clipped.Count] };
                    for (int i = 0; i < clipped.Count; i++)
                    {
                        piece.Weights[i] = clipped[i].Weights;
                    }

                    float area = ProjectedArea(piece, a, b, c);
                    if (Math.Abs(area) < MinPieceArea)
                    {
                        continue;
                    }

                    if ((area < 0f) != clockwise)
                    {
                        Array.Reverse(piece.Weights);
                    }

                    pieces[id].Add(piece);
                }
            }

            return pieces;
        }

        private static List<BaryPoint> ClipHalf(List<BaryPoint> subject, Vector2 e0, Vector2 e1)
        {
            Vector2 edge = e1 - e0;
            var output = new List<BaryPoint>(subject.Count + 2);
            for (int i = 0; i < subject.Count; i++)
            {
                BaryPoint current = subject[i];
                BaryPoint next = subject[(i + 1) % subject.Count];
                float sc = Poly.Cross(edge, current.Position - e0);
                float sn = Poly.Cross(edge, next.Position - e0);

                if (sc >= 0f)
                {
                    output.Add(current);
                }

                if ((sc > 0f && sn < 0f) || (sc < 0f && sn > 0f))
                {
                    output.Add(BaryPoint.Lerp(current, next, sc / (sc - sn)));
                }
            }

            return output;
        }

        private static void Chop(FractureLayout layout, List<TrianglePiece>[] pieces, int triangle,
            BaryPoint a, BaryPoint b, BaryPoint c, float maxLength, int depth)
        {
            float ab = Vector2.DistanceSquared(a.Position, b.Position);
            float bc = Vector2.DistanceSquared(b.Position, c.Position);
            float ca = Vector2.DistanceSquared(c.Position, a.Position);
            float longest = Math.Max(ab, Math.Max(bc, ca));

            if (longest <= maxLength * maxLength || depth >= MaxSplitDepth)
            {
                int owner = layout.FindCell(Clamp(layout, (a.Position + b.Position + c.Position) / 3f));
                if (owner >= 0)
                {
                    pieces[owner].Add(new TrianglePiece { Triangle = triangle, Weights = new[] { a.Weights, b.Weights, c.Weights } });
                }

                return;
            }

            if (longest == ab)
            {
                BaryPoint m = BaryPoint.Lerp(a, b, 0.5f);
                Chop(layout, pieces, triangle, a, m, c, maxLength, depth + 1);
                Chop(layout, pieces, triangle, m, b, c, maxLength, depth + 1);
            }
            else if (longest == bc)
            {
                BaryPoint m = BaryPoint.Lerp(b, c, 0.5f);
                Chop(layout, pieces, triangle, a, b, m, maxLength, depth + 1);
                Chop(layout, pieces, triangle, a, m, c, maxLength, depth + 1);
            }
            else
            {
                BaryPoint m = BaryPoint.Lerp(c, a, 0.5f);
                Chop(layout, pieces, triangle, a, b, m, maxLength, depth + 1);
                Chop(layout, pieces, triangle, m, b, c, maxLength, depth + 1);
            }
        }

        private static Vector2 Clamp(FractureLayout layout, Vector2 p)
        {
            return new Vector2(Math.Clamp(p.X, 0f, layout.Width), Math.Clamp(p.Y, 0f, layout.Height));
        }

        public static float Area(List<BaryPoint> polygon)
        {
            float twice = 0f;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                twice += Poly.Cross(polygon[j].Position, polygon[i].Position);
            }

            return twice * 0.5f;
        }

        public static float ProjectedArea(TrianglePiece piece, Vector2 a, Vector2 b, Vector2 c)
        {
            var points = new List<Vector2>(piece.Weights.Length);
            foreach (Vector3 w in piece.Weights)
            {
                points.Add(a * w.X + b * w.Y + c * w.Z);
            }

            return Poly.SignedArea(points);
        }
    }
}
