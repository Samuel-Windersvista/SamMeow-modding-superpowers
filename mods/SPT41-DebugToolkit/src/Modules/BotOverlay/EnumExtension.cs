using System;

namespace SamMeow.DebugToolkit.Modules.BotOverlay
{
    /// <summary>
    /// 枚举循环扩展（纯托管，零游戏 API 依赖）。
    /// 移植自 BotDebug <c>Helpers/EnumExtension.cs</c>，行为等价。
    /// </summary>
    internal static class EnumExtension
    {
        public static T Next<T>(this T src) where T : Enum
        {
            T[] values = (T[])Enum.GetValues(src.GetType());
            int nextIndex = Array.IndexOf<T>(values, src) + 1;
            if (values.Length <= nextIndex)
            {
                return values[0];
            }

            return values[nextIndex];
        }

        public static T Previous<T>(this T src) where T : Enum
        {
            T[] values = (T[])Enum.GetValues(src.GetType());
            int prevIndex = Array.IndexOf<T>(values, src) - 1;
            if (prevIndex < 0)
            {
                return values[values.Length - 1];
            }

            return values[prevIndex];
        }
    }
}
