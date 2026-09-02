using System;
using BepInEx;

namespace SamSWAT.ReflexSightsRework
{
    [BepInPlugin("com.samswat.reflexsightsrework", "SamSWAT.ReflexSightsRework", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private static Patch _patchInstance;

        private void Awake()
        {
            _patchInstance = new Patch();
            _patchInstance.Enable();
        }

        private void OnDestroy()
        {
            foreach (var obj in Patch.Instances)
            {
                Logger.LogDebug($"Destroying {obj}");
                Destroy(obj);
            }
            _patchInstance.Disable();
        }
    }
}