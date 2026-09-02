using EFT;
using UnityEngine;

namespace TarkovIRL;

public static class ParallaxController
{
	private static readonly float _ParallaxSetSizeFixed = 0.2f;

	private static float _rotAvgXSet = 0f;

	private static float _rotAvgYSet = 0f;

	private static float _rotAvgX = 0f;

	private static float _rotAvgY = 0f;

	private static float _posLerpXTarget = 0f;

	private static float _posLerpYTarget = 0f;

	private static float _rotLerpXTarget = 0f;

	private static float _rotLerpYTarget = 0f;

	private static float _posLerpX = 0f;

	private static float _posLerpY = 0f;

	private static float _rotLerpX = 0f;

	private static float _rotLerpY = 0f;

	private static Vector2 _playerRotationLastFrame = Vector2.zero;

	private static float _parallaxWeightADS = 1f;

	private static bool _aimingLastFrame = false;

	private static Player _player = null;

	public static void Update(float dt)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Invalid comparison between Unknown and I4
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Invalid comparison between Unknown and I4
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Invalid comparison between Unknown and I4
		if (!(_player == null))
		{
			Vector2 vector = _playerRotationLastFrame - _player.Rotation;
			vector *= dt;
			_playerRotationLastFrame = _player.Rotation;
			float num = (((int)StanceController.CurrentStance == 3) ? 2f : 1f);
			float num2 = _ParallaxSetSizeFixed * PrimeMover.ParallaxSetSizeMulti.Value * RealismWrapper.WeaponBalanceMulti * num;
			_rotAvgXSet += vector.x;
			_rotAvgYSet += vector.y;
			_rotAvgXSet -= _rotAvgX;
			_rotAvgYSet -= _rotAvgY;
			_rotAvgXSet = Mathf.Clamp(_rotAvgXSet, 0f - num2, num2);
			_rotAvgYSet = Mathf.Clamp(_rotAvgYSet, 0f - num2, num2);
			_rotAvgX = _rotAvgXSet * dt;
			_rotAvgY = _rotAvgYSet * dt;
			_parallaxWeightADS = ParallaxAdsController.ParallaxWeight;
			float num3 = (WeaponController.IsPistol ? PrimeMover.PistolSpecificParallax.Value : 1f);
			float num4 = Mathf.Pow(1f - Mathf.Clamp01(PlayerMotionController.RotationDelta / 0.1f), 2f);
			float num5 = (((int)StanceController.CurrentStance == 1) ? 0f : 1f);
			float num6 = (((int)StanceController.CurrentStance == 2) ? 0.25f : 1f);
			float num7 = (((int)StanceController.CurrentStance == 4) ? 0.5f : 1f);
			float f = PrimeMover.ParallaxMulti.Value * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier * num3 * num4 * num5 * num6 * num7;
			f = Mathf.Pow(f, 2f) / 100f;
			float t = dt * PrimeMover.ParallaxDTMulti.Value * EfficiencyController.EfficiencyModifierInverse;
			_posLerpXTarget = Mathf.Lerp(_posLerpXTarget, _rotAvgX * f, t);
			_posLerpYTarget = Mathf.Lerp(_posLerpYTarget, _rotAvgY * f, t);
			_rotLerpXTarget = Mathf.Lerp(_rotLerpXTarget, _rotAvgX * f, t);
			_rotLerpYTarget = Mathf.Lerp(_rotLerpYTarget, _rotAvgY * f, t);
			float num8 = (WeaponController.IsPistol ? PrimeMover.ParallaxHardClampPistols.Value : PrimeMover.ParallaxHardClamp.Value);
			float num9 = (WeaponController.IsPistol ? (PrimeMover.ParallaxHardClampPistols.Value * 0.5f) : (PrimeMover.ParallaxHardClamp.Value * 0.5f));
			_rotLerpXTarget = Mathf.Clamp(_rotLerpXTarget, 0f - num8, num8);
			_rotLerpYTarget = Mathf.Clamp(_rotLerpYTarget, 0f - num8, num8);
			_posLerpXTarget = Mathf.Clamp(_posLerpXTarget, 0f - num9, num9);
			_posLerpYTarget = Mathf.Clamp(_posLerpYTarget, 0f - num9, num9);
			_rotLerpX = Mathf.Lerp(_rotLerpX, _rotLerpXTarget, dt * PrimeMover.ParallaxRotationSmoothingMulti.Value);
			_rotLerpY = Mathf.Lerp(_rotLerpY, _rotLerpYTarget, dt * PrimeMover.ParallaxRotationSmoothingMulti.Value);
			_posLerpX = Mathf.Lerp(_posLerpX, _posLerpXTarget, dt * PrimeMover.ParallaxRotationSmoothingMulti.Value);
			_posLerpY = Mathf.Lerp(_posLerpY, _posLerpYTarget, dt * PrimeMover.ParallaxRotationSmoothingMulti.Value);
		}
	}

	public static void GetModifiedHandPosRotParallax(Player player, ref Vector3 position, ref Quaternion rotation)
	{
		if (_player == null)
		{
			_player = player;
		}
		if (_player == null)
		{
			return;
		}
		if (AnimStateController.IsBlindfire)
		{
			position = Vector3.zero;
			rotation = Quaternion.identity;
			return;
		}
		if (WeaponController.IsUsingMounted)
		{
			position = Vector3.zero;
			rotation = Quaternion.identity;
			return;
		}
		float weaponMulti = WeaponController.GetWeaponMulti(getInverse: true);
		float efficiencyModifierInverse = EfficiencyController.EfficiencyModifierInverse;
		float wpnMulti = weaponMulti * efficiencyModifierInverse;
		bool isAiming = player.ProceduralWeaponAnimation.IsAiming;
		if (WeaponController.HasCheekWeld())
		{
			bool flag = !_aimingLastFrame && isAiming;
			bool flag2 = _aimingLastFrame && !isAiming;
			_aimingLastFrame = isAiming;
			if (flag)
			{
				ParallaxAdsController.StartNewAds(intoAds: true, wpnMulti);
			}
			else if (flag2)
			{
				ParallaxAdsController.StartNewAds(intoAds: false, wpnMulti);
			}
		}
		rotation = Quaternion.Euler(0f, (0f - _rotLerpY) * 100f * _parallaxWeightADS, (0f - _rotLerpX) * 100f * _parallaxWeightADS);
		position = new Vector3(_posLerpX * _parallaxWeightADS, (0f - _posLerpY) * _parallaxWeightADS, 0f);
	}
}
