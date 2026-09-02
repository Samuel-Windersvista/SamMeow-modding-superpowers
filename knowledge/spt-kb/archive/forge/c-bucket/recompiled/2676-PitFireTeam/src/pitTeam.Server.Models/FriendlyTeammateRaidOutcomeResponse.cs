using System.Collections.Generic;

namespace pitTeam.Server.Models;

public record FriendlyTeammateRaidOutcomeResponse
{
	public List<FriendlyTeammateDeathEscapeEntry> Entries { get; set; } = new List<FriendlyTeammateDeathEscapeEntry>();
}
