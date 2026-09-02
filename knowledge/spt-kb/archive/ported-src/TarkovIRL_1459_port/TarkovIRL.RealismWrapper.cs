using UnityEngine;

namespace TarkovIRL;

// RealismMod integration shim (stripped for the SPT 4.1.2 port).
// The target pack runs with Realism disabled, so every Realism-backed
// modifier is neutral here. Re-enable by restoring the RealismMod
// dependency and wiring these members to RealismMod's statics.
internal class RealismWrapper
{
	public static bool IsAdrenaline => false;

	public static float WeaponBalanceMulti => 1f;

	public static bool IsTacSprint => false;

	public static bool IsOverdose => false;

	public static float GetRealismReloadSpeed()
	{
		return Mathf.Clamp(EfficiencyController.EfficiencyModifierInverse, 0.65f, 1.35f);
	}

	public static float GetRealismCheckMagSpeed()
	{
		return Mathf.Clamp(EfficiencyController.EfficiencyModifierInverse, 0.7f, 1.35f);
	}

	public static bool IsShoulderContact()
	{
		return !WeaponController.IsPistol && !WeaponController.IsStockFolded;
	}
}
