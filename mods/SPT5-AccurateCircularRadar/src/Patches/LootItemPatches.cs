using EFT.Interactive;
using SPTushonka.Reflection.Patching;
using System;
using System.Reflection;

namespace Radar.Patches
{
    /// <summary>
    /// Registers loot the game adds mid-raid: player drops, airdrops, gear from freshly killed bots.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配：<c>DictionaryListHydra&lt;TKey,TValue&gt;</c> 在 5.0 interop 里位于全局命名空间
    /// （不再是 <c>EFT.Interactive.DictionaryListHydra</c>），因此 <c>using EFT.Interactive;</c> 只用于
    /// <see cref="LootItem"/>。补丁目标仍是 <c>Add(TKey,TValue)</c> / <c>Remove(TKey)</c>，参数名保持
    /// key / value 以便 Harmony 注入。
    /// </remarks>
    internal class LootItemAddPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(DictionaryListHydra<int, LootItem>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

        [PatchPostfix]
        private static void PostFix(int key, LootItem value)
        {
            // [CRITICAL] 补丁体绝不向游戏代码抛异常（异常穿透 = 游戏闪退）。
            // [CRITICAL] 不要用 value.TrackableTransform：它是 virtual 属性，interop 需经
            // il2cpp_object_get_virtual_method 运行时解析；实测该解析返回坏指针，
            // il2cpp_runtime_invoke 直接 AccessViolation 崩溃（2026-09-16 实战 CTD，coreclr 0xc0000005）。
            // 改用非虚的标准 Component.transform（本 mod 敌人 blip 路径已实证安全）。
            try
            {
                // The transform has not settled at this point, hence lazyUpdate.
                InRaidRadarManager.LiveRadar?.AddLoot(value.ItemId, value.Item, value.transform, true);
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Loot add hook failed: {e.Message}");
            }
        }
    }

    /// <summary>Drops the blip for loot that has been picked up or otherwise removed from the world.</summary>
    internal class LootItemRemovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(DictionaryListHydra<int, LootItem>).GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        private static void PreFix(int key)
        {
            // [CRITICAL] 补丁体绝不向游戏代码抛异常（异常穿透 = 游戏闪退）。
            try
            {
                // Prefix, because the item still has to be resolvable by key.
                InRaidRadarManager.LiveRadar?.RemoveLootByKey(key);
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Loot remove hook failed: {e.Message}");
            }
        }
    }
}
