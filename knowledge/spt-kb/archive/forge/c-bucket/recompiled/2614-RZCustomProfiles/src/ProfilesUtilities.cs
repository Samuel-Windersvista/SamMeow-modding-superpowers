using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace RZCustomProfiles;

[Injectable(InjectionType.Scoped)]
public class ProfilesUtilities(ILogger<ProfilesUtilities> logger, TemplateTable templateTable, GlobalTable globalTable, InventoryHelper inventoryHelper, AssortUtilities assortUtilities)
{
	public void PatchSide(TemplateSide? side, string sideName, ProfileConfig config)
	{
		if (side?.Character == null)
		{
			logger.LogWarning("[RZCustomProfiles] {Side} template character is null, skipping.", sideName);
			return;
		}
		ClearStartingItems(side, sideName, config);
		AddStashItems(side, sideName, config);
		ReplaceSecureContainer(side, sideName, config);
		ApplyLevel(side, config);
		ApplySkills(side, config, logger);
		ApplyHideoutLevels(side, sideName, config);
	}

	private void ClearStartingItems(TemplateSide side, string sideName, ProfileConfig config)
	{
		BotBaseInventory inventory = side.Character.Inventory;
		if (inventory?.Items == null)
		{
			logger.LogWarning("[RZCustomProfiles] {Side} inventory is null, skipping item clear.", sideName);
			return;
		}
		if (config.ClearStash)
		{
			string? stashId = inventory.Stash?.ToString();
			if (stashId == null)
			{
				logger.LogWarning("[RZCustomProfiles] {Side} stash ID is null, skipping stash clear.", sideName);
			}
			else
			{
				inventory.Items.RemoveAll(i => i.ParentId == stashId);
			}
		}
		if (!config.ClearEquipment)
		{
			return;
		}
		string? text = inventory.Equipment?.ToString();
		if (text == null)
		{
			logger.LogWarning("[RZCustomProfiles] {Side} equipment ID is null, skipping equipment clear.", sideName);
			return;
		}
		HashSet<string> pocketIds = inventory.Items
			.Where(i => i.SlotId == "Pockets")
			.Select(i => i.Id.ToString())
			.ToHashSet();
		HashSet<string> equippedIds = new HashSet<string>();
		CollectEquippedIds(inventory.Items, text, equippedIds);
		inventory.Items.RemoveAll(i =>
			(equippedIds.Contains(i.Id.ToString()) && !MasterConfig.ProtectedSlots.Contains(i.SlotId ?? "") && !pocketIds.Contains(i.Id.ToString()))
			|| pocketIds.Contains(i.ParentId ?? ""));
	}

	private void CollectEquippedIds(List<Item> items, string parentId, HashSet<string> result)
	{
		foreach (Item item in items.Where(i => i.ParentId == parentId))
		{
			if (!MasterConfig.ProtectedSlots.Contains(item.SlotId ?? ""))
			{
				result.Add(item.Id.ToString());
				CollectEquippedIds(items, item.Id.ToString(), result);
			}
		}
	}

	private void AddStashItems(TemplateSide side, string sideName, ProfileConfig config)
	{
		BotBaseInventory inventory = side.Character.Inventory;
		List<Item>? list = inventory?.Items;
		if (list == null)
		{
			logger.LogWarning("[RZCustomProfiles] {Side} inventory is null, skipping items replacement.", sideName);
			return;
		}
		StartingItemsConfig? additionalStartingItems = config.AdditionalStartingItems;
		if (additionalStartingItems == null || !additionalStartingItems.Enabled || additionalStartingItems.Items.Count == 0)
		{
			return;
		}
		MongoId? val = side.Character.Inventory?.Stash;
		if (!val.HasValue)
		{
			logger.LogWarning("[RZCustomProfiles] {Side} stash ID is null, skipping starting items injection.", sideName);
			return;
		}
		(int w, int h) stashSize = GetStashSize(side);
		int item = stashSize.w;
		int item2 = stashSize.h;
		int[,] containerMap = inventoryHelper.GetContainerMap(item, item2, list, val.Value);
		foreach (ItemEntry item3 in additionalStartingItems.Items)
		{
			if (string.IsNullOrWhiteSpace(item3.Tpl) || item3.Count <= 0)
			{
				continue;
			}
			TemplateItem? obj = templateTable.Items?.GetValueOrDefault((MongoId)item3.Tpl);
			int? obj2 = obj?.Properties?.StackMaxSize;
			int val2 = obj2 ?? 1;
			int num = item3.Count;
			while (num > 0)
			{
				int num2 = Math.Min(num, val2);
				List<Item> list2 = assortUtilities.CreateRootItem(item3.Tpl, num2);
				if (inventoryHelper.PlaceItemInContainer(containerMap, list2, val.Value.ToString(), "hideout").Success != true)
				{
					logger.LogWarning("[RZCustomProfiles] {Side} stash full, could not place '{Tpl}'.", sideName, item3.Tpl);
					break;
				}
				list.AddRange(list2);
				num -= num2;
			}
		}
	}

	private void ReplaceSecureContainer(TemplateSide side, string sideName, ProfileConfig config)
	{
		if (config.SecureContainer == 0)
		{
			return;
		}
		PmcData character = side.Character;
		Item? val = character?.Inventory?.Items?.FirstOrDefault(i => i.SlotId == "SecuredContainer");
		if (val == null)
		{
			logger.LogWarning("[RZCustomProfiles] {Side} SecuredContainer not found : skipping.", sideName);
		}
		else if (config.SecureContainer == -1)
		{
			side.Character.Inventory.Items.Remove(val);
		}
		else if (!MasterConfig.SecureContainers.TryGetValue(config.SecureContainer, out string value))
		{
			logger.LogWarning("[RZCustomProfiles] Unknown SecureContainer index '{Index}' : skipping.", config.SecureContainer);
		}
		else
		{
			val.Template = value;
		}
	}

