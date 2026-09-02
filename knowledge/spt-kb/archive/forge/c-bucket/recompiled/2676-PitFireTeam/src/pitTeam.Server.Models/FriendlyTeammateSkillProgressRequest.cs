using System.Text.Json;

namespace pitTeam.Server.Models;

public record FriendlyTeammateSkillProgressRequest
{
	public JsonElement Id { get; set; }

	public double Current { get; set; }

	public double Progress { get; set; }

	public double PointsEarnedDuringSession { get; set; }
}
