using BepInEx;

namespace WhiteBoxFix;

[BepInPlugin("com.MarsyApp.WhiteBoxFix", "MarsyApp-WhiteBoxFix", "4.1.2")]
public class WhiteBoxFix : BaseUnityPlugin
{
	private void Awake()
	{
		Patcher.PatchAll();
	}
}
