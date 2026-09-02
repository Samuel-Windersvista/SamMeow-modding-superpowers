using UnityEngine;

namespace TarkovIRL;

public static class SwayController
{
	private static readonly float _BaseSwayLerpSpeed = 1f;

	public static bool IsSwayUpdatedThisFrame = false;

	private static float _addedSwayTarget = 0f;

	private static float _addedSwayLerp = 0f;

	public static void UpdateLerp(float dt)
	{
		_addedSwayLerp = Mathf.Lerp(_addedSwayLerp, _addedSwayTarget, dt * _BaseSwayLerpSpeed);
	}

	public static Vector3 GetNewSway(Vector3 newSwayFactors, bool isAiming)
	{
		if (!PrimeMover.IsWeaponSway.Value)
		{
			return Vector3.zero;
		}
		if (AnimStateController.IsBlindfire)
		{
			return Vector3.zero;
		}
		float weaponMulti = WeaponController.GetWeaponMulti(getInverse: false);
		newSwayFactors.x = 0f;
		newSwayFactors.y *= -0.2f * weaponMulti;
		newSwayFactors.z = 0f;
		return newSwayFactors;
	}
}
