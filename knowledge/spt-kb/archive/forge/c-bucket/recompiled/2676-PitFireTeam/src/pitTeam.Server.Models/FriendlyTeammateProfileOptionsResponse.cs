using System.Collections.Generic;

namespace pitTeam.Server.Models;

public record FriendlyTeammateProfileOptionsResponse
{
	public string CurrentLoadoutId { get; set; } = string.Empty;

	public string CurrentTactic { get; set; } = string.Empty;

	public float Aggression { get; set; } = 50f;

	public List<FriendlyTeammateLoadoutOption> Loadouts { get; set; } = new List<FriendlyTeammateLoadoutOption>();

	public List<FriendlyTeammateTacticOption> Tactics { get; set; } = new List<FriendlyTeammateTacticOption>();

	public FriendlyTeammateProfileRecoveryNotice? RecoveryNotice { get; set; }
}
