using System.Collections.Generic;

namespace pitTeam.Server.Models;

public record FriendlyTeammateDeathEscapeSummary
{
	public List<string> EscapedNames { get; set; } = new List<string>();

	public List<string> LostNames { get; set; } = new List<string>();

	public string ExtractName { get; set; } = string.Empty;
}
