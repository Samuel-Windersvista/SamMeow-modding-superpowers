using System;
using System.Linq;
using DoorBreach.Fika;
using DoorOverhaul.Actions;
using EFT;
using EFT.Interactive;
using EFT.UI;

namespace DoorBreach.Patches
{
    /// <summary>
    /// 门交互菜单单点合并：DO 三动作（Peek / Open Further / Open Quietly）与 Plant Explosive 共用一次注入。
    /// 去重按动作种类（DO）与动作名（C4）双重保证；KeyCard 重载复用同一入口。
    /// </summary>
    internal static class DoorMenuMerge
    {
        private const string PlantExplosiveName = "Plant Explosive";

        /// <summary>Door 重载：注入 DO 三动作 + Plant Explosive（一次调用完成合并）。</summary>
        public static void AppendAll(AvailableInteractionState state, GamePlayerOwner owner, Door door)
        {
            if (state?.Actions == null || owner?.Player == null || door == null || !owner.Player.IsYourPlayer)
            {
                return;
            }

            try
            {
                DoorActions.AppendMissing(state.Actions, owner, door);
                AppendPlantExplosive(state, owner, door);
            }
            catch (Exception ex)
            {
                // 补丁边界 fail-soft：菜单注入失败不穿透游戏调用链（记录后保留原生菜单）。
                DoorBreachPlugin.Logger?.LogError($"[Menu] 注入门交互菜单失败 ({door.Id})：{ex}");
            }
        }

        /// <summary>KeyCard 重载：只做 Plant Explosive 去重守卫，不重复注入 DO 动作。</summary>
        public static void AppendPlantExplosiveOnly(AvailableInteractionState state, GamePlayerOwner owner, Door door)
        {
            if (state?.Actions == null || owner?.Player == null || door == null || !owner.Player.IsYourPlayer)
            {
                return;
            }

            try
            {
                AppendPlantExplosive(state, owner, door);
            }
            catch (Exception ex)
            {
                DoorBreachPlugin.Logger?.LogError($"[Menu] 注入 Plant Explosive 失败 ({door.Id})：{ex}");
            }
        }

        private static void AppendPlantExplosive(AvailableInteractionState state, GamePlayerOwner owner, Door door)
        {
            if (state.Actions.Any(action =>
                    string.Equals(action.Name, PlantExplosiveName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            state.Actions.Add(CreatePlantExplosive(owner, door));
        }

        private static InteractionAction CreatePlantExplosive(GamePlayerOwner owner, Door door)
        {
            return new InteractionAction
            {
                Name = PlantExplosiveName,
                Action = () =>
                {
                    // 放置失败（物品模板缺失 / ItemFactory 未就绪）时不扣物品、不发包、不启动倒计时。
                    if (!ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player))
                    {
                        return;
                    }

                    ExplosiveBreachComponent.RemoveItemFromPlayerInventory(owner.Player);
                    FikaBridge.SendPlantC4PacketPacket(owner.Player, door.Id, DoorBreachPlugin.explosiveTimerInSec);
                },

                // 门控集中在组件内：持有 C4 + 门状态合规 + 破门角度合规；异常按禁用处理，不刷屏。
                Disabled = ExplosiveBreachComponent.IsPlantExplosiveDisabled(door, owner.Player)
            };
        }
    }
}
