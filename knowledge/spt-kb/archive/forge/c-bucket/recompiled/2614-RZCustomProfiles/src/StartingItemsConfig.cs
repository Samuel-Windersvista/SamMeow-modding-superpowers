using System.Collections.Generic;

namespace RZCustomProfiles;

public record StartingItemsConfig
{
	public bool Enabled { get; set; } = true;

	public List<ItemEntry> Items { get; set; } = new List<ItemEntry>();
}
