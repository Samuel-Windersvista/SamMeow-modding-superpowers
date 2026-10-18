using System;
using System.Collections.Generic;
using System.Numerics;

namespace DoorOverhaul.Destruction.Core
{
    public sealed class LayoutSettings
    {
        public float Width;
        public float Height;
        public float CellSize = 0.05f;
        public float Jitter = 0.85f;
        public uint Seed;
    }

    public sealed class Cell
    {
        public int Id;
        public int Column;
        public int Row;
        public Vector2 Site;
        public Vector2[] Polygon;
        public float Area;
        public Vector2 Centroid;
        public Vector2 Min;
        public Vector2 Max;
        public readonly List<int> Bonds = new List<int>();
    }

    public sealed class Bond
    {
        public int Id;
        public int A;
        public int B;
        public Vector2 P0;
        public Vector2 P1;
        public float Length;
        public Vector2 Mid => (P0 + P1) * 0.5f;

        public int Other(int cell)
        {
            return cell == A ? B : A;
        }
    }

    public sealed class FractureLayout
    {
        private const int NeighbourReach = 3;
        private const float EdgeTolerance = 1e-4f;

        public float Width { get; private set; }
        public float Height { get; private set; }
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public float CellWidth { get; private set; }
        public float CellHeight { get; private set; }
        public Cell[] Cells { get; private set; }
        public Bond[] Bonds { get; private set; }

        public static FractureLayout Generate(LayoutSettings settings)
        {
            if (settings.Width <= 0f || settings.Height <= 0f || settings.CellSize <= 0f)
            {
                throw new ArgumentException("Panel size and cell size must be positive");
            }

            var layout = new FractureLayout
            {
                Width = settings.Width,
                Height = settings.Height,
                Columns = Math.Max(1, (int)Math.Ceiling(settings.Width / settings.CellSize)),
                Rows = Math.Max(1, (int)Math.Ceiling(settings.Height / settings.CellSize))
            };
            layout.CellWidth = settings.Width / layout.Columns;
            layout.CellHeight = settings.Height / layout.Rows;

            float jitter = Math.Clamp(settings.Jitter, 0f, 0.98f);
            var rng = new Pcg32(settings.Seed);
            var sites = new Vector2[layout.Columns * layout.Rows];
            for (int row = 0; row < layout.Rows; row++)
            {
                for (int column = 0; column < layout.Columns; column++)
                {
                    float jx = 0.5f + (rng.NextFloat() - 0.5f) * jitter;
                    float jy = 0.5f + (rng.NextFloat() - 0.5f) * jitter;
                    sites[row * layout.Columns + column] = new Vector2((column + jx) * layout.CellWidth, (row + jy) * layout.CellHeight);
                }
            }

            var rect = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(settings.Width, 0f),
                new Vector2(settings.Width, settings.Height),
                new Vector2(0f, settings.Height)
            };

            layout.Cells = new Cell[sites.Length];
            var bonds = new List<Bond>();
            var bondIndex = new Dictionary<long, int>();

            for (int row = 0; row < layout.Rows; row++)
            {
                for (int column = 0; column < layout.Columns; column++)
                {
                    int id = row * layout.Columns + column;
                    Vector2 site = sites[id];
                    List<Vector2> polygon = rect;
                    var candidates = new List<int>();

                    for (int dr = -NeighbourReach; dr <= NeighbourReach; dr++)
                    {
                        for (int dc = -NeighbourReach; dc <= NeighbourReach; dc++)
                        {
                            int r = row + dr;
                            int c = column + dc;
                            if ((dr == 0 && dc == 0) || r < 0 || c < 0 || r >= layout.Rows || c >= layout.Columns)
                            {
                                continue;
                            }

                            int other = r * layout.Columns + c;
                            candidates.Add(other);
                            Vector2 normal = sites[other] - site;
                            float offset = (sites[other].LengthSquared() - site.LengthSquared()) * 0.5f;
                            polygon = Poly.ClipHalfPlane(polygon, normal, offset);
                        }
                    }

                    var cell = new Cell
                    {
                        Id = id,
                        Column = column,
                        Row = row,
                        Site = site,
                        Polygon = polygon.ToArray(),
                        Area = Poly.SignedArea(polygon),
                        Centroid = Poly.Centroid(polygon)
                    };
                    cell.Min = new Vector2(float.MaxValue, float.MaxValue);
                    cell.Max = new Vector2(float.MinValue, float.MinValue);
                    foreach (Vector2 p in cell.Polygon)
                    {
                        cell.Min = Vector2.Min(cell.Min, p);
                        cell.Max = Vector2.Max(cell.Max, p);
                    }

                    layout.Cells[id] = cell;
                    AddBonds(cell, candidates, sites, bonds, bondIndex);
                }
            }

