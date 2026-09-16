using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using Radar.Patches;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Plugin entry point.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配：<c>BaseUnityPlugin</c>+<c>Awake</c> 改为 <c>BasePlugin</c>+<c>Load</c>/<c>Unload</c>。
    /// BasePlugin 不是 MonoBehaviour，因此删除 gameObject / DontDestroyOnLoad / Destroy 相关代码；
    /// 日志走 <c>base.Log</c>（ManualLogSource）。IL2CPP 下任何要挂到 GameObject 上的托管类型
    /// （HaloRadar / InRaidRadarManager / PolygonGraphic）必须先用 ClassInjector 注册。
    /// </remarks>
    [BepInPlugin(PluginMetadata.Guid, PluginMetadata.Name, PluginMetadata.Version)]
    public class RadarPlugin : BasePlugin
    {
        internal static RadarPlugin Instance { get; private set; }

        // 隐藏 BasePlugin.Log（实例属性）；静态访问点保持与上游 4.1 的 RadarPlugin.Log 一致。
        internal static new ManualLogSource Log { get; private set; }

        public override void Load()
        {
            Log = base.Log;
            Instance = this;

            // Color 没有内置 TomlTypeConverter（BepInEx 6 只内置基元与枚举），
            // 必须在任何 ConfigEntry<Color> 绑定之前注册，否则 Bind 会抛 InvalidOperationException。
            RegisterColorConverter();

            RadarConfig.Bind(Config);
            AssetFileManager.Load();

            // 先注册再使用：任何 AddComponent<T>() / new GameObject(..., typeof(T)) 之前。
            ClassInjector.RegisterTypeInIl2Cpp<HaloRadar>();
            ClassInjector.RegisterTypeInIl2Cpp<InRaidRadarManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PolygonGraphic>();

            new GameStartPatch().Enable();

            Log.LogInfo($"{PluginMetadata.Name} {PluginMetadata.Version} — {PluginMetadata.Author}.");
            Log.LogInfo("Radar plugin enabled.");
        }

        /// <summary>
        /// BepInEx 6 的撤销路径。补丁与 HUD 由游戏对象生命周期自行销毁，这里只清理静态引用。
        /// </summary>
        public override bool Unload()
        {
            Instance = null;
            Log.LogInfo("Radar plugin disabled.");
            return true;
        }

        /// <summary>
        /// 注册 <see cref="Color"/> 的 TOML 转换器，格式与 ConfigurationManager 的显示一致（"r g b a"）。
        /// 若已有转换器（例如其它插件先注册）则本调用被忽略。
        /// </summary>
        private static void RegisterColorConverter()
        {
            try
            {
                TomlTypeConverter.AddConverter(typeof(Color), new TypeConverter
                {
                    ConvertToString = (obj, type) =>
                    {
                        Color color = (Color)obj;
                        return string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} {1} {2} {3}",
                            color.r, color.g, color.b, color.a);
                    },
                    ConvertToObject = (str, type) => ParseColor(str),
                });
            }
            catch (Exception e)
            {
                Log.LogWarning($"Could not register Color config converter: {e.Message}");
            }
        }

        /// <summary>宽容解析 "r g b a" / "r g b" / "r"，缺省分量按 Unity 默认值补齐。</summary>
        private static Color ParseColor(string value)
        {
            float r = 0f, g = 0f, b = 0f, a = 1f;
            if (!string.IsNullOrWhiteSpace(value))
            {
                string[] parts = value.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0) r = float.Parse(parts[0], CultureInfo.InvariantCulture);
                if (parts.Length > 1) g = float.Parse(parts[1], CultureInfo.InvariantCulture);
                if (parts.Length > 2) b = float.Parse(parts[2], CultureInfo.InvariantCulture);
                if (parts.Length > 3) a = float.Parse(parts[3], CultureInfo.InvariantCulture);
            }

            return new Color(r, g, b, a);
        }
    }
}
