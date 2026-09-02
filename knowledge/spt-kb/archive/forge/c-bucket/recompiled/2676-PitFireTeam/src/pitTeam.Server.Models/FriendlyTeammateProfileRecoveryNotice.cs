namespace pitTeam.Server.Models;

public record FriendlyTeammateProfileRecoveryNotice
{
	public bool Recovered { get; set; }

	public int RemovedItemCount { get; set; }

	public string Message { get; set; } = string.Empty;
}
