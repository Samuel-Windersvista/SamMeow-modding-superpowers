using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace RaidOverhaulMain.Models;

public record LogToServerRequestData : IRequestData
{
	[JsonPropertyName("message")]
	public string? Message { get; set; }
}
