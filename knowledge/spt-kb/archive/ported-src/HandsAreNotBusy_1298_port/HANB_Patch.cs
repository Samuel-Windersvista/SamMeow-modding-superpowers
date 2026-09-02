using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace HandsAreNotBusy;

internal class HANB_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld)
            .GetMethod(nameof(GameWorld.RegisterPlayer));
    }

    [PatchPostfix]
    public static void PostFix(IPlayer iPlayer)
    {
        if (iPlayer == null)
        {
            HANB_Plugin.HANB_Logger.LogError("Could not add component, player was null!");
            return;
        }

        if (!iPlayer.IsYourPlayer)
        {
            return;
        }

        Player player = Singleton<GameWorld>.Instance.AllPlayersEverExisted.FirstOrDefault(p => p.IsYourPlayer);
        if (player == null)
        {
            return;
        }

        player.gameObject.AddComponent<HANB_Component>();
        HANB_Plugin.HANB_Logger.LogInfo("Added HANB Component to player: " + player.Profile.Nickname);
    }
}
