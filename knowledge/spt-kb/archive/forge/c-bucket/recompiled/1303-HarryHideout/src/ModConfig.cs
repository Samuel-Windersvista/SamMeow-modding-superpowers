using System.Collections.Generic;

namespace _harryHideout;

public class ModConfig
{
	public float ItemPriceMultiplier { get; set; }

	public int TraderRefreshMin { get; set; }

	public int TraderRefreshMax { get; set; }

	public bool AddTraderToFlea { get; set; }

	public bool RandomizeBuyRestriction { get; set; }

	public bool RandomizeStockAvailable { get; set; }

	public int OutOfStockChance { get; set; }

	public List<string> IgnoreList { get; set; } = new List<string>();
}
