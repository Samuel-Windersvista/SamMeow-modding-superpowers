using System.ComponentModel;
using BepInEx.Configuration;

namespace SamMeow.DebugToolkit
{
    /// <summary>
    /// F12 配置描述工厂：统一装配中文显示名与 ConfigurationManager 反射属性。
    /// </summary>
    /// <remarks>
    /// 零依赖做法（见 5xx-client-mod-dev-lessons §F12 中文化）：
    /// 用 BCL 的 <see cref="DisplayNameAttribute"/> 提供显示名；Tags 中的字符串
    /// <c>"ReadOnly"</c> 由 ConfigurationManager（官方版与本机 fork 均）识别为灰显；
    /// 本地同名类 <see cref="ConfigurationManagerAttributes"/> 按类型名被反射读取
    /// （不硬引用 ConfigurationManager.dll，避免依赖缺失）。
    /// </remarks>
    internal static class ConfigDescriptions
    {
        /// <summary>构造带中文显示名的配置描述。</summary>
        /// <param name="displayName">F12 中显示的名称。</param>
        /// <param name="description">悬停说明文案。</param>
        /// <param name="acceptableValues">取值域（范围滑块 / 列表下拉），可为 null。</param>
        /// <param name="readOnly">是否只读灰显（内部键，如版本号）。</param>
        public static ConfigDescription Describe(
            string displayName,
            string description = null,
            AcceptableValueBase acceptableValues = null,
            bool readOnly = false)
        {
            ConfigurationManagerAttributes attributes = new ConfigurationManagerAttributes
            {
                DispName = displayName,
            };

            if (readOnly)
            {
                attributes.ReadOnly = true;
            }

            object[] tags = readOnly
                ? new object[] { new DisplayNameAttribute(displayName), "ReadOnly", attributes }
                : new object[] { new DisplayNameAttribute(displayName), attributes };

            return new ConfigDescription(description, acceptableValues, tags);
        }
    }
}
