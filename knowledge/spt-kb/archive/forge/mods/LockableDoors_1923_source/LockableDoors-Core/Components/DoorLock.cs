using EFT.Interactive;
using LockableDoors.Fika;
using LockableDoors.Helpers;
using LockableDoors.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static LockableDoors.Models.CustomInteraction;

namespace LockableDoors.Components
{
    internal class DoorLock : MonoBehaviour
    {
        public Door Door { get; private set; }
        public NavMeshObstacle Obstacle { get; private set; }
        private BoxCollider _collider;
        private GameObject _visualizer = null;

        public Vector3 ObstacleSize
        {
            get
            {
                return new Vector3(
                    Settings.NavMeshObstacleXSizeMultiplier.Value,
                    Settings.NavMeshObstacleYSizeMultiplier.Value,
                    Settings.NavMeshObstacleZSizeMultiplier.Value
                );
            }
        }

        private static string _doorLimitText => Settings.LockedDoorLimitsEnabled.Value
            ? Settings.GetLockedDoorLimit().ToString()
            : "∞";

        private void Awake()
        {
            Obstacle = gameObject.GetOrAddComponent<NavMeshObstacle>();
            _collider = gameObject.GetComponentInChildren<BoxCollider>();
            Obstacle.shape = NavMeshObstacleShape.Box;
            Obstacle.center = _collider.center;
            Obstacle.size = ObstacleSize;
            Obstacle.carving = true;
            Obstacle.carveOnlyStationary = false;
            Door = gameObject.GetComponent<Door>();
            LDSession.AddInitializedDoor(Door);
            Door.OnDoorStateChanged += OnDoorStateChanged;

            // if config option to require the key for re-unlocking is NOT set, make sure this door has no key assigned to it so that the vanilla "UNLOCK" option will not show
            if (!Settings.BSGLockedDoorsRequireKey.Value)
            {
                Door.KeyId = "";
            }
        }

        public virtual void OnDoorStateChanged(WorldInteractiveObject subscribee, EDoorState previous, EDoorState next)
        {
            Obstacle.enabled = next == EDoorState.Locked;
        }

        public void AddLockInteractionsToActionList(List<ActionsTypesClass> vanillaActionList)
        {

            if (Door.DoorState == EDoorState.Locked)
            {
                bool doorRequiresKey = !Door.KeyId.IsNullOrEmpty();

                // don't do anything if config is set to require the key for re-unlocking
                if (doorRequiresKey && Settings.BSGLockedDoorsRequireKey.Value) return;

                vanillaActionList.Insert(0, new UnlockInteraction(this).ActionsTypesClass);
                return;
            }
            if (Door.DoorState == EDoorState.Shut)
            {
                // we are doing this check again in case the player changes their config option mid-raid
                // and we are doing during the unlock to remove the door's key requirement as early as possible to reduce cases of 2 "UNLOCK" options in prompt
                if (!Settings.BSGLockedDoorsRequireKey.Value)
                {
                    Door.KeyId = "";
                }

                vanillaActionList.Insert(1, new LockInteraction(this).ActionsTypesClass);
                return;
            }
            vanillaActionList.Insert(1, new DisabledInteraction("Lock").ActionsTypesClass);
        }

        public static void AddUninitializedLockInteractionsToActionList(List<ActionsTypesClass> vanillaActionList, Door door)
        {

            if (door.DoorState == EDoorState.Shut)
            {
                vanillaActionList.Insert(1, new UninitializedLockInteration(door.Id).ActionsTypesClass);
                return;
            }

            if (door.DoorState == EDoorState.Open)
            {
                vanillaActionList.Insert(1, new DisabledInteraction("Lock").ActionsTypesClass);
                return;
            }
        }

