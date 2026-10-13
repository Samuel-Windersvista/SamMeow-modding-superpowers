namespace SamMeow.DebugToolkit.Modules.Tooltip
{
    /// <summary>
    /// Tooltip 调试信息的静态传递（移植自 DebugTooltip/DebugTooltip.cs；5.0 线命名沿用）。
    /// 各视图补丁在鼠标悬停时写入一个 <see cref="DebugInfo"/>，
    /// tooltip 显示补丁读取后清空——上游 <c>DebugTooltip</c> 静态类的等价物。
    /// </summary>
    internal static class DebugTooltipState
    {
        private static DebugInfo _next;

        public static void SetDebugInfo(DebugInfo debugInfo)
        {
            _next = debugInfo;
        }

        public static DebugInfo GetDebugInfo()
        {
            return _next;
        }

        public static void Clear()
        {
            _next = null;
        }
    }
}
