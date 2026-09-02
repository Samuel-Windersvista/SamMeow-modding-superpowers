using EFT;
using UnityEngine;

namespace TarkovIRL;

internal static class DeadzoneController
{
	private static readonly float _LerpRate = 10f;

	private static readonly float _RotDeltaThresh = 0.0002f;

	private static float _deadZoneLerp = 0f;

	private static bool _updateDZ = true;

	public static bool DeadzoneUpdatedThisFrame = false;

	private static float ProcessHeadDelta(float rawHeadDelta)
	{
		float num = rawHeadDelta / WeaponController.CurrentWeaponErgoNorm / 10f;
		return num * WeaponController.CurrentWeaponWeight * 0.1f;
	}

	public static Vector3 GetHeadRotationWithDeadzone(Player player, float deadzoneSetting, Vector3 headRotInitial)
	{
		return Vector3.zero;
	}
}
