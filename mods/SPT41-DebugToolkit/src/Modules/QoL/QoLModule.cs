using SamMeow.DebugToolkit.Modules.QoL.Patches;

namespace SamMeow.DebugToolkit.Modules.QoL
{
    /// <summary>
    /// 便利组 QOL 模块注册点（移植 DadGamerMode 1.9.3 QOL 全组）。
    /// </summary>
    /// <remarks>
    /// 生命周期：
    /// <list type="bullet">
    /// <item><see cref="Enable"/> = 绑定配置 + 逐项装配补丁（<c>GameWorld.OnGameStarted</c> 挂载点 +
    /// 瞬产 + 瞬建（载入 / 界面两路径）+ 负重）；</item>
    /// <item><see cref="Disable"/> = 撤销全部补丁 + 移除战局组件 + 还原换弹速度全局默认
    /// （<c>Destroy(this)</c>，绝不摧毁宿主 GameObject）。</item>
    /// </list>
    /// 补丁生命周期经共享 <see cref="PatchSet"/>：逐项装配 / 逐项撤销，全程 fail-open。
    /// <see cref="Disable"/> 无条件清理（不得因 <c>_enabled == false</c> 早退），覆盖部分装配状态。
    /// 对敌伤害倍率并入 05 的 <c>ApplyDamagePatch</c>（非本模块补丁）。
    /// </remarks>
    internal static class QoLModule
    {
        private static readonly PatchSet Patches = new PatchSet("QoL");

        private static bool _enabled;

        public static void Enable()
        {
            if (_enabled)
            {
                return;
            }

            QoLSettings.Init();

            Patches.Enable(new InstantProductionPatch());
            Patches.Enable(new InstantConstructionPatch());
            Patches.Enable(new InstantUpgradeWaitPatch());
            Patches.Enable(new OnWeightUpdatedPatch());
            Patches.Enable(new GameStartPatch());

            _enabled = true;
            Plugin.Logger?.LogInfo("[QoL] 模块已启用。");
        }

        public static void Disable()
        {
            // 无条件清理：撤销全部已装配补丁、移除战局组件、还原换弹全局默认（即使 Enable 曾中途失败）。
            Patches.DisableAll();
            QoLRaidComponent.DestroyInstance();
            QoLRaidComponent.RestoreReloadDefaults();

            _enabled = false;
            Plugin.Logger?.LogInfo("[QoL] 模块已停用。");
        }
    }
}
