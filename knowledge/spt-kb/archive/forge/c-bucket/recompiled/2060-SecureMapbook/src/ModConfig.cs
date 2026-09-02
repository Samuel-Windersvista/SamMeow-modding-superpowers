using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace securemapbooke.Models;

public class ModConfig
{
	public bool EnableDebugging { get; set; }

	public int LoyaltyLevelBuy { get; set; } = 2;

	public int Price { get; set; } = 500000;

	public int LoyaltyLevelBarter { get; set; } = 1;

	public SizeConfig Size { get; set; } = new SizeConfig
	{
		Width = 1,
		Height = 2
	};

	public bool AllowInsurance { get; set; }

	public bool AllowInSecureContainers { get; set; } = true;

	public bool AllowInOrganizationalPouchs { get; set; } = true;

	public bool AllowInSpecialSlots { get; set; } = true;

	[JsonIgnore]
	public Dictionary<string, string> SecureContainers { get; set; } = new Dictionary<string, string>();

	[JsonIgnore]
	public List<string> SpecialSlotsList { get; set; } = new List<string>();

	[JsonIgnore]
	public Dictionary<string, string> OrganizationalPouch { get; set; } = new Dictionary<string, string>();

	[JsonIgnore]
	public Dictionary<string, CustomLocaleDetails> Locales { get; set; } = new Dictionary<string, CustomLocaleDetails>();

	public List<BarterItemConfig> BarterItems { get; set; } = new List<BarterItemConfig>();
}
