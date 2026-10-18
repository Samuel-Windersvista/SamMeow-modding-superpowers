using DoorBreach;
using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using DoorOverhaul.Destruction.Core;
using UnityEngine;
using NVector3 = System.Numerics.Vector3;

namespace DoorOverhaul.Destruction
{
    internal sealed class DoorTakeover
    {
        private const float ChunkSize = 0.3f;
        private const float SettleAfter = 10f;
        private const float HollowCoreFactor = 0.6f;
        private const float SagDegrees = 12f;
        private const string BlockerName = "EDI leaf blocker";
        private const string ShapeName = "EDI leaf shape";
        private const float ShapeInset = 0.01f;
        private static readonly int ShapeLayer = LayerMask.NameToLayer("Shells");
        private static readonly int BlockerLayer = LayerMask.NameToLayer("DoorLowPolyCollider");

        private readonly DoorEntry _entry;
        private readonly GameObject _root;
        private readonly Rigidbody _body;
        private ConfigurableJoint _joint;
        private readonly GameObject _colliderTemplate;
        private readonly List<GameObject> _chunks = new List<GameObject>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<BallisticCollider> _hooks = new List<BallisticCollider>();
        private int[] _chunkSignature = Array.Empty<int>();
        private float _released;

        public Transform Leaf => _root != null ? _root.transform : null;
        public bool Hanging => _joint != null;
        public bool Moving => _body != null && !_body.IsSleeping();

        public DoorTakeover(DoorEntry entry, IReadOnlyList<MeshFilter> leafFilters, Action<MeshFilter, MeshFilter> retarget, Anchor hinge, Vector3 momentum)
        {
            _entry = entry;
            Door door = entry.Door;
            Transform source = door.transform;

            _root = new GameObject($"EDI leaf {door.Id}");
            _root.transform.SetPositionAndRotation(source.position, source.rotation);
            _root.transform.localScale = source.lossyScale;

            var moved = new HashSet<MeshFilter>(leafFilters);
            foreach (MeshFilter filter in leafFilters)
            {
                MeshFilter copy = CopyRenderer(filter);
                if (copy != null)
                {
                    retarget(filter, copy);
                }
            }

            foreach (MeshRenderer renderer in door.GetComponentsInChildren<MeshRenderer>(false))
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && !moved.Contains(filter) && filter.sharedMesh != null && renderer.gameObject.activeInHierarchy)
                {
                    CopyRenderer(filter);
                }
            }

            foreach (Renderer renderer in door.GetComponentsInChildren<Renderer>(true))
            {
                renderer.forceRenderingOff = true;
            }

            Collider template = LeafFrame.PrimaryCollider(door);
            _colliderTemplate = template != null ? template.gameObject : null;

            foreach (Collider collider in door.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            if (door.Obstacle != null)
            {
                door.Obstacle.enabled = false;
            }

            door.Operatable = false;
            door.NoInteractionsAllowed = true;
            door.DoorState = EDoorState.Open;
            if (door._occlusionPortal != null)
            {
                door._occlusionPortal.open = true;
            }

            FractureState state = entry.State;
            float area = 0f;
            for (int c = 0; c < state.Layout.Cells.Length; c++)
            {
                if (state.IsPartOfLeaf(c))
                {
                    area += state.Layout.Cells[c].Area;
                }
            }

            _body = _root.AddComponent<Rigidbody>();
            _body.mass = Mathf.Max(1f, area * entry.Frame.Thickness * state.Profile.Density * HollowCoreFactor);
            _body.drag = 0.1f;
            _body.angularDrag = 1.2f;
            _body.maxDepenetrationVelocity = 0.5f;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            RebuildColliders(state);

            if (hinge != null && !hinge.Failed)
            {
                _joint = _root.AddComponent<ConfigurableJoint>();
                _joint.autoConfigureConnectedAnchor = true;
                _joint.anchor = entry.Frame.Origin + entry.Frame.WidthAxis * hinge.Spec.Position.X + entry.Frame.HeightAxis * hinge.Spec.Position.Y;
                _joint.xMotion = ConfigurableJointMotion.Locked;
                _joint.yMotion = ConfigurableJointMotion.Locked;
                _joint.zMotion = ConfigurableJointMotion.Locked;
                _joint.axis = entry.Frame.HeightAxis;
                _joint.secondaryAxis = entry.Frame.WidthAxis;
                _joint.angularXMotion = ConfigurableJointMotion.Free;
                _joint.angularYMotion = ConfigurableJointMotion.Limited;
                _joint.angularZMotion = ConfigurableJointMotion.Limited;
                _joint.angularYLimit = new SoftJointLimit { limit = SagDegrees };
                _joint.angularZLimit = new SoftJointLimit { limit = SagDegrees };
                _joint.angularYZLimitSpring = new SoftJointLimitSpring { spring = 0f, damper = 0f };
                _joint.enablePreprocessing = false;
            }

            PhysicsExtensions.UpdateController.SupportRigidbody(_body, 0f);
            _body.AddForceAtPosition(Vector3.ClampMagnitude(momentum, _body.mass * 0.5f), _root.transform.TransformPoint(Centre(entry)), ForceMode.Impulse);

            Alert(door.transform.position);
        }

