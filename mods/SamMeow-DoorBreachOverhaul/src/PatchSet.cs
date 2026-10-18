using System;
using System.Collections.Generic;
using SPT.Reflection.Patching;

namespace DoorBreach
{
    /// <summary>
    /// 共享补丁集注册器：逐项装配并记账，逐项撤销后清空。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>装配过程中任一补丁失败只记日志、不中断其余补丁（fail-open）；</item>
    /// <item>撤销必须覆盖「已成功装配」的全部补丁，即使装配曾中途失败；</item>
    /// <item>幂等：重复 <see cref="DisableAll"/> 安全（集合已空）；同一实例重复
    /// <see cref="Enable"/> 被忽略。</item>
    /// </list>
    /// 4.1.6 = Mono：<c>SPT.Reflection.Patching.ModulePatch</c> 自带 <c>Disable()</c>，
    /// 撤销直接调用，无需 Harmony.UnpatchID。
    /// </remarks>
    internal sealed class PatchSet
    {
        private readonly string _owner;
        private readonly List<ModulePatch> _patches = new List<ModulePatch>();

        /// <param name="owner">日志前缀（模块名），用于定位失败补丁。</param>
        public PatchSet(string owner)
        {
            _owner = owner;
        }

        /// <summary>已成功装配的补丁数。</summary>
        public int Count => _patches.Count;

        /// <summary>装配单个补丁；成功才记账，失败仅日志（异常绝不穿透）。</summary>
        public void Enable(ModulePatch patch)
        {
            if (patch == null || _patches.Contains(patch))
            {
                return;
            }

            try
            {
                patch.Enable();
                _patches.Add(patch);
            }
            catch (Exception e)
            {
                DoorBreachPlugin.Logger?.LogWarning($"[{_owner}] 补丁 {patch.GetType().Name} 启用失败：{e.Message}");
            }
        }

        /// <summary>撤销全部已装配补丁并清空（逐项 fail-open，可重复调用）。</summary>
        public void DisableAll()
        {
            foreach (ModulePatch patch in _patches)
            {
                try
                {
                    patch.Disable();
                }
                catch (Exception e)
                {
                    DoorBreachPlugin.Logger?.LogWarning($"[{_owner}] 撤销补丁 {patch.GetType().Name} 失败：{e.Message}");
                }
            }

            _patches.Clear();
        }
    }
}
