using System;
using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.BotOverlay
{
    /// <summary>
    /// 补丁 <c>GameWorld.OnGameStarted</c>：每局开始时把覆盖层组件挂到 GameWorld 宿主上。
    /// 移植自 BotDebug <c>NewGamePatch</c>（1.8.0）。
    /// </summary>
    /// <remarks>
    /// 补丁体处于游戏方法调用链内：任何异常穿透即 CTD，故一律 try/catch + fail-open。
    /// 目标 <c>EFT.GameWorld.OnGameStarted</c> 为 <c>public virtual void</c>（0.16.9 实测），可补。
    /// </remarks>
    internal class GameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            try
            {
                BotDebugComponent.Enable();
            }
            catch (Exception ex)
            {
                // fail-open：挂载失败也绝不向游戏调用链抛异常。
                Plugin.Logger?.LogWarning($"[BotOverlay] OnGameStarted 挂载失败（已忽略）：{ex.Message}");
            }
        }
    }
}
