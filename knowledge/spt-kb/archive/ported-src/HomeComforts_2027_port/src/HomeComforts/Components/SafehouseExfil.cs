using System.Linq;
using EFT;
using EFT.Interactive;
using HomeComforts.Helpers;
using HomeComforts.Items.Safehouse;
using UnityEngine;

namespace HomeComforts.Components;

internal class SafehouseExfil : ExfiltrationPoint
{
	public Safehouse LastSafehouseThatUsedMe;

	public BoxCollider Collider { get; private set; }

	public bool ExfilIsEnabled => Collider.enabled;

	public static SafehouseExfil Create(string name)
	{
		GameObject gameObject = new GameObject
		{
			name = name + "_IEAPIIgnore",
			layer = LayerMask.NameToLayer("Triggers")
		};
		BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
		boxCollider.isTrigger = true;
		boxCollider.size = new Vector3(1f, 1f, 1f) * Helpers.Settings.ExfilSizeMultiplier.Value;
		boxCollider.enabled = false;
		gameObject.transform.position = new Vector3(0f, -999999f, 0f);
		SafehouseExfil safehouseExfil = gameObject.AddComponent<SafehouseExfil>();
		safehouseExfil.Collider = boxCollider;
		safehouseExfil.Settings.Name = name;
		return safehouseExfil;
	}

	private JsonType.BackendExitTriggerSettings GetSettings()
	{
		return new JsonType.BackendExitTriggerSettings
		{
			Name = Settings.Name,
			PassageRequirement = (ERequirementState)0,
			EventAvailable = false,
			EntryPoints = string.Join(",", Plugin.AllEntryPoints),
			ExfiltrationType = (EExfiltrationType)0,
			ExfiltrationTime = 7f,
			PlayersCount = 0,
			Chance = 100f,
			MinTime = 0f,
			MaxTime = 0f,
			RequirementTip = ""
		};
	}

	public void InitCustomExfil()
	{
		LoadSettings(MongoID.Generate(true), GetSettings(), true);
	}

	public void SetCustomExfilEnabled(bool enabled)
	{
		Collider.enabled = enabled;
		if (enabled && (EligibleEntryPoints == null || EligibleEntryPoints.Length == 0))
		{
			EligibleEntryPoints = new string[1] { HCSession.Instance.Player.Profile.Info.EntryPoint.ToLower() };
		}
		HomeComfortsUtils.ForceUpdatePlayerCollisions();
	}
}
