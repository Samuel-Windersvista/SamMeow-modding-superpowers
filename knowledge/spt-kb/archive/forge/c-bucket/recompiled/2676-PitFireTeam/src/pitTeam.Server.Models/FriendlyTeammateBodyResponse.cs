using System;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Enums;

namespace pitTeam.Server.Models;

public record FriendlyTeammateBodyResponse<T>
{
	[JsonPropertyName("Err")]
	public BackendErrorCodes? Err { get; set; }

	[JsonPropertyName("ErrMsg")]
	public string? ErrMsg { get; set; }

	[JsonPropertyName("Data")]
	public T? Data { get; set; }
}
