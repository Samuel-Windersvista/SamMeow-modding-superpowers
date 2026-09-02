using UnityEngine;

namespace TarkovIRL;

internal class HeadRotController
{
	private static Vector3 _headRotLerp = Vector3.zero;

	private static Vector3 _headRotLerpTarget = Vector3.zero;

	private static readonly float _HeadRotLerpSpeed = 7f;

	public static void UpdateLerp(float dt)
	{
		_headRotLerp = Vector3.Lerp(_headRotLerp, _headRotLerpTarget, dt * _HeadRotLerpSpeed);
	}

	public static Vector3 GetHeadRotThisFrame(Vector3 headRotThisFrame)
	{
		if (ThrowController.IsThrowing)
		{
			_headRotLerpTarget = ThrowController.GetThrowOffset;
		}
		else
		{
			_headRotLerpTarget = new Vector3(0f, 0f, PlayerMotionController.LeanNormal * PrimeMover.LeanCounterRotateMod.Value);
		}
		Vector3 augmentedReloadHeadOffset = AugmentedReloadController.GetAugmentedReloadHeadOffset();
		_headRotLerpTarget += augmentedReloadHeadOffset;
		if (PrimeMover.IsHeadTiltADS.Value)
		{
			ParallaxAdsController.GetParallaxADSHeadTilt(out var Y, out var Z);
			_headRotLerpTarget.y += Y;
			_headRotLerpTarget.z += Z;
		}
		return headRotThisFrame + _headRotLerp;
	}
}
