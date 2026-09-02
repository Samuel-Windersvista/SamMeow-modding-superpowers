using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyRecruitPickupRequest : IRequestData
{
	[JsonPropertyName("Candidates")]
	public List<FriendlyRecruitPickupCandidate> Candidates { get; set; } = new List<FriendlyRecruitPickupCandidate>();
}
