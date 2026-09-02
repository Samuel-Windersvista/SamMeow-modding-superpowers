using System.Collections.Generic;
using securemapbooke.Models;

namespace securemapbooke;

public static class ItemIds
{
	public static class Maps
	{
		public const string GroundZero = "6738033eb7305d3bdafe9518";

		public const string Streets = "673803448cb3819668d77b1b";

		public const string Reserve = "6738034a9713b5f42b4a8b78";

		public const string Labs = "6738034e9d22459ad7cd1b81";

		public const string Lighthouse = "6738035350b24a4ae4a57997";

		public const string Factory = "574eb85c245977648157eec3";

		public const string Woods = "5900b89686f7744e704a8747";

		public const string Interchange = "5be4038986f774527d3fae60";

		public const string Shoreline = "5a8036fb86f77407252ddc02";

		public const string Customs = "5798a2832459774b53341029";

		public const string Sanatorium = "5a80a29286f7742b25692012";

		public const string Labyrinth = "68f1ad32317cc52f4c0b6fae";
	}

	public static class Barter
	{
		public static readonly List<BarterItemConfig> Items = new List<BarterItemConfig>
		{
			new BarterItemConfig
			{
				ItemId = "62a0a124de7ac81993580542",
				Count = 2
			},
			new BarterItemConfig
			{
				ItemId = "5bc9c049d4351e44f824d360",
				Count = 1
			},
			new BarterItemConfig
			{
				ItemId = "590c621186f774138d11ea29",
				Count = 2
			}
		};
	}

	public static class OrganizationalPouches
	{
		public static readonly Dictionary<string, string> AllowedPouches = new Dictionary<string, string>
		{
			{ "S I C C organizational pouch", "5d235bb686f77443f4331278" },
			{ "Documents case", "590c60fc86f77412b13fddcf" }
		};
	}

	public static class Templates
	{
		public const string MoneyRoubles = "5449016a4bdc2d6f028b456f";

		public const string SecureContainerParent = "5448bf274bdc2dfc2f8b456a";
	}

	public const string CloneId = "5f4f9eb969cdc30ff33f09db";

	public const string ParentId = "55818a104bdc2db9688b4569";

	public const string HandbookParentId = "5b47574386f77428ca22b345";

	public const string MapbookItemId = "6621a2e3a8d8b1a9f0e3b4c5";

	public const string TraderId = "54cb50c76803fa8b248b4571";
}
