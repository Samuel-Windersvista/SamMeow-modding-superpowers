using LeaveItThere.Helpers;
using UnityEngine;

namespace LeaveItThere.Components;

public class Moveable : MonoBehaviour
{
	private int _frameCountBeforePausable;

	private bool _pausable = true;

	public Rigidbody Rigidbody { get; private set; }

	public bool PhysicsIsEnabled => !Rigidbody.isKinematic;

	private void Awake()
	{
		Rigidbody = GameObjectExtensions.GetOrAddComponent<Rigidbody>(((Component)this).gameObject);
		// PORT-NOTE: GClass723.SupportRigidbody had no 4.1 equivalent; removed.
		DisablePhysics();
	}

	private void FixedUpdate()
	{
		if (_pausable)
		{
			TryPausePhysics();
		}
	}

	private void TryPausePhysics()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		_frameCountBeforePausable++;
		if (_frameCountBeforePausable < Settings.FramesToWakeUpPhysicsObject.Value)
		{
			return;
		}
		Vector3 val = Rigidbody.velocity;
		if (val.sqrMagnitude < Settings.RigidbodySleepThreshold.Value)
		{
			val = Rigidbody.angularVelocity;
			if (val.sqrMagnitude < Settings.RigidbodySleepThreshold.Value)
			{
				_frameCountBeforePausable = 0;
				DisablePhysics();
			}
		}
	}

	public void SetPhysicsEnabled(bool enabled, bool pausable = true)
	{
		if (enabled)
		{
			EnablePhysics(pausable);
		}
		else
		{
			DisablePhysics();
		}
	}

	public void EnablePhysics(bool pausable)
	{
		((Behaviour)this).enabled = true;
		_pausable = pausable;
		Rigidbody.isKinematic = false;
		Rigidbody.collisionDetectionMode = (CollisionDetectionMode)0;
	}

	public void DisablePhysics()
	{
		Rigidbody.collisionDetectionMode = (CollisionDetectionMode)3;
		Rigidbody.isKinematic = true;
		((Behaviour)this).enabled = false;
	}

	public void MoveToPlayer()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).gameObject.transform.position = LITUtils.PlayerFront;
	}

	public void ResetRotation()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		SetRotation(Quaternion.identity);
	}

	public void SetRotation(Quaternion rotation)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).gameObject.transform.rotation = rotation;
	}
}
