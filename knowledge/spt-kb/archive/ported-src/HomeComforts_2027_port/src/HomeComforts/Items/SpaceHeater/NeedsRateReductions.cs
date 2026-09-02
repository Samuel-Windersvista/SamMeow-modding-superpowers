using System.Collections;
using Comfort.Common;
using EFT;
using HomeComforts.Components;
using HomeComforts.Helpers;
using UnityEngine;

namespace HomeComforts.Items.SpaceHeater;

public class NeedsRateReductions
{
	private Coroutine _routine;

	private float _hydrationBuffPerTick = Settings.SpaceHeaterHydrationBuff.Value / 4f;

	private float _energyBuffPerTick = Settings.SpaceHeaterEnergyBuff.Value / 4f;

	private WaitForSeconds _waitFor15Seconds = new WaitForSeconds(15f);

	public bool Enabled { get; private set; }

	public void SetEnabled(bool enabled)
	{
		if (Enabled == enabled)
		{
			return;
		}
		Enabled = enabled;
		if (Enabled)
		{
			_routine = EFT.StaticManager.BeginCoroutine(BuffRoutine());
			return;
		}
		if (_routine != null)
		{
			EFT.StaticManager.KillCoroutine(_routine);
			_routine = null;
		}
	}

	private IEnumerator BuffRoutine()
	{
		while (true)
		{
			yield return _waitFor15Seconds;
			if (!Singleton<GameWorld>.Instantiated)
			{
				break;
			}
			HCSession.Instance.Player.ActiveHealthController.ChangeHydration(_hydrationBuffPerTick);
			HCSession.Instance.Player.ActiveHealthController.ChangeEnergy(_energyBuffPerTick);
		}
	}
}
