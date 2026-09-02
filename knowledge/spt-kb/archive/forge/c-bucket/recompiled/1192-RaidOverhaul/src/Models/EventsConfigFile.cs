using System.Text.Json.Serialization;

namespace RaidOverhaulMain.Models;

public class EventsConfigFile
{
	[JsonPropertyName("SwitchToggle")]
	public int SwitchToggle { get; set; }

	[JsonPropertyName("DoorUnlock")]
	public int DoorUnlock { get; set; }

	[JsonPropertyName("KeycardUnlock")]
	public int KeycardUnlock { get; set; }

	[JsonPropertyName("DoorEventRangeMinimum")]
	public float DoorEventRangeMinimum { get; set; }

	[JsonPropertyName("DoorEventRangeMaximum")]
	public float DoorEventRangeMaximum { get; set; }

	[JsonPropertyName("DamageEvent")]
	public int DamageEvent { get; set; }

	[JsonPropertyName("AirdropEvent")]
	public int AirdropEvent { get; set; }

	[JsonPropertyName("BlackoutEvent")]
	public int BlackoutEvent { get; set; }

	[JsonPropertyName("JokeEvent")]
	public int JokeEvent { get; set; }

	[JsonPropertyName("HealEvent")]
	public int HealEvent { get; set; }

	[JsonPropertyName("ArmorEvent")]
	public int ArmorEvent { get; set; }

	[JsonPropertyName("SkillEvent")]
	public int SkillEvent { get; set; }

	[JsonPropertyName("MetabolismEvent")]
	public int MetabolismEvent { get; set; }

	[JsonPropertyName("MalfunctionEvent")]
	public int MalfunctionEvent { get; set; }

	[JsonPropertyName("TraderEvent")]
	public int TraderEvent { get; set; }

	[JsonPropertyName("BerserkEvent")]
	public int BerserkEvent { get; set; }

	[JsonPropertyName("WeightEvent")]
	public int WeightEvent { get; set; }

	[JsonPropertyName("MaxLLEvent")]
	public int MaxLLEvent { get; set; }

	[JsonPropertyName("LockdownEvent")]
	public int LockdownEvent { get; set; }

	[JsonPropertyName("ArtilleryEvent")]
	public int ArtilleryEvent { get; set; }

	[JsonPropertyName("InvasionEvent")]
	public int InvasionEvent { get; set; }

	[JsonPropertyName("RandomEventRangeMinimum")]
	public float RandomEventRangeMinimum { get; set; }

	[JsonPropertyName("RandomEventRangeMaximum")]
	public float RandomEventRangeMaximum { get; set; }
}
