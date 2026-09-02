using System.Collections.Generic;
using Comfort.Common;
using EFT;
using LeaveItThere.Fika;
using Newtonsoft.Json;

namespace LeaveItThere.Common;

internal class PlacedItemDataPack
{
	public string ProfileId;

	public string MapId;

	public List<PlacedItemData> ItemTemplates;

	public Dictionary<string, object> GlobalAddonData = new Dictionary<string, object>();

	[JsonIgnore]
	public static PlacedItemDataPack Request => new PlacedItemDataPack(new Dictionary<string, object>());

	public PlacedItemDataPack()
	{
	}

	public PlacedItemDataPack(Dictionary<string, object> globalAddonData, List<PlacedItemData> itemTemplates = null)
	{
		ProfileId = FikaBridge.GetRaidId();
		MapId = Singleton<GameWorld>.Instance.LocationId;
		ItemTemplates = new List<PlacedItemData>();
		GlobalAddonData = globalAddonData;
		if (itemTemplates != null)
		{
			ItemTemplates.AddRange(itemTemplates);
		}
	}
}
