using System;
using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.QoL.Patches
{
    /// <summary>
    /// <c>GameWorld.OnGameStarted()</c> postfix：每局开始时把 <see cref="QoLRaidComponent"/>
    /// 挂到 GameWorld GameObject 上。
    /// </summary>
    /// <remarks>
    /// 目标 <c>EFT.GameWorld.OnGameStarted</c> 为 <c>public virtual void</c>（0.16.9 实测）。
    /// 宿主是 GameWorld；挂载失败绝不摧毁宿主，异常一律 fail-open。
    /// </remarks>
    internal sealed class GameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            try
            {
                QoLRaidComponent.EnsureAttached(__instance);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning(
                    $"[QoL] OnGameStarted 挂载战局组件失败（已忽略）：{ex.Message}");
            }
        }
    }
}
