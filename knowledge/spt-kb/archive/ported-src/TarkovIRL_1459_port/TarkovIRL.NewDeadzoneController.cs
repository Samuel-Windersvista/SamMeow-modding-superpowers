using UnityEngine;

namespace TarkovIRL;

internal class NewDeadzoneController
{
	private static float _rotDeltaHistory;

	private static float _rotDeltaSmoothed;

	private static float _rotDeltaSmoothedInDeltaTime;

	public static void Update(float fdt)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Invalid comparison between Unknown and I4
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Invalid comparison between Unknown and I4
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Invalid comparison between Unknown and I4
		float num = PlayerMotionController.HorizontalRotationDelta * 100f;
		_rotDeltaHistory += num;
		_rotDeltaHistory -= _rotDeltaSmoothed;
		_rotDeltaSmoothed = _rotDeltaHistory * fdt * 9f;
		float num2 = PrimeMover.WeaponDeadzoneMulti.Value * WeaponController.GetWeaponMulti(getInverse: false);
		if (WeaponController.HasCheekWeld() && PlayerMotionController.IsAiming)
		{
			num2 *= PrimeMover.DeadzoneInADS.Value;
		}
		else if ((int)StanceController.CurrentStance == 3)
		{
			num2 *= PrimeMover.DeadzoneInShortStock.Value;
		}
		else if ((int)StanceController.CurrentStance == 4)
		{
			num2 *= PrimeMover.DeadzoneInActiveAim.Value;
		}
		else if ((int)StanceController.CurrentStance == 0)
		{
			num2 *= PrimeMover.DeadzoneInVanilla.Value;
		}
		else if ((int)StanceController.CurrentStance == 2)
		{
			num2 *= PrimeMover.DeadzoneInHighReady.Value;
		}
		else if ((int)StanceController.CurrentStance == 1)
		{
			num2 *= PrimeMover.DeadzoneInLowReady.Value;
		}
		float num3 = (PrimeMover.DeadzoneWeightForEfficiency.Value ? EfficiencyController.EfficiencyModifierInverse : 1f);
		_rotDeltaSmoothedInDeltaTime = Mathf.Lerp(_rotDeltaSmoothedInDeltaTime, _rotDeltaSmoothed * num2, fdt * PrimeMover.DeadzoneHeadFollowSpeedMulti.Value * num3);
	}

	public static Vector3 GetHeadRotWithDeadzone(Vector3 headRotInitial)
	{
		Vector3 result = headRotInitial;
		result.y += _rotDeltaSmoothedInDeltaTime;
		return result;
	}
}
