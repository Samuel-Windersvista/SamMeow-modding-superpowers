using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace WeaponCustomizer.Server;

[Injectable]
public class ReplaceIdsPatch : AbstractPatch
{
    private static ICloner _cloner = default!;
    private static WeaponCustomizer _weaponCustomizer = default!;

    public ReplaceIdsPatch(ICloner cloner, WeaponCustomizer weaponCustomizer)
    {
        _cloner = cloner;
        _weaponCustomizer = weaponCustomizer;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemExtensions), nameof(ItemExtensions.ReplaceIDs));
    }

    [PatchPrefix]
    public static void Prefix(IEnumerable<Item> items, ref IEnumerable<Item> __state)
    {
        __state = _cloner.Clone(items);
    }

    [PatchPostfix]
    public static void Postfix(IEnumerable<Item> __state, IEnumerable<Item> __result)
    {
        bool dirty = false;
        foreach (var (originalItem, newItem) in __state.Zip(__result))
        {
            if (_weaponCustomizer.Database.TryGetValue(originalItem.Id, out CustomizedObject customizedObject))
            {
                _weaponCustomizer.Database[newItem.Id] = _cloner.Clone(customizedObject);
                dirty = true;
            }
        }

        if (dirty)
        {
            // Fire and forget
            _ = _weaponCustomizer.Save();
        }
    }
}
