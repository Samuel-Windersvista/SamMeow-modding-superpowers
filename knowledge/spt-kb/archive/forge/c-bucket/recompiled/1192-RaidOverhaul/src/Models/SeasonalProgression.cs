using System.Text.Json.Serialization;

namespace RaidOverhaulMain.Models;

public class SeasonalProgression
{
	[JsonPropertyName("seasonsProgression")]
	public int SeasonsProgression { get; set; }
}
