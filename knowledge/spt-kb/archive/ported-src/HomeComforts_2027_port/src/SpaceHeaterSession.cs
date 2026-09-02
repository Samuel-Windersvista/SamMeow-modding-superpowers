using System.Collections.Generic;
using System.Linq;
using HomeComforts.Items.SpaceHeater;

internal class SpaceHeaterSession
{
	public List<SpaceHeater> SpaceHeaters = new List<SpaceHeater>();

	private List<string> _playerIsInSpaceHeaterItemIds = new List<string>();

	public List<string> PlayerIsInSpaceHeaterItemIds => _playerIsInSpaceHeaterItemIds;

	public bool PlayerIsInSpaceHeaterZone => _playerIsInSpaceHeaterItemIds.Count != 0;

	public void AddSpaceHeaterIdToPlayerIsIn(string id)
	{
		if (!_playerIsInSpaceHeaterItemIds.Contains(id))
		{
			_playerIsInSpaceHeaterItemIds.Add(id);
		}
	}

	public void RemoveSpaceHeaterIdFromPlayerIsIn(string id)
	{
		_playerIsInSpaceHeaterItemIds.Remove(id);
	}

	public SpaceHeater GetSpaceHeaterOrNull(string itemId)
	{
		return SpaceHeaters.FirstOrDefault((SpaceHeater heater) => heater.FakeItem.ItemId == itemId);
	}
}