            layout.Bonds = bonds.ToArray();
            foreach (Bond bond in layout.Bonds)
            {
                layout.Cells[bond.A].Bonds.Add(bond.Id);
                layout.Cells[bond.B].Bonds.Add(bond.Id);
            }

            return layout;
        }

        private static void AddBonds(Cell cell, List<int> candidates, Vector2[] sites, List<Bond> bonds, Dictionary<long, int> bondIndex)
        {
            Vector2[] polygon = cell.Polygon;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 p0 = polygon[i];
                Vector2 p1 = polygon[(i + 1) % polygon.Length];
                float length = Vector2.Distance(p0, p1);
                if (length < EdgeTolerance)
                {
                    continue;
                }

                foreach (int other in candidates)
                {
                    Vector2 normal = sites[other] - cell.Site;
                    float normalLength = normal.Length();
                    float offset = (sites[other].LengthSquared() - cell.Site.LengthSquared()) * 0.5f;
                    if (Math.Abs(Vector2.Dot(normal, p0) - offset) / normalLength > EdgeTolerance
                        || Math.Abs(Vector2.Dot(normal, p1) - offset) / normalLength > EdgeTolerance)
                    {
                        continue;
                    }

                    int a = Math.Min(cell.Id, other);
                    int b = Math.Max(cell.Id, other);
                    long key = ((long)a << 32) | (uint)b;
                    if (!bondIndex.ContainsKey(key))
                    {
                        bondIndex[key] = bonds.Count;
                        bonds.Add(new Bond { Id = bonds.Count, A = a, B = b, P0 = p0, P1 = p1, Length = length });
                    }

                    break;
                }
            }
        }

        public IEnumerable<int> CellsNear(Vector2 point, float radius)
        {
            int reachX = (int)Math.Ceiling(radius / CellWidth) + 1;
            int reachY = (int)Math.Ceiling(radius / CellHeight) + 1;
            int column = Math.Clamp((int)(point.X / CellWidth), 0, Columns - 1);
            int row = Math.Clamp((int)(point.Y / CellHeight), 0, Rows - 1);

            for (int r = Math.Max(0, row - reachY); r <= Math.Min(Rows - 1, row + reachY); r++)
            {
                for (int c = Math.Max(0, column - reachX); c <= Math.Min(Columns - 1, column + reachX); c++)
                {
                    yield return r * Columns + c;
                }
            }
        }

        public IEnumerable<int> CellsOverlapping(Vector2 min, Vector2 max)
        {
            int c0 = Math.Clamp((int)(min.X / CellWidth) - 1, 0, Columns - 1);
            int c1 = Math.Clamp((int)(max.X / CellWidth) + 1, 0, Columns - 1);
            int r0 = Math.Clamp((int)(min.Y / CellHeight) - 1, 0, Rows - 1);
            int r1 = Math.Clamp((int)(max.Y / CellHeight) + 1, 0, Rows - 1);

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    Cell cell = Cells[r * Columns + c];
                    if (cell.Max.X >= min.X && cell.Min.X <= max.X && cell.Max.Y >= min.Y && cell.Min.Y <= max.Y)
                    {
                        yield return cell.Id;
                    }
                }
            }
        }

        public int FindCell(Vector2 point)
        {
            Vector2 clamped = Vector2.Clamp(point, Vector2.Zero, new Vector2(Width, Height));
            int best = -1;
            float bestDistance = float.MaxValue;
            foreach (int id in CellsNear(clamped, Math.Max(CellWidth, CellHeight)))
            {
                float distance = Vector2.DistanceSquared(Cells[id].Site, clamped);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = id;
                }
            }

            return best;
        }
    }
}
