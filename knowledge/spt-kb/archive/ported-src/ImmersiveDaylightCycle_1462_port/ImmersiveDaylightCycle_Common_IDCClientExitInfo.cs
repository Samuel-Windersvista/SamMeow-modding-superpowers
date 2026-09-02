using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ImmersiveDaylightCycle.Common;

public class IDCClientExitInfo
{
	[JsonProperty("raid_id")]
	public string RaidId { get; set; }

	[JsonProperty("profile_id")]
	public string ProfileId { get; set; }

	[JsonProperty("exit_status")]
	[JsonConverter(typeof(StringEnumConverter))]
	public ExitStatus ExitStatus { get; set; }

	[JsonProperty("is_host")]
	public bool IsHost { get; set; }

	[JsonProperty("is_dedicated_client")]
	public bool IsDedicatedClient { get; set; }

	[JsonProperty("seconds_in_raid")]
	public int SecondsInRaid { get; set; }
}
