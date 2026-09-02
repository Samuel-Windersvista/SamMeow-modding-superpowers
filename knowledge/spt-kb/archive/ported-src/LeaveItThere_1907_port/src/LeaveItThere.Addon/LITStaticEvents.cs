using EFT;
using LeaveItThere.Components;

namespace LeaveItThere.Addon;

public static class LITStaticEvents
{
	public delegate void ItemPlacedStateChangedHandler(FakeItem fakeItem, bool isPlaced);

	public delegate void FakeItemInitializedHandler(FakeItem fakeItem);

	public delegate void FakeItemReclaimedHandler(FakeItem fakeItem);

	public delegate void PlacedItemSpawnedHandler(FakeItem fakeItem);

	public delegate void OnRaidEndHandler(LocalRaidSettings settings, object results, object lostInsuredItems, object transferItems, string exitName);

	public delegate void LastPlacedItemSpawnedHandler(FakeItem fakeItem);

	public static event ItemPlacedStateChangedHandler OnItemPlacedStateChanged;

	public static event FakeItemInitializedHandler OnFakeItemInitialized;

	public static event FakeItemReclaimedHandler OnFakeItemReclaimed;

	public static event PlacedItemSpawnedHandler OnPlacedItemSpawned;

	public static event OnRaidEndHandler OnRaidEnd;

	public static event LastPlacedItemSpawnedHandler OnLastPlacedItemSpawned;

	internal static void InvokeOnItemPlacedStateChanged(FakeItem fakeItem, bool isPlaced)
	{
		LITStaticEvents.OnItemPlacedStateChanged?.Invoke(fakeItem, isPlaced);
		if (!isPlaced)
		{
			InvokeOnFakeItemReclaimed(fakeItem);
		}
	}

	internal static void InvokeOnFakeItemInitialized(FakeItem fakeItem)
	{
		LITStaticEvents.OnFakeItemInitialized?.Invoke(fakeItem);
	}

	internal static void InvokeOnFakeItemReclaimed(FakeItem fakeItem)
	{
		LITStaticEvents.OnFakeItemReclaimed?.Invoke(fakeItem);
	}

	internal static void InvokeOnPlacedItemSpawned(FakeItem fakeItem)
	{
		LITStaticEvents.OnPlacedItemSpawned?.Invoke(fakeItem);
	}

	internal static void InvokeOnRaidEnd(LocalRaidSettings settings, object results, object lostInsuredItems, object transferItems, string exitName)
	{
		LITStaticEvents.OnRaidEnd?.Invoke(settings, results, lostInsuredItems, transferItems, exitName);
	}

	internal static void InvokeOnLastPlacedItemSpawned(FakeItem fakeItem)
	{
		LITStaticEvents.OnLastPlacedItemSpawned?.Invoke(fakeItem);
	}
}