	private void ApplyHideoutLevels(TemplateSide side, string sideName, ProfileConfig config)
	{
		Dictionary<string, int>? hideoutStartingLevels = config.HideoutStartingLevels;
		if (hideoutStartingLevels == null || hideoutStartingLevels.Count == 0)
		{
			return;
		}
		List<BotHideoutArea>? list = side.Character.Hideout?.Areas;
		if (list == null)
		{
			logger.LogWarning("[RZCustomProfiles] {Side} hideout areas list is null, skipping.", sideName);
			return;
		}
		foreach (KeyValuePair<string, int> item in hideoutStartingLevels)
		{
			string text = item.Key;
			int value2 = item.Value;
			if (!Enum.TryParse<HideoutAreas>(text, true, out HideoutAreas areaType))
			{
				logger.LogWarning("[RZCustomProfiles] Unknown hideout area '{Name}' : skipped.", text);
				continue;
			}
			BotHideoutArea? val = list.FirstOrDefault(a => a.Type == areaType);
			if (val != null)
			{
				val.Level = value2;
				val.Active = true;
				val.Constructing = false;
				val.CompleteTime = 0;
				val.PassiveBonusesEnabled = true;
			}
		}
	}

	private void ApplyLevel(TemplateSide side, ProfileConfig config)
	{
		Info? val = side.Character?.Info;
		if (val == null)
		{
			return;
		}
		ExpTable[] experienceTable = globalTable.Configuration.Exp.Level.ExperienceTable;
		if (config.MaxLevel)
		{
			val.Level = experienceTable.Length;
			val.Experience = experienceTable.Sum(e => e.Experience);
			return;
		}
		int? startingLevel = config.StartingLevel;
		if (startingLevel.HasValue)
		{
			int valueOrDefault = Math.Clamp(startingLevel.GetValueOrDefault(), 1, experienceTable.Length);
			val.Level = valueOrDefault;
			val.Experience = experienceTable.Take(valueOrDefault).Sum(e => e.Experience);
		}
	}

	private static void ApplyPrestige(TemplateSide side, ProfileConfig config, ILogger<ProfilesUtilities> logger)
	{
		int? startingPrestigeLevel = config.StartingPrestigeLevel;
		if (startingPrestigeLevel.HasValue)
		{
			int valueOrDefault = startingPrestigeLevel.GetValueOrDefault();
			Info? val = side.Character?.Info;
			if (val == null)
			{
				logger.LogWarning("[RZCustomProfiles] ApplyPrestige: Info is null, skipping.");
				return;
			}
			logger.LogInformation("[RZCustomProfiles] ApplyPrestige: Info found, PrestigeLevel before = {Before}", val.PrestigeLevel);
			val.PrestigeLevel = Math.Max(0, valueOrDefault);
			logger.LogInformation("[RZCustomProfiles] ApplyPrestige: PrestigeLevel after = {After}", val.PrestigeLevel);
		}
		else
		{
			logger.LogInformation("[RZCustomProfiles] ApplyPrestige: StartingPrestige is null, skipping.");
		}
	}

	private static void ApplySkills(TemplateSide side, ProfileConfig config, ILogger<ProfilesUtilities> logger)
	{
		IEnumerable<CommonSkill>? enumerable = side.Character?.Skills?.Common;
		if (enumerable == null)
		{
			return;
		}
		if (config.MaxSkills)
		{
			foreach (CommonSkill item in enumerable)
			{
				item.Progress = 5100.0;
			}
			return;
		}
		Dictionary<string, float>? skillOverrides = config.SkillOverrides;
		if (skillOverrides == null || skillOverrides.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<string, float> item2 in skillOverrides)
		{
			string skillId = item2.Key;
			float value2 = item2.Value;
			CommonSkill? val = enumerable.FirstOrDefault(s => string.Equals(s.Id.ToString(), skillId, StringComparison.OrdinalIgnoreCase));
			if (val == null)
			{
				logger.LogWarning("[RZCustomProfiles] Skill '{Id}' not found.", skillId);
			}
			else
			{
				val.Progress = Math.Clamp(value2, 0f, 51f) * 100f;
			}
		}
	}

	private void ApplyTradersLoyalty(TemplateSide side, string sideName, ProfileConfig config)
	{
		if (config.TradersLoyalty == null || config.TradersLoyalty.Count == 0)
		{
			return;
		}
		Dictionary<MongoId, TraderInfo>? dictionary = side.Character?.TradersInfo;
		if (dictionary == null)
		{
			logger.LogWarning("[RZCustomProfiles] {Side} TradersInfo is null, skipping.", sideName);
			return;
		}
		foreach (var (text2, traderLoyaltyConfig2) in config.TradersLoyalty)
		{
			if (dictionary.TryGetValue(new MongoId(text2), out TraderInfo? value))
			{
				value.Standing = traderLoyaltyConfig2.Standing;
				value.SalesSum = traderLoyaltyConfig2.SalesSum;
			}
		}
	}

	private (int w, int h) GetStashSize(TemplateSide side)
	{
		MongoId? stashId = side.Character?.Inventory?.Stash;
		Item? val = side.Character?.Inventory?.Items?.FirstOrDefault(i => stashId.HasValue && i.Id == stashId.Value);
		Grid? val2 = null;
		if (val != null)
		{
			TemplateItem? obj = templateTable.Items?.GetValueOrDefault(val.Template.ToString());
			val2 = obj?.Properties?.Grids?.FirstOrDefault();
		}
		int item = val2?.Properties?.CellsH ?? 10;
		int item2 = val2?.Properties?.CellsV ?? 68;
		return (w: item, h: item2);
	}
}
