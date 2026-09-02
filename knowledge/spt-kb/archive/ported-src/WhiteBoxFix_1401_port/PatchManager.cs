using System.Collections.Generic;
using SPT.Reflection.Patching;

namespace WhiteBoxFix;

public class PatchManager
{
	private readonly List<ModulePatch> _patches;

	public PatchManager()
	{
		_patches = new List<ModulePatch> { (ModulePatch)(object)new ItemViewPatches.DraggedItemViewMethodCheckItem() };
	}

	public void RunPatches()
	{
		foreach (ModulePatch patch in _patches)
		{
			patch.Enable();
		}
	}
}
