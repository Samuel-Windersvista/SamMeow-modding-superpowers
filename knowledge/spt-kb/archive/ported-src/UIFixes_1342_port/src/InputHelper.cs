using System.Collections.Generic;
using EFT.InputSystem;
using HarmonyLib;

namespace UIFixes;

public static class InputHelper
{
    private static readonly Dictionary<EGameKey, KeyBindingClass> KeyBindings = [];

    public static void MapKeyBindings(InputBindingsDataClass bindingsData)
    {
        KeyBindings.Clear();
        var keyCombinations = (InputKeyCombination[])AccessTools.Field(typeof(InputBindingsDataClass), "_inputKeyCombinations").GetValue(bindingsData);
        foreach (var entry in keyCombinations)
        {
            if (entry is not KeyBindingClass keyBinding)
            {
                continue;
            }

            KeyBindings[keyBinding.GameKey] = keyBinding;
        }
    }

    public static KeyBindingClass GetKeyBinding(EGameKey gameKey)
    {
        return KeyBindings.TryGetValue(gameKey, out KeyBindingClass keyBinding) ? keyBinding : null;
    }

    public static bool IsKeyHeld(EGameKey gameKey)
    {
        if (!KeyBindings.TryGetValue(gameKey, out KeyBindingClass keyBinding))
        {
            return false;
        }

        // _state is the current state
        var keyState = (KeyBindingClass.KeyCombinationState)AccessTools.Field(typeof(KeyBindingClass), "_state").GetValue(keyBinding);
        if (keyState.GetKeysStatus(out EKeyPress keyPress))
        {
            return keyPress switch
            {
                EKeyPress.Hold or EKeyPress.Down => true,
                _ => false,
            };
        }

        return false;
    }
}