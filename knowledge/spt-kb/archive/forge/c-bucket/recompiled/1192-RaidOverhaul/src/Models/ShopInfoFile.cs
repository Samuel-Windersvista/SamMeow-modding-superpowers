using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace RaidOverhaulMain.Models;

public class ShopInfoFile
{
	[JsonPropertyName("blacklist")]
	public MongoId[]? ShopBlacklist { get; set; }

	[JsonPropertyName("specialItems")]
	public MongoId[]? SpecialShopItems { get; set; }
}
