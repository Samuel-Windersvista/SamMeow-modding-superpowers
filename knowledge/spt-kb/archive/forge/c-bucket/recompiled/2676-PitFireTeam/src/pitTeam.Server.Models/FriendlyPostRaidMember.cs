using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace pitTeam.Server.Models;

public record FriendlyPostRaidMember
{
	[JsonPropertyName("_id")]
	public string? Id { get; set; }

	[JsonPropertyName("aid")]
	public string? Aid { get; set; }

	[JsonPropertyName("Info")]
	public UserDialogDetails? Info { get; set; }

	[JsonPropertyName("SquadInfo")]
	public FriendlyPostRaidSquadInfo? SquadInfo { get; set; }
}
