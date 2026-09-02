using Newtonsoft.Json;

namespace ImmersiveDaylightCycle.Common;

public class IDCTime
{
	[JsonProperty("hour")]
	public int Hour { get; set; }

	[JsonProperty("minute")]
	public int Minute { get; set; }

	[JsonProperty("second")]
	public int Second { get; set; }

	[JsonProperty("cycle_rate")]
	public int CycleRate { get; set; }
}