        public void Lock(bool sendPacket = true)
        {
            Obstacle.enabled = true;
            Door.DoorState = EDoorState.Locked;
            Obstacle.size = ObstacleSize;

            if (sendPacket)
            {
                FikaBridge.SendDoorLockedStatePacket(Door.Id, true);
            }

            if (Settings.VisualizerEnabled.Value)
            {
                EnabledVisualizer();
            }

            // make sure newly locked doors are last in the list for lock falloff to work correctly
            LDSession.Instance.DoorsWithLocks.Remove(Door);
            LDSession.Instance.DoorsWithLocks.Add(Door);
        }
        public static bool IsLockInteractionEnabled()
        {
            if (Settings.OldLockFalloff.Value) return true;
            bool doorLimitReached = LDSession.Instance.LockedDoorsCount >= Settings.GetLockedDoorLimit();
            return !doorLimitReached;
        }

        public class LockInteraction(DoorLock doorLock) : CustomInteraction
        {
            public override string Name => "Lock";
            public override bool Enabled => IsLockInteractionEnabled();

            public override void OnInteract()
            {
                HandleLockFalloff();
                doorLock.Lock();
                NotificationManagerClass.DisplayMessageNotification($"Locked! {LDSession.Instance.LockedDoorsCount}/{_doorLimitText} doors locked");
            }
        }

        public class UninitializedLockInteration(string doorId) : CustomInteraction
        {
            public override string Name => "Lock";
            public override bool Enabled => IsLockInteractionEnabled();

            public override void OnInteract()
            {
                HandleLockFalloff();
                LDSession.GetDoor(doorId).gameObject.AddComponent<DoorLock>().Lock();
                NotificationManagerClass.DisplayMessageNotification($"Locked! {LDSession.Instance.LockedDoorsCount}/{_doorLimitText} doors locked");
            }
        }
        private static void HandleLockFalloff()
        {
            if (Settings.OldLockFalloff.Value && LDSession.Instance.LockedDoorsCount >= Settings.GetLockedDoorLimit())
            {
                Door door = LDSession.Instance.DoorsWithLocks.FirstOrDefault(door => door.DoorState == EDoorState.Locked);
                if (door == null) return;
                door.gameObject.GetComponent<DoorLock>().Unlock();

                // recursively make sure only up to the number of doors allowed locked are, to cover config changes
                if (LDSession.Instance.LockedDoorsCount >= Settings.GetLockedDoorLimit())
                {
                    HandleLockFalloff();
                }
            }
        }
                public void Unlock(bool sendPacket = true)
        {
            Obstacle.enabled = false;
            Door.DoorState = EDoorState.Shut;

            if (sendPacket)
            {
                FikaBridge.SendDoorLockedStatePacket(Door.Id, false);
            }
        }

        public class UnlockInteraction(DoorLock doorLock) : CustomInteraction
        {
            public override string Name => "Unlock";

            public override void OnInteract()
            {
                doorLock.Unlock();
                NotificationManagerClass.DisplayMessageNotification($"Unlocked! {LDSession.Instance.LockedDoorsCount}/{_doorLimitText} doors locked");

                if (Settings.VisualizerEnabled.Value)
                {
                    doorLock.DisableVisualizer();
                }
            }
        }
                public static DoorLock GetLock(Door door)
        {
            return door.gameObject.GetComponent<DoorLock>();
        }
                public static DoorLock GetLock(string doorId)
        {
            return GetLock(LDSession.GetDoor(doorId));
        }

        public void EnabledVisualizer()
        {
            if (_visualizer == null)
            {
                _visualizer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Renderer renderer = _visualizer.GetComponent<Renderer>();
                Destroy(_visualizer.GetComponent<BoxCollider>());
                renderer.material.color = Color.magenta;
            }
            _visualizer.SetActive(true);
            _visualizer.transform.position = _collider.transform.TransformPoint(_collider.center);
            _visualizer.transform.rotation = _collider.transform.rotation;
            _visualizer.transform.localScale = ObstacleSize;
        }

        public void DisableVisualizer()
        {
            if (_visualizer == null) return;
            _visualizer.SetActive(false);
        }
    }
}
