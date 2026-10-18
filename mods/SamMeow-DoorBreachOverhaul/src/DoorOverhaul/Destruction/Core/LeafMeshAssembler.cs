using System;
using System.Collections.Generic;
using System.Numerics;

namespace DoorOverhaul.Destruction.Core
{
    public sealed class SourceMesh
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector4[] Tangents;
        public Vector2[] Uv0;
        public Vector2[] Uv1;
        public int[][] Submeshes;
    }

    public sealed class MeshData
    {
        public readonly List<Vector3> Positions = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector4> Tangents = new List<Vector4>();
        public readonly List<Vector2> Uv0 = new List<Vector2>();
        public readonly List<Vector2> Uv1 = new List<Vector2>();
        public List<int>[] Submeshes;
    }

    public sealed class LeafMeshAssembler
    {
        private sealed class CellGeometry
        {
            public readonly List<Vector3> Positions = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector4> Tangents = new List<Vector4>();
            public readonly List<Vector2> Uv0 = new List<Vector2>();
            public readonly List<Vector2> Uv1 = new List<Vector2>();
            public readonly List<Vector2> Panel = new List<Vector2>();
            public readonly List<float> Depth = new List<float>();
            public List<int>[] Submeshes;
        }

        private readonly FractureLayout _layout;
        private readonly CellGeometry[] _cells;
        private readonly bool _normals;
        private readonly bool _tangents;
        private readonly bool _uv0;
        private readonly bool _uv1;
        private readonly int _submeshCount;

        private readonly Matrix4x4 _panelToMesh;
        private readonly float _rimDepth;

        public int RimSubmesh { get; set; }
        public int SourceTriangles { get; }
        public int PreparedVertices { get; }

        public LeafMeshAssembler(FractureLayout layout, SourceMesh mesh, Func<Vector3, Vector3> toPanel, Matrix4x4 panelToMesh, float rimDepth)
        {
            _layout = layout;
            _panelToMesh = panelToMesh;
            _rimDepth = rimDepth;
            _submeshCount = mesh.Submeshes.Length;
            _normals = mesh.Normals != null && mesh.Normals.Length == mesh.Positions.Length;
            _tangents = mesh.Tangents != null && mesh.Tangents.Length == mesh.Positions.Length;
            _uv0 = mesh.Uv0 != null && mesh.Uv0.Length == mesh.Positions.Length;
            _uv1 = mesh.Uv1 != null && mesh.Uv1.Length == mesh.Positions.Length;

            var panel = new Vector2[mesh.Positions.Length];
            var depth = new float[mesh.Positions.Length];
            for (int i = 0; i < panel.Length; i++)
            {
                Vector3 p = toPanel(mesh.Positions[i]);
                panel[i] = new Vector2(p.X, p.Y);
                depth[i] = p.Z;
            }

            _cells = new CellGeometry[layout.Cells.Length];
            for (int c = 0; c < _cells.Length; c++)
            {
                _cells[c] = new CellGeometry { Submeshes = new List<int>[_submeshCount] };
                for (int s = 0; s < _submeshCount; s++)
                {
                    _cells[c].Submeshes[s] = new List<int>();
                }
            }

            int triangles = 0;
            for (int s = 0; s < _submeshCount; s++)
            {
                int[] indices = mesh.Submeshes[s];
                triangles += indices.Length / 3;
                List<TrianglePiece>[] pieces = LeafPartition.Build(layout, panel, indices);
                for (int c = 0; c < pieces.Length; c++)
                {
                    foreach (TrianglePiece piece in pieces[c])
                    {
                        AddPiece(_cells[c], s, mesh, indices, piece, panel, depth);
                    }
                }
            }

            SourceTriangles = triangles;
            int vertices = 0;
            foreach (CellGeometry cell in _cells)
            {
                vertices += cell.Positions.Count;
            }

            PreparedVertices = vertices;
        }

        private void AddPiece(CellGeometry cell, int submesh, SourceMesh mesh, int[] indices, TrianglePiece piece, Vector2[] panel, float[] depth)
        {
            int i0 = indices[piece.Triangle * 3];
            int i1 = indices[piece.Triangle * 3 + 1];
            int i2 = indices[piece.Triangle * 3 + 2];
            int first = cell.Positions.Count;

            foreach (Vector3 w in piece.Weights)
            {
                cell.Positions.Add(mesh.Positions[i0] * w.X + mesh.Positions[i1] * w.Y + mesh.Positions[i2] * w.Z);
                cell.Panel.Add(panel[i0] * w.X + panel[i1] * w.Y + panel[i2] * w.Z);
                cell.Depth.Add(depth[i0] * w.X + depth[i1] * w.Y + depth[i2] * w.Z);
                if (_normals)
                {
                    cell.Normals.Add(SafeNormalize(mesh.Normals[i0] * w.X + mesh.Normals[i1] * w.Y + mesh.Normals[i2] * w.Z));
                }

                if (_tangents)
                {
                    Vector4 t = mesh.Tangents[i0] * w.X + mesh.Tangents[i1] * w.Y + mesh.Tangents[i2] * w.Z;
                    Vector3 xyz = SafeNormalize(new Vector3(t.X, t.Y, t.Z));
                    cell.Tangents.Add(new Vector4(xyz, mesh.Tangents[i0].W < 0f ? -1f : 1f));
                }

                if (_uv0)
                {
                    cell.Uv0.Add(mesh.Uv0[i0] * w.X + mesh.Uv0[i1] * w.Y + mesh.Uv0[i2] * w.Z);
                }

                if (_uv1)
                {
                    cell.Uv1.Add(mesh.Uv1[i0] * w.X + mesh.Uv1[i1] * w.Y + mesh.Uv1[i2] * w.Z);
                }
            }

            List<int> target = cell.Submeshes[submesh];
            for (int k = 1; k + 1 < piece.Weights.Length; k++)
            {
                target.Add(first);
                target.Add(first + k);
                target.Add(first + k + 1);
            }
        }

        public MeshData Build(IReadOnlyList<bool> visible)
        {
            var data = new MeshData { Submeshes = new List<int>[_submeshCount] };
            for (int s = 0; s < _submeshCount; s++)
            {
                data.Submeshes[s] = new List<int>();
            }

            for (int c = 0; c < _cells.Length; c++)
            {
                if (!visible[c])
                {
                    continue;
                }

                CellGeometry cell = _cells[c];
                int offset = data.Positions.Count;
                data.Positions.AddRange(cell.Positions);
                data.Normals.AddRange(cell.Normals);
                data.Tangents.AddRange(cell.Tangents);
                data.Uv0.AddRange(cell.Uv0);
                data.Uv1.AddRange(cell.Uv1);
                for (int s = 0; s < _submeshCount; s++)
                {
                    foreach (int index in cell.Submeshes[s])
                    {
                        data.Submeshes[s].Add(offset + index);
                    }
                }
            }

            if (_rimDepth > 0f)
            {
                AddRims(data, visible);
            }

            return data;
        }

        private readonly Dictionary<int, MeshData> _edgeCache = new Dictionary<int, MeshData>();

        private void AddRims(MeshData data, IReadOnlyList<bool> visible)
        {
            List<int> target = data.Submeshes[Math.Clamp(RimSubmesh, 0, _submeshCount - 1)];
            foreach (Bond bond in _layout.Bonds)
            {
                bool a = visible[bond.A];
                bool b = visible[bond.B];
                if (a == b)
                {
                    continue;
                }

                int key = bond.Id * 2 + (a ? 0 : 1);
                if (!_edgeCache.TryGetValue(key, out MeshData edge))
                {
                    edge = new MeshData { Submeshes = new[] { new List<int>() } };
                    BuildEdge(edge, edge.Submeshes[0], bond, a ? bond.A : bond.B);
                    _edgeCache[key] = edge;
                }

                int offset = data.Positions.Count;
                data.Positions.AddRange(edge.Positions);
                data.Normals.AddRange(edge.Normals);
                data.Tangents.AddRange(edge.Tangents);
                data.Uv0.AddRange(edge.Uv0);
                data.Uv1.AddRange(edge.Uv1);
                foreach (int index in edge.Submeshes[0])
                {
                    target.Add(offset + index);
                }
            }
        }

        private void BuildEdge(MeshData data, List<int> target, Bond bond, int keep)
        {
            {
                Vector2 centre = _layout.Cells[keep].Centroid;

                Vector2 along = bond.P1 - bond.P0;
                if (along.LengthSquared() < 1e-10f)
                {
                    return;
                }

                Vector2 outward = Vector2.Normalize(new Vector2(along.Y, -along.X));
                if (Vector2.Dot(outward, bond.Mid - centre) < 0f)
                {
                    outward = -outward;
                }

                Vector2 p0 = bond.P0;
                Vector2 p1 = bond.P1;
                Vector3 normal = SafeNormalize(Vector3.TransformNormal(new Vector3(outward, 0f), _panelToMesh));
                Vector3 tangentDir = SafeNormalize(Vector3.TransformNormal(new Vector3(Vector2.Normalize(p1 - p0), 0f), _panelToMesh));
                Vector2 uv0a = NearestUv(_cells[keep], p0, true);
                Vector2 uv0b = NearestUv(_cells[keep], p1, true);
                Vector2 uv1a = NearestUv(_cells[keep], p0, false);
                Vector2 uv1b = NearestUv(_cells[keep], p1, false);

                int first = data.Positions.Count;
                AddRimVertex(data, p0, -_rimDepth, normal, tangentDir, uv0a, uv1a);
                AddRimVertex(data, p1, -_rimDepth, normal, tangentDir, uv0b, uv1b);
                AddRimVertex(data, p1, _rimDepth, normal, tangentDir, uv0b + new Vector2(0f, 0.01f), uv1b);
                AddRimVertex(data, p0, _rimDepth, normal, tangentDir, uv0a + new Vector2(0f, 0.01f), uv1a);

                Vector3 v0 = data.Positions[first], v1 = data.Positions[first + 1], v2 = data.Positions[first + 2];
                if (Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), normal) >= 0f)
                {
                    target.Add(first); target.Add(first + 1); target.Add(first + 2);
                    target.Add(first); target.Add(first + 2); target.Add(first + 3);
                }
                else
                {
                    target.Add(first); target.Add(first + 2); target.Add(first + 1);
                    target.Add(first); target.Add(first + 3); target.Add(first + 2);
                }

                if (Splinters)
                {
                    AddSplinters(data, target, bond, keep, p0, p1, outward);
                }
            }
        }

        public bool Splinters { get; set; } = true;

        private void AddSplinters(MeshData data, List<int> target, Bond bond, int keep, Vector2 p0, Vector2 p1, Vector2 outward)
        {
            float length = Vector2.Distance(p0, p1);
            if (length < 0.004f)
            {
                return;
            }

            var rng = new Pcg32(((ulong)(uint)bond.Id << 8) ^ 0x5bd1e995UL);
            int teeth = length > 0.035f ? 2 + (rng.NextFloat() < 0.4f ? 1 : 0) : length > 0.015f ? 1 + (rng.NextFloat() < 0.5f ? 1 : 0) : 1;

            var cuts = new float[teeth + 1];
            cuts[teeth] = 1f;
            for (int i = 1; i < teeth; i++)
            {
                cuts[i] = (i + (rng.NextFloat() - 0.5f) * 0.6f) / teeth;
            }

            for (int i = 0; i < teeth; i++)
            {
                Vector2 b0 = Vector2.Lerp(p0, p1, cuts[i]);
                Vector2 b1 = Vector2.Lerp(p0, p1, cuts[i + 1]);
                float width = (cuts[i + 1] - cuts[i]) * length;
                float reach = Math.Min(0.04f, width * (0.7f + 1.5f * rng.NextFloat()));
                Vector2 apex = Vector2.Lerp(b0, b1, 0.2f + 0.6f * rng.NextFloat())
                    + outward * reach
                    + Vector2.UnitY * ((rng.NextFloat() - 0.5f) * reach * 0.8f);

                float centre = (rng.NextFloat() - 0.5f) * _rimDepth * 0.8f;
                float half = _rimDepth * (0.3f + 0.5f * rng.NextFloat());
                float top = Math.Min(_rimDepth * 0.97f, centre + half);
                float bottom = Math.Max(-_rimDepth * 0.97f, centre - half);
                AddTooth(data, target, _cells[keep], b0, b1, apex, top, bottom);
            }
        }

        private void AddTooth(MeshData data, List<int> target, CellGeometry cell, Vector2 b0, Vector2 b1, Vector2 apex, float top, float bottom)
        {
            Vector3 up = SafeNormalize(Vector3.TransformNormal(Vector3.UnitZ, _panelToMesh));
            Vector3 tangent = SafeNormalize(Vector3.TransformNormal(Vector3.UnitX, _panelToMesh));
            Vector2[] corners = { b0, b1, apex };

            foreach ((float depth, bool front) in new[] { (top, true), (bottom, false) })
            {
                int first = data.Positions.Count;
                foreach (Vector2 p in corners)
                {
                    SampleUv(cell, p, front, out Vector2 uv0, out Vector2 uv1);
                    AddVertex(data, new Vector3(p, depth), front ? up : -up, tangent, uv0, uv1);
                }

                AddFacing(data, target, first, 3, front ? up : -up);
            }

            foreach ((Vector2 a, Vector2 b) in new[] { (b0, apex), (apex, b1) })
            {
                Vector2 d = b - a;
                Vector2 side = new Vector2(d.Y, -d.X);
                if (Vector2.Dot(side, (a + b) * 0.5f - (b0 + b1 + apex) / 3f) < 0f)
                {
                    side = -side;
                }

                Vector3 normal = SafeNormalize(Vector3.TransformNormal(new Vector3(side, 0f), _panelToMesh));
                Vector3 along = SafeNormalize(Vector3.TransformNormal(new Vector3(d, 0f), _panelToMesh));
                SampleUv(cell, a, true, out Vector2 ua0, out Vector2 ua1);
                SampleUv(cell, b, true, out Vector2 ub0, out Vector2 ub1);
                Vector2 grain = new Vector2(0f, 0.01f);

                int first = data.Positions.Count;
                AddVertex(data, new Vector3(a, bottom), normal, along, ua0, ua1);
                AddVertex(data, new Vector3(b, bottom), normal, along, ub0, ub1);
                AddVertex(data, new Vector3(b, top), normal, along, ub0 + grain, ub1);
                AddVertex(data, new Vector3(a, top), normal, along, ua0 + grain, ua1);
                AddFacing(data, target, first, 4, normal);
            }
        }

        private static void AddFacing(MeshData data, List<int> target, int first, int count, Vector3 facing)
        {
            Vector3 a = data.Positions[first], b = data.Positions[first + 1], c = data.Positions[first + 2];
            bool forward = Vector3.Dot(Vector3.Cross(b - a, c - a), facing) >= 0f;
            for (int k = 1; k + 1 < count; k++)
            {
                target.Add(first);
                target.Add(forward ? first + k : first + k + 1);
                target.Add(forward ? first + k + 1 : first + k);
            }
        }

        private void AddVertex(MeshData data, Vector3 panel, Vector3 normal, Vector3 tangent, Vector2 uv0, Vector2 uv1)
        {
            data.Positions.Add(Vector3.Transform(panel, _panelToMesh));
            if (_normals)
            {
                data.Normals.Add(normal);
            }

            if (_tangents)
            {
                data.Tangents.Add(new Vector4(tangent, 1f));
            }

            if (_uv0)
            {
                data.Uv0.Add(uv0);
            }

            if (_uv1)
            {
                data.Uv1.Add(uv1);
            }
        }

        private void SampleUv(CellGeometry cell, Vector2 point, bool front, out Vector2 uv0, out Vector2 uv1)
        {
            uv0 = Vector2.Zero;
            uv1 = Vector2.Zero;
            if (!_uv0 && !_uv1)
            {
                return;
            }

            int i0 = -1, i1 = -1, i2 = -1;
            float d0 = float.MaxValue, d1 = float.MaxValue, d2 = float.MaxValue;
            for (int i = 0; i < cell.Panel.Count; i++)
            {
                if ((cell.Depth[i] >= 0f) != front)
                {
                    continue;
                }

                float d = Vector2.DistanceSquared(cell.Panel[i], point);
                if (d < d0)
                {
                    (i2, d2) = (i1, d1);
                    (i1, d1) = (i0, d0);
                    (i0, d0) = (i, d);
                }
                else if (d < d1 && !Same(cell, i, i0))
                {
                    (i2, d2) = (i1, d1);
                    (i1, d1) = (i, d);
                }
                else if (d < d2 && !Same(cell, i, i0) && !Same(cell, i, i1))
                {
                    (i2, d2) = (i, d);
                }
            }

            if (i0 < 0)
            {
                uv0 = NearestUv(cell, point, true);
                uv1 = NearestUv(cell, point, false);
                return;
            }

            if (i2 < 0)
            {
                uv0 = _uv0 ? cell.Uv0[i0] : Vector2.Zero;
                uv1 = _uv1 ? cell.Uv1[i0] : Vector2.Zero;
                return;
            }

            Vector2 a = cell.Panel[i0], b = cell.Panel[i1], c = cell.Panel[i2];
            float area = Poly.Cross(b - a, c - a);
            if (Math.Abs(area) < 1e-9f)
            {
                uv0 = _uv0 ? cell.Uv0[i0] : Vector2.Zero;
                uv1 = _uv1 ? cell.Uv1[i0] : Vector2.Zero;
                return;
            }

            float wb = Math.Clamp(Poly.Cross(point - a, c - a) / area, -1f, 2f);
            float wc = Math.Clamp(Poly.Cross(b - a, point - a) / area, -1f, 2f);
            float wa = 1f - wb - wc;
            if (_uv0)
            {
                uv0 = cell.Uv0[i0] * wa + cell.Uv0[i1] * wb + cell.Uv0[i2] * wc;
            }

            if (_uv1)
            {
                uv1 = cell.Uv1[i0] * wa + cell.Uv1[i1] * wb + cell.Uv1[i2] * wc;
            }
        }

        private static bool Same(CellGeometry cell, int i, int j)
        {
            return j >= 0 && Vector2.DistanceSquared(cell.Panel[i], cell.Panel[j]) < 1e-10f;
        }

        private void AddRimVertex(MeshData data, Vector2 panel, float depth, Vector3 normal, Vector3 tangent, Vector2 uv0, Vector2 uv1)
        {
            data.Positions.Add(Vector3.Transform(new Vector3(panel, depth), _panelToMesh));
            if (_normals)
            {
                data.Normals.Add(normal);
            }

            if (_tangents)
            {
                data.Tangents.Add(new Vector4(tangent, 1f));
            }

            if (_uv0)
            {
                data.Uv0.Add(uv0);
            }

            if (_uv1)
            {
                data.Uv1.Add(uv1);
            }
        }

        private Vector2 NearestUv(CellGeometry cell, Vector2 point, bool first)
        {
            List<Vector2> uvs = first ? cell.Uv0 : cell.Uv1;
            if (uvs.Count == 0)
            {
                return Vector2.Zero;
            }

            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < cell.Panel.Count; i++)
            {
                float d = Vector2.DistanceSquared(cell.Panel[i], point);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }

            return uvs[best];
        }

        private static Vector3 SafeNormalize(Vector3 v)
        {
            float length = v.Length();
            return length > 1e-8f ? v / length : Vector3.UnitZ;
        }
    }
}
