using DoorBreach;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DoorOverhaul.Destruction.Core;
using UnityEngine;
using NMatrix = System.Numerics.Matrix4x4;
using NVector2 = System.Numerics.Vector2;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;

namespace DoorOverhaul.Destruction
{
    internal sealed class LeafVisual
    {
        private const float MinRebuildInterval = 0.1f;

        private sealed class Target
        {
            public MeshFilter Filter;
            public Mesh Original;
            public Mesh Replacement;
            public Task<LeafMeshAssembler> Prepare;
            public LeafMeshAssembler Assembler;
            public string Error;
        }

        private readonly DoorEntry _entry;
        private readonly List<Target> _targets = new List<Target>();
        private MeshCollider _collider;
        private Mesh _originalColliderMesh;
        private Mesh _colliderMesh;
        private bool _dirty = true;
        private float _nextBuild;
        private bool[] _visible;

        private bool _leafEvent;
        private Vector3 _leafMomentum;

        private readonly List<(DetachedGroup Group, Vector3 Momentum)> _pendingPieces = new List<(DetachedGroup, Vector3)>();
        private readonly HashSet<int> _pendingCells = new HashSet<int>();

        public int PiecesSpawned { get; private set; }
        public int DebrisBursts { get; private set; }

        public bool HasHole { get; private set; }
        public float LastBuildMs { get; private set; }
        public int Vertices { get; private set; }
        public string Status { get; private set; } = "reading meshes";

        public LeafVisual(DoorEntry entry)
        {
            _entry = entry;
            Start();
        }

        private void Start()
        {
            foreach (MeshFilter filter in LeafFrame.LeafMeshes(_entry.Door))
            {
                AddTarget(filter);
            }

            BindCollider(LeafFrame.PrimaryCollider(_entry.Door));
        }

        public void OnLeafSwapped()
        {
            if (_entry.Takeover != null)
            {
                return;
            }

            SwapLeafModel();
        }

        private void SwapLeafModel()
        {
            var active = new HashSet<MeshFilter>(LeafFrame.LeafMeshes(_entry.Door));
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                Target target = _targets[i];
                if (target.Filter == null || !active.Contains(target.Filter))
                {
                    if (target.Filter != null && target.Original != null)
                    {
                        target.Filter.sharedMesh = target.Original;
                    }

                    if (target.Replacement != null)
                    {
                        UnityEngine.Object.Destroy(target.Replacement);
                    }

                    _targets.RemoveAt(i);
                }
            }

            foreach (MeshFilter filter in active)
            {
                if (!_targets.Exists(t => t.Filter == filter))
                {
                    AddTarget(filter);
                }
            }

            Collider collider = LeafFrame.PrimaryCollider(_entry.Door);
            if (collider != _collider)
            {
                if (_collider != null && _originalColliderMesh != null)
                {
                    _collider.sharedMesh = _originalColliderMesh;
                }

                _collider = null;
                _originalColliderMesh = null;
                BindCollider(collider);
            }

            _dirty = true;
        }

        private void AddTarget(MeshFilter filter)
        {
            Transform door = _entry.Door.transform;
            LeafFrame frame = _entry.Frame;
            FractureLayout layout = _entry.State.Layout;
            Matrix4x4 panelToDoor = PanelToDoor(frame);

            var target = new Target { Filter = filter, Original = filter.sharedMesh };
            _targets.Add(target);

            Matrix4x4 meshToDoor = door.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Matrix4x4 meshToPanel = panelToDoor.inverse * meshToDoor;
            NMatrix toPanel = ToNumerics(meshToPanel);
            NMatrix panelToMesh = ToNumerics(meshToDoor.inverse * panelToDoor);
            float rim = Mathf.Clamp(frame.Thickness * 0.5f, 0.01f, 0.05f);

            MeshReadback.Read(target.Original, (copy, error) =>
            {
                string problem = error ?? MeshReadback.Validate(target.Original, copy);
                if (problem != null)
                {
                    target.Error = problem;
                    DoorBreachPlugin.Logger.LogWarning($"[EDI-Visual] {_entry.Door.Id}: reading {target.Original.name} failed: {problem}");
                    return;
                }

                SourceMesh source = ToSource(copy);
                bool splinters = _entry.Profile == null || _entry.Profile.AllowChunks;
                target.Prepare = Task.Run(() => new LeafMeshAssembler(layout, source, p => NVector3.Transform(p, toPanel), panelToMesh, rim) { Splinters = splinters });
            });
        }

