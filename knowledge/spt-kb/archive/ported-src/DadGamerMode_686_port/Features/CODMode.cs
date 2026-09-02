using BepInEx.Logging;
using Comfort.Common;
using dvize.GodModeTest;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using UnityEngine;

using AbstractIEffect = EFT.HealthSystem.ActiveHealthController.Effect;

namespace dvize.DadGamerMode.Features
{
    internal class CODModeComponent : MonoBehaviour
    {
        private static Player player;
        private static ActiveHealthController healthController;
        private static float timeSinceLastHit = 0f;
        private static bool isRegenerating = false;
        private static float newHealRate;
        private static DamageInfo tmpDmg;
        private static HealthValue currentHealth;
        private static int frameCount = 0;

        private static readonly EBodyPart[] bodyPartsDict = { EBodyPart.Stomach, EBodyPart.Chest, EBodyPart.Head, EBodyPart.RightLeg,
EBodyPart.LeftLeg, EBodyPart.LeftArm, EBodyPart.RightArm };

        protected static ManualLogSource Logger
        {
            get; private set;
        }
        private CODModeComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(CODModeComponent));
            }
        }
        internal static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<CODModeComponent>();

                Logger.LogDebug("DadGamerMode: CODModeComponent enabled");
            }
        }
        private void Start()
        {
            player = Singleton<GameWorld>.Instance.MainPlayer;
            healthController = player.ActiveHealthController;
            isRegenerating = false;
            timeSinceLastHit = 0f;
            newHealRate = 0f;
            tmpDmg = new DamageInfo();
            currentHealth = null;
            frameCount = 0;

            player.OnPlayerDeadOrUnspawn += Player_OnPlayerDeadOrUnspawn;
            player.BeingHitAction += Player_BeingHitAction;
            healthController.EffectAddedEvent += HealthController_EffectAddedEvent;
        }

        private void HealthController_EffectAddedEvent(IHealthEffect effect)
        {

#if DEBUG
            Logger.LogWarning("Effect added is of type: " + effect.Type);
            Logger.LogWarning("The Effect state is: " + effect.State);
            Logger.LogWarning("The BodyPart is: " + effect.BodyPart);
            Logger.LogWarning("The Effect Strength is: " + effect.Strength);
#endif

            //grabbed this from remove negative effects method
            if (dadGamerPlugin.CODModeToggle.Value && !dadGamerPlugin.CODBleedingDamageToggle.Value)
            {
                // SPT 4.1 port: original checked !(effect is GInterface252) && !(effect is GInterface253)
                // (unobfuscated equivalents of the negative-effect guard the author intended).
                // Keep positive/neutral effects, remove bleeding/fracture/pain/tremor.
                if (!(effect is ILightBleeding) && !(effect is IHeavyBleeding) &&
                    !(effect is IFracture) && !(effect is IPain) && !(effect is ITremor))
                {
                    healthController.RemoveEffectFromList(effect as AbstractIEffect);
#if DEBUG
                    Logger.LogDebug("Effect is a Fracture, Bleeding, or Pain and has been removed");
#endif
                }
            }
        }

        private void Update()
        {
            if (dadGamerPlugin.CODModeToggle.Value)
            {
                frameCount++;
                timeSinceLastHit += Time.unscaledDeltaTime;

                if (frameCount >= 60) // Check every 60 frames instead
                {
                    frameCount = 0;

                    if (timeSinceLastHit >= dadGamerPlugin.CODModeHealWait.Value)
                    {
                        if (!isRegenerating)
                        {
                            isRegenerating = true;
                        }

                        StartHealing();
                    }
                }
            }
        }

        private void StartHealing()
        {
            if (isRegenerating && dadGamerPlugin.CODModeToggle.Value)
            {
                newHealRate = dadGamerPlugin.CODModeHealRate.Value;

                foreach (var limb in bodyPartsDict)
                {
                    //if bleeding damage is not enabled, repair the limb

                    currentHealth = healthController.BodyState[limb].Health;
                    //bool isBleeding = healthController.FindActiveEffect<GInterface242>(limb) != null || healthController.FindActiveEffect<GInterface243>(limb) != null;

                    if (!currentHealth.AtMaximum && !healthController.BodyState[limb].IsDestroyed)
                    {
                        currentHealth.Current += newHealRate;
                    }

                    
                }
            }
        }
        private void Disable()
        {
            if (player != null)
            {
                player.OnPlayerDeadOrUnspawn -= Player_OnPlayerDeadOrUnspawn;
                player.BeingHitAction -= Player_BeingHitAction;
                healthController.EffectAddedEvent -= HealthController_EffectAddedEvent;
            }
        }

        private void Player_BeingHitAction(DamageInfo arg1, EBodyPart arg2, float arg3)
        {
            //Logger.LogDebug("DadGamerMode: Player_BeingHitAction called");
            timeSinceLastHit = 0f;
            isRegenerating = false;
        }


        private void Player_OnPlayerDeadOrUnspawn(Player player)
        {
            Disable();
        }
    }
}
