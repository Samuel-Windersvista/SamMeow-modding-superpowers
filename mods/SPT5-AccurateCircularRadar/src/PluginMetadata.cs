using System.Reflection;

[assembly: AssemblyVersion(Radar.PluginMetadata.AssemblyVersion)]
[assembly: AssemblyFileVersion(Radar.PluginMetadata.AssemblyVersion)]
[assembly: AssemblyInformationalVersion(Radar.PluginMetadata.Version)]

namespace Radar
{
    /// <summary>Single source for the plugin identity and version.</summary>
    /// <remarks>
    /// 移植署名（Overseer 要求）：版本号与显示名标注 SamMeow 移植版；GUID 保持上游不变
    /// （配置文件 com.leonana69.radar.cfg 连续）；作者信息经 <see cref="Author"/> 在启动日志
    /// 输出，并同步到 README / LICENSE / MO2 meta.ini。
    /// </remarks>
    internal static class PluginMetadata
    {
        public const string Guid = "com.leonana69.radar";
        public const string Name = "Accurate Circular Radar - SPT5";

        /// <summary>移植作者与上游作者（启动日志逐字输出）。</summary>
        public const string Author = "SamMeow (port); original mod by Leonana69";

        // Change only this value when releasing a new plugin version.
        // 上游 1.3.4 + SPT5 移植修订号（semver 预发布段）。
        public const string Version = "1.3.4-spt5.1";

        // 程序集版本必须是纯数字四段式（不接受预发布后缀）。
        public const string AssemblyVersion = "1.3.4.1";
    }
}
