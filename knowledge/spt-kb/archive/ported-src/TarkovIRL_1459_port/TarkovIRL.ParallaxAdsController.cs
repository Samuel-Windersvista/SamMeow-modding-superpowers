using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovIRL;

public static class ParallaxAdsController
{
	private static float _parallaxWeight = 1f;

	private static bool _intoAds = false;

	private static float _adsSpeedMod = 1f;

	private static float _adsLerpWeight = 1f;

	private static bool _shotSwitch = false;

	private static bool _intoShot = false;

	private static float _shotLerp = 1f;

	private static float _shotWeight = 1f;

	private static readonly float _HeadTiltValY = 2f;

	private static readonly float _HeadTiltValZ = 6f;

	public static float ParallaxWeight => _parallaxWeight;

	public static void UpdateLerps(float dt)
	{
		LerpAds(dt);
		LerpShot(dt);
		DefineParallaxWeight();
	}

	public static void StartNewAds(bool intoAds, float wpnMulti)
	{
		_intoAds = intoAds;
		_adsSpeedMod = wpnMulti;
	}

	private static void LerpAds(float dt)
	{
		float b = (_intoAds ? PrimeMover.ParallaxInAds.Value : 1f);
		_adsLerpWeight = Mathf.Lerp(_adsLerpWeight, b, dt * _adsSpeedMod * PrimeMover.AdsParallaxTimeMulti.Value);
	}

	private static void LerpShot(float dt)
	{
		if (_intoShot)
		{
			_shotLerp = Mathf.Lerp(_shotLerp, _shotWeight * 1.05f, dt * _shotWeight * PrimeMover.ShotParallaxResetTimeMulti.Value * 3f);
			if (_shotLerp >= _shotWeight * 0.95f)
			{
				_intoShot = false;
			}
		}
		else
		{
			_shotLerp = Mathf.Lerp(_shotLerp, _adsLerpWeight * 0.95f, dt * (1f / _shotWeight) * PrimeMover.ShotParallaxResetTimeMulti.Value);
			if (_shotLerp <= _adsLerpWeight)
			{
				_shotSwitch = false;
			}
		}
	}

	public static void StartNewShot(Weapon weapon)
	{
		float num = weapon.Weight * PrimeMover.ShotParallaxWeaponWeightMulti.Value;
		float bulletMassGram = weapon.CurrentAmmoTemplate.BulletMassGram;
		float shotWeight = bulletMassGram / num;
		_shotWeight = shotWeight;
		_shotSwitch = true;
		_intoShot = true;
	}

	private static void DefineParallaxWeight()
	{
		_parallaxWeight = (_shotSwitch ? _shotLerp : _adsLerpWeight);
	}

	public static void GetParallaxADSHeadTilt(out float Y, out float Z)
	{
		if (PlayerMotionController.IsAiming && WeaponController.HasCheekWeld())
		{
			float num = (StanceController.IsLeftShoulder ? (-1f) : 1f);
			Y = _HeadTiltValY * _adsLerpWeight * num;
			Z = _HeadTiltValZ * _adsLerpWeight * num;
		}
		else
		{
			Y = 0f;
			Z = 0f;
		}
	}
}