        public void Release(Vector3 momentum)
        {
            if (_joint != null)
            {
                UnityEngine.Object.Destroy(_joint);
                _joint = null;
            }

            _released = Time.time;
            if (_body != null)
            {
                _body.WakeUp();
                _body.AddForce(Vector3.ClampMagnitude(momentum, _body.mass * 0.5f), ForceMode.Impulse);
            }

            Alert(_root.transform.position);
        }

        public void Tick()
        {
            if (_body != null && _joint == null && (_body.IsSleeping() || (_released > 0f && Time.time - _released > SettleAfter)))
            {
                PhysicsExtensions.UpdateController.UnsupportRigidbody(_body);
                UnityEngine.Object.Destroy(_body);
            }
        }

        public void RebuildColliders(FractureState state)
        {
            if (_colliderTemplate == null)
            {
                return;
            }

            FractureLayout layout = state.Layout;
            int columns = Mathf.Max(1, Mathf.CeilToInt(layout.Width / ChunkSize));
            int rows = Mathf.Max(1, Mathf.CeilToInt(layout.Height / ChunkSize));
            var members = new List<int>[columns * rows];
            var signature = new int[columns * rows];
            for (int c = 0; c < layout.Cells.Length; c++)
            {
                if (!state.IsPartOfLeaf(c))
                {
                    continue;
                }

                System.Numerics.Vector2 centre = layout.Cells[c].Centroid;
                int chunk = Mathf.Clamp((int)(centre.Y / ChunkSize), 0, rows - 1) * columns + Mathf.Clamp((int)(centre.X / ChunkSize), 0, columns - 1);
                (members[chunk] ??= new List<int>()).Add(c);
                signature[chunk] = signature[chunk] * 31 + c + 1;
            }

            while (_chunks.Count < members.Length)
            {
                _chunks.Add(null);
            }

            float half = _entry.Frame.Thickness * 0.5f;
            Matrix4x4 panelToLeaf = PanelToLeaf(_entry.Frame);
            for (int k = 0; k < members.Length; k++)
            {
                if (k < _chunkSignature.Length && _chunkSignature[k] == signature[k] && (members[k] == null) == (_chunks[k] == null))
                {
                    continue;
                }

                if (members[k] == null)
                {
                    if (_chunks[k] != null)
                    {
                        UnityEngine.Object.Destroy(_chunks[k]);
                        _chunks[k] = null;
                    }

                    continue;
                }

                var visible = new bool[layout.Cells.Length];
                foreach (int c in members[k])
                {
                    visible[c] = true;
                }

                (List<NVector3> positions, List<int> triangles) = SlabBuilder.Build(layout, visible, half * 0.95f);
                var vertices = new List<Vector3>(positions.Count);
                foreach (NVector3 p in positions)
                {
                    vertices.Add(panelToLeaf.MultiplyPoint3x4(new Vector3(p.X, p.Y, p.Z)));
                }

                var mesh = new Mesh { name = "door leaf chunk" };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                _meshes.Add(mesh);

                if (_chunks[k] == null)
                {
                    _chunks[k] = MakeChunk();
                }

                MeshCollider collider = _chunks[k].GetComponent<MeshCollider>();
                collider.sharedMesh = null;
                collider.convex = true;
                collider.sharedMesh = mesh;

                if (ShapeLayer >= 0)
                {
                    var inset = new List<Vector3>(positions.Count);
                    foreach (NVector3 p in positions)
                    {
                        float x = Mathf.Clamp(p.X, ShapeInset, layout.Width - ShapeInset);
                        float y = Mathf.Clamp(p.Y, ShapeInset, layout.Height - ShapeInset);
                        inset.Add(panelToLeaf.MultiplyPoint3x4(new Vector3(x, y, p.Z * 0.9f)));
                    }

                    var shapeMesh = new Mesh { name = "door leaf shape" };
                    shapeMesh.SetVertices(inset);
                    shapeMesh.SetTriangles(triangles, 0);
                    shapeMesh.RecalculateBounds();
                    _meshes.Add(shapeMesh);
                    SetChildShape(_chunks[k].transform, ShapeName, ShapeLayer, shapeMesh);
                }

                if (Settings.LeafBlocksPlayers.Value && BlockerLayer >= 0)
                {
                    SetChildShape(_chunks[k].transform, BlockerName, BlockerLayer, mesh);
                }
            }

            _chunkSignature = signature;
        }

