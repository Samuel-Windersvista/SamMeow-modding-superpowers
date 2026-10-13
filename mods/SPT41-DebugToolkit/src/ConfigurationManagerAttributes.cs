/// <summary>
/// 指定配置项在 ConfigurationManager（F12）窗口中的显示方式。
/// </summary>
/// <remarks>
/// BepInEx.ConfigurationManager 不引用插件程序集，而是按<b>类型名</b>
/// <c>ConfigurationManagerAttributes</c> 反射读取本类的字段（官方模板要求把本类复制进插件工程）。
/// 字段与官方模板同构，未使用的字段保持 null（ConfigurationManager 忽略 null 字段）。
/// 移植自 5.0 线（SamMeow-DebugToolkit）同名文件；Mono / BepInEx 5 形态下语义一致。
/// </remarks>
#pragma warning disable 0169, 0414, 0649

namespace SamMeow.DebugToolkit
{
    internal sealed class ConfigurationManagerAttributes
    {
        /// <summary>是否以百分比显示（仅用于值域设置）。</summary>
        public bool? ShowRangeAsPercent;

        /// <summary>
        /// 自定义绘制器：替代 ConfigurationManager 默认编辑器。
        /// 启用后若干其他字段不再生效。
        /// </summary>
        public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;

        /// <summary>是否在设置界面显示该项（false = 不显示）。</summary>
        public bool? Browsable;

        /// <summary>该项所属分类；null 表示直接挂在插件下。</summary>
        public string Category;

        /// <summary>若设置，则显示 "Default" 按钮以便恢复默认值。</summary>
        public object DefaultValue;

        /// <summary>强制不显示 "Reset" 按钮（即使 DefaultValue 可用）。</summary>
        public bool? HideDefaultButton;

        /// <summary>强制不显示设置名（通常配合 <see cref="CustomDrawer"/> 换取空间）。</summary>
        public bool? HideSettingName;

        /// <summary>悬停时显示的说明（不推荐，建议在创建配置项时给描述）。</summary>
        public string Description;

        /// <summary>设置项显示名。</summary>
        public string DispName;

        /// <summary>在分类列表中的排序（越大越靠上）。</summary>
        public int? Order;

        /// <summary>只显示值，不允许编辑。</summary>
        public bool? ReadOnly;

        /// <summary>是否仅在"高级设置"或搜索时才显示。</summary>
        public bool? IsAdvanced;

        /// <summary>内置文本框：设置值 → 字符串的自定义转换。</summary>
        public System.Func<object, string> ObjToStr;

        /// <summary>内置文本框：字符串 → 设置值的自定义转换。</summary>
        public System.Func<string, object> StrToObj;
    }
}
