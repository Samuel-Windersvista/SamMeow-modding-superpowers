using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyPostRaidTeamEscapedRequest : IRequestData
{
	[JsonPropertyName("member")]
	public FriendlyPostRaidMember? Member { get; set; }
}