        private static void SetChildShape(Transform parent, string name, int layer, Mesh mesh)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name) { layer = layer };
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            MeshCollider shape = child.GetComponent<MeshCollider>() ?? child.gameObject.AddComponent<MeshCollider>();
            shape.sharedMesh = null;
            shape.convex = true;
            shape.sharedMesh = mesh;
        }

        private GameObject MakeChunk()
        {
            GameObject chunk = UnityEngine.Object.Instantiate(_colliderTemplate, _root.transform, false);
            chunk.name = "EDI leaf chunk";
            chunk.transform.localPosition = Vector3.zero;
            chunk.transform.localRotation = Quaternion.identity;
            chunk.transform.localScale = Vector3.one;
            chunk.SetActive(true);
            foreach (Collider other in chunk.GetComponents<Collider>())
            {
                if (!(other is MeshCollider))
                {
                    UnityEngine.Object.Destroy(other);
                }
            }

            MeshCollider collider = chunk.GetComponent<MeshCollider>() ?? chunk.AddComponent<MeshCollider>();
            collider.enabled = true;

            BallisticCollider ballistic = chunk.GetComponent<BallisticCollider>();
            if (ballistic != null)
            {
                DoorDestructionRegistry.RegisterCollider(ballistic, _entry);
                _hooks.Add(ballistic);
            }

            return chunk;
        }

        private MeshFilter CopyRenderer(MeshFilter filter)
        {
            MeshRenderer source = filter.GetComponent<MeshRenderer>();
            if (source == null)
            {
                return null;
            }

            var child = new GameObject(filter.name);
            child.layer = filter.gameObject.layer;
            child.transform.SetParent(_root.transform, false);
            child.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
            child.transform.localScale = Divide(filter.transform.lossyScale, _root.transform.lossyScale);
            MeshFilter copy = child.AddComponent<MeshFilter>();
            copy.sharedMesh = filter.sharedMesh;
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
            renderer.lightProbeUsage = source.lightProbeUsage;
            renderer.reflectionProbeUsage = source.reflectionProbeUsage;
            _copies[filter] = copy;
            return copy;
        }

        private readonly Dictionary<MeshFilter, MeshFilter> _copies = new Dictionary<MeshFilter, MeshFilter>();

        public MeshFilter CopyOf(MeshFilter original)
        {
            return original != null && _copies.TryGetValue(original, out MeshFilter copy) ? copy : null;
        }

        private void Alert(Vector3 position)
        {
            try
            {
                IPlayer source = _entry.LastShooter;
                if (source != null && Singleton<GlobalEventDispatcher>.Instantiated)
                {
                    Singleton<GlobalEventDispatcher>.Instance.PlaySound(source, position, BotInternalSettingsController.Core.SOUND_DOOR_BREACH_METERS, AISoundType.step);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Destroy()
        {
            foreach (BallisticCollider collider in _hooks)
            {
                if (collider != null)
                {
                    DoorDestructionRegistry.UnregisterCollider(collider);
                }
            }

            _hooks.Clear();
            if (_body != null)
            {
                PhysicsExtensions.UpdateController.UnsupportRigidbody(_body);
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            foreach (Mesh mesh in _meshes)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
            }
        }

        private static Vector3 Centre(DoorEntry entry)
        {
            return entry.Frame.Origin + entry.Frame.WidthAxis * (entry.Frame.Width * 0.5f) + entry.Frame.HeightAxis * (entry.Frame.Height * 0.5f);
        }

        private static Matrix4x4 PanelToLeaf(LeafFrame frame)
        {
            var m = new Matrix4x4();
            m.SetColumn(0, frame.WidthAxis);
            m.SetColumn(1, frame.HeightAxis);
            m.SetColumn(2, frame.NormalAxis);
            m.SetColumn(3, new Vector4(frame.Origin.x, frame.Origin.y, frame.Origin.z, 1f));
            return m;
        }

        private static Vector3 Divide(Vector3 a, Vector3 b)
        {
            return new Vector3(b.x != 0f ? a.x / b.x : a.x, b.y != 0f ? a.y / b.y : a.y, b.z != 0f ? a.z / b.z : a.z);
        }
    }
}
