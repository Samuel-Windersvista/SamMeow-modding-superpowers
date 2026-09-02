using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace pitTeam.Server.Models;

public record FriendlyTeammateDeathEscapeEntry
{
	public string Aid { get; set; } = string.Empty;

	public string ProfileId { get; set; } = string.Empty;

	public string Nickname { get; set; } = string.Empty;

	public bool Escaped { get; set; }

	public bool RollEscape { get; set; }

	public double Chance { get; set; }

	public string ExtractName { get; set; } = string.Empty;

	public double Distance { get; set; }

	public double HealthRatio { get; set; }

	public double EquipmentPower { get; set; }

	public double EnemyAveragePower { get; set; }

	public double RouteEnemyAveragePower { get; set; }

	public double CurrentFightEnemyAveragePower { get; set; }

	public int RouteEnemyCount { get; set; }

	public int CurrentFightEnemyCount { get; set; }

	public int AliveSquadmates { get; set; }

	public bool HasSecureMeds { get; set; }

	public bool VitalsDestroyed { get; set; }

	public List<Item>? EquipmentItems { get; set; }

	public List<string>? TrackedItemIds { get; set; }
}
