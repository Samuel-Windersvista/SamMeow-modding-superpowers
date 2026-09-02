using Comfort.Common;
using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LockableDoors.Components
{
    internal class LDSession : MonoBehaviour
    {
        private LDSession() { }
        private static LDSession _instance = null;

        public Player Player { get; private set; }
        public GameWorld GameWorld { get; private set; }
        public GamePlayerOwner GamePlayerOwner { get; private set; }
        public static LDSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!Singleton<GameWorld>.Instantiated)
                    {
                        throw new Exception("Can't get ModSession Instance when GameWorld is not instantiated!");
                    }

                    _instance = Singleton<GameWorld>.Instance.MainPlayer.gameObject.GetOrAddComponent<LDSession>();
                }
                return _instance;
            }
        }

        public Dictionary<string, Door> WorldDoors { get; private set; } = new();

        //public Dictionary<string, Door> DoorsWithLocks = new();
        public List<Door> DoorsWithLocks = [];

        public int LockedDoorsCount
        {
            get
            {
                int i = 0;
                foreach (var door in DoorsWithLocks)
                {
                    if (door.DoorState == EDoorState.Locked) i++;
                }
                return i;
            }
        }

        private void Awake()
        {
            GameWorld = Singleton<GameWorld>.Instance;
            Player = GameWorld.MainPlayer;
            GamePlayerOwner = Player.gameObject.GetComponent<GamePlayerOwner>();

            FindObjectsOfType<Door>().ExecuteForEach(door => { WorldDoors[door.Id] = door; });
        }

        public static void AddInitializedDoor(Door door)
        {
            if (Instance.DoorsWithLocks.Contains(door))
            {
                throw new Exception("Tried to add door to DoorLookup with a doorId that was already in the dictionary!");
            }

            Instance.DoorsWithLocks.Add(door);
        }

        public static Door GetDoor(string id)
        {
            return Instance.WorldDoors[id];
        }
    }
}