        private void BindCollider(Collider collider)
        {
            if (collider is MeshCollider meshCollider)
            {
                _collider = meshCollider;
                _originalColliderMesh = meshCollider.sharedMesh;
            }
            else if (collider != null)
            {
                DoorBreachPlugin.Logger.LogWarning(
                    $"[EDI-Visual] {_entry.Door.Id}: ballistic collider is a {collider.GetType().Name}; holes are visual only on this door");
            }
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public void Tick()
        {
            foreach (Target target in _targets)
            {
                if (target.Prepare == null || !target.Prepare.IsCompleted)
                {
                    continue;
                }

                if (target.Prepare.IsFaulted)
                {
                    target.Error = target.Prepare.Exception?.GetBaseException().Message ?? "failed";
                    DoorBreachPlugin.Logger.LogError($"[EDI-Visual] {_entry.Door.Id}: cutting {target.Original.name} failed: {target.Prepare.Exception}");
                }
                else
                {
                    target.Assembler = target.Prepare.Result;
                }

                target.Prepare = null;
                _dirty = true;
            }

            if (!_dirty || Time.time < _nextBuild)
            {
                return;
            }

            _dirty = false;
            _nextBuild = Time.time + MinRebuildInterval;
            Build();
        }

        public void OnGroups(DestructionResult result, Vector3 momentum)
        {
            foreach (DetachedGroup group in result.Groups)
            {
                if (group.IsLeafBody)
                {
                    continue;
                }

                _pendingPieces.Add((group, momentum));
                foreach (int cell in group.Cells)
                {
                    _pendingCells.Add(cell);
                }
            }

            if (result.BecameHanging || result.LeafDetached)
            {
                _leafEvent = true;
                _leafMomentum = momentum;
            }

            _dirty = true;
        }

        private void Build()
        {
            FractureState state = _entry.State;
            int count = state.Layout.Cells.Length;
            if (_visible == null)
            {
                _visible = new bool[count];
            }

            if (_pendingPieces.Count > 0 && _targets.TrueForAll(t => t.Prepare == null))
            {
                foreach ((DetachedGroup group, Vector3 momentum) in _pendingPieces)
                {
                    try
                    {
                        SpawnPiece(group, momentum);
                    }
                    catch (Exception ex)
                    {
                        DoorBreachPlugin.Logger.LogError($"[EDI-Fragments] {_entry.Door.Id}: spawning a piece failed: {ex}");
                    }
                }

                _pendingPieces.Clear();
                _pendingCells.Clear();
            }

            bool hole = false;
            for (int c = 0; c < count; c++)
            {
                _visible[c] = state.IsPartOfLeaf(c) || _pendingCells.Contains(c);
                hole |= !_visible[c];
            }

            var timer = Stopwatch.StartNew();
            int vertices = 0;
            int ready = 0;
            foreach (Target target in _targets)
            {
                if (target.Assembler == null || target.Filter == null)
                {
                    continue;
                }

                MeshData data = target.Assembler.Build(_visible);
                if (target.Replacement == null)
                {
                    target.Replacement = new Mesh { name = target.Original.name + " (damaged)" };
                    target.Replacement.MarkDynamic();
                }

                Apply(target.Replacement, data, target.Original.bounds);
                target.Filter.sharedMesh = target.Replacement;
                vertices += data.Positions.Count;
                ready++;
            }

            if (_leafEvent && _targets.TrueForAll(t => t.Prepare == null))
            {
                _leafEvent = false;
                try
                {
                    HandleLeaf(state);
                }
                catch (Exception ex)
                {
                    DoorBreachPlugin.Logger.LogError($"[EDI-Leaf] {_entry.Door.Id}: taking over the leaf failed: {ex}");
                }
            }

            if (_entry.Takeover != null)
            {
                _entry.Takeover.RebuildColliders(state);
            }
            else if (_collider != null && hole)
            {
                BuildCollider(state);
            }

            if (hole && !HasHole)
            {
                HasHole = true;
                _entry.Door.CheckOcclusionPortal();
            }

            LastBuildMs = (float)timer.Elapsed.TotalMilliseconds;
            Vertices = vertices;
            int failed = _targets.FindAll(t => t.Error != null).Count;
            int waiting = _targets.Count - ready - failed;
            Status = $"{ready}/{_targets.Count} meshes cut" + (waiting > 0 ? $", {waiting} preparing" : string.Empty) +
                (failed > 0 ? $", {failed} failed" : string.Empty) + (_collider != null ? ", collider swapped" : ", collider unchanged");
        }

        private void HandleLeaf(FractureState state)
        {
            if (_entry.Takeover == null)
            {
                if (state.Leaf == LeafStatus.Attached)
                {
                    return;
                }

                Anchor hinge = null;
                if (state.Leaf == LeafStatus.Hanging)
                {
                    foreach (Anchor anchor in state.Anchors)
                    {
                        if (anchor.Spec.Kind == AnchorKind.Hinge && !anchor.Failed)
                        {
                            hinge = anchor;
                        }
                    }
                }

                var filters = new List<MeshFilter>();
                foreach (Target target in _targets)
                {
                    if (target.Filter != null)
                    {
                        filters.Add(target.Filter);
                    }
                }

                _entry.Takeover = new DoorTakeover(_entry, filters, Retarget, hinge, _leafMomentum);
            }
            else if (state.Leaf == LeafStatus.Detached && _entry.Takeover.Hanging)
            {
                _entry.Takeover.Release(_leafMomentum);
            }
        }

        private void Retarget(MeshFilter from, MeshFilter to)
        {
            Target target = _targets.Find(t => t.Filter == from);
            if (target != null)
            {
                target.Filter = to;
                if (target.Replacement != null)
                {
                    to.sharedMesh = target.Replacement;
                }
            }
        }

        private const float PushBoost = 2f;

        private void SpawnPiece(DetachedGroup group, Vector3 momentum)
        {
            Transform door = _entry.LeafTransform;
            LeafFrame frame = _entry.Frame;
            FractureState state = _entry.State;
            Vector3 normal = frame.NormalWorld(door);
            if (Vector3.Dot(momentum, normal) < 0f)
            {
                normal = -normal;
            }

            Vector3 centre = frame.ToWorld(door, group.Centroid);
            if (group.IsDebris)
            {
                DestructionFragments.Burst(_entry.Material, centre, normal, 0.5f);
                DebrisBursts++;
                return;
            }

            var cells = new bool[state.Layout.Cells.Length];
            foreach (int cell in group.Cells)
            {
                cells[cell] = true;
            }

            var parts = new List<(Mesh, MeshRenderer, Matrix4x4)>();
            foreach (Target target in _targets)
            {
                MeshRenderer renderer = target.Filter != null ? target.Filter.GetComponent<MeshRenderer>() : null;
                if (target.Assembler == null || renderer == null)
                {
                    continue;
                }

                MeshData data = target.Assembler.Build(cells);
                if (data.Positions.Count == 0)
                {
                    continue;
                }

                var mesh = new Mesh { name = target.Original.name + " (piece)" };
                Apply(mesh, data, null);
                parts.Add((mesh, renderer, target.Filter.transform.localToWorldMatrix));
            }

            if (parts.Count == 0)
            {
                DestructionFragments.Burst(_entry.Material, centre, normal, 0.5f);
                DebrisBursts++;
                return;
            }

            Matrix4x4 rootPose = Matrix4x4.TRS(door.position, door.rotation, Vector3.one);
            Matrix4x4 panelToRoot = rootPose.inverse * door.localToWorldMatrix * PanelToDoor(frame);
            (List<NVector3> positions, List<int> triangles) = SlabBuilder.Build(state.Layout, cells, frame.Thickness * 0.5f);
            var vertices = new List<Vector3>(positions.Count);
            foreach (NVector3 p in positions)
            {
                vertices.Add(panelToRoot.MultiplyPoint3x4(new Vector3(p.X, p.Y, p.Z)));
            }

            var colliderMesh = new Mesh { name = "door piece collider" };
            colliderMesh.SetVertices(vertices);
            colliderMesh.SetTriangles(triangles, 0);
            colliderMesh.RecalculateBounds();

            float mass = group.Area * frame.Thickness * state.Profile.Density;

            Vector3 velocity = momentum * PushBoost / Mathf.Max(0.05f, mass);
            velocity += normal * 0.4f + UnityEngine.Random.insideUnitSphere * 0.3f;
            Vector3 spin = UnityEngine.Random.insideUnitSphere * 4f;

            DestructionFragments.Spawn($"EDI piece {_entry.Door.Id}", parts, colliderMesh, rootPose, mass, velocity, spin,
                door.GetComponentsInChildren<Collider>(true));
            DestructionFragments.Burst(_entry.Material, centre, normal, 0.35f);
            PiecesSpawned++;
        }

        private void BuildCollider(FractureState state)
        {
            LeafFrame frame = _entry.Frame;
            (List<NVector3> positions, List<int> triangles) = SlabBuilder.Build(state.Layout, _visible, frame.Thickness * 0.5f);

            Matrix4x4 panelToCollider = _collider.transform.worldToLocalMatrix * _entry.Door.transform.localToWorldMatrix * PanelToDoor(frame);
            var vertices = new List<Vector3>(positions.Count);
            foreach (NVector3 p in positions)
            {
                vertices.Add(panelToCollider.MultiplyPoint3x4(new Vector3(p.X, p.Y, p.Z)));
            }

            if (panelToCollider.determinant < 0f)
            {
                for (int i = 0; i + 2 < triangles.Count; i += 3)
                {
                    (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
                }
            }

            if (_colliderMesh == null)
            {
                _colliderMesh = new Mesh { name = _originalColliderMesh != null ? _originalColliderMesh.name + " (damaged)" : "door collider (damaged)" };
            }

            _colliderMesh.Clear();
            _colliderMesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            _colliderMesh.SetVertices(vertices);
            _colliderMesh.SetTriangles(triangles, 0);
            _colliderMesh.RecalculateBounds();

            _collider.sharedMesh = null;
            _collider.sharedMesh = _colliderMesh;
        }

        public void TickLeaf()
        {
            _entry.Takeover?.Tick();
        }

        public void Restore()
        {
            foreach (Target target in _targets)
            {
                if (target.Filter != null && target.Original != null)
                {
                    target.Filter.sharedMesh = target.Original;
                }

                if (target.Replacement != null)
                {
                    UnityEngine.Object.Destroy(target.Replacement);
                }
            }

            if (_collider != null && _originalColliderMesh != null)
            {
                _collider.sharedMesh = _originalColliderMesh;
            }

            if (_colliderMesh != null)
            {
                UnityEngine.Object.Destroy(_colliderMesh);
            }

            _targets.Clear();
            if (_entry.Takeover != null)
            {
                _entry.Takeover.Destroy();
                _entry.Takeover = null;
            }
        }

        private static void Apply(Mesh mesh, MeshData data, Bounds? bounds)
        {
            mesh.Clear();
            mesh.indexFormat = data.Positions.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(Convert(data.Positions));
            if (data.Normals.Count == data.Positions.Count)
            {
                mesh.SetNormals(Convert(data.Normals));
            }

            if (data.Tangents.Count == data.Positions.Count)
            {
                var tangents = new List<Vector4>(data.Tangents.Count);
                foreach (NVector4 t in data.Tangents)
                {
                    tangents.Add(new Vector4(t.X, t.Y, t.Z, t.W));
                }

                mesh.SetTangents(tangents);
            }

            if (data.Uv0.Count == data.Positions.Count)
            {
                mesh.SetUVs(0, Convert(data.Uv0));
            }

            if (data.Uv1.Count == data.Positions.Count)
            {
                mesh.SetUVs(1, Convert(data.Uv1));
            }

            mesh.subMeshCount = data.Submeshes.Length;
            for (int s = 0; s < data.Submeshes.Length; s++)
            {
                mesh.SetTriangles(data.Submeshes[s], s, calculateBounds: false);
            }

            if (bounds.HasValue)
            {
                mesh.bounds = bounds.Value;
            }
            else
            {
                mesh.RecalculateBounds();
            }
        }

        private static Matrix4x4 PanelToDoor(LeafFrame frame)
        {
            var m = new Matrix4x4();
            m.SetColumn(0, frame.WidthAxis);
            m.SetColumn(1, frame.HeightAxis);
            m.SetColumn(2, frame.NormalAxis);
            m.SetColumn(3, new Vector4(frame.Origin.x, frame.Origin.y, frame.Origin.z, 1f));
            return m;
        }

        private static NMatrix ToNumerics(Matrix4x4 m)
        {
            return new NMatrix(
                m.m00, m.m10, m.m20, m.m30,
                m.m01, m.m11, m.m21, m.m31,
                m.m02, m.m12, m.m22, m.m32,
                m.m03, m.m13, m.m23, m.m33);
        }

        private static SourceMesh ToSource(MeshCopy copy)
        {
            var source = new SourceMesh
            {
                Positions = Array.ConvertAll(copy.Positions, v => new NVector3(v.x, v.y, v.z)),
                Normals = copy.Normals != null ? Array.ConvertAll(copy.Normals, v => new NVector3(v.x, v.y, v.z)) : null,
                Tangents = copy.Tangents != null ? Array.ConvertAll(copy.Tangents, v => new NVector4(v.x, v.y, v.z, v.w)) : null,
                Uv0 = copy.Uv0 != null ? Array.ConvertAll(copy.Uv0, v => new NVector2(v.x, v.y)) : null,
                Uv1 = copy.Uv1 != null ? Array.ConvertAll(copy.Uv1, v => new NVector2(v.x, v.y)) : null,
                Submeshes = copy.SubmeshTriangles
            };
            return source;
        }

        private static List<Vector3> Convert(List<NVector3> source)
        {
            var result = new List<Vector3>(source.Count);
            foreach (NVector3 v in source)
            {
                result.Add(new Vector3(v.X, v.Y, v.Z));
            }

            return result;
        }

        private static List<Vector2> Convert(List<NVector2> source)
        {
            var result = new List<Vector2>(source.Count);
            foreach (NVector2 v in source)
            {
                result.Add(new Vector2(v.X, v.Y));
            }

            return result;
        }
    }

    internal static class DestructionVisuals
    {
        private static readonly Dictionary<DoorEntry, LeafVisual> Visuals = new Dictionary<DoorEntry, LeafVisual>();

        public static int Count => Visuals.Count;

        public static void OnResolved(DoorEntry entry, DestructionResult result, Vector3 momentum)
        {
            if (!Settings.ShowHoles.Value || entry.State == null || entry.Frame == null)
            {
                return;
            }

            if (!Visuals.TryGetValue(entry, out LeafVisual visual))
            {
                try
                {
                    visual = new LeafVisual(entry);
                }
                catch (Exception ex)
                {
                    DoorBreachPlugin.Logger.LogError($"[EDI-Visual] {entry.Door.Id}: setting up the damaged leaf failed: {ex}");
                    return;
                }

                Visuals[entry] = visual;
            }

            visual.OnGroups(result, momentum);
        }

        public static void Tick()
        {
            if (Visuals.Count == 0)
            {
                return;
            }

            bool holes = Settings.ShowHoles.Value;
            if (!holes)
            {
                Clear(raidEnd: false);
            }

            foreach (LeafVisual visual in Visuals.Values)
            {
                try
                {
                    if (holes)
                    {
                        visual.Tick();
                    }

                    visual.TickLeaf();
                }
                catch (Exception ex)
                {
                    DoorBreachPlugin.Logger.LogError($"[EDI-Visual] rebuild failed: {ex}");
                }
            }
        }

        public static bool TryGet(DoorEntry entry, out LeafVisual visual)
        {
            visual = null;
            return entry != null && Visuals.TryGetValue(entry, out visual);
        }

        public static void OnLeafSwapped(EFT.Interactive.Door door)
        {
            if (DoorDestructionRegistry.TryGet(door, out DoorEntry entry) && Visuals.TryGetValue(entry, out LeafVisual visual))
            {
                visual.OnLeafSwapped();
            }
        }

        public static bool HasHole(EFT.Interactive.Door door)
        {
            return DoorDestructionRegistry.TryGet(door, out DoorEntry entry) && Visuals.TryGetValue(entry, out LeafVisual visual) && visual.HasHole;
        }

        public static void Clear(bool raidEnd = true)
        {
            DestructionFragments.Clear();
            var keep = new List<DoorEntry>();
            foreach (KeyValuePair<DoorEntry, LeafVisual> pair in Visuals)
            {
                if (!raidEnd && pair.Key.Takeover != null)
                {
                    keep.Add(pair.Key);
                    continue;
                }

                try
                {
                    pair.Value.Restore();
                }
                catch (Exception)
                {
                }
            }

            var kept = new Dictionary<DoorEntry, LeafVisual>();
            foreach (DoorEntry entry in keep)
            {
                kept[entry] = Visuals[entry];
            }

            Visuals.Clear();
            foreach (KeyValuePair<DoorEntry, LeafVisual> pair in kept)
            {
                Visuals[pair.Key] = pair.Value;
            }
        }
    }
}
