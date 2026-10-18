using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT.Ballistics;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

namespace DoorOverhaul.Destruction
{
    internal static class DestructionFragments
    {
        public const int Cap = 200;
        private const float SettleAfter = 6f;
        private const float MinMass = 0.05f;
        private const float MaxSpeed = 6f;

        private sealed class Fragment
        {
            public GameObject Root;
            public Rigidbody Body;
            public float Spawned;
            public readonly List<Mesh> Meshes = new List<Mesh>();
        }

        private static readonly LinkedList<Fragment> Fragments = new LinkedList<Fragment>();
        private static AccessTools.FieldRef<Effects, Dictionary<MaterialType, Effects.Effect>> _effectsDict;
        private static int _shellsLayer = -2;

        public static int Count => Fragments.Count;
        public static int Moving { get; private set; }

        public static int ShellsLayer
        {
            get
            {
                if (_shellsLayer == -2)
                {
                    _shellsLayer = LayerMask.NameToLayer("Shells");
                }

                return _shellsLayer;
            }
        }

        public static void Spawn(string name, List<(Mesh Mesh, MeshRenderer Source, Matrix4x4 World)> parts,
            Mesh collider, Matrix4x4 rootPose, float mass, Vector3 velocity, Vector3 angularVelocity, IEnumerable<Collider> ignore)
        {
            var fragment = new Fragment { Spawned = Time.time };
            var root = new GameObject(name);
            fragment.Root = root;
            root.layer = ShellsLayer >= 0 ? ShellsLayer : 0;
            root.transform.SetPositionAndRotation(rootPose.GetColumn(3), rootPose.rotation);

            foreach ((Mesh mesh, MeshRenderer source, Matrix4x4 world) in parts)
            {
                var child = new GameObject(mesh.name);
                child.layer = source.gameObject.layer;
                child.transform.SetParent(root.transform, false);
                child.transform.SetPositionAndRotation(world.GetColumn(3), world.rotation);
                child.transform.localScale = world.lossyScale;
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = source.sharedMaterials;
                renderer.shadowCastingMode = source.shadowCastingMode;
                renderer.receiveShadows = source.receiveShadows;
                renderer.lightProbeUsage = source.lightProbeUsage;
                renderer.reflectionProbeUsage = source.reflectionProbeUsage;
                fragment.Meshes.Add(mesh);
            }

            MeshCollider shape = root.AddComponent<MeshCollider>();
            shape.convex = true;
            shape.sharedMesh = collider;
            fragment.Meshes.Add(collider);

            foreach (Collider other in ignore)
            {
                if (other != null && other.enabled && other.gameObject.activeInHierarchy && !other.isTrigger)
                {
                    Physics.IgnoreCollision(shape, other, true);
                }
            }

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = Mathf.Max(MinMass, mass);
            body.drag = 0.05f;
            body.angularDrag = 0.3f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.velocity = Vector3.ClampMagnitude(velocity, MaxSpeed);
            body.angularVelocity = angularVelocity;
            fragment.Body = body;
            PhysicsExtensions.UpdateController.SupportRigidbody(body, 0f);

            Fragments.AddLast(fragment);
            while (Fragments.Count > Cap)
            {
                Destroy(Fragments.First.Value);
                Fragments.RemoveFirst();
            }
        }

        public static void Burst(MaterialType material, Vector3 position, Vector3 normal, float volume)
        {
            if (!Singleton<Effects>.Instantiated)
            {
                return;
            }

            try
            {
                _effectsDict ??= AccessTools.FieldRefAccess<Effects, Dictionary<MaterialType, Effects.Effect>>("_dict");
                Dictionary<MaterialType, Effects.Effect> effects = _effectsDict(Singleton<Effects>.Instance);
                if (effects != null && effects.TryGetValue(material, out Effects.Effect effect))
                {
                    effect.Emit(position, normal, null, withDecal: false, volume: volume);
                }
            }
            catch (Exception)
            {
            }
        }

        public static void Tick()
        {
            int moving = 0;
            LinkedListNode<Fragment> node = Fragments.First;
            while (node != null)
            {
                LinkedListNode<Fragment> next = node.Next;
                Fragment fragment = node.Value;
                if (fragment.Root == null)
                {
                    Fragments.Remove(node);
                }
                else if (fragment.Body != null)
                {
                    if (fragment.Root.transform.position.y < -500f)
                    {
                        Destroy(fragment);
                        Fragments.Remove(node);
                    }
                    else if (fragment.Body.IsSleeping() || Time.time - fragment.Spawned > SettleAfter)
                    {
                        PhysicsExtensions.UpdateController.UnsupportRigidbody(fragment.Body);
                        UnityEngine.Object.Destroy(fragment.Body);
                        fragment.Body = null;
                    }
                    else
                    {
                        moving++;
                    }
                }

                node = next;
            }

            Moving = moving;
        }

        public static void Clear()
        {
            foreach (Fragment fragment in Fragments)
            {
                Destroy(fragment);
            }

            Fragments.Clear();
            Moving = 0;
        }

        private static void Destroy(Fragment fragment)
        {
            if (fragment.Body != null)
            {
                PhysicsExtensions.UpdateController.UnsupportRigidbody(fragment.Body);
            }

            if (fragment.Root != null)
            {
                UnityEngine.Object.Destroy(fragment.Root);
            }

            foreach (Mesh mesh in fragment.Meshes)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
            }
        }
    }
}
