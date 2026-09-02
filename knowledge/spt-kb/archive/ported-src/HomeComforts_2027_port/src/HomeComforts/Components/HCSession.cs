using System;
using Comfort.Common;
using EFT;
using HomeComforts.Items.SpaceHeater;
using LeaveItThere.Components;
using UnityEngine;

namespace HomeComforts.Components;

internal class HCSession : MonoBehaviour
{
	public SafehouseExfil CustomSafehouseExfil;

	public Vector3 InitialExfilPosition = new Vector3(-1E+09f, -1E+09f, -1E+09f);

	private Player _player;

	private GamePlayerOwner _gamePlayerOwner;

	private static HCSession _instance;

	public SafehouseSession SafehouseSession { get; private set; } = new SafehouseSession();

	public SpaceHeaterSession SpaceHeaterSession { get; private set; } = new SpaceHeaterSession();

	public NeedsRateReductions NeedsRateReductions { get; private set; } = new NeedsRateReductions();

	public GameWorld GameWorld { get; private set; }

	public Player Player
	{
		get
		{
			if (_player == null)
			{
				_player = GameWorld.MainPlayer;
			}
			return _player;
		}
	}

	public GamePlayerOwner GamePlayerOwner
	{
		get
		{
			if (_gamePlayerOwner == null)
			{
				_gamePlayerOwner = Player.gameObject.GetComponent<GamePlayerOwner>();
			}
			return _gamePlayerOwner;
		}
	}

	public static HCSession Instance
	{
		get
		{
			if (!Singleton<GameWorld>.Instantiated)
			{
				throw new Exception("Tried to get HomeComfortsSession when game world was not instantiated!");
			}
			if (_instance == null)
			{
				_instance = GameObjectExtensions.GetOrAddComponent<HCSession>(Singleton<GameWorld>.Instance.gameObject);
			}
			return _instance;
		}
	}

	private HCSession()
	{
	}

	private void Awake()
	{
		GameWorld = Singleton<GameWorld>.Instance;
	}

	public static void OnRaidEnd(LocalRaidSettings settings, object results, object lostInsuredItems, object transferItems, string exitName)
	{
		SafehouseSession.OnRaidEnd(settings, results, lostInsuredItems, transferItems, exitName);
	}

	public static void OnLastPlacedItemSpawned(FakeItem fakeItem)
	{
		SafehouseSession.OnLastPlacedItemSpawned(fakeItem);
	}
}
