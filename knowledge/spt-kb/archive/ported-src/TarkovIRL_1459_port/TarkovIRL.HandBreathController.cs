using EFT;
using UnityEngine;

namespace TarkovIRL;

internal class HandBreathController
{
	private static readonly float _BreathSpeedModifier = 0.55f;

	private static readonly float _BreathVerticalOffsetModifier = 0.0075f;

	private static float _breathUpdateTimer = 0f;

	public static Vector3 GetModifiedHandPosForBreath(Player player)
	{
		AnimationCurve breathCurve = PrimeMover.Instance.BreathCurve;
		float num = player.Physical.Stamina.Current / 104f;
		float num2 = Mathf.Clamp(1f - num, 0.15f, 1f);
		float num3 = (RealismWrapper.IsAdrenaline ? 1.25f : 1f);
		float num4 = 1f + num2 * num3;
		bool flag = _breathUpdateTimer < 0.55f && _breathUpdateTimer > 0.26f && PlayerMotionController.IsHoldingBreath;
		flag &= PlayerMotionController.IsAiming;
		flag = (PlayerMotionController.IsAugmentedBreath = flag & (player.Physical.Stamina.NormalValue > 0.01f));
		float num5 = (RealismWrapper.IsAdrenaline ? 0.2f : 0.4f);
		float num6 = (flag ? num5 : 1f);
		_breathUpdateTimer += player.DeltaTime * num4 * _BreathSpeedModifier * num6;
		if (_breathUpdateTimer >= 1f)
		{
			_breathUpdateTimer -= 1f;
		}
		float num7 = breathCurve.Evaluate(_breathUpdateTimer);
		float num8 = Mathf.Clamp(num2, 0.025f, 1f);
		float y = num7 * _BreathVerticalOffsetModifier * num8 * PrimeMover.BreathingEffectMulti.Value;
		return new Vector3(0f, y, 0f);
	}
}
