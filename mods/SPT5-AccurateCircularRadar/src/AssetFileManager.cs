using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Loads the radar's prefabs and blip sprites out of the assembly's embedded resources.
    /// </summary>
    /// <remarks>
    /// Every blip set is a triple: level, above the player, below the player.
    /// <para>
    /// 4.1 -> 5.0 适配（IL2CPP）：
    /// (1) <c>LoadFromStream(Stream)</c> 的 interop 参数是 <c>Il2CppSystem.IO.Stream</c>，托管 Stream
    ///     无法直接封送 → 改 <c>LoadFromMemory(byte[])</c>；
    /// (2) <c>LoadAsset&lt;T&gt;</c> 泛型重载在 interop 不存在 → <c>LoadAsset(name)</c> + <c>TryCast</c>，
    ///     并按资产名扫描兜底；
    /// (3) 生命周期：bundle 必须被静态字段持有 —— IL2CPP 下场景切换触发的资产清理会回收
    ///     无引用链的 bundle 资产，令 prefab 变成失效引用（局内 Instantiate 抛 NullReferenceException）。
    ///     加载出的 prefab/贴图额外标记 <c>HideFlags.DontUnloadUnusedAsset</c> 双保险。
    /// </para>
    /// </remarks>
    internal static class AssetFileManager
    {
        private const string ResourcePrefix = "Radar.bundle.";
        private const string BundleResource = ResourcePrefix + "radarhud.bundle";
        private const string HudPrefabPath = "Assets/Examples/Halo Reach/Hud/RadarHUD.prefab";
        private const string BlipPrefabPath = "Assets/Examples/Halo Reach/Hud/RadarBlipHUD.prefab";
        private const string HudPrefabName = "RadarHUD";
        private const string BlipPrefabName = "RadarBlipHUD";

        /// <summary>
        /// 静态持有 bundle：IL2CPP 下没有引用链的 bundle 会在资产清理时被回收，
        /// 其资产随之失效。整个插件生命周期只需要这一个 bundle。
        /// </summary>
        private static AssetBundle _bundle;

        public static GameObject RadarHudPrefab { get; private set; }
        public static GameObject RadarBlipHudPrefab { get; private set; }

        public static Sprite[] NormalEnemyBlips { get; private set; } = new Sprite[3];
        public static Sprite[] BossEnemyBlips { get; private set; } = new Sprite[3];
        public static Sprite[] DeadEnemyBlips { get; private set; } = new Sprite[3];
        public static Sprite[] LootBlips { get; private set; } = new Sprite[3];
        public static Sprite[] BtrBlips { get; private set; } = new Sprite[3];
        public static Sprite[] MineBlips { get; private set; } = new Sprite[3];
        public static Sprite[] ExfiltrationBlips { get; private set; } = new Sprite[3];

        internal static bool Loaded { get; private set; }

        internal static void Load()
        {
            if (Loaded) return;

            byte[] bundleBytes = ReadResourceBytes(BundleResource);
            if (bundleBytes == null)
                return;

            try
            {
                _bundle = AssetBundle.LoadFromMemory(bundleBytes);
                if (_bundle == null)
                {
                    RadarPlugin.Log.LogError($"Could not load AssetBundle from {BundleResource}");
                    return;
                }

                RadarHudPrefab = LoadPrefab(HudPrefabPath, HudPrefabName);
                RadarBlipHudPrefab = LoadPrefab(BlipPrefabPath, BlipPrefabName);

                if (RadarHudPrefab == null || RadarBlipHudPrefab == null)
                {
                    RadarPlugin.Log.LogError(
                        $"Radar prefab load incomplete: hud={(RadarHudPrefab != null)}, blip={(RadarBlipHudPrefab != null)}");
                }
                else
                {
                    RadarPlugin.Log.LogInfo("Radar prefabs loaded.");
                }

                NormalEnemyBlips = LoadBlipSet("normal_enemy");
                BossEnemyBlips = LoadBlipSet("boss_enemy");
                DeadEnemyBlips = LoadBlipSet("dead_enemy");
                LootBlips = LoadBlipSet("loot");
                BtrBlips = LoadBlipSet("btr");
                MineBlips = LoadBlipSet("mine");
                ExfiltrationBlips = LoadBlipSet("exfiltraction");

                Loaded = true;
                RadarPlugin.Log.LogInfo("Radar assets loaded.");
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogError($"Error loading assets: {e}");
            }
        }

        /// <summary>
        /// Loads a prefab by its bundle path; falls back to a name scan over all bundle assets
        /// when the path lookup returns nothing, and pins the result against asset cleanup.
        /// </summary>
        private static GameObject LoadPrefab(string path, string name)
        {
            try
            {
                UnityEngine.Object asset = _bundle.LoadAsset(path);
                GameObject prefab = asset != null ? asset.TryCast<GameObject>() : null;
                if (prefab != null)
                    return Protect(prefab);

                RadarPlugin.Log.LogWarning(
                    $"Prefab '{path}' not resolved by path (asset null: {asset == null}); scanning bundle by name.");

                foreach (UnityEngine.Object candidate in _bundle.LoadAllAssets())
                {
                    GameObject gameObject = candidate != null ? candidate.TryCast<GameObject>() : null;
                    if (gameObject != null && gameObject.name == name)
                    {
                        RadarPlugin.Log.LogInfo($"Prefab '{name}' recovered by name scan.");
                        return Protect(gameObject);
                    }
                }
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogError($"Prefab load failed for '{path}': {e}");
            }

            return null;
        }

        /// <summary>Pins an asset so scene transitions' asset cleanup cannot unload it.</summary>
        private static GameObject Protect(GameObject prefab)
        {
            prefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return prefab;
        }

        private static Sprite[] LoadBlipSet(string name) => new[]
        {
            LoadSprite($"{name}.png"),
            LoadSprite($"{name}_up.png"),
            LoadSprite($"{name}_down.png"),
        };

        private static Sprite LoadSprite(string fileName)
        {
            string resourceName = ResourcePrefix + fileName;

            byte[] bytes = ReadResourceBytes(resourceName);
            if (bytes == null)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, true))
            {
                RadarPlugin.Log.LogError($"Failed to decode PNG resource: {resourceName}");
                return null;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        /// <summary>Reads an embedded resource into a managed byte array; null (and logs) when absent.</summary>
        private static byte[] ReadResourceBytes(string resourceName)
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                RadarPlugin.Log.LogError($"Missing embedded resource: {resourceName}");
                return null;
            }

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
    }
}
