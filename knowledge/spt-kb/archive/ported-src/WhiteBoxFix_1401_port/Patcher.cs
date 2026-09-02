namespace WhiteBoxFix;

internal class Patcher
{
	public static void PatchAll()
	{
		new PatchManager().RunPatches();
	}
}
