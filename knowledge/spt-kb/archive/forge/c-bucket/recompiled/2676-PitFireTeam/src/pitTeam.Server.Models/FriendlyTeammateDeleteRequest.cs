using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateDeleteRequest : IRequestData
{
	[JsonPropertyName("accountId")]
	public string? AccountId { get; set; }
}
