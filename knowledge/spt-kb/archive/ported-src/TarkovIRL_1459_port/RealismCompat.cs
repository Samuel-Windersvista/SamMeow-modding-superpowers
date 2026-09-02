// TarkovIRL 1459 -> SPT 4.1.2 port
// RealismMod dependency shim: target environment has no RealismMod 4.1,
// so every Realism API used by TarkovIRL is emulated with vanilla-4.1
// fallback values (same approach as the FOV-Fix 701 port "Realism 兼容层裁剪").
// Behaviour note: without RealismMod installed, stance/hydration driven
// modifiers become neutral (1f / false), which matches the "关现实主义" pack.

using Comfort.Common;
using EFT;

namespace TarkovIRL;

public enum EStance
{
	None,
	LowReady,
	HighReady,
	ShortStock,
	ActiveAiming,
	PatrolStance,
	Melee,
	PistolCompressed
}

public static class StanceController
{
	public static EStance CurrentStance => EStance.None;

	public static bool IsMounting => false;

	public static bool IsLeftShoulder => false;

	public static bool IsDoingTacSprint => false;

	public static float ActiveAimManipBuff => 1f;
}

public static class PlayerState
{
	public static float ReloadSkillMulti = 1f;

	public static float GearErgoPenalty = 1f;
}

public static class WeaponStats
{
	public static float Balance => 0f;

	public static float CurrentMagReloadSpeed => 1f;

	public static string _WeapClass => WeaponController.IsPistol ? "pistol" : "rifle";

	public static bool HasShoulderContact => !WeaponController.IsPistol && !WeaponController.IsStockFolded;
}

public static class PluginConfig
{
	public static float GlobalCheckAmmoMulti => 1f;

	public static float GlobalCheckAmmoPistolSpeedMulti => 1f;
}

public static class GameWorldController
{
	public static float TimeInRaid
	{
		get
		{
			GameWorld gw = Singleton<GameWorld>.Instance;
			if (gw == null || gw.GameDateTime == null)
			{
				return 0f;
			}
			return 1f;
		}
	}
}
