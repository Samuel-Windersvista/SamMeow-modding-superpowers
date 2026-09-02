using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace RaidOverhaulMain.Models;

public record BotSpawnData
{
	[JsonPropertyName("spawnData")]
	public Dictionary<string, Dictionary<string, BossLocationSpawn>> SpawnData { get; set; }
}
