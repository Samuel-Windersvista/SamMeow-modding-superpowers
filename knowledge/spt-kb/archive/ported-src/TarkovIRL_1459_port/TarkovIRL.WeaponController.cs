using EFT;
using EFT.InventoryLogic;

namespace TarkovIRL;

public static class WeaponController
{
	public static float CurrentWeaponErgoNorm = 0f;

	public static float CurrentWeaponWeight = 0f;

	public static bool IsStocked = false;

	public static bool IsStockFolded = false;

	public static bool IsPistol = false;

	public static bool SwayThisFrame = false;

	public static bool IsFoldable = false;

	public static bool IsUsingMounted = false;

	private static int _currentWeaponHash = 0;

	private static readonly int _MP5KHash = 25347301;

	public static int WeaponHash => _currentWeaponHash;

	public static void UpdateWpnStats(Player.FirearmController fc)
	{
		if (fc != null)
		{
			CurrentWeaponWeight = PrimeMover.Instance.WeightAttenuationCurve.Evaluate(fc.Weapon.TotalWeight);
			CurrentWeaponErgoNorm = PrimeMover.Instance.ErgoAttenuationCurve.Evaluate(fc.TotalErgonomics / 100f);
			IsStocked = CheckForStock(fc.Weapon);
		}
		else
		{
			CurrentWeaponWeight = 0f;
			CurrentWeaponErgoNorm = 1f;
		}
	}

	public static void ToggleFolded()
	{
		if (IsFoldable && AnimStateController.WeaponState == AnimStateController.EWeaponState.IDLE)
		{
			IsStockFolded = !IsStockFolded;
		}
	}

	private static bool CheckForStock(Weapon weapon)
	{
		if (IsPistol = weapon.WeapClass == "pistol")
		{
			return false;
		}
		if (_currentWeaponHash == _MP5KHash)
		{
			return false;
		}
		if (weapon.GetFoldable() != null)
		{
			IsFoldable = true;
			IsStockFolded = weapon.Folded;
		}
		return true;
	}

	public static float GetWeaponMulti(bool getInverse)
	{
		float num = CurrentWeaponWeight * (1f - CurrentWeaponErgoNorm);
		if (getInverse)
		{
			return 1f / num;
		}
		return num;
	}

	public static void SetCurrentWeaponHash(int weaponHash)
	{
		_currentWeaponHash = weaponHash;
	}

	public static bool HasCheekWeld()
	{
		return RealismWrapper.IsShoulderContact();
	}
}
