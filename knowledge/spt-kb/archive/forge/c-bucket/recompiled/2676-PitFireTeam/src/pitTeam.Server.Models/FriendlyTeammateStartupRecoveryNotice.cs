using System.Collections.Generic;

namespace pitTeam.Server.Models;

public record FriendlyTeammateStartupRecoveryNotice
{
	public bool Recovered { get; set; }

	public int RemovedItemCount { get; set; }

	public List<string> TeammateNames { get; set; } = new List<string>();

	public string Title { get; set; } = string.Empty;

	public string Message { get; set; } = string.Empty;
}
