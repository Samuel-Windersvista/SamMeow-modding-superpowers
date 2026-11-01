using EFT.InputSystem;
using UnityEngine;

namespace DoorOverhaul.Helpers
{
    internal static class InteractKey
    {
        private static IGameKey[] _keys;

        public static void Capture(IGameKey[] keys)
        {
            if (keys != null && keys.Length > 0)
            {
                _keys = keys;
            }
        }

        public static bool Held()
        {
            if (_keys == null)
            {
                return Input.GetKey(KeyCode.F);
            }

            bool any = false;
            foreach (IGameKey key in _keys)
            {
                if (key is InputKey inputKey)
                {
                    if (!Input.GetKey(inputKey.Key))
                    {
                        return false;
                    }

                    any = true;
                }
            }

            return any || Input.GetKey(KeyCode.F);
        }
    }
}
