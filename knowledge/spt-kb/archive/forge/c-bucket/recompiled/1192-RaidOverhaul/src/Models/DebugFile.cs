using System.Text.Json.Serialization;

namespace RaidOverhaulMain.Models;

public class DebugFile
{
	[JsonPropertyName("isDev")]
	public bool IsDev { get; set; }

	[JsonPropertyName("debugMode")]
	public bool DebugMode { get; set; }

	[JsonPropertyName("dumpData")]
	public bool DumpData { get; set; }
}
