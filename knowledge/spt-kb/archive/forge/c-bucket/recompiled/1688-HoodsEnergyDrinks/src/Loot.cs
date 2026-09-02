using System.Collections.Generic;

namespace HoodsEnergyDrinks_CSharp;

internal class Loot
{
	public Dictionary<string, StaticLoot> StaticLoot { get; set; } = new Dictionary<string, StaticLoot>();

	public Dictionary<string, float> LooseLoot { get; set; } = new Dictionary<string, float>();
}
