using System;
using System.Reflection;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.Tooltip.Patches
{
    /// <summary>
    /// 任务列表项：后置挂 HoverTrigger，悬停显示 Quest ID + 状态。
    /// </summary>
    /// <remarks>
    /// 触点表 #27：0.16.9 仍为 <c>EFT.UI.QuestListItem</c>（**未改名**；改名 QuestListItemView 只发生在
    /// SPT 5.0 线 / EFT 1.1.5），<c>Init</c> 为 2 参 <c>(Quest quest, Action&lt;QuestListItem&gt; onSelect)</c>。
    /// 后置按参数名绑定 <c>quest</c> 有效。
    /// </remarks>
    internal sealed class QuestListItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(QuestListItem), nameof(QuestListItem.Init));

        [PatchPostfix]
        private static void Postfix(QuestListItem __instance, Quest quest)
        {
            try
            {
                HoverTrigger hoverTrigger = __instance.GetOrAddComponent<HoverTrigger>();
                TooltipHelpers.AttachDebugHover(__instance, hoverTrigger, () => new QuestDebugInfo(quest));
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] QuestListItem.Init postfix 失败：{e.Message}");
            }
        }
    }

    /// <summary>
    /// 任务详情（NotesTask）：在 TaskShortInfo 子物体上订阅悬停，显示 Quest ID + 状态。
    /// </summary>
    /// <remarks>
    /// 触点表 #29（[UNCERTAIN]）：<c>transform.Find("TaskShortInfo")</c> 为运行期字符串，
    /// 静态不可核实，故对 Find 结果补空判后再取组件。
    /// </remarks>
    internal sealed class NotesTaskPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.DeclaredMethod(typeof(NotesTask), nameof(NotesTask.Show));

        [PatchPostfix]
        private static void Postfix(NotesTask __instance, Quest quest)
        {
            try
            {
                var found = __instance.transform.Find("TaskShortInfo");
                if (found == null)
                {
                    return;
                }

                HoverTrigger hoverTrigger = found.gameObject.GetOrAddComponent<HoverTrigger>();
                TooltipHelpers.AttachDebugHover(__instance, hoverTrigger, () => new QuestDebugInfo(quest));
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] NotesTask.Show postfix 失败：{e.Message}");
            }
        }
    }

    /// <summary>任务目标：后置挂 HoverTrigger，悬停显示 Condition ID。</summary>
    /// <remarks>
    /// 触点表 #30：0.16.9 <c>QuestObjectiveView.Show</c> 为 6 参（无 1.1.5 的尾参 <c>string prefix</c>），
    /// 后置按参数名绑定 <c>condition</c> 有效。
    /// </remarks>
    internal sealed class QuestObjectivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(QuestObjectiveView), nameof(QuestObjectiveView.Show));

        [PatchPostfix]
        private static void Postfix(QuestObjectiveView __instance, Condition condition)
        {
            try
            {
                HoverTrigger hoverTrigger = __instance.GetOrAddComponent<HoverTrigger>();
                TooltipHelpers.AttachDebugHover(__instance, hoverTrigger, () => new QuestConditionDebugInfo(condition));
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] QuestObjectiveView.Show postfix 失败：{e.Message}");
            }
        }
    }
}
