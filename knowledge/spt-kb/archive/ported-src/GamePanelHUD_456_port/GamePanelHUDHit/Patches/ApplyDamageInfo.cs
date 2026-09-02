#if !UNITY_EDITOR

using EFT;
using GamePanelHUDCore.Models;

namespace GamePanelHUDHit
{
    public partial class GamePanelHUDHitPlugin
    {
        private static void ApplyDamageInfo(Player __instance, EFT.Ballistics.DamageInfo damageInfo, EBodyPart bodyPartType)
        {
            if ((Player)damageInfo.Player?.iPlayer != HUDCoreModel.Instance.YourPlayer)
                return;

            BaseApplyDamageInfo(damageInfo, bodyPartType, __instance.HealthController);
        }
    }
}

#endif