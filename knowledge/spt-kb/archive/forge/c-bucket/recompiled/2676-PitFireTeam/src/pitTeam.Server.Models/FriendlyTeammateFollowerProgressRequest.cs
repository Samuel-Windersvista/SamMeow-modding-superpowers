using System.Collections.Generic;

namespace pitTeam.Server.Models;

public record FriendlyTeammateFollowerProgressRequest
{
	public string Aid { get; set; } = string.Empty;

	public double BotExperienceSession { get; set; }

	public int KillCount { get; set; }

	public int RaidSeconds { get; set; }

	public List<FriendlyTeammateSkillProgressRequest> Skills { get; set; } = new List<FriendlyTeammateSkillProgressRequest>();
}
