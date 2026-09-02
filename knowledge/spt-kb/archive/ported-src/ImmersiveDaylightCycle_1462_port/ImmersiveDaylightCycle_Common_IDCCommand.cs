using Newtonsoft.Json;

namespace ImmersiveDaylightCycle.Common;

public class IDCCommand
{
	[JsonProperty("type")]
	public string Type { get; set; }

	[JsonProperty("message")]
	public string Message { get; set; } = "";

	public IDCCommand(string type)
	{
		Type = type;
	}
}
