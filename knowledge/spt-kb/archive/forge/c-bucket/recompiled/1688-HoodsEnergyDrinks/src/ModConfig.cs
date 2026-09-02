using System.Collections.Generic;

namespace HoodsEnergyDrinks_CSharp;

public class ModConfig
{
	public required bool instant_energy_and_hydration { get; set; }

	public required bool use_alternate_buffs { get; set; }

	public required int alternate_trader_price { get; set; }

	public required int alternate_flea_price { get; set; }

	public required int alternate_handbook_price { get; set; }

	public required Dictionary<string, DrinkConfig> drinks { get; set; }
}
