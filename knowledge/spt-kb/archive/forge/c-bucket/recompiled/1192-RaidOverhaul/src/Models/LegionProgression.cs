using System.Text.Json.Serialization;

namespace RaidOverhaulMain.Models;

public class LegionProgression
{
	[JsonPropertyName("LegionChance")]
	public double LegionChance { get; set; }
}
