using UnityEngine;

namespace tarkin
{
    /// <summary>
    /// Compile-time stub for the 3.11 companion plugin "AssetBundleLoader" (tarkin toolchain).
    /// That dll is not part of the SPT 4.1.2 ref set (Refs-410), so this type provides the
    /// surface the plugin compiled against. A functional 4.1 deployment still needs the real
    /// AssetBundleLoader plugin shipped next to hideoutcat.bepinex.dll, otherwise the cat
    /// bundles can never be loaded (LoadAssetBundle returns null and the cat is not spawned).
    /// </summary>
    public static class AssetBundleLoader
    {
        public static AssetBundle LoadAssetBundle(string bundleName)
        {
            return null;
        }

        public static void ReplaceShadersToNative(GameObject go)
        {
        }
    }
}
