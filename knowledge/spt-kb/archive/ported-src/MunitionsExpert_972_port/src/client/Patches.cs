using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static MunitionsExpert.Attributes;

namespace MunitionsExpert.Patches
{
	public class PatchManager
	{
		public PatchManager()
		{
			this._patches = new List<ModulePatch>
			{
				new MunitionsExpert_CachedAttributesPatch(),
                new MunitionsExpert_StaticIconsPatch()
			};
		}

		public void RunPatches()
		{
			foreach (ModulePatch patch in this._patches)
			{
				patch.Enable();
			}
		}

		private readonly List<ModulePatch> _patches;
	}

    public class MunitionsExpert_StaticIconsPatch : ModulePatch
    {
        public MunitionsExpert_StaticIconsPatch() : base("com.faupi.munitionsexpert") { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(StaticIcons).GetMethod("GetAttributeIcon", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref Sprite __result, Enum id)
        {
            if (id == null || !MunitionsExpert.iconCache.ContainsKey(id)) return true;

            Sprite sprite = MunitionsExpert.iconCache[id];

            if (sprite != null)
            {
                __result = sprite;
                return false; //Skip the default getter
            }
            return true; //Continue with default getter
        }
    }

    public class MunitionsExpert_CachedAttributesPatch : ModulePatch
    {
        public MunitionsExpert_CachedAttributesPatch() : base("com.faupi.munitionsexpert") { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("GetCachedReadonlyQualities", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref AmmoTemplate __instance, ref List<ItemAttribute> __result)
        {
            bool converted = __result.Any(a => (ENewItemAttributeId)a.Id == ENewItemAttributeId.Damage); //Damage is pretty much guaranteed
            if (!converted) //If it has any of the custom attributes, it has all of them (the ones that apply ofc)
            {
                MunitionsExpert.FormatExistingAttributes(ref __result, __instance);
                MunitionsExpert.AddNewAttributes(ref __result, __instance);
            }
        }
    }
}
