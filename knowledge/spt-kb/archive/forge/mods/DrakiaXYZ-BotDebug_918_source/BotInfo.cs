using BepInEx.Logging;
using EFT;
using System;
using System.Text;
using UnityEngine;
using DrakiaXYZ.BotDebug.Helpers;

#if !STANDALONE
using DrakiaXYZ.BigBrain.Brains;
#endif

namespace DrakiaXYZ.BotDebug
{
    internal class BotInfo
    {
        private static readonly StringBuilder stringBuilder = new StringBuilder();
        private static readonly string greyTextColor = "#CCCCCC";
        private static readonly string greenTextColor = "#40FF33";
        private static ManualLogSource Logger;

        public static StringBuilder GetInfoText(DebugBotStruct debugBotStruct, Player localPlayer, BotInfoMode mode)
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(sourceName: typeof(BotInfo).Name);
            }

            Color botNameColor = Color.white;
            var playerOwner = debugBotStruct.PlayerOwner;
            if (playerOwner != null)
            {
                botNameColor = Color.green;
                IAIData aiData = playerOwner.AIData;
                foreach (var enemyInfo in aiData.BotOwner.EnemiesController.EnemyInfos.Values)
                {
                    if (enemyInfo.ProfileId == localPlayer.ProfileId)
                    {
                        botNameColor = Color.red;
                        break;
                    }
                }
            }

