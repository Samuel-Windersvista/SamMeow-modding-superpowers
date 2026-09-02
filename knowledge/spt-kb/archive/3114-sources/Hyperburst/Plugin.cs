using BepInEx;

namespace Mass.HyperBurst
{
    ///<summary>Plugin</summary>
    [BepInPlugin("com.mass.hyperburst", "Mass.HyperBurst", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new Patches.ShootPatch().Enable();
            new Patches.ShotVectorPatch().Enable();
            new Patches.FireRatePatch().Enable();
            new Patches.UpdateWeaponVariablesPatch().Enable();
            new Patches.FireBulletPatch().Enable();
            new Patches.UpdatePitch().Enable();
        }
    }
}