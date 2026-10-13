using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SamMeow.DebugToolkit.Modules.Tooltip
{
    /// <summary>
    /// 悬挂在 tooltip 实例上的剪贴板组件（移植自 DebugTooltip/TooltipCopier.cs）。
    /// 三档复制：ctrl-c 短 ID / ctrl-alt-c 备用 ID / ctrl-shift-c 完整调试块。
    /// </summary>
    /// <remarks>
    /// 4.1.6 = Mono：自定义 MonoBehaviour 无需 ClassInjector 注册、无需 IntPtr 构造，
    /// 可直接 GetOrAddComponent/AddComponent。
    /// 剪贴板用 <c>UnityEngine.GUIUtility.systemCopyBuffer</c>（public static string，get/set；
    /// 位于 UnityEngine.IMGUIModule）。
    /// 上游 <c>ctrlDown</c> 检测写成 <c>LeftControl || LeftControl</c>（笔误），
    /// 此处修正为 <c>LeftControl || RightControl</c>（与 5.0 线一致，记入偏差表）。
    /// </remarks>
    internal sealed class TooltipCopier : MonoBehaviour
    {
        /// <summary>当前已挂载的实例（Awake 登记 / OnDestroy 注销），供模块停用时统一清理。</summary>
        private static readonly List<TooltipCopier> Instances = new List<TooltipCopier>();

        private string _shortText;
        private string _altShortText;
        private string _longText;

        private void Awake()
        {
            Instances.Add(this);
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
        }

        /// <summary>把调试信息渲染成三档纯文本（去掉富文本颜色标签）。</summary>
        public void SetDebugInfo(DebugInfo debugInfo)
        {
            if (debugInfo == null)
            {
                return;
            }

            _shortText = StripTags(debugInfo.ToShortString());
            _altShortText = StripTags(debugInfo.ToAltShortString());
            _longText = StripTags(debugInfo.ToString());
        }

        /// <summary>清空本实例持有的临时调试文本（模块停用时调用）。</summary>
        public void ClearDebugInfo()
        {
            _shortText = null;
            _altShortText = null;
            _longText = null;
        }

        /// <summary>
        /// 模块停用：清空并销毁全部已挂载实例（只销毁本组件，绝不触碰 tooltip 宿主对象）；fail-open。
        /// </summary>
        public static void RemoveAll()
        {
            try
            {
                // 快照后清空，避免遍历中 OnDestroy 修改集合。
                TooltipCopier[] snapshot = Instances.ToArray();
                Instances.Clear();

                foreach (TooltipCopier copier in snapshot)
                {
                    if (copier == null)
                    {
                        continue;
                    }

                    copier.ClearDebugInfo();
                    UnityEngine.Object.Destroy(copier);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 清理 TooltipCopier 失败：{e.Message}");
            }
        }

        private void Update()
        {
            try
            {
                if (!Input.GetKeyDown(KeyCode.C))
                {
                    return;
                }

                bool ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                // 顺序与上游一致：ctrl-shift 优先并短路；随后 ctrl、alt 依次可覆盖。
                if (ctrlDown && shiftDown)
                {
                    GUIUtility.systemCopyBuffer = _longText;
                    return;
                }

                if (ctrlDown)
                {
                    GUIUtility.systemCopyBuffer = _shortText;
                }

                if (altDown)
                {
                    GUIUtility.systemCopyBuffer = _altShortText;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 剪贴板复制失败：{e.Message}");
            }
        }

        private static string StripTags(string input)
        {
            return string.IsNullOrEmpty(input) ? input : Regex.Replace(input, "</?color[^>]*>", string.Empty);
        }
    }
}