            switch (mode)
            {
                case BotInfoMode.Behaviour:
                    return GetBehaviour(debugBotStruct, botNameColor, localPlayer);
                case BotInfoMode.BattleState:
                    return GetBattleState(debugBotStruct, botNameColor);
                case BotInfoMode.Health:
                    return GetHealth(debugBotStruct, botNameColor);
                case BotInfoMode.Specials:
                    return GetSpecial(debugBotStruct, botNameColor);
                case BotInfoMode.Custom:
                    return GetCustom(debugBotStruct, botNameColor);
#if !STANDALONE
                case BotInfoMode.BigBrainLayer:
                    return GetBigBrainLayer(debugBotStruct, botNameColor);
                case BotInfoMode.BigBrainLogic:
                    return GetBigBrainLogic(debugBotStruct, botNameColor);
#endif
                default:
                    return null;
            }
        }

        private static string GetBlackoutLabel(bool val)
        {
            return val ? "(BL)" : "";
        }

        private static string GetBrokenLabel(bool val)
        {
            return val ? "(Broken)" : "";
        }

        private static StringBuilder GetCustom(DebugBotStruct debugBotStruct, Color botNameColor)
        {
            var botData = debugBotStruct.BotData;
            string name = botData.Name;
            string strategyName = botData.StrategyName;
            string layerName = botData.LayerName;
            string customData = botData.CustomData;

            var playerOwner = debugBotStruct.PlayerOwner;
            string nickname = "";
            if (playerOwner != null)
            {
                nickname = playerOwner.Nickname;
            }

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);
            AppendLabeledValue(stringBuilder, "Layer", layerName, Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Nickname", nickname, Color.white, Color.white, true);
            if (string.IsNullOrEmpty(customData))
            {
                stringBuilder.AppendLine("No Custom Data");
            }
            else
            {
                stringBuilder.AppendLine(customData);
            }
            return stringBuilder;
        }

        private static StringBuilder GetSpecial(DebugBotStruct debugBotStruct, Color botNameColor)
        {
            var botData = debugBotStruct.BotData;
            string name = botData.Name;
            string strategyName = botData.StrategyName;
            bool isInSpawnWeapon = botData.IsInSpawnWeapon;
            bool haveAxeEnemy = botData.HaveAxeEnemy;
            int botRole = botData.BotRole;
            EPlayerState playerState = botData.PlayerState;

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);

            for (int i = 0; i < debugBotStruct.ProfileId.Length; i += 12)
            {
                int chunkSize = Math.Min(12, debugBotStruct.ProfileId.Length - i);
                if (i == 0)
                {
                    AppendLabeledValue(stringBuilder, "Id", debugBotStruct.ProfileId.Substring(0, chunkSize), Color.white, Color.white, true);
                }
                else
                {
                    AppendLabeledValue(stringBuilder, "", debugBotStruct.ProfileId.Substring(i, chunkSize), Color.white, Color.white, false);
                }
            }

            AppendLabeledValue(stringBuilder, "WeapSpawn", $"{isInSpawnWeapon}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "AxeEnemy", $"{haveAxeEnemy}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Role", $"{botRole}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "State", $"{playerState}", Color.white, Color.white, true);
            string data = "no data";
            var playerOwner = debugBotStruct.PlayerOwner;
            if (playerOwner != null)
            {
                data = playerOwner.CurrentStataName.ToString();
            }

            AppendLabeledValue(stringBuilder, "StateLc", data, Color.white, Color.white, true);
            return stringBuilder;
        }

        private static StringBuilder GetBehaviour(DebugBotStruct debugBotStruct, Color botNameColor, Player localPlayer)
        {
            var botData = debugBotStruct.BotData;
            string name = botData.Name;
            string strategyName = botData.StrategyName;
            string layerName = botData.LayerName;
            string nodeName = botData.NodeName;
            string reason = botData.Reason;
            string prevNodeName = botData.PrevNodeName;
            string prevNodeExitReason = botData.PrevNodeExitReason;

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);
            AppendLabeledValue(stringBuilder, "Layer", layerName, Color.yellow, Color.yellow, true);
            AppendLabeledValue(stringBuilder, "Node", nodeName, Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "EnterBy", reason, greenTextColor, greenTextColor, true);
            if (!string.IsNullOrEmpty(prevNodeName))
            {
                AppendLabeledValue(stringBuilder, "PrevNode", prevNodeName, greyTextColor, greyTextColor, true);
                AppendLabeledValue(stringBuilder, "ExitBy", prevNodeExitReason, greyTextColor, greyTextColor, true);
            }

            var playerOwner = debugBotStruct.PlayerOwner;
            if (playerOwner != null)
            {
                IPlayer iPlayer = playerOwner.iPlayer;
                int dist = Mathf.RoundToInt((iPlayer.Position - localPlayer.Transform.position).magnitude);
                AppendLabeledValue(stringBuilder, "Dist", $"{dist}", Color.white, Color.white, true);
            }

            return stringBuilder;
        }

        private static StringBuilder GetBattleState(DebugBotStruct debugBotStruct, Color botNameColor)
        {
            var botData = debugBotStruct.BotData;
            string name = botData.Name;
            string strategyName = botData.StrategyName;
            int ammo = botData.Ammo;
            int hitsOnMe = botData.HitsOnMe;
            int shootsOnMe = botData.ShootsOnMe;
            bool reloading = botData.Reloading;
            int coverIndex = botData.CoverIndex;

            var playerOwner = debugBotStruct.PlayerOwner;
            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);
            try
            {
                if (playerOwner != null)
                {
                    IAIData aiData = playerOwner.AIData;
                    BotOwner botOwner = aiData.BotOwner;
                    Player.FirearmController firearmController = botOwner.GetPlayer.HandsController as Player.FirearmController;
                    if (firearmController != null)
                    {
                        int chamberAmmoCount = firearmController.Item.ChamberAmmoCount;
                        int currentMagazineCount = firearmController.Item.GetCurrentMagazineCount();
                        AppendLabeledValue(stringBuilder, "Ammo", $"C: {chamberAmmoCount} M: {currentMagazineCount} T: {ammo}", Color.white, Color.white, true);
                    }
                    AppendLabeledValue(stringBuilder, "Hits", $"{hitsOnMe} / {shootsOnMe}", Color.white, Color.white, true);
                    AppendLabeledValue(stringBuilder, "Reloading", $"{reloading}", Color.white, Color.white, true);
                    AppendLabeledValue(stringBuilder, "CoverId", $"{coverIndex}", Color.white, Color.white, true);

                    bool weaponReady = botOwner.WeaponManager?.Selector?.IsWeaponReady == true;
                    bool hasMalfunction = botOwner.WeaponManager?.Malfunctions?.HaveMalfunction() == true;
                    AppendLabeledValue(stringBuilder, "WeaponReady", $"{weaponReady}", Color.white, weaponReady ? Color.white : Color.red, true);
                    AppendLabeledValue(stringBuilder, "Malfunction", $"{hasMalfunction}", Color.white, !hasMalfunction ? Color.white : Color.red, true);

                }
                else
                {
                    stringBuilder.Append("no battle data");
                }
            }
            catch (Exception ex)
            {
                AppendLabeledValue(stringBuilder, "Error", "Debug panel firearms error", Color.red, Color.red, true);
                Logger.LogError(ex);
            }

            if (playerOwner != null)
            {
                IAIData aiData = playerOwner.AIData;
                var goalEnemy = aiData.BotOwner.Memory.GoalEnemy;
                if (goalEnemy?.Person?.IsAI == true)
                {
                    if (goalEnemy?.Person?.AIData?.BotOwner != null)
                    {
                        AppendLabeledValue(stringBuilder, "GoalEnemy", $"{goalEnemy?.Person?.AIData?.BotOwner?.name}", Color.white, Color.white, true);
                    }
                }
                else
                {
                    AppendLabeledValue(stringBuilder, "GoalEnemy", $"{goalEnemy?.Nickname}", Color.white, Color.white, true);
                }
            }

            return stringBuilder;
        }

        private static StringBuilder GetHealth(DebugBotStruct debugBotStruct, Color botNameColor)
        {
            var botData = debugBotStruct.BotData;
            string name = botData.Name;
            string strategyName = botData.StrategyName;

            var healthData = debugBotStruct.HeathsData;
            int healthHead = healthData.HealthHead;
            bool healthHeadBL = healthData.HealthHeadBL;
            bool healthHeadBroken = healthData.HealthHeadBroken;
            int healthBody = healthData.HealthBody;
            bool healthBodyBL = healthData.HealthBodyBL;
            int healthStomach = healthData.HealthStomach;
            bool healthStomachBL = healthData.HealthStomachBL;
            int healthLeftArm = healthData.HealthLeftArm;
            bool healthLeftArmBL = healthData.HealthLeftArmBL;
            bool healthLeftArmBroken = healthData.HealthLeftArmBroken;
            int healthRightArm = healthData.HealthRightArm;
            bool healthRightArmBL = healthData.HealthRightArmBL;
            bool healthRightArmBroken = healthData.HealthRightArmBroken;
            int healthLeftLeg = healthData.HealthLeftLeg;
            bool healthLeftLegBL = healthData.HealthLeftLegBL;
            bool healthLeftLegBroken = healthData.HealthLeftLegBroken;
            int healthRightLeg = healthData.HealthRightLeg;
            bool healthRightLegBL = healthData.HealthRightLegBL;
            bool healthRightLegBroken = healthData.HealthRightLegBroken;

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);
            AppendLabeledValue(stringBuilder, "Head", $"{healthHead}{GetBlackoutLabel(healthHeadBL)}{GetBrokenLabel(healthHeadBroken)}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Chest", $"{healthBody}{GetBlackoutLabel(healthBodyBL)}{GetBrokenLabel(healthHeadBroken)}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Stomach", $"{healthStomach}{GetBlackoutLabel(healthStomachBL)}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Arms", $"{healthLeftArm}{GetBlackoutLabel(healthLeftArmBL)}{GetBrokenLabel(healthLeftArmBroken)} {healthRightArm}{GetBlackoutLabel(healthRightArmBL)}{GetBrokenLabel(healthRightArmBroken)}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Legs", $"{healthLeftLeg}{GetBlackoutLabel(healthLeftLegBL)}{GetBrokenLabel(healthLeftLegBroken)} {healthRightLeg}{GetBlackoutLabel(healthRightLegBL)}{GetBrokenLabel(healthRightLegBroken)}", Color.white, Color.white, true);
            return stringBuilder;
        }

        private static void AppendLabeledValue(StringBuilder builder, string label, string data, Color labelColor, Color dataColor, bool labelEnabled = true)
        {
            string labelColorString = GetColorString(labelColor);
            string dataColorString = GetColorString(dataColor);

            AppendLabeledValue(builder, label, data, labelColorString, dataColorString, labelEnabled);
        }

        private static void AppendLabeledValue(StringBuilder builder, string label, string data, string labelColor, string dataColor, bool labelEnabled = true)
        {
            if (labelEnabled)
            {
                builder.AppendFormat("<color={0}>{1}:</color>", labelColor, label);
            }

            builder.AppendFormat("<color={0}>{1}</color>\n", dataColor, data);
        }

        private static string GetColorString(Color color)
        {
            if (color == Color.black) return "black";
            if (color == Color.white) return "white";
            if (color == Color.yellow) return "yellow";
            if (color == Color.red) return "red";
            if (color == Color.green) return "green";
            if (color == Color.blue) return "blue";
            if (color == Color.cyan) return "cyan";
            if (color == Color.magenta) return "magenta";
            if (color == Color.gray) return "gray";
            if (color == Color.clear) return "clear";
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

#if !STANDALONE
        private static StringBuilder GetBigBrainLayer(DebugBotStruct debugBotStruct, Color botNameColor)
        {
            var botData = debugBotStruct.BotData;

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);

            object activeLayer = BrainManager.GetActiveLayer(debugBotStruct.PlayerOwner.AIData.BotOwner);
            if (activeLayer != null)
            {
                AppendLabeledValue(stringBuilder, "Class", $"{activeLayer.GetType().Name}", Color.white, Color.white, true);
                AddActiveLayer(stringBuilder, activeLayer);
                (activeLayer as CustomLayer)?.BuildDebugText(stringBuilder);
            }

            return stringBuilder;
        }

        private static StringBuilder GetBigBrainLogic(DebugBotStruct debugBotStruct, Color botNameColor)
        {
            var botData = debugBotStruct.BotData;

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);

            object activeLayer = BrainManager.GetActiveLayer(debugBotStruct.PlayerOwner.AIData.BotOwner);
            AddActiveLayer(stringBuilder, activeLayer);

            object activeLogic = BrainManager.GetActiveLogic(debugBotStruct.PlayerOwner.AIData.BotOwner);
            if (activeLogic != null)
            {
                AppendLabeledValue(stringBuilder, "Logic", $"{activeLogic.GetType().Name}", Color.white, Color.white, true);
                if (activeLogic is CustomLogic customLogic)
                {
                    customLogic?.BuildDebugText(stringBuilder);
                }
            }

            return stringBuilder;
        }

        private static void AddActiveLayer(StringBuilder stringBuilder, object activeLayer)
        {
            if (activeLayer != null)
            {
                if (activeLayer is CustomLayer customLayer)
                {
                    AppendLabeledValue(stringBuilder, "CLayer", $"{customLayer.GetName()}", Color.white, Color.white, true);
                }
                else if (activeLayer is BaseLogicLayerSimple logicLayer)
                {
                    AppendLabeledValue(stringBuilder, "Layer", $"{logicLayer.Name()}", Color.grey, Color.grey, true);
                }
            }
        }
#endif

        public enum BotInfoMode
        {
            Minimized = 0,
            Behaviour,
            BattleState,
            Health,
            Specials,
            Custom,
#if !STANDALONE
            BigBrainLayer,
            BigBrainLogic
#endif
        }
    }
}
