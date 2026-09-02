using System.Collections.Generic;

namespace RZCustomProfiles;

public record ProfileConfig
{
	public bool Enabled { get; set; } = true;

	public int BaseProfile { get; set; } = 6;

	public string Name { get; set; } = "DefaultProfileName";

	public string? Description { get; set; }

	public bool AllItemsExamined { get; set; }

	public bool MaxLevel { get; set; }

	public int? StartingLevel { get; set; }

	public int? StartingPrestigeLevel { get; set; }

	public bool SkipPrestigeRewards { get; set; } = true;

	public bool UnlockAllAchievements { get; set; }

	public bool MaxSkills { get; set; }

	public Dictionary<string, float>? SkillOverrides { get; set; }

	public Dictionary<string, TraderLoyaltyConfig>? TradersLoyalty { get; set; }

	public bool ClearStash { get; set; }

	public bool ClearEquipment { get; set; }

	public int SecureContainer { get; set; }

	public StartingItemsConfig? AdditionalStartingItems { get; set; }

	public Dictionary<string, int>? HideoutStartingLevels { get; set; }
}
