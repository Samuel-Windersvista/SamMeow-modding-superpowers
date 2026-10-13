using SamMeow.DebugToolkit.Modules.Invincibility.Patches;

namespace SamMeow.DebugToolkit.Modules.Invincibility
{
    /// <summary>
    /// 无敌 / 部分无敌模块注册点（移植 DadGamerMode 1.9.3 健康组 + CODMode）。
    /// </summary>
    /// <remarks>
    /// 生命周期：
    /// <list type="bullet">
    /// <item><see cref="Enable"/> = 绑定配置 + 逐项装配三支补丁
    /// （<c>GameWorld.OnGameStarted</c> 挂载点 + <c>ApplyDamage</c> + <c>DestroyBodyPart</c>）；</item>
    /// <item><see cref="Disable"/> = 撤销全部补丁 + 移除战局组件（<c>Destroy(this)</c>，绝不摧毁宿主 GameObject）。</item>
    /// </list>
    /// 补丁生命周期经共享 <see cref="PatchSet"/>：逐项装配 / 逐项撤销，全程 fail-open。
    /// <see cref="Disable"/> 无条件清理（不得因 <c>_enabled == false</c> 早退），覆盖部分装配状态。
    /// </remarks>
    internal static class InvincibilityModule
    {
        private static readonly PatchSet Patches = new PatchSet("Invincibility");

        private static bool _enabled;

        public static void Enable()
        {
            if (_enabled)
            {
                return;
            }

            InvincibilityConfig.Init();

            Patches.Enable(new ApplyDamagePatch());
            Patches.Enable(new DestroyBodyPartPatch());
            Patches.Enable(new GameStartPatch());

            _enabled = true;
            Plugin.Logger?.LogInfo("[Invincibility] 模块已启用。");
        }

        public static void Disable()
        {
            // 无条件清理：撤销全部已装配补丁并移除战局组件（即使 Enable 曾中途失败）。
            Patches.DisableAll();
            InvincibilityRaidComponent.DestroyInstance();

            _enabled = false;
            Plugin.Logger?.LogInfo("[Invincibility] 模块已停用。");
        }
    }
}
