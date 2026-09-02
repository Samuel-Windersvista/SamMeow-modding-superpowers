using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Enums;

namespace pitTeam.Server.Models;

public record FriendlySocialFriendInfo
{
	[JsonPropertyName("Nickname")]
	public string Nickname { get; set; } = string.Empty;

	[JsonPropertyName("Side")]
	public string Side { get; set; } = string.Empty;

	[JsonPropertyName("Level")]
	public int Level { get; set; }

	[JsonPropertyName("MemberCategory")]
	public MemberCategory MemberCategory { get; set; }

	[JsonPropertyName("SelectedMemberCategory")]
	public MemberCategory SelectedMemberCategory { get; set; }

	[JsonPropertyName("Ignored")]
	public bool Ignored { get; set; }

	[JsonPropertyName("Banned")]
	public bool Banned { get; set; }
}
