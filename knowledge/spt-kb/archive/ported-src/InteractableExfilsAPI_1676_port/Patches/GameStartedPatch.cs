using Comfort.Common;
using EFT;
using InteractableExfilsAPI.Components;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace InteractableExfilsAPI.Patches
{
    internal class GameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        protected static bool PatchPrefix()
        {
            Player player = Singleton<GameWorld>.Instance.AllPlayersEverExisted.FirstOrDefault(p => p.IsYourPlayer);
            player.gameObject.AddComponent<InteractableExfilsSession>();
            return true;
        }
    }
}
