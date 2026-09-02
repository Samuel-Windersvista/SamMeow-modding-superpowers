using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Models.Eft.Repair;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.RaidSettings;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Models.Spt.Services;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Services.Profile;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using pitTeam.Server.Models;
using Info = SPTarkov.Server.Core.Models.Eft.Common.Tables.Info;
using Customization = SPTarkov.Server.Core.Models.Eft.Common.Tables.Customization;
using Path = System.IO.Path;

namespace pitTeam.Server.Services;

[Injectable(InjectionType.Singleton)]
public class FriendlyTeammateService(BotGenerator botGenerator, GlobalTable globalTable, SaveServer saveServer, FileUtil fileUtil, HashUtil hashUtil, JsonUtil jsonUtil, ItemHelper itemHelper, MailSendService mailSendService, ProfileHelper profileHelper, ProfileActivityService profileActivityService, RepairService repairService, FriendlyServerSettingsService settingsService, FriendlyLanguageService languageService, ICloner cloner, ISptLogger<FriendlyTeammateService> logger)
{
	private sealed class AvailableAmmoStack(Item item, int remaining)
	{
		public Item Item { get; } = item;

		public int Remaining { get; set; } = remaining;
	}

	private const string ProfileRecoveryMessage = "The profile of this teammate has been recovered from a bad state. Some items from his inventory may have been deleted in the process.";

	private const string DuplicateProfileRecoveryTitleFallback = "Profile recovered";

	private const string DuplicateProfileRecoveryBodyFallback = "Duplicate items were found in both player and teammate profiles. The following teammate profiles have been stripped of the duplicate in order to safely recover them: {0}";

	private const string LockedStashItemBlockedFallback = "Teammate loadout save blocked: an item inside a locked stash container was moved or changed. Unlock the container and try again. Locked container: id={0}, tpl={1}. Blocked item: id={2}, tpl={3}.";

	private readonly Dictionary<string, FriendlyTeammateProfileRecoveryNotice> profileRecoveryNotices = new Dictionary<string, FriendlyTeammateProfileRecoveryNotice>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, FriendlyTeammateStartupRecoveryNotice> startupRecoveryNotices = new Dictionary<string, FriendlyTeammateStartupRecoveryNotice>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> duplicateRecoveryCheckedSessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly string[] WeaponSkillNames = new string[7] { "Pistol", "Revolver", "SMG", "Assault", "Shotgun", "Sniper", "LMG" };

	private const string ModFolderName = "pitFireTeam-ServerMod";

	private const string TeammateFolderName = "teammates";

	private const string DefaultLoadoutName = "Default";

	private const string DefaultLoadoutId = "000000000000000000000000";

	private static readonly string[] TacticOptions = new string[2] { "Rifleman", "Marksman" };

	private const int RelativeLevelDelta = 5;

	private const int SecureContainerAmmoStackCount = 10;

	private const string TeammateGenerationLocation = "factory4_day";

	private const double DeathEscapeMinChance = 0.05;

	private const double DeathEscapeMaxChance = 0.9;

	private const double DeathEscapeCloseExtractDistance = 150.0;

	private const double DeathEscapeFarExtractDistance = 900.0;

	private static readonly Random DeathEscapeRandom = new Random();

	private static readonly object DeathEscapeRandomLock = new object();

	private static readonly object RaidResultPersistenceLock = new object();

	private static readonly string[] RequiredRaidWeaponSlots = new string[3] { "FirstPrimaryWeapon", "SecondPrimaryWeapon", "Holster" };

	private static readonly HashSet<string> LoadedAmmoSlotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cartridges" };

	private static readonly HashSet<string> SurgicalKitTemplateIds = new HashSet<string> { "5d02797c86f774203f38e30a", "60d4399358ef941a33423dad" };

	public void RecoverDuplicateTeammateItemsForAllProfiles()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			foreach (KeyValuePair<MongoId, SptProfile> profile in saveServer.GetProfiles())
			{
				string text = profile.Key.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					try
					{
						RecoverDuplicateTeammateItemsForSession(new MongoId(text));
					}
					catch (Exception ex)
					{
						logger.Warning($"{"FriendlyTeammateService"}: failed duplicate item startup recovery for session '{text}': {ex.Message}", (Exception)null);
					}
				}
			}
		}
		catch (Exception ex2)
		{
			logger.Warning("FriendlyTeammateService: failed duplicate item startup recovery: " + ex2.Message, (Exception)null);
		}
	}

	public unsafe FriendlyTeammateStartupRecoveryNotice GetStartupRecoveryNotice(MongoId sessionId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		RecoverDuplicateTeammateItemsForSession(sessionId);
		string key = sessionId.ToString();
		if (!startupRecoveryNotices.TryGetValue(key, out FriendlyTeammateStartupRecoveryNotice value))
		{
			return new FriendlyTeammateStartupRecoveryNotice();
		}
		LocalizeStartupRecoveryNotice(sessionId, value);
		return value;
	}

	public unsafe void AcknowledgeStartupRecoveryNotice(MongoId sessionId)
	{
		string key = sessionId.ToString();
		startupRecoveryNotices.Remove(key);
	}

	public SearchFriendResponse CreateTeammate(MongoId sessionId, FriendlyTeammateCreateRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Expected O, but got Unknown
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		PmcData playerProfile = GetPlayerProfile(sessionId);
		string text = NormalizeRequiredValue(request.Nickname, "nickname");
		string text2 = NormalizeRequiredValue(request.Voice, "voice");
		string text3 = NormalizeRequiredValue(request.Head, "head");
		EnsureNicknameIsUnique(sessionId, text);
		BotBase val = GenerateTeammateBot(sessionId, new BotGenerationDetails
		{
			IsPmc = true,
			Side = ((BotBase)playerProfile).Info.Side,
			Role = GetPmcRole(((BotBase)playerProfile).Info.Side),
			PlayerLevel = Math.Max(1, ((BotBase)playerProfile).Info.Level ?? 1),
			PlayerName = ((BotBase)playerProfile).Info.Nickname,
			BotRelativeLevelDeltaMax = 5,
			BotRelativeLevelDeltaMin = 5,
			BotCountToGenerate = 1,
			BotDifficulty = "hard",
			Location = "factory4_day",
			LocationSpecificPmcLevelOverride = new MinMax<int>
			{
				Min = Math.Max(1, (((BotBase)playerProfile).Info.Level ?? 1) - 5),
				Max = Math.Min(100, (((BotBase)playerProfile).Info.Level ?? 1) + 5)
			},
			IsPlayerScav = false,
			AllPmcsHaveSameNameAsPlayer = false
		});
		val.Info.Nickname = text;
		val.Info.LowerNickname = text.ToLowerInvariant();
		val.Customization.Voice = (text2);
		val.Customization.Head = (text3);
		val.Aid = GetUniqueAccountId(sessionId);
		NormalizeTeammateProfile(val, playerProfile);
		NormalizeTeammateSkillsForCreation(val, playerProfile);
		PrepareNewTeammateDefaultForCurrentLoadoutMode(val);
		SaveDefaultEquipmentSnapshot(sessionId, val, overwrite: true, IsCurrentLoadoutManagementModeExtreme());
		SaveTeammate(sessionId, val);
		SaveTeammateSettings(sessionId, val, CreateDefaultTeammateSettings());
		logger.Info($"Created teammate '{text}' for session '{sessionId}' with aid '{val.Aid}'", (Exception)null);
		return ToFriendSummary(val);
	}

	public SearchFriendResponse CreateTeammateFromRecruitCandidate(MongoId sessionId, FriendlyRecruitPickupCandidate candidate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected O, but got Unknown
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		//IL_0130: Expected O, but got Unknown
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Expected O, but got Unknown
		//IL_014c: Expected O, but got Unknown
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		PmcData playerProfile = GetPlayerProfile(sessionId);
		string text = EnsureUniqueRecruitNickname(sessionId, NormalizeRequiredValue(candidate.Nickname, "nickname"));
		string text2 = NormalizeRequiredValue(candidate.Voice, "voice");
		string text3 = NormalizeRequiredValue(candidate.Head, "head");
		int num = Math.Max(1, candidate.Level);
		BotBase teammate;
		bool flag = TryDeserializeRecruitProfile(candidate, out teammate);
		if (!flag)
		{
			teammate = GenerateTeammateBot(sessionId, new BotGenerationDetails
			{
				IsPmc = true,
				Side = ((BotBase)playerProfile).Info.Side,
				Role = GetPmcRole(((BotBase)playerProfile).Info.Side),
				PlayerLevel = num,
				PlayerName = ((BotBase)playerProfile).Info.Nickname,
				BotRelativeLevelDeltaMax = 0,
				BotRelativeLevelDeltaMin = 0,
				BotCountToGenerate = 1,
				BotDifficulty = "hard",
				Location = "factory4_day",
				LocationSpecificPmcLevelOverride = new MinMax<int>
				{
					Min = num,
					Max = num
				},
				IsPlayerScav = false,
				AllPmcsHaveSameNameAsPlayer = false
			});
		}
		BotBase val = teammate;
		if (val.Info == null)
		{
			BotBase obj = val;
			Info val2 = new Info();
			Info val3 = val2;
			obj.Info = val2;
		}
		val = teammate;
		if (val.Customization == null)
		{
			BotBase obj2 = val;
			Customization val4 = new Customization();
			Customization val5 = val4;
			obj2.Customization = val4;
		}
		teammate.Info.Nickname = text;
		teammate.Info.LowerNickname = text.ToLowerInvariant();
		teammate.Customization.Voice = (text2);
		teammate.Customization.Head = (text3);
		teammate.Aid = GetRecruitAccountIdOrUnique(sessionId, candidate.AccountId);
		if (flag)
		{
			NormalizeCapturedRecruitProfile(teammate, playerProfile, candidate);
		}
		else
		{
			NormalizeTeammateProfile(teammate, playerProfile);
		}
		NormalizeTeammateSkillsForCreation(teammate, playerProfile);
		InitializeRecruitRaidStats(teammate, num, GetRecruitStatsSeed(candidate));
		PrepareNewTeammateDefaultForCurrentLoadoutMode(teammate);
		SaveDefaultEquipmentSnapshot(sessionId, teammate, overwrite: true, IsCurrentLoadoutManagementModeExtreme());
		SaveTeammate(sessionId, teammate);
		SaveTeammateSettings(sessionId, teammate, CreateDefaultTeammateSettings());
		logger.Info($"Accepted recruit pickup '{text}' for session '{sessionId}' with aid '{teammate.Aid}' capturedProfile={flag}", (Exception)null);
		return ToFriendSummary(teammate);
	}

	public bool TryGetRecruitCandidateProfile(MongoId sessionId, FriendlyRecruitPickupCandidate candidate, out GetOtherProfileResponse? profile)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_0030: Expected O, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0048: Expected O, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		profile = null;
		if (!TryDeserializeRecruitProfile(candidate, out BotBase teammate))
		{
			return false;
		}
		PmcData playerProfile = GetPlayerProfile(sessionId);
		BotBase val = teammate;
		if (val.Info == null)
		{
			BotBase obj = val;
			Info val2 = new Info();
			Info val3 = val2;
			obj.Info = val2;
		}
		val = teammate;
		if (val.Customization == null)
		{
			BotBase obj2 = val;
			Customization val4 = new Customization();
			Customization val5 = val4;
			obj2.Customization = val4;
		}
		if (!string.IsNullOrWhiteSpace(candidate.Nickname))
		{
			teammate.Info.Nickname = candidate.Nickname.Trim();
			teammate.Info.LowerNickname = teammate.Info.Nickname.ToLowerInvariant();
		}
		if (!string.IsNullOrWhiteSpace(candidate.Voice))
		{
			teammate.Customization.Voice = (candidate.Voice.Trim());
		}
		if (!string.IsNullOrWhiteSpace(candidate.Head))
		{
			teammate.Customization.Head = (candidate.Head.Trim());
		}
		if (int.TryParse(candidate.AccountId, out var result) && result > 0)
		{
			teammate.Aid = result;
		}
		NormalizeCapturedRecruitProfile(teammate, playerProfile, candidate);
		NormalizeTeammateSkillsForCreation(teammate, playerProfile);
		InitializeRecruitRaidStats(teammate, Math.Max(1, candidate.Level), GetRecruitStatsSeed(candidate));
		profile = ToOtherProfileResponse(teammate);
		profile.Hideout = null;
		profile.HideoutAreaStashes = new Dictionary<string, MongoId>();
		profile.CustomizationStash = string.Empty;
		return true;
	}

	private bool TryDeserializeRecruitProfile(FriendlyRecruitPickupCandidate candidate, out BotBase teammate)
	{
		teammate = null;
		if (string.IsNullOrWhiteSpace(candidate.ProfileJson))
		{
			return false;
		}
		try
		{
			BotBase val = jsonUtil.Deserialize<BotBase>(candidate.ProfileJson);
			if (((val != null) ? val.Info : null) == (Info)null)
			{
				return false;
			}
			teammate = val;
			return true;
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to deserialize captured recruit profile '" + candidate.ProfileId + "'; falling back to generated recruit profile: " + ex.Message, (Exception)null);
			teammate = null;
			return false;
		}
	}

	private void NormalizeCapturedRecruitProfile(BotBase teammate, PmcData playerPmc, FriendlyRecruitPickupCandidate candidate)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0017: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_002e: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0050: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_0085: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00a5: Expected O, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		//IL_00d5: Expected O, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected O, but got Unknown
		//IL_0117: Expected O, but got Unknown
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		BotBase val = teammate;
		Info val3;
		if (val.Info == null)
		{
			BotBase obj = val;
			Info val2 = new Info();
			val3 = val2;
			obj.Info = val2;
		}
		val = teammate;
		if (val.Customization == null)
		{
			BotBase obj2 = val;
			Customization val4 = new Customization();
			Customization val5 = val4;
			obj2.Customization = val4;
		}
		val = teammate;
		BotBaseInventory val7;
		if (val.Inventory == null)
		{
			BotBase obj3 = val;
			BotBaseInventory val6 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val7 = val6;
			obj3.Inventory = val6;
		}
		val7 = teammate.Inventory;
		if (val7.Items == null)
		{
			List<Item> list = (val7.Items = new List<Item>());
		}
		val = teammate;
		Stats val9;
		if (val.Stats == null)
		{
			BotBase obj4 = val;
			Stats val8 = new Stats();
			val9 = val8;
			obj4.Stats = val8;
		}
		val9 = teammate.Stats;
		EftStats val11;
		if (val9.Eft == null)
		{
			Stats obj5 = val9;
			EftStats val10 = new EftStats();
			val11 = val10;
			obj5.Eft = val10;
		}
		val11 = teammate.Stats.Eft;
		OverallCounters val13;
		if (val11.OverallCounters == null)
		{
			EftStats obj6 = val11;
			OverallCounters val12 = new OverallCounters
			{
				Items = new List<CounterKeyValue>()
			};
			val13 = val12;
			obj6.OverallCounters = val12;
		}
		val13 = teammate.Stats.Eft.OverallCounters;
		if (val13.Items == null)
		{
			List<CounterKeyValue> list3 = (val13.Items = new List<CounterKeyValue>());
		}
		val = teammate;
		if (val.Hideout == null)
		{
			BotBase obj7 = val;
			Hideout val14 = new Hideout();
			Hideout val15 = val14;
			obj7.Hideout = val14;
		}
		val7 = teammate.Inventory;
		if (val7.HideoutAreaStashes == null)
		{
			Dictionary<string, MongoId> dictionary = (val7.HideoutAreaStashes = new Dictionary<string, MongoId>());
		}
		if (!teammate.Id.HasValue && !string.IsNullOrWhiteSpace(candidate.ProfileId))
		{
			teammate.Id = new MongoId(candidate.ProfileId);
		}
		val3 = teammate.Info;
		if (val3.Side == null)
		{
			Info obj8 = val3;
			Info info = ((BotBase)playerPmc).Info;
			string text = (obj8.Side = ((info != null) ? info.Side : null));
		}
		teammate.Info.LowerNickname = teammate.Info.Nickname?.ToLowerInvariant();
		teammate.Info.MemberCategory = (MemberCategory)1024;
		teammate.Info.SelectedMemberCategory = (MemberCategory)1024;
		Info info2 = teammate.Info;
		Info info3 = ((BotBase)playerPmc).Info;
		info2.BannedState = ((info3 != null) ? info3.BannedState : ((bool?)null));
		Info info4 = teammate.Info;
		Info info5 = ((BotBase)playerPmc).Info;
		info4.BannedUntil = ((info5 != null) ? info5.BannedUntil : ((long?)null));
	}

	public List<object> ListTeammates(MongoId sessionId)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return (from teammate in LoadTeammates(sessionId)
			select ToTeammateSummary(teammate, GetTeammateSettings(sessionId, teammate))).ToList();
	}

	public HashSet<string> GetProtectedTeammateItemIdsForExtraction(MongoId sessionId)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		string mode = NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode);
		if (IsImmersiveLikeLoadoutManagementMode(mode))
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (HashSet<string> value in BuildProtectedTeammateItemIdsByAid(sessionId).Values)
		{
			hashSet.UnionWith(value);
		}
		return hashSet;
	}

	public HashSet<string> GetProtectedSpawnItemIdsForExtraction(BotBase? profile)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		string mode = NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode);
		if (IsImmersiveLikeLoadoutManagementMode(mode))
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		object obj;
		if (profile == null)
		{
			obj = null;
		}
		else
		{
			BotBaseInventory inventory = profile.Inventory;
			obj = ((inventory != null) ? inventory.Items : null);
		}
		List<Item> list = (List<Item>)obj;
		if (list == null || list.Count == 0)
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		object obj2;
		if (profile == null)
		{
			obj2 = null;
		}
		else
		{
			BotBaseInventory inventory2 = profile.Inventory;
			if (inventory2 == null)
			{
				obj2 = null;
			}
			else
			{
				MongoId? equipment = inventory2.Equipment;
				obj2 = (equipment.HasValue ? equipment.GetValueOrDefault().ToString() : null);
			}
		}
		string equipmentRootId = (string)obj2;
		if (string.IsNullOrWhiteSpace(equipmentRootId))
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Item item in list.Where(delegate(Item item)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				if (string.Equals(item.ParentId, equipmentRootId, StringComparison.OrdinalIgnoreCase))
				{
					return !IsIgnoredSpawnProtectionSlot(item.SlotId);
				}
			}
			return false;
		}))
		{
			hashSet.UnionWith(GetItemTreeIds(list, item.Id.ToString()));
		}
		return hashSet;
	}

	private Dictionary<string, HashSet<string>> BuildProtectedTeammateItemIdsByAid(MongoId sessionId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, HashSet<string>> dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (BotBase item in LoadTeammates(sessionId))
		{
			string text = item.Aid?.ToString() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(text))
			{
				HashSet<string> protectedTeammateItemIds = GetProtectedTeammateItemIds(item);
				if (protectedTeammateItemIds.Count > 0)
				{
					dictionary[text] = protectedTeammateItemIds;
				}
			}
		}
		return dictionary;
	}

	private static HashSet<string> GetProtectedTeammateItemIds(BotBase? teammate)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		object obj;
		if (teammate == null)
		{
			obj = null;
		}
		else
		{
			BotBaseInventory inventory = teammate.Inventory;
			obj = ((inventory != null) ? inventory.Items : null);
		}
		List<Item> list = (List<Item>)obj;
		if (list == null || list.Count == 0)
		{
			return hashSet;
		}
		object obj2;
		if (teammate == null)
		{
			obj2 = null;
		}
		else
		{
			BotBaseInventory inventory2 = teammate.Inventory;
			if (inventory2 == null)
			{
				obj2 = null;
			}
			else
			{
				MongoId? equipment = inventory2.Equipment;
				obj2 = (equipment.HasValue ? equipment.GetValueOrDefault().ToString() : null);
			}
		}
		string b = (string)obj2;
		foreach (Item item in list)
		{
			if (item != null)
			{
				_ = item.Id;
				if (0 == 0 && !string.Equals(item.Id.ToString(), b, StringComparison.OrdinalIgnoreCase))
				{
					hashSet.Add(item.Id.ToString());
				}
			}
		}
		return hashSet;
	}

	private static HashSet<string> GetOtherProtectedTeammateItemIds(Dictionary<string, HashSet<string>> protectedItemIdsByAid, string? currentAid)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (protectedItemIdsByAid == null || protectedItemIdsByAid.Count == 0)
		{
			return hashSet;
		}
		foreach (KeyValuePair<string, HashSet<string>> item in protectedItemIdsByAid)
		{
			if (string.IsNullOrWhiteSpace(currentAid) || !string.Equals(item.Key, currentAid, StringComparison.OrdinalIgnoreCase))
			{
				hashSet.UnionWith(item.Value);
			}
		}
		return hashSet;
	}

	private static bool IsIgnoredSpawnProtectionSlot(string? slotId)
	{
		if (!string.IsNullOrWhiteSpace(slotId))
		{
			if (!slotId.Contains("Dogtag", StringComparison.OrdinalIgnoreCase) && !slotId.Contains("SpecialSlot", StringComparison.OrdinalIgnoreCase) && !slotId.Contains("ArmBand", StringComparison.OrdinalIgnoreCase) && !slotId.Contains("Armband", StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(slotId, "Scabbard", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	public void LogLoadoutManagementModeChange(MongoId sessionId, string previousMode, string nextMode)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		logger.Info($"Loadout management mode changed for session '{sessionId}' from '{previousMode}' to '{nextMode}'.", (Exception)null);
	}

	public void SelectDefaultLoadoutForAllTeammates(MongoId sessionId, string? previousMode = null, string? nextMode = null)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		List<BotBase> list = LoadTeammates(sessionId);
		string mode = NormalizeLoadoutManagementMode(previousMode);
		string mode2 = NormalizeLoadoutManagementMode(nextMode);
		bool flag = IsExtremeLoadoutManagementMode(mode) || IsExtremeLoadoutManagementMode(mode2);
		bool flag2 = IsSimpleLoadoutManagementMode(mode) && !IsSimpleLoadoutManagementMode(mode2);
		foreach (BotBase item in list)
		{
			try
			{
				if (flag && RemoveSecureContainerTree(item))
				{
					SaveDefaultEquipmentSnapshot(sessionId, item, overwrite: true, includeSecureContainer: true);
					SaveTeammate(sessionId, item);
					logger.Info($"Removed secure container from teammate '{item.Aid}' default loadout after Realistic boundary switch.", (Exception)null);
				}
				if (flag2)
				{
					FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, item);
					teammateSettings.SelectedLoadoutId = "000000000000000000000000";
					SaveTeammateSettings(sessionId, item, teammateSettings);
					logger.Info($"Selected existing Default loadout for teammate '{item.Aid}' after leaving Simple loadout management.", (Exception)null);
				}
			}
			catch (Exception ex)
			{
				logger.Warning($"Failed to apply loadout management mode change for teammate '{item.Aid}': {ex.Message}", (Exception)null);
			}
		}
	}

	public List<string> GetAutoJoinTeammateAccountIds(MongoId sessionId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		foreach (BotBase item in LoadTeammates(sessionId))
		{
			if (!GetTeammateSettings(sessionId, item).AutoJoinEnabled)
			{
				continue;
			}
			BotBase teammate = PrepareTeammateForFetch(item);
			if (!HasProperRaidKit(teammate))
			{
				logger.Info("Skipped auto-join for teammate '" + GetTeammateDisplayName(teammate) + "' because their Default loadout has no primary or pistol weapon.", (Exception)null);
				continue;
			}
			string text = item.Aid?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	private void PrepareNewTeammateDefaultForCurrentLoadoutMode(BotBase teammate)
	{
		string mode = NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode);
		if (IsExtremeLoadoutManagementMode(mode))
		{
			AssignInitialRealisticSecureContainer(teammate);
		}
	}

	private void AssignInitialRealisticSecureContainer(BotBase teammate)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_0022: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Expected O, but got Unknown
		//IL_0111: Expected O, but got Unknown
		BotBaseInventory val2;
		if (teammate.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			teammate.Inventory = val;
		}
		val2 = teammate.Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		if (teammate.Inventory.Items.Count != 0)
		{
			RemoveSecureContainerTree(teammate);
			Info info = teammate.Info;
			int num = Math.Max(1, ((info != null) ? info.Level : ((int?)null)) ?? 1);
			string text = ((num < 15) ? "5857a8b324597729ab0a0e7d" : ((num < 30) ? "59db794186f77448bc595262" : "5857a8bc2459772bad15db29"));
			teammate.Inventory.Items.Add(new Item
			{
				Id = new MongoId(),
				Template = new MongoId(text),
				ParentId = GetEquipmentRootId(teammate),
				SlotId = "SecuredContainer",
				Location = null,
				Upd = new Upd
				{
					StackObjectsCount = 1.0,
					SpawnedInSession = false
				}
			});
			logger.Info($"Assigned initial Realistic secure container '{text}' to teammate '{teammate.Aid}' at level {num}.", (Exception)null);
		}
	}

	private static bool RemoveSecureContainerTree(BotBase teammate)
	{
		object inventoryItems;
		if (teammate == null)
		{
			inventoryItems = null;
		}
		else
		{
			BotBaseInventory inventory = teammate.Inventory;
			inventoryItems = ((inventory != null) ? inventory.Items : null);
		}
		return RemoveSecureContainerTree((List<Item>?)inventoryItems);
	}

	private static bool RemoveSecureContainerTree(List<Item>? inventoryItems)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		if (inventoryItems == null || inventoryItems.Count == 0)
		{
			return false;
		}
		Item val = inventoryItems.FirstOrDefault((Item item) => string.Equals((item != null) ? item.SlotId : null, "SecuredContainer", StringComparison.OrdinalIgnoreCase));
		if (val != null)
		{
			_ = val.Id;
			if (0 == 0)
			{
				HashSet<string> removeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { val.Id.ToString() };
				bool flag = true;
				while (flag)
				{
					flag = false;
					foreach (Item inventoryItem in inventoryItems)
					{
						if (inventoryItem != null)
						{
							_ = inventoryItem.Id;
							if (0 == 0 && !string.IsNullOrWhiteSpace(inventoryItem.ParentId) && removeIds.Contains(inventoryItem.ParentId) && removeIds.Add(inventoryItem.Id.ToString()))
							{
								flag = true;
							}
						}
					}
				}
				return inventoryItems.RemoveAll(delegate(Item item)
				{
					//IL_0004: Unknown result type (might be due to invalid IL or missing references)
					//IL_0011: Unknown result type (might be due to invalid IL or missing references)
					//IL_0016: Unknown result type (might be due to invalid IL or missing references)
					if (item != null)
					{
						_ = item.Id;
						return removeIds.Contains(item.Id.ToString());
					}
					return false;
				}) > 0;
			}
		}
		return false;
	}

	private static bool RemoveItemTreesById(List<Item>? inventoryItems, IEnumerable<string>? rootItemIds)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		if (inventoryItems == null || inventoryItems.Count == 0 || rootItemIds == null)
		{
			return false;
		}
		HashSet<string> removeIds = rootItemIds.Where((string id) => !string.IsNullOrWhiteSpace(id)).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (removeIds.Count == 0)
		{
			return false;
		}
		bool flag = true;
		while (flag)
		{
			flag = false;
			foreach (Item inventoryItem in inventoryItems)
			{
				if (inventoryItem != null)
				{
					_ = inventoryItem.Id;
					if (0 == 0 && !string.IsNullOrWhiteSpace(inventoryItem.ParentId) && removeIds.Contains(inventoryItem.ParentId) && removeIds.Add(inventoryItem.Id.ToString()))
					{
						flag = true;
					}
				}
			}
		}
		return inventoryItems.RemoveAll(delegate(Item item)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				return removeIds.Contains(item.Id.ToString());
			}
			return false;
		}) > 0;
	}

	private BotBase GenerateTeammateBot(MongoId sessionId, BotGenerationDetails details)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		ProfileActivityRaidData profileActivityRaidData = profileActivityService.GetProfileActivityRaidData(sessionId);
		GetRaidConfigurationRequestData raidConfiguration = profileActivityRaidData.RaidConfiguration;
		bool flag = raidConfiguration == (GetRaidConfigurationRequestData)null;
		if (flag)
		{
			profileActivityRaidData.RaidConfiguration = CreateMenuTeammateGenerationRaidConfiguration();
		}
		try
		{
			return botGenerator.PrepareAndGenerateBot(sessionId, details);
		}
		finally
		{
			if (flag)
			{
				profileActivityRaidData.RaidConfiguration = raidConfiguration;
			}
		}
	}

	private static GetRaidConfigurationRequestData CreateMenuTeammateGenerationRaidConfiguration()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		return new GetRaidConfigurationRequestData
		{
			Location = "factory4_day",
			TimeVariant = (DateTimeEnum)0,
			IsNightRaid = false,
			RaidMode = (RaidMode)1,
			Side = (SideType)0,
			PlayersSpawnPlace = (PlayersSpawnPlace)0,
			TransitionType = (TransitionType)0,
			WavesSettings = new WavesSettings
			{
				BotAmount = (BotAmount)0,
				BotDifficulty = (BotDifficulty)3,
				IsBosses = true,
				IsTaggedAndCursed = false
			},
			BotSettings = new BotSettings
			{
				BotAmount = (BotAmount)0,
				IsScavWars = false
			},
			TimeAndWeatherSettings = new TimeAndWeatherSettings
			{
				IsRandomTime = false,
				IsRandomWeather = false
			},
			IsLocationTransition = false,
			CanShowGroupPreview = false,
			OnlinePveRaidStates = new Dictionary<string, bool>()
		};
	}

	public List<FriendlyTeammateFollowerDetailsResponse> ListFollowerDetails(MongoId sessionId)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		SptProfile fullProfile = profileHelper.GetFullProfile(sessionId);
		Dictionary<string, string> loadoutNames = GetCustomEquipmentBuilds(fullProfile).ToDictionary<EquipmentBuild, string, string>((EquipmentBuild build) => ((UserBuild)build).Id.ToString(), (EquipmentBuild build) => ((UserBuild)build).Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
		return LoadTeammates(sessionId).Select(delegate(BotBase teammate)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
			FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, teammate);
			string text = NormalizeCurrentLoadoutId(fullProfile, teammateSettings.SelectedLoadoutId);
			string value;
			string equipment = (string.Equals(text, "000000000000000000000000", StringComparison.OrdinalIgnoreCase) ? "Default" : (loadoutNames.TryGetValue(text, out value) ? value : "Default"));
			FriendlyTeammateFollowerDetailsResponse obj = new FriendlyTeammateFollowerDetailsResponse
			{
				Aid = (teammate.Aid?.ToString() ?? string.Empty),
				Tactic = NormalizeCombatTactic(teammateSettings.CombatTactic),
				Aggression = NormalizeAggression(teammateSettings.Aggression),
				Equipment = equipment
			};
			Customization customization = teammate.Customization;
			object obj2;
			if (customization == null)
			{
				obj2 = null;
			}
			else
			{
				MongoId? voice = customization.Voice;
				obj2 = (voice.HasValue ? voice.GetValueOrDefault().ToString() : null);
			}
			if (obj2 == null)
			{
				obj2 = string.Empty;
			}
			obj.Voice = (string)obj2;
			Customization customization2 = teammate.Customization;
			object obj3;
			if (customization2 == null)
			{
				obj3 = null;
			}
			else
			{
				MongoId? voice = customization2.Head;
				obj3 = (voice.HasValue ? voice.GetValueOrDefault().ToString() : null);
			}
			if (obj3 == null)
			{
				obj3 = string.Empty;
			}
			obj.Head = (string)obj3;
			return obj;
		}).ToList();
	}

	public List<UserDialogInfo> ListTeammateDialogs(MongoId sessionId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return LoadTeammates(sessionId).Select(ToFriendDialog).ToList();
	}

	public GetOtherProfileResponse GetTeammateProfile(MongoId sessionId, GetOtherProfileRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		BotBase teammate = FindByAccountId(sessionId, request.AccountId);
		return ToOtherProfileResponse(PrepareTeammateForFetch(teammate));
	}

	public FriendlyTeammateProfileOptionsResponse GetProfileOptions(MongoId sessionId, FriendlyTeammateProfileOptionsRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		BotBase teammate = FindByAccountId(sessionId, request.Aid);
		SptProfile fullProfile = profileHelper.GetFullProfile(sessionId);
		string currentLoadoutId = NormalizeCurrentLoadoutId(fullProfile, GetTeammateSettings(sessionId, teammate).SelectedLoadoutId);
		FriendlyTeammateProfileOptionsResponse obj = new FriendlyTeammateProfileOptionsResponse
		{
			CurrentLoadoutId = currentLoadoutId,
			CurrentTactic = NormalizeCombatTactic(GetTeammateSettings(sessionId, teammate).CombatTactic),
			Aggression = NormalizeAggression(GetTeammateSettings(sessionId, teammate).Aggression),
			RecoveryNotice = ConsumeProfileRecoveryNotice(sessionId, teammate)
		};
		int num = 1;
		List<FriendlyTeammateLoadoutOption> list = new List<FriendlyTeammateLoadoutOption>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<FriendlyTeammateLoadoutOption> span = CollectionsMarshal.AsSpan(list);
		int index = 0;
		span[index] = new FriendlyTeammateLoadoutOption
		{
			Id = "000000000000000000000000",
			Name = "Default"
		};
		obj.Loadouts = list;
		obj.Tactics = TacticOptions.Select((string tactic) => new FriendlyTeammateTacticOption
		{
			Id = tactic,
			Name = tactic
		}).ToList();
		FriendlyTeammateProfileOptionsResponse friendlyTeammateProfileOptionsResponse = obj;
		foreach (EquipmentBuild customEquipmentBuild in GetCustomEquipmentBuilds(fullProfile))
		{
			friendlyTeammateProfileOptionsResponse.Loadouts.Add(new FriendlyTeammateLoadoutOption
			{
				Id = ((UserBuild)customEquipmentBuild).Id.ToString(),
				Name = (((UserBuild)customEquipmentBuild).Name ?? string.Empty)
			});
		}
		return friendlyTeammateProfileOptionsResponse;
	}

	public void SetTeammateSuit(MongoId sessionId, FriendlyTeammateSuitRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_0043: Expected O, but got Unknown
		BotBase val = FindByAccountId(sessionId, request.Aid);
		if (request.Suit == null || request.Suit.Length < 2)
		{
			throw new FriendlyTeammateException("Missing teammate suit values");
		}
		BotBase val2 = val;
		if (val2.Customization == null)
		{
			Customization val3 = new Customization();
			Customization val4 = val3;
			val2.Customization = val3;
		}
		val.Customization.Body = (NormalizeRequiredValue(request.Suit[0], "body"));
		val.Customization.Feet = (NormalizeRequiredValue(request.Suit[1], "feet"));
		SaveTeammate(sessionId, val);
	}

	public void RenameTeammate(MongoId sessionId, FriendlyTeammateRenameRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_0045: Expected O, but got Unknown
		BotBase val = FindByAccountId(sessionId, request.Aid);
		string text = NormalizeRequiredValue(request.Nickname, "nickname");
		EnsureNicknameIsUnique(sessionId, text, val.Aid);
		BotBase val2 = val;
		if (val2.Info == null)
		{
			Info val3 = new Info();
			Info val4 = val3;
			val2.Info = val3;
		}
		val.Info.Nickname = text;
		val.Info.LowerNickname = text.ToLowerInvariant();
		SaveTeammate(sessionId, val);
	}

	public void SetTeammateLoadout(MongoId sessionId, FriendlyTeammateLoadoutRequest request)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		BotBase teammate = FindByAccountId(sessionId, request.Aid);
		string selectedLoadoutId = NormalizeRequiredValue(request.LoadoutId, "loadoutId");
		FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, teammate);
		if (string.Equals(selectedLoadoutId, "000000000000000000000000", StringComparison.OrdinalIgnoreCase))
		{
			RestoreDefaultEquipment(sessionId, teammate);
			teammateSettings.SelectedLoadoutId = "000000000000000000000000";
			SaveTeammateSettings(sessionId, teammate, teammateSettings);
			SaveTeammate(sessionId, teammate);
			return;
		}
		SptProfile fullProfile = profileHelper.GetFullProfile(sessionId);
		PmcData playerProfile = GetPlayerProfile(sessionId);
		EquipmentBuild val = GetCustomEquipmentBuilds(fullProfile).FirstOrDefault((EquipmentBuild build) => string.Equals(((UserBuild)build).Id.ToString(), selectedLoadoutId, StringComparison.OrdinalIgnoreCase));
		if (val == (EquipmentBuild)null)
		{
			throw new FriendlyTeammateException("Unable to find teammate equipment build '" + selectedLoadoutId + "'");
		}
		ApplyEquipmentBuild(teammate, val, playerProfile);
		teammateSettings.SelectedLoadoutId = selectedLoadoutId;
		SaveTeammateSettings(sessionId, teammate, teammateSettings);
		SaveTeammate(sessionId, teammate);
	}

	public FriendlyTeammateDefaultEquipmentResponse SaveTeammateDefaultEquipment(MongoId sessionId, FriendlyTeammateDefaultEquipmentRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_00a8: Expected O, but got Unknown
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		BotBase val = FindByAccountId(sessionId, request.Aid);
		List<Item> list = request.Items?.Where((Item item) => item != (Item)null).ToList();
		if (list == null || list.Count == 0)
		{
			throw new FriendlyTeammateException("Missing teammate default equipment items");
		}
		string mode = NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode);
		if (request.RealItemCommit && IsRealTransferLoadoutManagementMode(mode))
		{
			return SaveTeammateDefaultEquipmentWithRealItemCommit(sessionId, val, request, list, mode);
		}
		BotBase val2 = val;
		if (val2.Inventory == null)
		{
			BotBaseInventory val3 = new BotBaseInventory();
			BotBaseInventory val4 = val3;
			val2.Inventory = val3;
		}
		List<Item> items = MergeEquipmentWithPreservedSpecialItems(val.Inventory.Items, cloner.Clone<List<Item>>(list) ?? list);
		val.Inventory.Items = items;
		val.Inventory.Equipment = val.Inventory.Items.First().Id;
		FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, val);
		teammateSettings.SelectedLoadoutId = "000000000000000000000000";
		SaveDefaultEquipmentSnapshot(sessionId, val, overwrite: true, IsExtremeLoadoutManagementMode(mode));
		SaveTeammateSettings(sessionId, val, teammateSettings);
		SaveTeammate(sessionId, val);
		return new FriendlyTeammateDefaultEquipmentResponse();
	}

	public FriendlyTeammateBuyKitResponse BuyTeammateKit(MongoId sessionId, FriendlyTeammateBuyKitRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected O, but got Unknown
		//IL_00c3: Expected O, but got Unknown
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		//IL_010a: Expected O, but got Unknown
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		BotBase val = FindByAccountId(sessionId, request.Aid);
		string mode = NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode);
		if (!IsRealTransferLoadoutManagementMode(mode))
		{
			throw CreateKitPurchaseException(sessionId, "Rejected teammate kit purchase outside a real-transfer loadout management mode.");
		}
		List<Item> list = request.Items?.Where((Item item) => item != (Item)null).ToList();
		if (list == null || list.Count == 0)
		{
			throw new FriendlyTeammateException("Missing teammate kit equipment items");
		}
		int num = Math.Max(0, request.Price);
		PmcData playerProfile = GetPlayerProfile(sessionId);
		PmcData val2 = playerProfile;
		BotBaseInventory val4;
		if (((BotBase)val2).Inventory == null)
		{
			BotBaseInventory val3 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val4 = val3;
			((BotBase)val2).Inventory = val3;
		}
		val4 = ((BotBase)playerProfile).Inventory;
		if (val4.Items == null)
		{
			List<Item> list2 = (val4.Items = new List<Item>());
		}
		BotBase val5 = val;
		if (val5.Inventory == null)
		{
			BotBaseInventory val6 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val4 = val6;
			val5.Inventory = val6;
		}
		val4 = val.Inventory;
		if (val4.Items == null)
		{
			List<Item> list2 = (val4.Items = new List<Item>());
		}
		List<Item> items = cloner.Clone<List<Item>>(((BotBase)playerProfile).Inventory.Items) ?? ((BotBase)playerProfile).Inventory.Items.ToList();
		List<Item> items2 = cloner.Clone<List<Item>>(val.Inventory.Items) ?? val.Inventory.Items.ToList();
		MongoId? equipment = val.Inventory.Equipment;
		SptProfile fullProfile = profileHelper.GetFullProfile(sessionId);
		Dictionary<MongoId, Dialogue> dialogueRecords = cloner.Clone<Dictionary<MongoId, Dialogue>>(fullProfile.DialogueRecords);
		try
		{
			if (request.UseItemsInStash)
			{
				ConsumeStashItemsForKit(sessionId, playerProfile, request.UsedItems);
			}
			DeductRoublesFromPlayerStash(playerProfile, num);
			List<Item> list5 = cloner.Clone<List<Item>>(list) ?? list;
			List<Item> list6 = itemHelper.ReplaceIDs((IEnumerable<Item>)list5, playerProfile, (IEnumerable<InsuredItem>)null).ToList();
			MongoId id = list6.First().Id;
			List<Item> deliveryItems = BuildCurrentTeammateKitDeliveryItems(val, IsExtremeLoadoutManagementMode(mode));
			val.Inventory.Items = MergeEquipmentWithPreservedSpecialItems(val.Inventory.Items, list6, IsExtremeLoadoutManagementMode(mode));
			val.Inventory.Equipment = id;
			FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, val);
			teammateSettings.SelectedLoadoutId = "000000000000000000000000";
			SendPreviousTeammateKitDelivery(sessionId, val, deliveryItems);
			SaveDefaultEquipmentSnapshot(sessionId, val, overwrite: true, IsExtremeLoadoutManagementMode(mode));
			SaveTeammateSettings(sessionId, val, teammateSettings);
			SaveTeammate(sessionId, val);
			saveServer.SaveProfileAsync(sessionId).GetAwaiter().GetResult();
			logger.Info($"Bought teammate kit for '{val.Aid}' with price={num}, useItemsInStash={request.UseItemsInStash}; previous active kit sent by delivery when present.", (Exception)null);
			return new FriendlyTeammateBuyKitResponse
			{
				PlayerStashItems = GetPlayerStashItems(playerProfile)
			};
		}
		catch
		{
			((BotBase)playerProfile).Inventory.Items = items;
			val.Inventory.Items = items2;
			val.Inventory.Equipment = equipment;
			fullProfile.DialogueRecords = dialogueRecords;
			throw;
		}
	}

	public FriendlyTeammateRepairEquipmentResponse RepairTeammateDefaultEquipment(MongoId sessionId, FriendlyTeammateRepairEquipmentRequest request)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_00df: Expected O, but got Unknown
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Expected O, but got Unknown
		//IL_015c: Expected O, but got Unknown
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Expected O, but got Unknown
		//IL_0294: Expected O, but got Unknown
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Unknown result type (might be due to invalid IL or missing references)
		//IL_0472: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Expected O, but got Unknown
		//IL_0486: Unknown result type (might be due to invalid IL or missing references)
		//IL_048b: Unknown result type (might be due to invalid IL or missing references)
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_074f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0754: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Expected O, but got Unknown
		//IL_075c: Expected O, but got Unknown
		//IL_07b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0630: Unknown result type (might be due to invalid IL or missing references)
		//IL_0662: Unknown result type (might be due to invalid IL or missing references)
		//IL_0667: Unknown result type (might be due to invalid IL or missing references)
		//IL_066a: Expected O, but got Unknown
		//IL_066f: Expected O, but got Unknown
		BotBase val = FindByAccountId(sessionId, request.Aid);
		string targetItemId = NormalizeRequiredValue(request.Target, "target");
		List<RepairKitsInfo> list = request.RepairKitsInfo?.Where((RepairKitsInfo kit) => kit != (RepairKitsInfo)null).ToList();
		bool flag = list != null && list.Count > 0;
		bool flag2 = !string.IsNullOrWhiteSpace(request.TraderId) && (request.RepairCount ?? 0.0) > 0.0;
		if (!flag && !flag2)
		{
			throw new FriendlyTeammateException("Missing repair information for teammate repair");
		}
		BotBase val2 = val;
		BotBaseInventory val4;
		if (val2.Inventory == null)
		{
			BotBaseInventory val3 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val4 = val3;
			val2.Inventory = val3;
		}
		val4 = val.Inventory;
		if (val4.Items == null)
		{
			List<Item> list2 = (val4.Items = new List<Item>());
		}
		Item val5 = val.Inventory.Items.FirstOrDefault((Item item) => string.Equals(item.Id.ToString(), targetItemId, StringComparison.OrdinalIgnoreCase)) ?? throw new FriendlyTeammateException("Unable to find teammate equipment item to repair");
		PmcData playerProfile = GetPlayerProfile(sessionId);
		PmcData val6 = playerProfile;
		if (((BotBase)val6).Inventory == null)
		{
			PmcData obj = val6;
			BotBaseInventory val7 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val4 = val7;
			((BotBase)obj).Inventory = val7;
		}
		val4 = ((BotBase)playerProfile).Inventory;
		if (val4.Items == null)
		{
			List<Item> list2 = (val4.Items = new List<Item>());
		}
		if (flag)
		{
			foreach (RepairKitsInfo repairKit in list)
			{
				if ((repairKit.Count ?? 0f) <= 0f)
				{
					throw new FriendlyTeammateException("Invalid repair kit amount for teammate repair");
				}
				if (!((BotBase)playerProfile).Inventory.Items.Any((Item item) => item.Id == repairKit.Id))
				{
					throw new FriendlyTeammateException($"Player repair kit '{repairKit.Id}' was unavailable for teammate repair");
				}
			}
		}
		PmcData val8 = cloner.Clone<PmcData>(playerProfile) ?? throw new FriendlyTeammateException("Unable to clone player profile for teammate repair");
		val6 = val8;
		if (((BotBase)val6).Inventory == null)
		{
			PmcData obj2 = val6;
			BotBaseInventory val9 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val4 = val9;
			((BotBase)obj2).Inventory = val9;
		}
		val4 = ((BotBase)val8).Inventory;
		if (val4.Items == null)
		{
			List<Item> list2 = (val4.Items = new List<Item>());
		}
		List<Item> list6 = cloner.Clone<List<Item>>(val.Inventory.Items) ?? val.Inventory.Items.ToList();
		HashSet<string> hashSet = ((BotBase)val8).Inventory.Items.Select((Item item) => item.Id.ToString()).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Item item in list6)
		{
			if (item != null)
			{
				_ = item.Id;
				if (0 == 0 && !hashSet.Contains(item.Id.ToString()))
				{
					((BotBase)val8).Inventory.Items.Add(item);
				}
			}
		}
		List<Item> list7 = cloner.Clone<List<Item>>(((BotBase)playerProfile).Inventory.Items) ?? ((BotBase)playerProfile).Inventory.Items.ToList();
		List<Item> items = cloner.Clone<List<Item>>(val.Inventory.Items) ?? val.Inventory.Items.ToList();
		MongoId? equipment = val.Inventory.Equipment;
		try
		{
			ItemEventRouterResponse val10 = CreateEmptyRepairOutput(sessionId, playerProfile);
			if (flag)
			{
				RepairDetails val11 = repairService.RepairItemByKit(sessionId, val8, list, new MongoId(targetItemId), val10);
				repairService.AddBuffToItem(val11, val8);
				repairService.AddRepairSkillPoints(sessionId, val11, val8);
			}
			else
			{
				MongoId val12 = default(MongoId);
				val12 = new MongoId(NormalizeRequiredValue(request.TraderId, "traderId"));
				RepairItem val13 = new RepairItem
				{
					Id = new MongoId(targetItemId),
					Count = request.RepairCount
				};
				RepairDetails val11 = repairService.RepairItemByTrader(sessionId, val8, val13, val12);
				repairService.PayForRepair(sessionId, playerProfile, targetItemId, val11.RepairCost ?? 0.0, val12, val10);
				List<Warning> warnings = ((ItemEventRouterBase)val10).Warnings;
				if (warnings != null && warnings.Count > 0)
				{
					throw new FriendlyTeammateException("Unable to pay for teammate trader repair");
				}
				repairService.AddRepairSkillPoints(sessionId, val11, val8);
			}
			Item val14 = ((BotBase)val8).Inventory.Items.FirstOrDefault((Item item) => string.Equals(item.Id.ToString(), targetItemId, StringComparison.OrdinalIgnoreCase)) ?? throw new FriendlyTeammateException("Stock repair did not return the repaired teammate item");
			Upd upd = val14.Upd;
			UpdRepairable val15 = ((upd != null) ? upd.Repairable : null) ?? throw new FriendlyTeammateException("Stock repair did not produce teammate repair durability data");
			Upd upd2 = val14.Upd;
			UpdBuff val16 = ((upd2 != null) ? upd2.Buff : null);
			Item val19;
			if (flag)
			{
				foreach (RepairKitsInfo repairKit2 in list)
				{
					Item val17 = ((BotBase)val8).Inventory.Items.FirstOrDefault((Item item) => item.Id == repairKit2.Id);
					Item val18 = ((BotBase)playerProfile).Inventory.Items.FirstOrDefault((Item item) => item.Id == repairKit2.Id);
					if (val17 == (Item)null)
					{
						RemoveItemTreesById(((BotBase)playerProfile).Inventory.Items, new List<string> { repairKit2.Id.ToString() });
						continue;
					}
					if (val18 == (Item)null)
					{
						throw new FriendlyTeammateException($"Player repair kit '{repairKit2.Id}' disappeared during teammate repair");
					}
					val19 = val18;
					if (val19.Upd == null)
					{
						Item obj3 = val19;
						Upd val20 = new Upd();
						Upd val21 = val20;
						obj3.Upd = val20;
					}
					Upd upd3 = val18.Upd;
					ICloner obj4 = cloner;
					Upd upd4 = val17.Upd;
					upd3.RepairKit = obj4.Clone<UpdRepairKit>((upd4 != null) ? upd4.RepairKit : null);
					Upd upd5 = val18.Upd;
					Upd upd6 = val17.Upd;
					upd5.StackObjectsCount = ((upd6 != null) ? upd6.StackObjectsCount : ((double?)null)) ?? val18.Upd.StackObjectsCount;
				}
			}
			((BotBase)playerProfile).Skills = cloner.Clone<Skills>(((BotBase)val8).Skills) ?? ((BotBase)playerProfile).Skills;
			((BotBase)playerProfile).Bonuses = cloner.Clone<List<Bonus>>(((BotBase)val8).Bonuses) ?? ((BotBase)playerProfile).Bonuses;
			val19 = val5;
			if (val19.Upd == null)
			{
				Item obj5 = val19;
				Upd val22 = new Upd();
				Upd val21 = val22;
				obj5.Upd = val22;
			}
			UpdRepairable val23 = cloner.Clone<UpdRepairable>(val15) ?? val15;
			val5.Upd.Repairable = val23;
			val5.Upd.Buff = cloner.Clone<UpdBuff>(val16);
			string mode = NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode);
			SaveDefaultEquipmentSnapshot(sessionId, val, overwrite: true, IsExtremeLoadoutManagementMode(mode));
			SaveTeammate(sessionId, val);
			saveServer.SaveProfileAsync(sessionId).GetAwaiter().GetResult();
			logger.Info($"Repaired teammate '{val.Aid}' default equipment item '{targetItemId}' through stock {(flag ? "kit" : "trader")} repair service.", (Exception)null);
			(List<Item>, List<Item>, List<string>) tuple = BuildPlayerStashDelta(playerProfile, list7);
			FriendlyTeammateRepairEquipmentResponse obj6 = new FriendlyTeammateRepairEquipmentResponse
			{
				ItemId = targetItemId,
				Durability = val23.Durability,
				MaxDurability = val23.MaxDurability,
				PlayerStashItems = GetPlayerStashItems(playerProfile)
			};
			(obj6.PlayerNewStashItems, obj6.PlayerChangedStashItems, obj6.PlayerDeletedStashItemIds) = tuple;
			return obj6;
		}
		catch
		{
			((BotBase)playerProfile).Inventory.Items = list7;
			val.Inventory.Items = items;
			val.Inventory.Equipment = equipment;
			throw;
		}
	}

	private FriendlyTeammateDefaultEquipmentResponse SaveTeammateDefaultEquipmentWithRealItemCommit(MongoId sessionId, BotBase teammate, FriendlyTeammateDefaultEquipmentRequest request, List<Item> replacementEquipmentItems, string mode)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_00cd: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		//IL_0113: Expected O, but got Unknown
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		List<Item> list = request.PlayerStashItems?.Where((Item item) => item != (Item)null).ToList();
		if (list == null || list.Count == 0)
		{
			throw new FriendlyTeammateException("Missing player stash items for real teammate equipment save");
		}
		replacementEquipmentItems = PruneSubmittedEquipmentToRootTree(replacementEquipmentItems, out var prunedCount);
		if (prunedCount > 0)
		{
			logger.Warning($"Pruned {prunedCount} foreign/orphan items from submitted teammate equipment before real commit validation.", (Exception)null);
		}
		PmcData playerProfile = GetPlayerProfile(sessionId);
		PmcData val = playerProfile;
		BotBaseInventory val3;
		if (((BotBase)val).Inventory == null)
		{
			BotBaseInventory val2 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val3 = val2;
			((BotBase)val).Inventory = val2;
		}
		val3 = ((BotBase)playerProfile).Inventory;
		if (val3.Items == null)
		{
			List<Item> list2 = (val3.Items = new List<Item>());
		}
		if (teammate.Inventory == null)
		{
			BotBaseInventory val4 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val3 = val4;
			teammate.Inventory = val4;
		}
		val3 = teammate.Inventory;
		if (val3.Items == null)
		{
			List<Item> list2 = (val3.Items = new List<Item>());
		}
		string playerStashRootId = GetPlayerStashRootId(playerProfile);
		string b = list.First().Id.ToString();
		if (!string.Equals(playerStashRootId, b, StringComparison.OrdinalIgnoreCase))
		{
			throw new FriendlyTeammateException("Submitted player stash root does not match the active profile stash");
		}
		HashSet<string> currentPlayerStashIds = GetItemTreeIds(((BotBase)playerProfile).Inventory.Items, playerStashRootId);
		HashSet<string> hashSet = teammate.Inventory.Items.Select((Item item) => item.Id.ToString()).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet2 = new HashSet<string>(currentPlayerStashIds, StringComparer.OrdinalIgnoreCase);
		hashSet2.UnionWith(hashSet);
		HashSet<string> other = RemapReplacementEquipmentPlayerEquippedIdCollisions(playerProfile, replacementEquipmentItems, currentPlayerStashIds, hashSet);
		hashSet2.UnionWith(other);
		ValidateRealCommitItemSet(replacementEquipmentItems, hashSet2, "teammate equipment", allowGeneratedSlotDescendants: true);
		ValidateRealCommitItemSet(list, hashSet2, "player stash", allowGeneratedSlotDescendants: true);
		ValidateNoRealCommitOverlap(replacementEquipmentItems, list, playerStashRootId);
		ValidateNoEquippedPlayerItemCommit(playerProfile, replacementEquipmentItems, currentPlayerStashIds);
		string languageValue = GetLanguageValue(languageService.GetStringMap(sessionId, "socialUi"), "LockedStashItemBlocked", "Teammate loadout save blocked: an item inside a locked stash container was moved or changed. Unlock the container and try again. Locked container: id={0}, tpl={1}. Blocked item: id={2}, tpl={3}.");
		ValidateLockedPlayerStashItemsUnchanged(((BotBase)playerProfile).Inventory.Items, replacementEquipmentItems, list, playerStashRootId, languageValue);
		List<Item> list5 = cloner.Clone<List<Item>>(((BotBase)playerProfile).Inventory.Items) ?? ((BotBase)playerProfile).Inventory.Items.ToList();
		List<Item> items = cloner.Clone<List<Item>>(teammate.Inventory.Items) ?? teammate.Inventory.Items.ToList();
		MongoId? equipment = teammate.Inventory.Equipment;
		try
		{
			List<Item> replacementItems = cloner.Clone<List<Item>>(replacementEquipmentItems) ?? replacementEquipmentItems;
			List<Item> items2 = MergeEquipmentWithPreservedSpecialItems(teammate.Inventory.Items, replacementItems, IsExtremeLoadoutManagementMode(mode));
			((BotBase)playerProfile).Inventory.Items = ((BotBase)playerProfile).Inventory.Items.Where((Item item) => !currentPlayerStashIds.Contains(item.Id.ToString())).ToList();
			((BotBase)playerProfile).Inventory.Items.AddRange(cloner.Clone<List<Item>>(list) ?? list);
			teammate.Inventory.Items = items2;
			teammate.Inventory.Equipment = teammate.Inventory.Items.First().Id;
			FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, teammate);
			teammateSettings.SelectedLoadoutId = "000000000000000000000000";
			SaveDefaultEquipmentSnapshot(sessionId, teammate, overwrite: true, IsExtremeLoadoutManagementMode(mode));
			SaveTeammateSettings(sessionId, teammate, teammateSettings);
			SaveTeammate(sessionId, teammate);
			saveServer.SaveProfileAsync(sessionId).GetAwaiter().GetResult();
			(List<Item>, List<Item>, List<string>) tuple = BuildPlayerStashDelta(playerProfile, list5);
			logger.Info($"Committed real default equipment movement for teammate '{teammate.Aid}' in loadout management mode '{NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode)}'.", (Exception)null);
			FriendlyTeammateDefaultEquipmentResponse obj = new FriendlyTeammateDefaultEquipmentResponse
			{
				RealItemCommit = true,
				PlayerStashItems = (cloner.Clone<List<Item>>(list) ?? list)
			};
			(obj.PlayerNewStashItems, obj.PlayerChangedStashItems, obj.PlayerDeletedStashItemIds) = tuple;
			return obj;
		}
		catch
		{
			((BotBase)playerProfile).Inventory.Items = list5;
			teammate.Inventory.Items = items;
			teammate.Inventory.Equipment = equipment;
			throw;
		}
	}

	public void SetTeammateAggression(MongoId sessionId, FriendlyTeammateAggressionRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		BotBase teammate = FindByAccountId(sessionId, request.Aid);
		FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, teammate);
		teammateSettings.Aggression = NormalizeAggression(request.Aggression);
		SaveTeammateSettings(sessionId, teammate, teammateSettings);
	}

	public void SetTeammateTactic(MongoId sessionId, FriendlyTeammateTacticRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		BotBase val = FindByAccountId(sessionId, request.Aid);
		FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, val);
		teammateSettings.CombatTactic = NormalizeCombatTactic(request.Tactic);
		teammateSettings.Aggression = GetDefaultAggressionForTactic(teammateSettings.CombatTactic);
		SaveTeammateSettings(sessionId, val, teammateSettings);
		logger.Info($"Set teammate tactic '{teammateSettings.CombatTactic}' for aid '{val.Aid}' in session '{sessionId}'", (Exception)null);
	}

	public void SetTeammateAutoJoin(MongoId sessionId, FriendlyTeammateAutoJoinRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		BotBase teammate = FindByAccountId(sessionId, request.Aid);
		FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, teammate);
		teammateSettings.AutoJoinEnabled = request.Enabled;
		SaveTeammateSettings(sessionId, teammate, teammateSettings);
	}

	public bool TryGetTeammateProfile(MongoId sessionId, string? accountId, out GetOtherProfileResponse? profile)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		profile = null;
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return false;
		}
		BotBase val = LoadTeammates(sessionId).FirstOrDefault((BotBase candidate) => candidate.Aid?.ToString() == accountId);
		if (val == (BotBase)null)
		{
			return false;
		}
		profile = ToOtherProfileResponse(PrepareTeammateForFetch(val));
		return true;
	}

	public bool TryGetRaidGroupCharacter(MongoId sessionId, string? accountId, out GroupCharacter? groupCharacter)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		string rejectionReason;
		bool flag = TryGetRaidGroupCharacter(sessionId, accountId, out groupCharacter, out rejectionReason);
		if (!flag)
		{
			groupCharacter = null;
		}
		return flag;
	}

	public bool TryGetRaidGroupCharacter(MongoId sessionId, string? accountId, out GroupCharacter? groupCharacter, out string? rejectionReason)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		groupCharacter = null;
		rejectionReason = null;
		if (!TryFindByAccountId(sessionId, accountId, out BotBase teammate))
		{
			return false;
		}
		BotBase teammate2 = PrepareTeammateForFetch(teammate);
		groupCharacter = ToGroupCharacter(teammate2);
		if (!HasProperRaidKit(teammate2))
		{
			rejectionReason = "Cannot add " + GetTeammateDisplayName(teammate2) + " to the raid group without a proper kit.";
			return false;
		}
		return true;
	}

	public bool TryGetSpawnProfile(MongoId sessionId, string? accountId, double? healthMultiplier, out BotBase? profile)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		profile = null;
		if (!TryFindByAccountId(sessionId, accountId, out BotBase teammate))
		{
			return false;
		}
		profile = PrepareTeammateForFetch(teammate, healthMultiplier, refillMagazinesForSpawn: true);
		return true;
	}

	private BotBase PrepareTeammateForFetch(BotBase teammate, double? healthMultiplier = null, bool refillMagazinesForSpawn = false)
	{
		BotBase val = cloner.Clone<BotBase>(teammate) ?? teammate;
		ApplyTemporaryHealthMultiplier(val, healthMultiplier);
		EnsureFollowerHasPockets(val);
		string mode = NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode);
		if (!IsExtremeLoadoutManagementMode(mode))
		{
			EnsureManagedSecureContainer(val);
			EnsureFollowerHasSecureContainerSupplies(val);
		}
		if (refillMagazinesForSpawn)
		{
			EnsureFollowerHasScabbardKnife(val);
			RefillFollowerMagazinesFromInventoryAmmo(val);
		}
		return val;
	}

	public void PersistFollowerProgress(MongoId sessionId, IEnumerable<FriendlyTeammateFollowerProgressRequest>? progressEntries)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (progressEntries == null)
		{
			return;
		}
		lock (RaidResultPersistenceLock)
		{
			foreach (FriendlyTeammateFollowerProgressRequest progressEntry in progressEntries)
			{
				if (TryFindByAccountId(sessionId, progressEntry.Aid, out BotBase teammate) && !(teammate == (BotBase)null))
				{
					ApplyFollowerProgress(teammate, progressEntry);
					SaveTeammate(sessionId, teammate);
				}
			}
		}
	}

	public FriendlyTeammateDeathEscapeSummary PersistDeathEscapeOutcomes(MongoId sessionId, IEnumerable<FriendlyTeammateDeathEscapeEntry>? entries)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		lock (RaidResultPersistenceLock)
		{
			FriendlyTeammateDeathEscapeSummary friendlyTeammateDeathEscapeSummary = new FriendlyTeammateDeathEscapeSummary();
			if (entries == null)
			{
				return friendlyTeammateDeathEscapeSummary;
			}
			Dictionary<string, HashSet<string>> protectedTeammateItemIdsByAid = BuildProtectedTeammateItemIdsByAid(sessionId);
			foreach (FriendlyTeammateDeathEscapeEntry entry in entries)
			{
				if (!(entry == null) && !string.IsNullOrWhiteSpace(entry.Aid))
				{
					string item = (string.IsNullOrWhiteSpace(entry.Nickname) ? "Squadmate" : entry.Nickname);
					if (entry.Escaped)
					{
						friendlyTeammateDeathEscapeSummary.EscapedNames.Add(item);
					}
					else
					{
						friendlyTeammateDeathEscapeSummary.LostNames.Add(item);
					}
					if (string.IsNullOrWhiteSpace(friendlyTeammateDeathEscapeSummary.ExtractName) && !string.IsNullOrWhiteSpace(entry.ExtractName))
					{
						friendlyTeammateDeathEscapeSummary.ExtractName = entry.ExtractName;
					}
					if (TryFindByAccountId(sessionId, entry.Aid, out BotBase teammate) && !(teammate == (BotBase)null))
					{
						ApplyFollowerRaidOutcomeStats(teammate, entry.Escaped);
						ApplyDeathEscapeOutcome(teammate, entry);
						ApplyImmersiveEscapedDefaultEquipmentState(sessionId, teammate, entry, protectedTeammateItemIdsByAid);
						ApplyRestrictedGearMaintenanceDeathEquipmentState(sessionId, teammate, entry, protectedTeammateItemIdsByAid);
						ApplyImmersiveDefaultGearLoss(sessionId, teammate, entry);
						SaveTeammate(sessionId, teammate);
					}
				}
			}
			return friendlyTeammateDeathEscapeSummary;
		}
	}

	public FriendlyTeammateRaidOutcomeResponse ResolveRaidOutcomes(IEnumerable<FriendlyTeammateDeathEscapeEntry>? entries)
	{
		FriendlyTeammateRaidOutcomeResponse friendlyTeammateRaidOutcomeResponse = new FriendlyTeammateRaidOutcomeResponse();
		if (entries == null)
		{
			return friendlyTeammateRaidOutcomeResponse;
		}
		foreach (FriendlyTeammateDeathEscapeEntry entry in entries)
		{
			if (!(entry == null))
			{
				FriendlyTeammateDeathEscapeEntry friendlyTeammateDeathEscapeEntry = CloneRaidOutcomeEntry(entry);
				if (friendlyTeammateDeathEscapeEntry.RollEscape)
				{
					friendlyTeammateDeathEscapeEntry.Chance = CalculateDeathEscapeChance(friendlyTeammateDeathEscapeEntry);
					friendlyTeammateDeathEscapeEntry.Escaped = RollDeathEscape(friendlyTeammateDeathEscapeEntry.Chance);
					logger.Info($"Resolved teammate raid escape roll for '{friendlyTeammateDeathEscapeEntry.Nickname ?? friendlyTeammateDeathEscapeEntry.Aid}': escaped={friendlyTeammateDeathEscapeEntry.Escaped}, chance={friendlyTeammateDeathEscapeEntry.Chance:P0}, health={friendlyTeammateDeathEscapeEntry.HealthRatio:P0}, gear={friendlyTeammateDeathEscapeEntry.EquipmentPower:0.0}, routeEnemyAvg={friendlyTeammateDeathEscapeEntry.RouteEnemyAveragePower:0.0}, fightEnemyAvg={friendlyTeammateDeathEscapeEntry.CurrentFightEnemyAveragePower:0.0}.", (Exception)null);
				}
				friendlyTeammateRaidOutcomeResponse.Entries.Add(friendlyTeammateDeathEscapeEntry);
			}
		}
		return friendlyTeammateRaidOutcomeResponse;
	}

	private static FriendlyTeammateDeathEscapeEntry CloneRaidOutcomeEntry(FriendlyTeammateDeathEscapeEntry entry)
	{
		return new FriendlyTeammateDeathEscapeEntry
		{
			Aid = entry.Aid,
			ProfileId = entry.ProfileId,
			Nickname = entry.Nickname,
			Escaped = entry.Escaped,
			RollEscape = entry.RollEscape,
			Chance = entry.Chance,
			ExtractName = entry.ExtractName,
			Distance = entry.Distance,
			HealthRatio = entry.HealthRatio,
			EquipmentPower = entry.EquipmentPower,
			EnemyAveragePower = entry.EnemyAveragePower,
			RouteEnemyAveragePower = entry.RouteEnemyAveragePower,
			CurrentFightEnemyAveragePower = entry.CurrentFightEnemyAveragePower,
			RouteEnemyCount = entry.RouteEnemyCount,
			CurrentFightEnemyCount = entry.CurrentFightEnemyCount,
			AliveSquadmates = entry.AliveSquadmates,
			HasSecureMeds = entry.HasSecureMeds,
			VitalsDestroyed = entry.VitalsDestroyed,
			EquipmentItems = entry.EquipmentItems,
			TrackedItemIds = entry.TrackedItemIds
		};
	}

	private static double CalculateDeathEscapeChance(FriendlyTeammateDeathEscapeEntry entry)
	{
		double num = CalculateDeathEscapeDistanceScore(entry.Distance);
		double num2 = Math.Clamp((double)entry.AliveSquadmates / 3.0, 0.0, 1.0);
		if (entry.AliveSquadmates == 1)
		{
			num2 = 0.35;
		}
		else if (entry.AliveSquadmates == 2)
		{
			num2 = 0.7;
		}
		double enemyAveragePower = ((entry.RouteEnemyAveragePower > 0.0) ? entry.RouteEnemyAveragePower : entry.EnemyAveragePower);
		double currentFightEnemyAveragePower = entry.CurrentFightEnemyAveragePower;
		double num3 = CalculateDeathEscapeEquipmentScore(entry.EquipmentPower, enemyAveragePower);
		double num4 = (entry.HasSecureMeds ? 1.0 : 0.0);
		double num5 = 0.2 + 0.25 * num + 0.25 * Math.Clamp(entry.HealthRatio, 0.0, 1.0) + 0.2 * num3 + 0.15 * num2 + 0.1 * num4;
		if (entry.VitalsDestroyed)
		{
			num5 *= 0.25;
		}
		num5 *= CalculateCurrentFightSurvivalMultiplier(entry.AliveSquadmates, entry.HealthRatio, entry.EquipmentPower, currentFightEnemyAveragePower, entry.CurrentFightEnemyCount);
		return Math.Clamp(num5, 0.05, 0.9);
	}

	private static double CalculateDeathEscapeDistanceScore(double distance)
	{
		if (distance <= 0.0)
		{
			return 0.5;
		}
		return 1.0 - Math.Clamp((distance - 150.0) / 750.0, 0.0, 1.0);
	}

	private static double CalculateDeathEscapeEquipmentScore(double followerPower, double enemyAveragePower)
	{
		if (enemyAveragePower <= 0.01)
		{
			return 0.75;
		}
		double num = Math.Clamp(followerPower / enemyAveragePower, 0.25, 1.25);
		return Math.Clamp((num - 0.25) / 1.0, 0.0, 1.0);
	}

	private static double CalculateCurrentFightSurvivalMultiplier(int aliveCount, double healthRatio, double equipmentPower, double currentFightEnemyAveragePower, int currentFightEnemyCount)
	{
		if (currentFightEnemyCount <= 0 || currentFightEnemyAveragePower <= 0.01)
		{
			return 1.0;
		}
		double num = aliveCount switch
		{
			2 => 0.7, 
			1 => 0.35, 
			_ => Math.Clamp((double)aliveCount / 3.0, 0.0, 1.0), 
		};
		double num2 = CalculateDeathEscapeEquipmentScore(equipmentPower, currentFightEnemyAveragePower);
		double num3 = Math.Clamp((double)(currentFightEnemyCount - aliveCount) / 4.0, 0.0, 1.0);
		double value = 0.35 + 0.25 * Math.Clamp(healthRatio, 0.0, 1.0) + 0.25 * num2 + 0.15 * num - 0.15 * num3;
		return Math.Clamp(value, 0.25, 0.95);
	}

	private static bool RollDeathEscape(double chance)
	{
		lock (DeathEscapeRandomLock)
		{
			return DeathEscapeRandom.NextDouble() <= chance;
		}
	}

	private void ApplyImmersiveEscapedDefaultEquipmentState(MongoId sessionId, BotBase teammate, FriendlyTeammateDeathEscapeEntry entry, Dictionary<string, HashSet<string>> protectedTeammateItemIdsByAid)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Expected O, but got Unknown
		//IL_0175: Expected O, but got Unknown
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		if (!entry.Escaped)
		{
			return;
		}
		FriendlyServerSettingsRequest friendlyServerSettingsRequest = settingsService.LoadSettings();
		string text = NormalizeLoadoutManagementMode(friendlyServerSettingsRequest.LoadoutManagementMode);
		if (!ShouldPersistEscapedDefaultEquipmentState(text, friendlyServerSettingsRequest))
		{
			return;
		}
		FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, teammate);
		if (!string.Equals(teammateSettings.SelectedLoadoutId, "000000000000000000000000", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		List<Item> list = entry.EquipmentItems?.Where((Item item) => item != (Item)null).ToList();
		if (list == null || list.Count == 0)
		{
			logger.Warning($"Skipped escaped Default equipment persistence for teammate '{teammate.Aid}' because no equipment snapshot was provided.", (Exception)null);
			return;
		}
		List<Item> list2 = cloner.Clone<List<Item>>(list) ?? list;
		RemoveItemTreesById(list2, entry.TrackedItemIds);
		RemoveItemTreesById(list2, GetOtherProtectedTeammateItemIds(protectedTeammateItemIdsByAid, entry.Aid));
		if (list2.Count == 0)
		{
			logger.Warning($"Skipped escaped Default equipment persistence for teammate '{teammate.Aid}' because the filtered equipment snapshot was empty.", (Exception)null);
			return;
		}
		if (teammate.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			BotBaseInventory val2 = val;
			teammate.Inventory = val;
		}
		teammate.Inventory.Items = MergeEquipmentWithPreservedSpecialItems(teammate.Inventory.Items, list2, IsExtremeLoadoutManagementMode(text));
		teammate.Inventory.Equipment = teammate.Inventory.Items.First().Id;
		bool flag = IsExtremeLoadoutManagementMode(text);
		if (!flag)
		{
			RemoveSecureContainerTree(teammate);
		}
		EnsureFollowerHasScabbardKnife(teammate);
		SaveDefaultEquipmentSnapshot(sessionId, teammate, overwrite: true, flag);
		logger.Info($"Persisted escaped Default equipment state for teammate '{teammate.Aid}' in loadout management mode '{text}'.", (Exception)null);
	}

	private void ApplyRestrictedGearMaintenanceDeathEquipmentState(MongoId sessionId, BotBase teammate, FriendlyTeammateDeathEscapeEntry entry, Dictionary<string, HashSet<string>> protectedTeammateItemIdsByAid)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		//IL_0178: Expected O, but got Unknown
		if (entry.Escaped)
		{
			return;
		}
		FriendlyServerSettingsRequest friendlyServerSettingsRequest = settingsService.LoadSettings();
		string text = NormalizeLoadoutManagementMode(friendlyServerSettingsRequest.LoadoutManagementMode);
		if (!ShouldPersistRestrictedGearMaintenanceDeathEquipmentState(text, friendlyServerSettingsRequest))
		{
			return;
		}
		FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, teammate);
		if (!string.Equals(teammateSettings.SelectedLoadoutId, "000000000000000000000000", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		List<Item> list = entry.EquipmentItems?.Where((Item item) => item != (Item)null).ToList();
		if (list == null || list.Count == 0)
		{
			logger.Warning($"Skipped fallen Default equipment maintenance persistence for teammate '{teammate.Aid}' because no death-time equipment snapshot was provided.", (Exception)null);
			return;
		}
		List<Item> list2 = cloner.Clone<List<Item>>(list) ?? list;
		RemoveItemTreesById(list2, entry.TrackedItemIds);
		RemoveItemTreesById(list2, GetOtherProtectedTeammateItemIds(protectedTeammateItemIdsByAid, entry.Aid));
		if (list2.Count == 0)
		{
			logger.Warning($"Skipped fallen Default equipment maintenance persistence for teammate '{teammate.Aid}' because the filtered death-time equipment snapshot was empty.", (Exception)null);
			return;
		}
		if (teammate.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			BotBaseInventory val2 = val;
			teammate.Inventory = val;
		}
		teammate.Inventory.Items = MergeEquipmentWithPreservedSpecialItems(teammate.Inventory.Items, list2);
		teammate.Inventory.Equipment = teammate.Inventory.Items.First().Id;
		RemoveSecureContainerTree(teammate);
		EnsureFollowerHasScabbardKnife(teammate);
		SaveDefaultEquipmentSnapshot(sessionId, teammate, overwrite: true);
		logger.Info($"Persisted fallen Default equipment maintenance state for teammate '{teammate.Aid}' in loadout management mode '{text}'.", (Exception)null);
	}

	private void ApplyImmersiveDefaultGearLoss(MongoId sessionId, BotBase teammate, FriendlyTeammateDeathEscapeEntry entry)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (entry.Escaped)
		{
			return;
		}
		string text = NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode);
		if (IsImmersiveLikeLoadoutManagementMode(text))
		{
			FriendlyTeammateSettings teammateSettings = GetTeammateSettings(sessionId, teammate);
			if (string.Equals(teammateSettings.SelectedLoadoutId, "000000000000000000000000", StringComparison.OrdinalIgnoreCase))
			{
				bool flag = IsExtremeLoadoutManagementMode(text);
				StripDefaultEquipmentAfterDeath(teammate, flag);
				SaveDefaultEquipmentSnapshot(sessionId, teammate, overwrite: true, flag);
				logger.Info($"Stripped teammate '{teammate.Aid}' default equipment after death in loadout management mode '{text}'.", (Exception)null);
			}
		}
	}

	private void StripDefaultEquipmentAfterDeath(BotBase teammate, bool keepSecureContainer)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_002f: Expected O, but got Unknown
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		BotBaseInventory val2;
		if (teammate.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			teammate.Inventory = val;
		}
		val2 = teammate.Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		if (teammate.Inventory.Items.Count == 0)
		{
			return;
		}
		EnsureFollowerHasPockets(teammate);
		EnsureFollowerHasScabbardKnife(teammate);
		string equipmentRootId = GetEquipmentRootId(teammate);
		HashSet<string> keepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { equipmentRootId };
		foreach (Item item in teammate.Inventory.Items.Where((Item item) => !string.IsNullOrWhiteSpace((item != null) ? item.SlotId : null) && IsPermanentTeammateEquipmentSlot(item.SlotId, keepSecureContainer)).ToList())
		{
			if (IsPocketsSlotItem(item))
			{
				keepIds.Add(item.Id.ToString());
			}
			else
			{
				AddItemAndDescendantsToKeepSet(teammate.Inventory.Items, item.Id.ToString(), keepIds);
			}
		}
		teammate.Inventory.Items = teammate.Inventory.Items.Where((Item item) => keepIds.Contains(item.Id.ToString())).ToList();
		teammate.Inventory.Equipment = new MongoId(equipmentRootId);
	}

	private static void AddItemAndDescendantsToKeepSet(List<Item> inventoryItems, string itemId, HashSet<string> keepIds)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (!keepIds.Add(itemId))
		{
			return;
		}
		foreach (Item item in inventoryItems.Where((Item item) => string.Equals(item.ParentId, itemId, StringComparison.OrdinalIgnoreCase)).ToList())
		{
			AddItemAndDescendantsToKeepSet(inventoryItems, item.Id.ToString(), keepIds);
		}
	}

	private void EnsureFollowerHasScabbardKnife(BotBase profile)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_0022: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_00f7: Expected O, but got Unknown
		BotBaseInventory val2;
		if (profile.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			profile.Inventory = val;
		}
		val2 = profile.Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		if (profile.Inventory.Items.Count != 0 && !profile.Inventory.Items.Any((Item item) => string.Equals(item.SlotId, "Scabbard", StringComparison.OrdinalIgnoreCase)))
		{
			string equipmentRootId = GetEquipmentRootId(profile);
			profile.Inventory.Items.Add(new Item
			{
				Id = new MongoId(),
				Template = new MongoId("54491bb74bdc2d09088b4567"),
				ParentId = equipmentRootId,
				SlotId = "Scabbard",
				Location = null,
				Upd = new Upd
				{
					StackObjectsCount = 1.0,
					SpawnedInSession = false
				}
			});
			logger.Info($"Injected default scabbard knife for teammate '{profile.Aid}'.", (Exception)null);
		}
	}

	private void EnsureFollowerHasPockets(BotBase profile)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_0023: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_00f3: Expected O, but got Unknown
		BotBaseInventory val2;
		if (profile.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			profile.Inventory = val;
		}
		val2 = profile.Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		if (profile.Inventory.Items.Count == 0)
		{
			return;
		}
		string equipmentRootId = GetEquipmentRootId(profile);
		Item val3 = ((IEnumerable<Item>)profile.Inventory.Items).FirstOrDefault((Func<Item, bool>)IsPocketsSlotItem);
		if (val3 == (Item)null)
		{
			val3 = new Item
			{
				Id = new MongoId(),
				Template = new MongoId("627a4e6b255f7527fb05a0f6"),
				ParentId = equipmentRootId,
				SlotId = "Pockets",
				Location = null,
				Upd = new Upd
				{
					StackObjectsCount = 1.0,
					SpawnedInSession = false
				}
			};
			profile.Inventory.Items.Add(val3);
			logger.Info($"Injected missing pockets container for teammate '{profile.Aid}'.", (Exception)null);
		}
		string parentId = val3.Id.ToString();
		foreach (Item item in profile.Inventory.Items.Where((Item item) => item != null && item.SlotId != null && item.SlotId.Contains("SpecialSlot", StringComparison.OrdinalIgnoreCase)))
		{
			item.ParentId = parentId;
		}
	}

	private void EnsureManagedSecureContainer(BotBase profile)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_0022: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_00f7: Expected O, but got Unknown
		BotBaseInventory val2;
		if (profile.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			profile.Inventory = val;
		}
		val2 = profile.Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		if (profile.Inventory.Items.Count != 0 && !profile.Inventory.Items.Any((Item item) => string.Equals((item != null) ? item.SlotId : null, "SecuredContainer", StringComparison.OrdinalIgnoreCase)))
		{
			profile.Inventory.Items.Add(new Item
			{
				Id = new MongoId(),
				Template = new MongoId("5c0a794586f77461c458f892"),
				ParentId = GetEquipmentRootId(profile),
				SlotId = "SecuredContainer",
				Location = null,
				Upd = new Upd
				{
					StackObjectsCount = 1.0,
					SpawnedInSession = false
				}
			});
			logger.Debug($"Injected managed secure container for teammate '{profile.Aid}' in non-Realistic loadout mode.", (Exception)null);
		}
	}

	private static string GetEquipmentRootId(BotBase profile)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		BotBaseInventory inventory = profile.Inventory;
		object obj;
		if (inventory == null)
		{
			obj = null;
		}
		else
		{
			MongoId? equipment = inventory.Equipment;
			obj = (equipment.HasValue ? equipment.GetValueOrDefault().ToString() : null);
		}
		string text = (string)obj;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		BotBaseInventory inventory2 = profile.Inventory;
		Item val = ((inventory2 == null) ? null : inventory2.Items?.FirstOrDefault()) ?? throw new FriendlyTeammateException("Teammate inventory is missing equipment root item");
		profile.Inventory.Equipment = val.Id;
		return val.Id.ToString();
	}

	private static string GetPlayerStashRootId(PmcData profile)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		BotBaseInventory inventory = ((BotBase)profile).Inventory;
		object obj;
		if (inventory == null)
		{
			obj = null;
		}
		else
		{
			MongoId? stash = inventory.Stash;
			obj = (stash.HasValue ? stash.GetValueOrDefault().ToString() : null);
		}
		string text = (string)obj;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		BotBaseInventory inventory2 = ((BotBase)profile).Inventory;
		Item val = ((inventory2 == null) ? null : inventory2.Items?.FirstOrDefault((Item item) => string.Equals(item.SlotId, "hideout", StringComparison.OrdinalIgnoreCase) || string.Equals(item.SlotId, "main", StringComparison.OrdinalIgnoreCase))) ?? throw new FriendlyTeammateException("Player inventory is missing stash root item");
		((BotBase)profile).Inventory.Stash = val.Id;
		return val.Id.ToString();
	}

	private List<Item> GetPlayerStashItems(PmcData profile)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0028: Expected O, but got Unknown
		BotBaseInventory val2;
		if (((BotBase)profile).Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			((BotBase)profile).Inventory = val;
		}
		val2 = ((BotBase)profile).Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		string playerStashRootId = GetPlayerStashRootId(profile);
		HashSet<string> stashIds = GetItemTreeIds(((BotBase)profile).Inventory.Items, playerStashRootId);
		return cloner.Clone<List<Item>>(((BotBase)profile).Inventory.Items.Where((Item item) => stashIds.Contains(item.Id.ToString())).ToList()) ?? ((BotBase)profile).Inventory.Items.Where((Item item) => stashIds.Contains(item.Id.ToString())).ToList();
	}

	private (List<Item> NewItems, List<Item> ChangedItems, List<string> DeletedItemIds) BuildPlayerStashDelta(PmcData profile, List<Item> originalPlayerItems)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_002c: Expected O, but got Unknown
		BotBaseInventory val2;
		if (((BotBase)profile).Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			((BotBase)profile).Inventory = val;
		}
		val2 = ((BotBase)profile).Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		if (originalPlayerItems == null)
		{
			originalPlayerItems = new List<Item>();
		}
		string playerStashRootId = GetPlayerStashRootId(profile);
		HashSet<string> originalStashIds = GetItemTreeIds(originalPlayerItems, playerStashRootId);
		HashSet<string> currentStashIds = GetItemTreeIds(((BotBase)profile).Inventory.Items, playerStashRootId);
		Dictionary<string, Item> dictionary = ToItemDictionary(originalPlayerItems.Where(delegate(Item item)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				return originalStashIds.Contains(item.Id.ToString());
			}
			return false;
		}));
		Dictionary<string, Item> dictionary2 = ToItemDictionary(((BotBase)profile).Inventory.Items.Where(delegate(Item item)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				return currentStashIds.Contains(item.Id.ToString());
			}
			return false;
		}));
		List<Item> list3 = new List<Item>();
		List<Item> list4 = new List<Item>();
		List<string> list5 = new List<string>();
		foreach (string item in originalStashIds)
		{
			if (!currentStashIds.Contains(item))
			{
				list5.Add(item);
			}
		}
		foreach (string item2 in currentStashIds)
		{
			if (dictionary2.TryGetValue(item2, out var value))
			{
				if (!dictionary.TryGetValue(item2, out var value2))
				{
					list3.Add(value);
				}
				else if (!ItemPlacementEquals(value2, value))
				{
					list5.Add(item2);
					list3.Add(value);
				}
				else if (!JsonValueEquals(value2.Upd, value.Upd))
				{
					list4.Add(value);
				}
			}
		}
		return (NewItems: cloner.Clone<List<Item>>(list3) ?? list3, ChangedItems: cloner.Clone<List<Item>>(list4) ?? list4, DeletedItemIds: list5.Distinct<string>(StringComparer.OrdinalIgnoreCase).ToList());
	}

	private static bool ItemPlacementEquals(Item left, Item right)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (left != (Item)null && right != (Item)null && string.Equals(left.Template.ToString(), right.Template.ToString(), StringComparison.OrdinalIgnoreCase) && string.Equals(left.ParentId, right.ParentId, StringComparison.OrdinalIgnoreCase) && string.Equals(left.SlotId, right.SlotId, StringComparison.Ordinal))
		{
			return JsonValueEquals(left.Location, right.Location);
		}
		return false;
	}

	private List<Item> BuildCurrentTeammateKitDeliveryItems(BotBase teammate, bool includeSecureContainer)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		BotBaseInventory inventory = teammate.Inventory;
		List<Item> list = ((inventory != null) ? inventory.Items : null);
		if (list == null || list.Count == 0)
		{
			return new List<Item>();
		}
		string equipmentRootId = GetEquipmentRootId(teammate);
		List<Item> list2 = new List<Item>();
		foreach (Item slotItem in list.Where((Item item) => item != null && item.ParentId != null && string.Equals(item.ParentId, equipmentRootId, StringComparison.OrdinalIgnoreCase)).ToList())
		{
			Item obj = slotItem;
			if (obj == null)
			{
				continue;
			}
			_ = obj.Id;
			if (false || string.IsNullOrWhiteSpace(slotItem.SlotId) || IsIgnoredReturnedEquipmentSlot(slotItem.SlotId, includeSecureContainer))
			{
				continue;
			}
			if (IsPocketsSlotItem(slotItem))
			{
				foreach (Item item in list.Where((Item item) => item != null && item.ParentId != null && string.Equals(item.ParentId, slotItem.Id.ToString(), StringComparison.OrdinalIgnoreCase)).ToList())
				{
					AddDeliveryItemTree(list, item, list2);
				}
			}
			else
			{
				AddDeliveryItemTree(list, slotItem, list2);
			}
		}
		return list2;
	}

	private void AddDeliveryItemTree(List<Item> sourceItems, Item rootItem, List<Item> deliveryItems)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (rootItem == null)
		{
			return;
		}
		_ = rootItem.Id;
		if (0 == 0 && !IsIgnoredKitRequirementItem(rootItem))
		{
			HashSet<string> treeIds = GetItemTreeIds(sourceItems, rootItem.Id.ToString());
			List<Item> list = cloner.Clone<List<Item>>(sourceItems.Where((Item item) => treeIds.Contains(item.Id.ToString())).ToList()) ?? sourceItems.Where((Item item) => treeIds.Contains(item.Id.ToString())).ToList();
			if (list.Count != 0)
			{
				list[0].ParentId = null;
				list[0].SlotId = null;
				list[0].Location = null;
				deliveryItems.AddRange(list);
			}
		}
	}

	private void SendPreviousTeammateKitDelivery(MongoId sessionId, BotBase teammate, List<Item> deliveryItems)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		if (deliveryItems.Count != 0)
		{
			mailSendService.SendMessageToPlayer(new SendMessageDetails
			{
				RecipientId = sessionId,
				Sender = (MessageType)2,
				DialogType = (MessageType)2,
				Trader = "67d3a28a3d6f4f7dbd09ed13",
				MessageText = "Teammate's previous kit is ready for pickup.",
				Items = deliveryItems,
				ItemsMaxStorageLifetimeSeconds = 86400L
			});
			logger.Info($"Sent {deliveryItems.Count} previous teammate kit items by delivery for '{teammate.Aid}'.", (Exception)null);
		}
	}

	private static bool IsIgnoredReturnedEquipmentSlot(string slotId, bool includeSecureContainer)
	{
		if (!slotId.Contains("Dogtag", StringComparison.OrdinalIgnoreCase))
		{
			if (!includeSecureContainer)
			{
				return slotId.Contains("SecuredContainer", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
		return true;
	}

	private static bool IsPocketsSlotItem(Item item)
	{
		return string.Equals((item != null) ? item.SlotId : null, "Pockets", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPermanentTeammateEquipmentSlot(string? slotId, bool keepSecureContainer)
	{
		if (!string.IsNullOrWhiteSpace(slotId))
		{
			if (!slotId.Contains("Dogtag", StringComparison.OrdinalIgnoreCase) && !slotId.Contains("SpecialSlot", StringComparison.OrdinalIgnoreCase) && (!keepSecureContainer || !slotId.Contains("SecuredContainer", StringComparison.OrdinalIgnoreCase)) && !string.Equals(slotId, "Pockets", StringComparison.OrdinalIgnoreCase) && !string.Equals(slotId, "Scabbard", StringComparison.OrdinalIgnoreCase) && !string.Equals(slotId, "ArmBand", StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(slotId, "Armband", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	private void ConsumeStashItemsForKit(MongoId sessionId, PmcData profile, IEnumerable<FriendlyTeammateBuyKitUsedItem>? usedItems)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0026: Expected O, but got Unknown
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Expected O, but got Unknown
		//IL_0415: Expected O, but got Unknown
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		BotBaseInventory val2;
		if (((BotBase)profile).Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			((BotBase)profile).Inventory = val;
		}
		val2 = ((BotBase)profile).Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		List<FriendlyTeammateBuyKitUsedItem> list3 = usedItems?.ToList() ?? new List<FriendlyTeammateBuyKitUsedItem>();
		if (list3.Count == 0)
		{
			return;
		}
		string playerStashRootId = GetPlayerStashRootId(profile);
		HashSet<string> itemTreeIds = GetItemTreeIds(((BotBase)profile).Inventory.Items, playerStashRootId);
		Dictionary<string, Item> dictionary = ToItemDictionary(((BotBase)profile).Inventory.Items);
		Dictionary<string, (Item, int)> dictionary2 = new Dictionary<string, (Item, int)>(StringComparer.OrdinalIgnoreCase);
		foreach (FriendlyTeammateBuyKitUsedItem item2 in list3)
		{
			if (item2 == null || string.IsNullOrWhiteSpace(item2.ItemId) || string.IsNullOrWhiteSpace(item2.TemplateId) || item2.Count <= 0)
			{
				throw CreateKitStashSelectionException(sessionId, "Rejected an invalid exact player stash item selection for teammate kit purchase.");
			}
			if (!dictionary2.TryAdd(item2.ItemId, default((Item, int))))
			{
				throw CreateKitStashSelectionException(sessionId, "Player stash item '" + item2.ItemId + "' was selected more than once for teammate kit purchase.");
			}
			if (dictionary.TryGetValue(item2.ItemId, out var value) && value != null)
			{
				_ = value.Id;
				if (0 == 0 && itemTreeIds.Contains(item2.ItemId) && !IsIgnoredKitRequirementItem(value) && !IsLockedForStashUse(value, dictionary) && string.Equals(value.Template.ToString(), item2.TemplateId, StringComparison.OrdinalIgnoreCase))
				{
					int itemStackCount = GetItemStackCount(value);
					if (item2.Count > itemStackCount)
					{
						throw CreateKitStashSelectionException(sessionId, "Player stash item '" + item2.ItemId + "' did not contain the requested quantity for teammate kit purchase.");
					}
					dictionary2[item2.ItemId] = (value, item2.Count);
					continue;
				}
			}
			throw CreateKitStashSelectionException(sessionId, "Player stash item '" + item2.ItemId + "' was unavailable, locked, outside the stash, or changed template before teammate kit purchase.");
		}
		HashSet<string> hashSet = (from entry in dictionary2
			where entry.Value.Item2 == GetItemStackCount(entry.Value.Item1)
			select entry.Key).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item3 in hashSet)
		{
			foreach (string itemTreeId in GetItemTreeIds(((BotBase)profile).Inventory.Items, item3))
			{
				if (!string.Equals(itemTreeId, item3, StringComparison.OrdinalIgnoreCase) && dictionary.TryGetValue(itemTreeId, out var value2) && !IsImplicitKitTreeItem(value2) && (!dictionary2.TryGetValue(itemTreeId, out var value3) || value3.Item2 != GetItemStackCount(value2)))
				{
					throw CreateKitStashContainerContentsException(sessionId, $"Player stash item '{item3}' contains descendant '{itemTreeId}' that was not selected in full.");
				}
			}
		}
		foreach (KeyValuePair<string, (Item, int)> item4 in dictionary2)
		{
			int itemStackCount2 = GetItemStackCount(item4.Value.Item1);
			if (item4.Value.Item2 < itemStackCount2)
			{
				if (GetItemTreeIds(((BotBase)profile).Inventory.Items, item4.Key).Count > 1)
				{
					throw CreateKitStashContainerContentsException(sessionId, "Player stash item '" + item4.Key + "' was selected partially while it still contained descendants.");
				}
				Item item = item4.Value.Item1;
				if (item.Upd == null)
				{
					Upd val3 = new Upd();
					Upd val4 = val3;
					item.Upd = val3;
				}
				item4.Value.Item1.Upd.StackObjectsCount = itemStackCount2 - item4.Value.Item2;
			}
		}
		if (hashSet.Count > 0)
		{
			RemoveItemTreesById(((BotBase)profile).Inventory.Items, hashSet);
		}
	}

	private void DeductRoublesFromPlayerStash(PmcData profile, int amount)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_002c: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Expected O, but got Unknown
		//IL_01d6: Expected O, but got Unknown
		BotBaseInventory val2;
		if (((BotBase)profile).Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val2 = val;
			((BotBase)profile).Inventory = val;
		}
		val2 = ((BotBase)profile).Inventory;
		if (val2.Items == null)
		{
			List<Item> list = (val2.Items = new List<Item>());
		}
		if (amount <= 0)
		{
			return;
		}
		string playerStashRootId = GetPlayerStashRootId(profile);
		HashSet<string> stashIds = GetItemTreeIds(((BotBase)profile).Inventory.Items, playerStashRootId);
		Dictionary<string, Item> inventoryById = ToItemDictionary(((BotBase)profile).Inventory.Items);
		int num = ((BotBase)profile).Inventory.Items.Where(delegate(Item item)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				if (stashIds.Contains(item.Id.ToString()) && !IsLockedForStashUse(item, inventoryById))
				{
					return string.Equals(item.Template.ToString(), "5449016a4bdc2d6f028b456f", StringComparison.OrdinalIgnoreCase);
				}
			}
			return false;
		}).Sum((Func<Item, int>)GetItemStackCount);
		if (num < amount)
		{
			throw new FriendlyTeammateException($"Not enough roubles to buy teammate kit. Required {amount}, available {num}");
		}
		int num2 = amount;
		List<string> list3 = new List<string>();
		foreach (Item item in ((BotBase)profile).Inventory.Items.ToList())
		{
			if (num2 <= 0)
			{
				break;
			}
			if (item == null)
			{
				continue;
			}
			_ = item.Id;
			if (false || !stashIds.Contains(item.Id.ToString()) || IsLockedForStashUse(item, inventoryById) || !string.Equals(item.Template.ToString(), "5449016a4bdc2d6f028b456f", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int itemStackCount = GetItemStackCount(item);
			if (itemStackCount > num2)
			{
				Item val3 = item;
				if (val3.Upd == null)
				{
					Upd val4 = new Upd();
					Upd val5 = val4;
					val3.Upd = val4;
				}
				item.Upd.StackObjectsCount = itemStackCount - num2;
				num2 = 0;
				break;
			}
			list3.Add(item.Id.ToString());
			num2 -= itemStackCount;
		}
		RemoveItemTreesById(((BotBase)profile).Inventory.Items, list3);
	}

	private static bool IsIgnoredKitRequirementItem(Item? item)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (item != null)
		{
			_ = item.Id;
			if (0 == 0 && !string.IsNullOrWhiteSpace(item.Template.ToString()))
			{
				if (string.IsNullOrWhiteSpace(item.ParentId))
				{
					return true;
				}
				if (string.Equals(item.SlotId, "Dogtag", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				return IsPocketsSlotItem(item);
			}
		}
		return true;
	}

	private static int GetItemStackCount(Item item)
	{
		Upd upd = item.Upd;
		return Math.Max(1, (int)Math.Ceiling(((upd != null) ? upd.StackObjectsCount : ((double?)null)) ?? 1.0));
	}

	private bool IsImplicitKitTreeItem(Item item)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (!IsIgnoredKitRequirementItem(item))
		{
			if (item != null)
			{
				MongoId template = item.Template;
				if (!template.IsEmpty)
				{
					return itemHelper.IsOfBaseclass(item.Template, BaseClasses.BUILT_IN_INSERTS);
				}
			}
			return false;
		}
		return true;
	}

	private FriendlyTeammateException CreateKitStashSelectionException(MongoId sessionId, string diagnostic)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		logger.Warning(diagnostic, (Exception)null);
		Dictionary<string, string> stringMap = languageService.GetStringMap(sessionId, "socialUi");
		string languageValue = GetLanguageValue(stringMap, "KitLoadoutPurchaseFailed", "KitStashSelectionChanged");
		return new FriendlyTeammateException(GetLanguageValue(stringMap, "KitStashSelectionChanged", languageValue));
	}

	private FriendlyTeammateException CreateKitStashContainerContentsException(MongoId sessionId, string diagnostic)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		logger.Warning(diagnostic, (Exception)null);
		Dictionary<string, string> stringMap = languageService.GetStringMap(sessionId, "socialUi");
		string languageValue = GetLanguageValue(stringMap, "KitStashSelectionChanged", "KitLoadoutPurchaseFailed");
		return new FriendlyTeammateException(GetLanguageValue(stringMap, "KitStashContainerHasUnselectedContents", languageValue));
	}

	private FriendlyTeammateException CreateKitPurchaseException(MongoId sessionId, string diagnostic)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		logger.Warning(diagnostic, (Exception)null);
		Dictionary<string, string> stringMap = languageService.GetStringMap(sessionId, "socialUi");
		return new FriendlyTeammateException(GetLanguageValue(stringMap, "KitLoadoutPurchaseFailed", "KitLoadoutPurchaseFailed"));
	}

	private unsafe static ItemEventRouterResponse CreateEmptyRepairOutput(MongoId sessionId, PmcData profile)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_0146: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		ItemEventRouterResponse val = new ItemEventRouterResponse
		{
			Warnings = new List<Warning>()
		};
		Dictionary<MongoId, ProfileChange> dictionary = new Dictionary<MongoId, ProfileChange>();
		MongoId key = sessionId;
		ProfileChange val2 = new ProfileChange
		{
			Id = sessionId.ToString()
		};
		Info info = ((BotBase)profile).Info;
		val2.Experience = ((info != null) ? info.Experience : ((int?)null));
		val2.Quests = new List<Quest>();
		val2.RagFairOffers = new List<RagfairOffer>();
		val2.WeaponBuilds = new List<WeaponBuildChange>();
		val2.EquipmentBuilds = new List<EquipmentBuildChange>();
		val2.Items = new ItemChanges
		{
			NewItems = new List<Item>(),
			ChangedItems = new List<Item>(),
			DeletedItems = new List<DeletedItem>()
		};
		val2.Production = new Dictionary<MongoId, Production>();
		val2.Improvements = new Dictionary<MongoId, HideoutImprovement>();
		val2.Skills = new Skills
		{
			Common = Array.Empty<CommonSkill>(),
			Mastering = Array.Empty<MasterySkill>(),
			Points = 0.0
		};
		val2.Health = (BotBaseHealth)(((object)((BotBase)profile).Health) ?? ((object)new BotBaseHealth()));
		val2.TraderRelations = new Dictionary<MongoId, TraderData>();
		val2.QuestsStatus = new List<QuestStatus>();
		dictionary.Add(key, val2);
		((ItemEventRouterBase)val).ProfileChanges = dictionary;
		return val;
	}

	private static HashSet<string> GetItemTreeIds(List<Item> items, string rootId)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddItemTreeIds(items, rootId, hashSet);
		return hashSet;
	}

	private static void AddItemTreeIds(List<Item> items, string itemId, HashSet<string> treeIds)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (!treeIds.Add(itemId))
		{
			return;
		}
		foreach (Item item in items.Where((Item item) => string.Equals(item.ParentId, itemId, StringComparison.OrdinalIgnoreCase)).ToList())
		{
			AddItemTreeIds(items, item.Id.ToString(), treeIds);
		}
	}

	private static Dictionary<string, Item> ToItemDictionary(IEnumerable<Item>? items)
	{
		return (items ?? Array.Empty<Item>()).Where(delegate(Item item)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (item == null)
			{
				return false;
			}
			_ = item.Id;
			return true;
		}).GroupBy<Item, string>((Item item) => item.Id.ToString(), StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, Item>, string, Item>((IGrouping<string, Item> group) => group.Key, (IGrouping<string, Item> group) => group.First(), StringComparer.OrdinalIgnoreCase);
	}

	private static bool IsLockedForStashUse(Item item, IReadOnlyDictionary<string, Item> inventoryById)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		if (item == (Item)null)
		{
			return false;
		}
		Upd upd = item.Upd;
		if (upd != null && (int)upd.PinLockState.GetValueOrDefault() == 2)
		{
			return true;
		}
		string parentId = item.ParentId;
		while (!string.IsNullOrWhiteSpace(parentId))
		{
			if (!inventoryById.TryGetValue(parentId, out Item value))
			{
				return false;
			}
			Upd upd2 = value.Upd;
			if (upd2 != null && (int)upd2.PinLockState.GetValueOrDefault() == 2)
			{
				return true;
			}
			parentId = value.ParentId;
		}
		return false;
	}

	private static void ValidateLockedPlayerStashItemsUnchanged(List<Item> currentPlayerItems, List<Item> replacementEquipmentItems, List<Item> replacementStashItems, string playerStashRootId, string messageTemplate)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Invalid comparison between Unknown and I4
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, Item> dictionary = ToItemDictionary(currentPlayerItems);
		Dictionary<string, Item> dictionary2 = ToItemDictionary(replacementStashItems);
		HashSet<string> hashSet = (from item in replacementEquipmentItems.Where(delegate(Item item)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				if (item == null)
				{
					return false;
				}
				_ = item.Id;
				return true;
			})
			select item.Id.ToString()).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> itemTreeIds = GetItemTreeIds(currentPlayerItems, playerStashRootId);
		Dictionary<string, string> dictionary3 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (Item currentPlayerItem in currentPlayerItems)
		{
			if (currentPlayerItem == null)
			{
				continue;
			}
			_ = currentPlayerItem.Id;
			if (false)
			{
				continue;
			}
			Upd upd = currentPlayerItem.Upd;
			if (upd == null || (int)upd.PinLockState.GetValueOrDefault() != 2 || !itemTreeIds.Contains(currentPlayerItem.Id.ToString()))
			{
				continue;
			}
			string text = currentPlayerItem.Id.ToString();
			foreach (string itemTreeId in GetItemTreeIds(currentPlayerItems, text))
			{
				dictionary3.TryAdd(itemTreeId, text);
			}
		}
		foreach (KeyValuePair<string, string> item in dictionary3)
		{
			string key = item.Key;
			if (dictionary.TryGetValue(key, out var value) && itemTreeIds.Contains(key))
			{
				dictionary.TryGetValue(item.Value, out var value2);
				if (hashSet.Contains(key))
				{
					throw new FriendlyTeammateException(FormatLockedStashItemMessage(messageTemplate, value2, value));
				}
				if (!dictionary2.TryGetValue(key, out var value3))
				{
					throw new FriendlyTeammateException(FormatLockedStashItemMessage(messageTemplate, value2, value));
				}
				if (!LockedStashItemStateEquals(value, value3))
				{
					throw new FriendlyTeammateException(FormatLockedStashItemMessage(messageTemplate, value2, value));
				}
				PreservePinLockState(value, value3);
			}
		}
	}

	private static string FormatLockedStashItemMessage(string messageTemplate, Item? lockedRootItem, Item blockedItem)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		string text = ((lockedRootItem != (Item)null) ? lockedRootItem.Id.ToString() : blockedItem.Id.ToString());
		string text2 = ((lockedRootItem != (Item)null) ? lockedRootItem.Template.ToString() : blockedItem.Template.ToString());
		string text3 = blockedItem.Id.ToString();
		string text4 = blockedItem.Template.ToString();
		try
		{
			return string.Format(messageTemplate, text, text2, text3, text4);
		}
		catch
		{
			return string.Format("Teammate loadout save blocked: an item inside a locked stash container was moved or changed. Unlock the container and try again. Locked container: id={0}, tpl={1}. Blocked item: id={2}, tpl={3}.", text, text2, text3, text4);
		}
	}

	private static bool LockedStashItemStateEquals(Item currentItem, Item replacementItem)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (currentItem != (Item)null && replacementItem != (Item)null && string.Equals(currentItem.Template.ToString(), replacementItem.Template.ToString(), StringComparison.OrdinalIgnoreCase) && string.Equals(currentItem.ParentId, replacementItem.ParentId, StringComparison.OrdinalIgnoreCase) && string.Equals(currentItem.SlotId, replacementItem.SlotId, StringComparison.OrdinalIgnoreCase) && JsonValueEquals(currentItem.Location, replacementItem.Location))
		{
			return GetItemStackCount(currentItem) == GetItemStackCount(replacementItem);
		}
		return false;
	}

	private static void PreservePinLockState(Item currentItem, Item replacementItem)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_0046: Expected O, but got Unknown
		if (currentItem == null)
		{
			return;
		}
		Upd upd = currentItem.Upd;
		if (upd != null && upd.PinLockState.HasValue && !(replacementItem == (Item)null))
		{
			if (replacementItem.Upd == null)
			{
				Upd val = new Upd();
				Upd val2 = val;
				replacementItem.Upd = val;
			}
			replacementItem.Upd.PinLockState = currentItem.Upd.PinLockState;
		}
	}

	private static bool JsonValueEquals(object? left, object? right)
	{
		if (left == null || right == null)
		{
			return left == right;
		}
		return string.Equals(JsonSerializer.Serialize(left), JsonSerializer.Serialize(right), StringComparison.Ordinal);
	}

	private static List<Item> PruneSubmittedEquipmentToRootTree(List<Item> items, out int prunedCount)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		prunedCount = 0;
		if (items == null || items.Count == 0)
		{
			return new List<Item>();
		}
		string item = items[0].Id.ToString();
		HashSet<string> keepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { item };
		bool flag;
		do
		{
			flag = false;
			foreach (Item item2 in items)
			{
				if (item2 != null)
				{
					_ = item2.Id;
					if (0 == 0 && !string.IsNullOrWhiteSpace(item2.ParentId) && keepIds.Contains(item2.ParentId) && keepIds.Add(item2.Id.ToString()))
					{
						flag = true;
					}
				}
			}
		}
		while (flag);
		List<Item> list = items.Where(delegate(Item val)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			if (val != null)
			{
				_ = val.Id;
				return keepIds.Contains(val.Id.ToString());
			}
			return false;
		}).ToList();
		prunedCount = items.Count - list.Count;
		return list;
	}

	private void ValidateRealCommitItemSet(List<Item> items, HashSet<string> allowedItemIds, string setName, bool allowGeneratedSlotDescendants = false)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, Item> submittedById = items.Where(delegate(Item item)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (item == null)
			{
				return false;
			}
			_ = item.Id;
			return true;
		}).GroupBy<Item, string>((Item item) => item.Id.ToString(), StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, Item>, string, Item>((IGrouping<string, Item> group) => group.Key, (IGrouping<string, Item> group) => group.First(), StringComparer.OrdinalIgnoreCase);
		foreach (Item item in items)
		{
			string text = item.Id.ToString();
			bool flag = allowedItemIds.Contains(text);
			if (!flag && allowGeneratedSlotDescendants && IsGeneratedSlotDescendantOfAllowedItem(item, submittedById, allowedItemIds))
			{
				flag = true;
			}
			if (!flag)
			{
				throw new FriendlyTeammateException($"Submitted {setName} contains an item that was not available for movement: id={text}, tpl={item.Template}, parent={item.ParentId}, slot={item.SlotId}");
			}
			if (!hashSet.Add(text))
			{
				throw new FriendlyTeammateException("Submitted " + setName + " contains duplicate item ids");
			}
		}
	}

	private bool IsGeneratedSlotDescendantOfAllowedItem(Item item, Dictionary<string, Item> submittedById, HashSet<string> allowedItemIds)
	{
		bool flag = IsLoadedAmmoSlotId((item != null) ? item.SlotId : null) && IsAmmoItem(item);
		if (item == (Item)null || (item.Location != null && !flag) || string.IsNullOrWhiteSpace(item.ParentId) || string.IsNullOrWhiteSpace(item.SlotId) || string.Equals(item.SlotId, "main", StringComparison.OrdinalIgnoreCase) || string.Equals(item.SlotId, "hideout", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string parentId = item.ParentId;
		while (!string.IsNullOrWhiteSpace(parentId))
		{
			if (allowedItemIds.Contains(parentId))
			{
				return true;
			}
			if (!submittedById.TryGetValue(parentId, out Item value))
			{
				return false;
			}
			parentId = value.ParentId;
		}
		return false;
	}

	private static bool IsLoadedAmmoSlotId(string? slotId)
	{
		if (string.IsNullOrWhiteSpace(slotId))
		{
			return false;
		}
		if (!LoadedAmmoSlotIds.Contains(slotId) && !slotId.StartsWith("patron_in_weapon", StringComparison.OrdinalIgnoreCase))
		{
			if (int.TryParse(slotId, out var result))
			{
				return result >= 0;
			}
			return false;
		}
		return true;
	}

	private bool IsAmmoItem(Item? item)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (item != null)
		{
			MongoId template = item.Template;
			if (!template.IsEmpty)
			{
				return itemHelper.IsOfBaseclass(item.Template, BaseClasses.AMMO);
			}
		}
		return false;
	}

	private static void ValidateNoRealCommitOverlap(List<Item> replacementEquipmentItems, List<Item> replacementStashItems, string playerStashRootId)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		HashSet<string> hashSet = replacementEquipmentItems.Select((Item item) => item.Id.ToString()).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Item replacementStashItem in replacementStashItems)
		{
			string text = replacementStashItem.Id.ToString();
			if (!string.Equals(text, playerStashRootId, StringComparison.OrdinalIgnoreCase) && hashSet.Contains(text))
			{
				throw new FriendlyTeammateException("Submitted teammate equipment and player stash both contain the same moved item");
			}
		}
	}

	private static void ValidateNoEquippedPlayerItemCommit(PmcData playerPmc, List<Item> replacementEquipmentItems, HashSet<string> currentPlayerStashIds)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		BotBaseInventory inventory = ((BotBase)playerPmc).Inventory;
		HashSet<string> hashSet = (from item in ((inventory != null) ? inventory.Items : null) ?? new List<Item>()
			select item.Id.ToString() into id
			where !currentPlayerStashIds.Contains(id)
			select id).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Item replacementEquipmentItem in replacementEquipmentItems)
		{
			if (hashSet.Contains(replacementEquipmentItem.Id.ToString()))
			{
				throw new FriendlyTeammateException($"Submitted teammate equipment contains an item equipped on the player: id={replacementEquipmentItem.Id}, tpl={replacementEquipmentItem.Template}, parent={replacementEquipmentItem.ParentId}, slot={replacementEquipmentItem.SlotId}");
			}
		}
	}

	private HashSet<string> RemapReplacementEquipmentPlayerEquippedIdCollisions(PmcData playerPmc, List<Item> replacementEquipmentItems, HashSet<string> currentPlayerStashIds, HashSet<string> currentTeammateItemIds)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		BotBaseInventory inventory = ((BotBase)playerPmc).Inventory;
		HashSet<string> hashSet2 = (from val in ((inventory != null) ? inventory.Items : null) ?? new List<Item>()
			select val.Id.ToString() into id
			where !currentPlayerStashIds.Contains(id)
			select id).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (Item replacementEquipmentItem in replacementEquipmentItems)
		{
			if (replacementEquipmentItem == null)
			{
				continue;
			}
			_ = replacementEquipmentItem.Id;
			if (0 == 0)
			{
				string text = replacementEquipmentItem.Id.ToString();
				if (hashSet2.Contains(text) && currentTeammateItemIds.Contains(text))
				{
					string item = (dictionary[text] = new MongoId().ToString());
					hashSet.Add(item);
				}
			}
		}
		if (dictionary.Count == 0)
		{
			return hashSet;
		}
		foreach (Item replacementEquipmentItem2 in replacementEquipmentItems)
		{
			if (replacementEquipmentItem2 != null)
			{
				_ = replacementEquipmentItem2.Id;
				if (dictionary.TryGetValue(replacementEquipmentItem2.Id.ToString(), out var value))
				{
					replacementEquipmentItem2.Id = new MongoId(value);
				}
			}
			if (!string.IsNullOrWhiteSpace((replacementEquipmentItem2 != null) ? replacementEquipmentItem2.ParentId : null) && dictionary.TryGetValue(replacementEquipmentItem2.ParentId, out var value2))
			{
				replacementEquipmentItem2.ParentId = value2;
			}
		}
		logger.Warning($"Remapped {dictionary.Count} teammate-owned item id collision(s) with currently equipped player gear during real loadout commit.", (Exception)null);
		return hashSet;
	}

	private static string NormalizeLoadoutManagementMode(string? mode)
	{
		if (!string.IsNullOrWhiteSpace(mode))
		{
			return mode.Trim();
		}
		return "Restricted";
	}

	private static bool IsExtremeLoadoutManagementMode(string mode)
	{
		return string.Equals(mode, "Extreme", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSimpleLoadoutManagementMode(string mode)
	{
		return string.Equals(mode, "Simple", StringComparison.OrdinalIgnoreCase);
	}

	private bool IsCurrentLoadoutManagementModeExtreme()
	{
		return IsExtremeLoadoutManagementMode(NormalizeLoadoutManagementMode(settingsService.LoadSettings().LoadoutManagementMode));
	}

	private static bool IsImmersiveLikeLoadoutManagementMode(string mode)
	{
		if (!string.Equals(mode, "Immersive", StringComparison.OrdinalIgnoreCase))
		{
			return IsExtremeLoadoutManagementMode(mode);
		}
		return true;
	}

	private static bool IsRestrictedLoadoutManagementMode(string mode)
	{
		return string.Equals(mode, "Restricted", StringComparison.OrdinalIgnoreCase);
	}

	private static bool ShouldPersistEscapedDefaultEquipmentState(string mode, FriendlyServerSettingsRequest settings)
	{
		if (!IsImmersiveLikeLoadoutManagementMode(mode))
		{
			if (IsRestrictedLoadoutManagementMode(mode))
			{
				return settings?.RestrictedGearMaintenance ?? false;
			}
			return false;
		}
		return true;
	}

	private static bool ShouldPersistRestrictedGearMaintenanceDeathEquipmentState(string mode, FriendlyServerSettingsRequest settings)
	{
		if (IsRestrictedLoadoutManagementMode(mode))
		{
			return settings?.RestrictedGearMaintenance ?? false;
		}
		return false;
	}

	private static bool IsRealTransferLoadoutManagementMode(string mode)
	{
		if (!IsRestrictedLoadoutManagementMode(mode))
		{
			return IsImmersiveLikeLoadoutManagementMode(mode);
		}
		return true;
	}

	private void EnsureFollowerHasSecureContainerSupplies(BotBase profile)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected O, but got Unknown
		//IL_019b: Expected O, but got Unknown
		object obj;
		if (profile == null)
		{
			obj = null;
		}
		else
		{
			BotBaseInventory inventory = profile.Inventory;
			obj = ((inventory != null) ? inventory.Items : null);
		}
		if (obj == null || !TryGetSecureContainerId(profile, out string secureContainerId))
		{
			return;
		}
		bool flag = HasTemplateInBackpack(profile.Inventory.Items, "590c657e86f77412b013051d");
		bool flag2 = HasAnyTemplateInBackpack(profile.Inventory.Items, SurgicalKitTemplateIds);
		ClearSecureContainerContents(profile.Inventory.Items, secureContainerId);
		if (!flag)
		{
			AddSecureContainerSupply(profile.Inventory.Items, secureContainerId, "590c657e86f77412b013051d");
		}
		if (!flag2)
		{
			AddSecureContainerSupply(profile.Inventory.Items, secureContainerId, "5d02797c86f774203f38e30a");
		}
		MongoId? val = FindMainWeaponAmmoTemplate(profile.Inventory.Items.ToList());
		if (!val.HasValue)
		{
			return;
		}
		MongoId value = val.Value;
		if (value.IsEmpty)
		{
			return;
		}
		KeyValuePair<bool, TemplateItem> item = itemHelper.GetItem(val.Value);
		if (!item.Key || item.Value == (TemplateItem)null)
		{
			return;
		}
		TemplateItemProperties properties = item.Value.Properties;
		int valueOrDefault = ((properties != null) ? properties.StackMaxSize : ((int?)null)).GetValueOrDefault();
		if (valueOrDefault > 0)
		{
			for (int i = 0; i < 10; i++)
			{
				profile.Inventory.Items.Add(new Item
				{
					Id = new MongoId(),
					Template = val.Value,
					ParentId = secureContainerId,
					SlotId = "main",
					Location = null,
					Upd = new Upd
					{
						StackObjectsCount = valueOrDefault,
						SpawnedInSession = false
					}
				});
			}
		}
	}

	private static void AddSecureContainerSupply(List<Item> inventoryItems, string secureContainerId, string templateId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		//IL_005f: Expected O, but got Unknown
		inventoryItems.Add(new Item
		{
			Id = new MongoId(),
			Template = new MongoId(templateId),
			ParentId = secureContainerId,
			SlotId = "main",
			Location = null,
			Upd = new Upd
			{
				StackObjectsCount = 1.0,
				SpawnedInSession = false
			}
		});
	}

	private void RefillFollowerMagazinesFromInventoryAmmo(BotBase profile)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Expected O, but got Unknown
		//IL_0147: Expected O, but got Unknown
		BotBaseInventory inventory = profile.Inventory;
		List<Item> list = ((inventory != null) ? inventory.Items : null);
		if (list == null || list.Count == 0)
		{
			return;
		}
		try
		{
			List<AvailableAmmoStack> availableLooseAmmoStacks = GetAvailableLooseAmmoStacks(list);
			if (availableLooseAmmoStacks.Count == 0)
			{
				return;
			}
			int num = 0;
			foreach (Item item2 in list.ToList())
			{
				if (item2 != null)
				{
					_ = item2.Id;
					if (0 == 0 && itemHelper.IsOfBaseclass(item2.Template, BaseClasses.MAGAZINE))
					{
						num += RefillMagazineFromAmmoStacks(list, item2, availableLooseAmmoStacks);
					}
				}
			}
			RemoveItemTreesById(list, from stack in availableLooseAmmoStacks
				where stack.Remaining <= 0
				select stack.Item.Id.ToString());
			foreach (AvailableAmmoStack item3 in availableLooseAmmoStacks.Where((AvailableAmmoStack stack) => stack.Remaining > 0))
			{
				Item item = item3.Item;
				if (item.Upd == null)
				{
					Upd val = new Upd();
					Upd val2 = val;
					item.Upd = val;
				}
				item3.Item.Upd.StackObjectsCount = item3.Remaining;
			}
			if (num > 0)
			{
				logger.Debug($"Refilled {num} follower magazine rounds from carried ammo for teammate '{profile.Aid}'.", (Exception)null);
			}
		}
		catch (Exception ex)
		{
			logger.Warning($"Skipped follower magazine spawn refill for teammate '{profile.Aid}': {ex.Message}", (Exception)null);
		}
	}

	private List<AvailableAmmoStack> GetAvailableLooseAmmoStacks(List<Item> inventoryItems)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		List<AvailableAmmoStack> list = new List<AvailableAmmoStack>();
		foreach (Item inventoryItem in inventoryItems)
		{
			if (inventoryItem == null)
			{
				continue;
			}
			_ = inventoryItem.Id;
			if (false)
			{
				continue;
			}
			MongoId template = inventoryItem.Template;
			if (!template.IsEmpty && !string.IsNullOrWhiteSpace(inventoryItem.SlotId) && !IsLoadedAmmoSlotId(inventoryItem.SlotId) && itemHelper.IsOfBaseclass(inventoryItem.Template, BaseClasses.AMMO))
			{
				int itemStackCount = GetItemStackCount(inventoryItem);
				if (itemStackCount > 0)
				{
					list.Add(new AvailableAmmoStack(inventoryItem, itemStackCount));
				}
			}
		}
		return list;
	}

	private int RefillMagazineFromAmmoStacks(List<Item> inventoryItems, Item magazine, List<AvailableAmmoStack> availableAmmo)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		KeyValuePair<bool, TemplateItem> item = itemHelper.GetItem(magazine.Template);
		TemplateItem val = (item.Key ? item.Value : null);
		object obj;
		if (val == null)
		{
			obj = null;
		}
		else
		{
			TemplateItemProperties properties = val.Properties;
			obj = ((properties == null) ? null : properties.Cartridges?.SelectMany(delegate(Slot slot)
			{
				SlotProperties properties3 = slot.Properties;
				return ((properties3 != null) ? properties3.Filters : null) ?? Array.Empty<SlotFilter>();
			}).SelectMany((SlotFilter filter) => filter.Filter ?? new HashSet<MongoId>()).ToHashSet());
		}
		HashSet<MongoId> allowedAmmoTemplates = (HashSet<MongoId>)obj;
		double? obj2;
		if (val == null)
		{
			obj2 = null;
		}
		else
		{
			TemplateItemProperties properties2 = val.Properties;
			if (properties2 == null)
			{
				obj2 = null;
			}
			else
			{
				IEnumerable<Slot> cartridges = properties2.Cartridges;
				if (cartridges == null)
				{
					obj2 = null;
				}
				else
				{
					Slot? obj3 = cartridges.FirstOrDefault();
					obj2 = ((obj3 != null) ? obj3.MaxCount : ((double?)null));
				}
			}
		}
		int? num = (int?)obj2;
		bool flag = allowedAmmoTemplates == null || allowedAmmoTemplates.Count == 0;
		bool flag2 = flag;
		bool flag3;
		if (!flag2)
		{
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				if (valueOrDefault > 0)
				{
					flag3 = false;
					goto IL_0159;
				}
			}
			flag3 = true;
			goto IL_0159;
		}
		goto IL_015d;
		IL_015d:
		if (flag2)
		{
			return 0;
		}
		string magazineId = magazine.Id.ToString();
		int num2 = inventoryItems.Where((Item val2) => ((val2 != null) ? val2.ParentId : null) == magazineId && string.Equals(val2.SlotId, "cartridges", StringComparison.OrdinalIgnoreCase)).Sum((Func<Item, int>)GetItemStackCount);
		int num3 = num.Value - num2;
		if (num3 <= 0)
		{
			return 0;
		}
		HashSet<MongoId> existingAmmoTemplates = (from val2 in inventoryItems
			where ((val2 != null) ? val2.ParentId : null) == magazineId && string.Equals(val2.SlotId, "cartridges", StringComparison.OrdinalIgnoreCase)
			select val2.Template).ToHashSet();
		int num4 = 0;
		while (num3 > 0)
		{
			AvailableAmmoStack availableAmmoStack = (from stack in availableAmmo
				where stack.Remaining > 0 && allowedAmmoTemplates.Contains(stack.Item.Template)
				orderby existingAmmoTemplates.Contains(stack.Item.Template) descending
				select stack).FirstOrDefault();
			if (availableAmmoStack == null)
			{
				break;
			}
			int roundsToAdd = Math.Min(num3, availableAmmoStack.Remaining);
			int num5 = AddCartridgesToMagazine(inventoryItems, magazine, availableAmmoStack.Item.Template, roundsToAdd);
			if (num5 <= 0)
			{
				break;
			}
			availableAmmoStack.Remaining -= num5;
			num4 += num5;
			num3 -= num5;
			existingAmmoTemplates.Add(availableAmmoStack.Item.Template);
		}
		NormalizeMagazineCartridgeLocations(inventoryItems, magazineId);
		return num4;
		IL_0159:
		flag2 = flag3;
		goto IL_015d;
	}

	private int AddCartridgesToMagazine(List<Item> inventoryItems, Item magazine, MongoId ammoTemplate, int roundsToAdd)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected O, but got Unknown
		//IL_0100: Expected O, but got Unknown
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		if (roundsToAdd <= 0)
		{
			return 0;
		}
		TemplateItem value = itemHelper.GetItem(ammoTemplate).Value;
		int? obj;
		if (value == null)
		{
			obj = null;
		}
		else
		{
			TemplateItemProperties properties = value.Properties;
			obj = ((properties != null) ? properties.StackMaxSize : ((int?)null));
		}
		int val = obj ?? 1;
		val = Math.Max(1, val);
		string magazineId = magazine.Id.ToString();
		int num = roundsToAdd;
		foreach (Item item in inventoryItems.Where((Item item) => ((item != null) ? item.ParentId : null) == magazineId && string.Equals(item.SlotId, "cartridges", StringComparison.OrdinalIgnoreCase) && item.Template == ammoTemplate).ToList())
		{
			int itemStackCount = GetItemStackCount(item);
			int num2 = val - itemStackCount;
			if (num2 > 0)
			{
				int num3 = Math.Min(num2, num);
				Item val2 = item;
				if (val2.Upd == null)
				{
					Upd val3 = new Upd();
					Upd val4 = val3;
					val2.Upd = val3;
				}
				item.Upd.StackObjectsCount = itemStackCount + num3;
				num -= num3;
				if (num <= 0)
				{
					return roundsToAdd;
				}
			}
		}
		while (num > 0)
		{
			int num4 = Math.Min(val, num);
			inventoryItems.Add(itemHelper.CreateCartridges(magazine.Id, ammoTemplate, num4, 0.0));
			num -= num4;
		}
		return roundsToAdd;
	}

	private static void NormalizeMagazineCartridgeLocations(List<Item> inventoryItems, string magazineId)
	{
		List<Item> list = inventoryItems.Where((Item item) => ((item != null) ? item.ParentId : null) == magazineId && string.Equals(item.SlotId, "cartridges", StringComparison.OrdinalIgnoreCase)).ToList();
		if (list.Count == 0)
		{
			return;
		}
		if (list.Count == 1)
		{
			list[0].Location = null;
			return;
		}
		for (int num = 0; num < list.Count; num++)
		{
			list[num].Location = num;
		}
	}

	private bool TryGetSecureContainerId(BotBase profile, out string secureContainerId)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		secureContainerId = string.Empty;
		object obj;
		if (profile == null)
		{
			obj = null;
		}
		else
		{
			BotBaseInventory inventory = profile.Inventory;
			obj = ((inventory != null) ? inventory.Items : null);
		}
		if (obj == null)
		{
			return false;
		}
		Item val = profile.Inventory.Items.FirstOrDefault((Item item) => string.Equals(item.SlotId, "SecuredContainer", StringComparison.OrdinalIgnoreCase));
		if (val != null)
		{
			_ = val.Id;
			if (0 == 0)
			{
				string text = val.Id.ToString();
				secureContainerId = text;
				return true;
			}
		}
		return false;
	}

	private void ClearSecureContainerContents(List<Item> inventoryItems, string secureContainerId)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (inventoryItems == null || string.IsNullOrWhiteSpace(secureContainerId))
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { secureContainerId };
		HashSet<string> removedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool flag = true;
		while (flag)
		{
			flag = false;
			foreach (Item inventoryItem in inventoryItems)
			{
				if (inventoryItem == null)
				{
					continue;
				}
				_ = inventoryItem.Id;
				if (0 == 0 && !string.IsNullOrEmpty(inventoryItem.ParentId) && hashSet.Contains(inventoryItem.ParentId))
				{
					string item = inventoryItem.Id.ToString();
					if (removedIds.Add(item))
					{
						hashSet.Add(item);
						flag = true;
					}
				}
			}
		}
		inventoryItems.RemoveAll(delegate(Item val)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			if (val != null)
			{
				_ = val.Id;
				return removedIds.Contains(val.Id.ToString());
			}
			return false;
		});
	}

	private bool HasTemplateInBackpack(List<Item> inventoryItems, string templateId)
	{
		if (string.IsNullOrWhiteSpace(templateId))
		{
			return false;
		}
		return HasAnyTemplateInBackpack(inventoryItems, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { templateId });
	}

	private bool HasAnyTemplateInBackpack(List<Item> inventoryItems, IReadOnlySet<string> templateIds)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		if (inventoryItems == null || templateIds == null || templateIds.Count == 0)
		{
			return false;
		}
		Item val = inventoryItems.FirstOrDefault((Item val2) => string.Equals(val2.SlotId, "Backpack", StringComparison.OrdinalIgnoreCase));
		if (val != null)
		{
			_ = val.Id;
			if (0 == 0)
			{
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { val.Id.ToString() };
				bool flag = true;
				while (flag)
				{
					flag = false;
					foreach (Item inventoryItem in inventoryItems)
					{
						if (inventoryItem == null)
						{
							continue;
						}
						_ = inventoryItem.Id;
						if (false || string.IsNullOrEmpty(inventoryItem.ParentId) || !hashSet.Contains(inventoryItem.ParentId))
						{
							continue;
						}
						string item = inventoryItem.Id.ToString();
						if (hashSet.Add(item))
						{
							flag = true;
							if (templateIds.Contains(inventoryItem.Template.ToString()))
							{
								return true;
							}
						}
					}
				}
				return false;
			}
		}
		return false;
	}

	private MongoId? FindMainWeaponAmmoTemplate(List<Item> inventoryItems)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		Item mainWeapon = inventoryItems.FirstOrDefault((Item item) => item.SlotId == "FirstPrimaryWeapon") ?? inventoryItems.FirstOrDefault((Item item) => item.SlotId == "SecondPrimaryWeapon") ?? inventoryItems.FirstOrDefault((Item item) => item.SlotId == "Holster");
		Item obj = mainWeapon;
		if (obj != null)
		{
			_ = obj.Id;
			if (0 == 0)
			{
				foreach (Item item in inventoryItems.Where((Item item) => item.ParentId == mainWeapon.Id.ToString()))
				{
					MongoId? result = FindAmmoTemplateRecursive(inventoryItems, item);
					if (result.HasValue)
					{
						return result;
					}
				}
				return null;
			}
		}
		return null;
	}

	private bool HasProperRaidKit(BotBase teammate)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		BotBaseInventory inventory = teammate.Inventory;
		List<Item> list = ((inventory != null) ? inventory.Items : null);
		if (list == null || list.Count == 0)
		{
			return false;
		}
		string[] requiredRaidWeaponSlots = RequiredRaidWeaponSlots;
		foreach (string slotId in requiredRaidWeaponSlots)
		{
			Item val = list.FirstOrDefault((Item candidate) => string.Equals(candidate.SlotId, slotId, StringComparison.OrdinalIgnoreCase));
			if (val != null)
			{
				_ = val.Template;
				if (0 == 0 && itemHelper.IsOfBaseclass(val.Template, BaseClasses.WEAPON) && !itemHelper.IsOfBaseclass(val.Template, BaseClasses.KNIFE))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static string GetTeammateDisplayName(BotBase teammate)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		Info info = teammate.Info;
		object obj = ((info != null) ? info.Nickname : null);
		if (obj == null)
		{
			obj = teammate.Aid?.ToString();
			if (obj == null)
			{
				MongoId? id = teammate.Id;
				obj = (id.HasValue ? id.GetValueOrDefault().ToString() : null) ?? "teammate";
			}
		}
		return (string)obj;
	}

	private MongoId? FindAmmoTemplateRecursive(List<Item> inventoryItems, Item item)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (itemHelper.IsOfBaseclass(item.Template, BaseClasses.AMMO))
		{
			return item.Template;
		}
		IEnumerable<Item> enumerable = inventoryItems.Where((Item child) => child.ParentId == item.Id.ToString());
		foreach (Item item2 in enumerable)
		{
			MongoId? result = FindAmmoTemplateRecursive(inventoryItems, item2);
			if (result.HasValue)
			{
				return result;
			}
		}
		return null;
	}

	private bool IsMedicalItem(string? templateId, bool isSurgical = false)
	{
		if (string.IsNullOrEmpty(templateId))
		{
			return false;
		}
		if (isSurgical)
		{
			string[] source = new string[2] { "5d02797c86f774203f38e30a", "60d4399358ef941a33423dad" };
			return Enumerable.Contains<string>(source, templateId);
		}
		string[] source2 = new string[8] { "590c657e86f77412b013051d", "544fb45d4bdc2dee738b4568", "544fc38949f06fd411383b42", "5c0e30fa86f77413531e1cd3", "5e831507ea0a7c419314e497", "5e8488fa988873513c331205", "544fb37d4bdc2dee738b4567", "544fb44d4bdc2dee738b4568" };
		return Enumerable.Contains<string>(source2, templateId);
	}

	public bool DeleteTeammate(MongoId sessionId, FriendlyTeammateDeleteRequest request)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		BotBase teammate = FindByAccountId(sessionId, request.AccountId);
		return DeleteTeammate(sessionId, teammate);
	}

	public bool DeleteTeammateByProfileId(MongoId sessionId, MongoId teammateId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		BotBase val = LoadTeammates(sessionId).FirstOrDefault(delegate(BotBase profile)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			MongoId? id = profile.Id;
			MongoId val2 = teammateId;
			return id.HasValue && id.GetValueOrDefault() == val2;
		});
		if (val == (BotBase)null)
		{
			return false;
		}
		return DeleteTeammate(sessionId, val);
	}

	public bool IsTeammateIdentity(MongoId sessionId, MongoId? profileId, string? accountId)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		int? aid = null;
		if (!string.IsNullOrWhiteSpace(accountId) && int.TryParse(accountId, out var result))
		{
			aid = result;
		}
		if (!profileId.HasValue && !aid.HasValue)
		{
			return false;
		}
		return LoadTeammates(sessionId).Any(delegate(BotBase profile)
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			if (profileId.HasValue)
			{
				MongoId? id = profile.Id;
				MongoId? val = profileId;
				if (id.HasValue == val.HasValue && (!id.HasValue || id.GetValueOrDefault() == val.GetValueOrDefault()))
				{
					return true;
				}
			}
			return aid.HasValue && profile.Aid == aid.Value;
		});
	}

	private bool DeleteTeammate(MongoId sessionId, BotBase teammate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		string teammateFilePath = GetTeammateFilePath(sessionId, teammate);
		bool flag = fileUtil.DeleteFile(teammateFilePath);
		fileUtil.DeleteFile(GetTeammateSettingsFilePath(sessionId, teammate));
		fileUtil.DeleteFile(GetDefaultEquipmentFilePath(sessionId, teammate));
		if (flag)
		{
			ISptLogger<FriendlyTeammateService> obj = logger;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Deleted teammate '");
			Info info = teammate.Info;
			defaultInterpolatedStringHandler.AppendFormatted((info != null) ? info.Nickname : null);
			defaultInterpolatedStringHandler.AppendLiteral("' for session '");
			defaultInterpolatedStringHandler.AppendFormatted<MongoId>(sessionId);
			defaultInterpolatedStringHandler.AppendLiteral("'");
			obj.Info(defaultInterpolatedStringHandler.ToStringAndClear(), (Exception)null);
		}
		return flag;
	}

	private string GetPmcRole(string? side)
	{
		if (!(side == "Usec"))
		{
			if (side == "Bear")
			{
				return "pmcBEAR";
			}
			throw new FriendlyTeammateException("Unsupported teammate side '" + side + "'");
		}
		return "pmcUSEC";
	}

	private PmcData GetPlayerProfile(MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		PmcData pmcProfile = profileHelper.GetPmcProfile(sessionId);
		object obj;
		if (pmcProfile == null)
		{
			obj = null;
		}
		else
		{
			Info info = ((BotBase)pmcProfile).Info;
			obj = ((info != null) ? info.Side : null);
		}
		if (obj == null)
		{
			throw new FriendlyTeammateException($"Unable to resolve PMC profile for session '{sessionId}'");
		}
		return pmcProfile;
	}

	private void EnsureNicknameIsUnique(MongoId sessionId, string nickname, int? ignoreAid = null)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (LoadTeammates(sessionId).Any(delegate(BotBase profile)
		{
			if (!ignoreAid.HasValue || profile.Aid != ignoreAid.Value)
			{
				Info info = profile.Info;
				return string.Equals((info != null) ? info.Nickname : null, nickname, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}))
		{
			throw new FriendlyTeammateException("Teammate nickname '" + nickname + "' already exists");
		}
	}

	private string EnsureUniqueRecruitNickname(MongoId sessionId, string nickname)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (!LoadTeammates(sessionId).Any(delegate(BotBase profile)
		{
			Info info = profile.Info;
			return string.Equals((info != null) ? info.Nickname : null, nickname, StringComparison.OrdinalIgnoreCase);
		}))
		{
			return nickname;
		}
		int num = 1;
		string candidate;
		do
		{
			candidate = $"{nickname}{num}";
			num++;
		}
		while (LoadTeammates(sessionId).Any(delegate(BotBase profile)
		{
			Info info = profile.Info;
			return string.Equals((info != null) ? info.Nickname : null, candidate, StringComparison.OrdinalIgnoreCase);
		}));
		return candidate;
	}

	private void NormalizeTeammateProfile(BotBase teammate, PmcData playerPmc)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0017: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_002e: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0050: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0068: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_0088: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00a0: Expected O, but got Unknown
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		BotBase val = teammate;
		if (val.Info == null)
		{
			BotBase obj = val;
			Info val2 = new Info();
			Info val3 = val2;
			obj.Info = val2;
		}
		val = teammate;
		if (val.Customization == null)
		{
			BotBase obj2 = val;
			Customization val4 = new Customization();
			Customization val5 = val4;
			obj2.Customization = val4;
		}
		val = teammate;
		BotBaseInventory val7;
		if (val.Inventory == null)
		{
			BotBase obj3 = val;
			BotBaseInventory val6 = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			val7 = val6;
			obj3.Inventory = val6;
		}
		val = teammate;
		Stats val9;
		if (val.Stats == null)
		{
			BotBase obj4 = val;
			Stats val8 = new Stats();
			val9 = val8;
			obj4.Stats = val8;
		}
		val9 = teammate.Stats;
		if (val9.Eft == null)
		{
			Stats obj5 = val9;
			EftStats val10 = new EftStats();
			EftStats val11 = val10;
			obj5.Eft = val10;
		}
		val = teammate;
		if (val.Hideout == null)
		{
			BotBase obj6 = val;
			Hideout val12 = new Hideout();
			Hideout val13 = val12;
			obj6.Hideout = val12;
		}
		val7 = teammate.Inventory;
		if (val7.HideoutAreaStashes == null)
		{
			Dictionary<string, MongoId> dictionary = (val7.HideoutAreaStashes = new Dictionary<string, MongoId>());
		}
		Info info = teammate.Info;
		Info info2 = ((BotBase)playerPmc).Info;
		info.Side = ((info2 != null) ? info2.Side : null);
		teammate.Info.MemberCategory = (MemberCategory)1024;
		teammate.Info.SelectedMemberCategory = (MemberCategory)1024;
		Info info3 = teammate.Info;
		Info info4 = ((BotBase)playerPmc).Info;
		info3.BannedState = ((info4 != null) ? info4.BannedState : ((bool?)null));
		Info info5 = teammate.Info;
		Info info6 = ((BotBase)playerPmc).Info;
		info5.BannedUntil = ((info6 != null) ? info6.BannedUntil : ((long?)null));
		teammate.Info.RegistrationDate = GetCurrentUnixTimestampSeconds();
		teammate.Achievements = ((BotBase)playerPmc).Achievements;
		teammate.Stats.Eft.TotalInGameTime = 0L;
		teammate.Stats.Eft.OverallCounters = new OverallCounters
		{
			Items = new List<CounterKeyValue>()
		};
	}

	private static void NormalizeTeammateSkillsForCreation(BotBase teammate, PmcData playerPmc)
	{
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		object obj;
		if (teammate == null)
		{
			obj = null;
		}
		else
		{
			Skills skills = teammate.Skills;
			obj = ((skills != null) ? skills.Common : null);
		}
		if (obj == null)
		{
			return;
		}
		object obj2;
		if (playerPmc == null)
		{
			obj2 = null;
		}
		else
		{
			Skills skills2 = ((BotBase)playerPmc).Skills;
			obj2 = ((skills2 != null) ? skills2.Common : null);
		}
		if (obj2 == null)
		{
			return;
		}
		Dictionary<SkillTypes, double> generatedSkillProgress = (from skill in teammate.Skills.Common
			where skill != (CommonSkill)null
			group skill by skill.Id).ToDictionary((IGrouping<SkillTypes, CommonSkill> group) => group.Key, (IGrouping<SkillTypes, CommonSkill> group) => group.First().Progress);
		Dictionary<SkillTypes, double> dictionary = (from skill in ((BotBase)playerPmc).Skills.Common
			where skill != (CommonSkill)null
			group skill by skill.Id).ToDictionary((IGrouping<SkillTypes, CommonSkill> group) => group.Key, (IGrouping<SkillTypes, CommonSkill> group) => group.First().Progress);
		if (dictionary.Count == 0)
		{
			return;
		}
		double val = dictionary.Values.Max();
		foreach (CommonSkill item in teammate.Skills.Common)
		{
			if (!(item == (CommonSkill)null))
			{
				if (!dictionary.TryGetValue(item.Id, out var value))
				{
					item.Progress = Math.Min(item.Progress, val);
					item.PointsEarnedDuringSession = 0.0;
				}
				else
				{
					item.Progress = Math.Min(item.Progress, value);
					item.PointsEarnedDuringSession = 0.0;
				}
			}
		}
		ApplyRandomWeaponSpecialty(teammate.Skills.Common, dictionary, generatedSkillProgress);
	}

	private static void ApplyRandomWeaponSpecialty(IEnumerable<CommonSkill> teammateSkills, Dictionary<SkillTypes, double> playerSkillProgress, Dictionary<SkillTypes, double> generatedSkillProgress)
	{
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (teammateSkills == null)
		{
			return;
		}
		HashSet<SkillTypes> weaponSkillTypes = ResolveWeaponSkillTypes();
		if (weaponSkillTypes.Count == 0)
		{
			return;
		}
		List<CommonSkill> list = teammateSkills.Where((CommonSkill skill) => skill != (CommonSkill)null && weaponSkillTypes.Contains(skill.Id)).ToList();
		if (list.Count != 0)
		{
			double num = list.Select((CommonSkill skill) => (!playerSkillProgress.TryGetValue(skill.Id, out var value2)) ? skill.Progress : value2).DefaultIfEmpty(0.0).Max();
			CommonSkill specialtySkill = list[Random.Shared.Next(list.Count)];
			double num2 = (from skill in list
				where skill != specialtySkill
				select skill.Progress).DefaultIfEmpty(specialtySkill.Progress).Max();
			int num3 = Random.Shared.Next(125, 276);
			double num4 = Math.Min(5100.0, num + (double)num3);
			double num5 = Math.Min(num4, num2 + (double)num3);
			double value;
			double val = (generatedSkillProgress.TryGetValue(specialtySkill.Id, out value) ? value : num5);
			specialtySkill.Progress = Math.Max(specialtySkill.Progress, Math.Min(val, num4));
			specialtySkill.Progress = Math.Max(specialtySkill.Progress, num5);
			specialtySkill.PointsEarnedDuringSession = 0.0;
		}
	}

	private static HashSet<SkillTypes> ResolveWeaponSkillTypes()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		HashSet<SkillTypes> hashSet = new HashSet<SkillTypes>();
		string[] weaponSkillNames = WeaponSkillNames;
		foreach (string value in weaponSkillNames)
		{
			if (Enum.TryParse<SkillTypes>(value, false, out SkillTypes result))
			{
				hashSet.Add(result);
			}
		}
		return hashSet;
	}

	private void ApplyFollowerProgress(BotBase teammate, FriendlyTeammateFollowerProgressRequest progressEntry)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0017: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_0054: Expected O, but got Unknown
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Expected O, but got Unknown
		BotBase val = teammate;
		if (val.Info == null)
		{
			BotBase obj = val;
			Info val2 = new Info();
			Info val3 = val2;
			obj.Info = val2;
		}
		val = teammate;
		if (val.Skills == null)
		{
			BotBase obj2 = val;
			Skills val4 = new Skills
			{
				Common = Array.Empty<CommonSkill>(),
				Mastering = Array.Empty<MasterySkill>(),
				Points = 0.0
			};
			Skills val5 = val4;
			obj2.Skills = val4;
		}
		int num = (int)Math.Round(progressEntry.BotExperienceSession);
		if (num > 0)
		{
			Info info = teammate.Info;
			info.Experience += num;
			RecalculateTeammateLevel(teammate);
		}
		ApplyFollowerLifetimeProgress(teammate, progressEntry);
		if (progressEntry.Skills == null || progressEntry.Skills.Count == 0)
		{
			return;
		}
		List<CommonSkill> list = teammate.Skills.Common?.ToList() ?? new List<CommonSkill>();
		foreach (FriendlyTeammateSkillProgressRequest skill in progressEntry.Skills)
		{
			if (TryParseSkillType(skill.Id, out var skillType))
			{
				double progress = Math.Round(skill.Current + skill.Progress, 2);
				CommonSkill val6 = list.FirstOrDefault((CommonSkill skill) => skill.Id == skillType);
				if (val6 != (CommonSkill)null)
				{
					val6.Progress = progress;
					continue;
				}
				list.Add(new CommonSkill
				{
					Id = skillType,
					Progress = progress,
					PointsEarnedDuringSession = 0.0,
					LastAccess = 0L
				});
			}
		}
		teammate.Skills.Common = list;
	}

	private static void ApplyFollowerLifetimeProgress(BotBase teammate, FriendlyTeammateFollowerProgressRequest progressEntry)
	{
		if (progressEntry.KillCount > 0)
		{
			AddOverallCounter(teammate, progressEntry.KillCount, "Kills");
		}
		if (progressEntry.RaidSeconds > 0)
		{
			EnsureTeammateEftStats(teammate);
			teammate.Stats.Eft.TotalInGameTime = teammate.Stats.Eft.TotalInGameTime.GetValueOrDefault() + progressEntry.RaidSeconds;
			AddOverallCounter(teammate, progressEntry.RaidSeconds, "LifeTime", "Pmc");
		}
	}

	private static void ApplyFollowerRaidOutcomeStats(BotBase teammate, bool escaped)
	{
		AddOverallCounter(teammate, 1.0, "Sessions", "Pmc");
		if (escaped)
		{
			AddOverallCounter(teammate, 1.0, "ExitStatus", "Survived", "Pmc");
		}
		else
		{
			AddOverallCounter(teammate, 1.0, "Deaths");
		}
	}

	private static void InitializeRecruitRaidStats(BotBase teammate, int targetLevel, int? deterministicSeed = null)
	{
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Expected O, but got Unknown
		EnsureTeammateEftStats(teammate);
		Random random = (deterministicSeed.HasValue ? new Random(deterministicSeed.Value) : Random.Shared);
		int num = Math.Clamp(targetLevel, 1, 79);
		int num2 = Math.Max(1, (int)Math.Round((double)num * RandomRange(random, 2.5, 5.5) + (double)random.Next(0, 13)));
		double num3 = Math.Clamp(0.25 + (double)num * 0.0045 + RandomRange(random, -0.08, 0.12), 0.18, 0.72);
		int num4 = Math.Clamp((int)Math.Round((double)num2 * num3), 0, num2);
		int num5 = Math.Max(0, num2 - num4);
		double num6 = Math.Clamp(0.55 + (double)num * 0.035 + RandomRange(random, -0.35, 0.65), 0.2, 4.25);
		int num7 = Math.Max(0, (int)Math.Round((double)num2 * num6));
		long num8 = Math.Max(600L, (long)Math.Round((double)num2 * RandomRange(random, 850.0, 2100.0)));
		teammate.Stats.Eft.TotalInGameTime = num8;
		teammate.Stats.Eft.OverallCounters = new OverallCounters
		{
			Items = new List<CounterKeyValue>()
		};
		AddOverallCounter(teammate, num2, "Sessions", "Pmc");
		AddOverallCounter(teammate, num4, "ExitStatus", "Survived", "Pmc");
		AddOverallCounter(teammate, num5, "Deaths");
		AddOverallCounter(teammate, num7, "Kills");
		AddOverallCounter(teammate, num8, "LifeTime", "Pmc");
	}

	private static int GetRecruitStatsSeed(FriendlyRecruitPickupCandidate candidate)
	{
		int num = 17;
		num = num * 31 + StableStringHash(candidate.ProfileId);
		num = num * 31 + StableStringHash(candidate.AccountId);
		num = num * 31 + Math.Max(1, candidate.Level);
		return num & 0x7FFFFFFF;
	}

	private static int StableStringHash(string? value)
	{
		int num = 23;
		string text = value ?? string.Empty;
		foreach (char c in text)
		{
			num = num * 31 + c;
		}
		return num;
	}

	private static double RandomRange(Random rng, double min, double max)
	{
		return min + rng.NextDouble() * (max - min);
	}

	private static void AddOverallCounter(BotBase teammate, double value, params string[] key)
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		if (!(value <= 0.0) && key != null && key.Length != 0)
		{
			EnsureTeammateEftStats(teammate);
			OverallCounters overallCounters = teammate.Stats.Eft.OverallCounters;
			OverallCounters val = overallCounters;
			if (val.Items == null)
			{
				List<CounterKeyValue> list = (val.Items = new List<CounterKeyValue>());
			}
			CounterKeyValue val2 = overallCounters.Items.FirstOrDefault((CounterKeyValue counter) => CounterKeyMatches((counter != null) ? counter.Key : null, key));
			if (val2 != (CounterKeyValue)null)
			{
				val2.Value = (long?)(val2.Value.GetValueOrDefault() + value);
				return;
			}
			overallCounters.Items.Add(new CounterKeyValue
			{
				Key = new HashSet<string>(key, StringComparer.Ordinal),
				Value = (long?)value
			});
		}
	}

	private static bool CounterKeyMatches(HashSet<string>? existingKey, string[] expectedKey)
	{
		if (existingKey == null || existingKey.Count != expectedKey.Length)
		{
			return false;
		}
		foreach (string item in expectedKey)
		{
			if (!existingKey.Contains(item))
			{
				return false;
			}
		}
		return true;
	}

	private static void EnsureTeammateEftStats(BotBase teammate)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0017: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0033: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		//IL_005f: Expected O, but got Unknown
		Stats val2;
		if (teammate.Stats == null)
		{
			Stats val = new Stats();
			val2 = val;
			teammate.Stats = val;
		}
		val2 = teammate.Stats;
		EftStats val4;
		if (val2.Eft == null)
		{
			Stats obj = val2;
			EftStats val3 = new EftStats();
			val4 = val3;
			obj.Eft = val3;
		}
		val4 = teammate.Stats.Eft;
		OverallCounters val6;
		if (val4.OverallCounters == null)
		{
			EftStats obj2 = val4;
			OverallCounters val5 = new OverallCounters
			{
				Items = new List<CounterKeyValue>()
			};
			val6 = val5;
			obj2.OverallCounters = val5;
		}
		val6 = teammate.Stats.Eft.OverallCounters;
		if (val6.Items == null)
		{
			List<CounterKeyValue> list = (val6.Items = new List<CounterKeyValue>());
		}
		val4 = teammate.Stats.Eft;
		long? totalInGameTime = val4.TotalInGameTime;
		long valueOrDefault = totalInGameTime.GetValueOrDefault();
		if (!totalInGameTime.HasValue)
		{
			valueOrDefault = 0L;
			EftStats obj3 = val4;
			long? totalInGameTime2 = valueOrDefault;
			obj3.TotalInGameTime = totalInGameTime2;
		}
	}

	private static void ApplyDeathEscapeOutcome(BotBase teammate, FriendlyTeammateDeathEscapeEntry entry)
	{
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Expected O, but got Unknown
		//IL_01ac: Expected O, but got Unknown
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Expected O, but got Unknown
		//IL_0208: Expected O, but got Unknown
		BotBaseHealth health = teammate.Health;
		Dictionary<string, BodyPartHealth> dictionary = ((health != null) ? health.BodyParts : null);
		if (dictionary == null || dictionary.Count == 0)
		{
			return;
		}
		double num = Math.Clamp(entry.HealthRatio, 0.05, 1.0);
		foreach (KeyValuePair<string, BodyPartHealth> item in dictionary)
		{
			item.Deconstruct(out var key, out var value);
			string partName = key;
			BodyPartHealth val = value;
			CurrentMinMax val2 = ((val != null) ? val.Health : null);
			if (!(val2 == (CurrentMinMax)null))
			{
				double num2 = Math.Max(1.0, val2.Maximum ?? val2.Current ?? 1.0);
				val2.Maximum = num2;
				if (!entry.Escaped)
				{
					val2.Current = 0.0;
					continue;
				}
				double min = (IsVitalBodyPart(partName) ? 1.0 : 0.0);
				val2.Current = Math.Clamp(num2 * num, min, num2);
			}
		}
		if (entry.Escaped)
		{
			BotBaseHealth health2 = teammate.Health;
			if (health2.Hydration == null)
			{
				BotBaseHealth obj = health2;
				CurrentMinMax val3 = new CurrentMinMax
				{
					Current = 100.0,
					Maximum = 100.0,
					Minimum = 0.0
				};
				CurrentMinMax val4 = val3;
				obj.Hydration = val3;
			}
			health2 = teammate.Health;
			if (health2.Energy == null)
			{
				BotBaseHealth obj2 = health2;
				CurrentMinMax val5 = new CurrentMinMax
				{
					Current = 100.0,
					Maximum = 100.0,
					Minimum = 0.0
				};
				CurrentMinMax val4 = val5;
				obj2.Energy = val5;
			}
			teammate.Health.Hydration.Current = Math.Max(teammate.Health.Hydration.Current.GetValueOrDefault(), 1.0);
			teammate.Health.Energy.Current = Math.Max(teammate.Health.Energy.Current.GetValueOrDefault(), 1.0);
		}
	}

	private static bool IsVitalBodyPart(string partName)
	{
		if (!string.Equals(partName, "Head", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(partName, "Chest", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private void ApplyPmcFollowerSkillBaseline(BotBase teammate)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0017: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_0054: Expected O, but got Unknown
		BotBase val = teammate;
		if (val.Info == null)
		{
			BotBase obj = val;
			Info val2 = new Info();
			Info val3 = val2;
			obj.Info = val2;
		}
		val = teammate;
		if (val.Skills == null)
		{
			BotBase obj2 = val;
			Skills val4 = new Skills
			{
				Common = Array.Empty<CommonSkill>(),
				Mastering = Array.Empty<MasterySkill>(),
				Points = 0.0
			};
			Skills val5 = val4;
			obj2.Skills = val4;
		}
		List<CommonSkill> list = teammate.Skills.Common?.ToList() ?? new List<CommonSkill>();
		int num = Math.Max(1, teammate.Info.Level ?? 1);
		EnsureSkillProgressFloor(list, (SkillTypes)64, 4500.0);
		EnsureSkillProgressFloor(list, (SkillTypes)3, Math.Min(40 * num, 5100));
		EnsureSkillProgressFloor(list, (SkillTypes)2, Math.Min(30 * num, 5100));
		EnsureSkillProgressFloor(list, (SkillTypes)38, Math.Min(20 * num, 5100));
		EnsureSkillProgressFloor(list, (SkillTypes)37, Math.Min(20 * num, 5100));
		EnsureSkillProgressFloor(list, (SkillTypes)4, Math.Min(20 * num, 5100));
		teammate.Skills.Common = list;
	}

	private static void EnsureSkillProgressFloor(List<CommonSkill> commonSkills, SkillTypes skillType, double minimumProgress)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		CommonSkill val = commonSkills.FirstOrDefault((CommonSkill existingSkill) => existingSkill.Id == skillType);
		if (val == (CommonSkill)null)
		{
			commonSkills.Add(new CommonSkill
			{
				Id = skillType,
				Progress = minimumProgress,
				PointsEarnedDuringSession = 0.0,
				LastAccess = 0L
			});
		}
		else
		{
			val.Progress = Math.Max(val.Progress, minimumProgress);
		}
	}

	private void RecalculateTeammateLevel(BotBase teammate)
	{
		if (teammate.Info == (Info)null)
		{
			return;
		}
		int num = 0;
		ExpTable[] experienceTable = globalTable.Configuration.Exp.Level.ExperienceTable;
		for (int i = 0; i < experienceTable.Length; i++)
		{
			num += experienceTable[i].Experience;
			if (!(teammate.Info.Experience < num))
			{
				teammate.Info.Level = i + 1;
				continue;
			}
			break;
		}
	}

	private static bool TryParseSkillType(JsonElement idValue, out SkillTypes skillType)
	{
		skillType = (SkillTypes)0;
		if (idValue.ValueKind == JsonValueKind.String)
		{
			string value = idValue.GetString();
			if (!string.IsNullOrWhiteSpace(value))
			{
				return Enum.TryParse<SkillTypes>(value, true, out skillType);
			}
			return false;
		}
		if (idValue.ValueKind == JsonValueKind.Number && idValue.TryGetInt32(out var value2))
		{
			skillType = (SkillTypes)value2;
			return Enum.IsDefined<SkillTypes>(skillType);
		}
		return false;
	}

	private void LocalizeStartupRecoveryNotice(MongoId sessionId, FriendlyTeammateStartupRecoveryNotice notice)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if ((object)notice == null || !notice.Recovered)
		{
			return;
		}
		Dictionary<string, string> stringMap = languageService.GetStringMap(sessionId, "socialUi");
		notice.Title = GetLanguageValue(stringMap, "DuplicateProfileRecoveryTitle", "Profile recovered");
		string languageValue = GetLanguageValue(stringMap, "DuplicateProfileRecoveryBody", "Duplicate items were found in both player and teammate profiles. The following teammate profiles have been stripped of the duplicate in order to safely recover them: {0}");
		string text = ((notice.TeammateNames == null || notice.TeammateNames.Count == 0) ? string.Empty : string.Join(", ", notice.TeammateNames.Where((string name) => !string.IsNullOrWhiteSpace(name))));
		try
		{
			notice.Message = string.Format(languageValue, text);
		}
		catch
		{
			notice.Message = languageValue + " " + text;
		}
	}

	private static string GetLanguageValue(Dictionary<string, string> values, string key, string fallback)
	{
		if (values == null || !values.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
		{
			return fallback;
		}
		return value;
	}

	private unsafe void RecoverDuplicateTeammateItemsForSession(MongoId sessionId)
	{
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		string text = sessionId.ToString();
		if (duplicateRecoveryCheckedSessions.Contains(text))
		{
			return;
		}
		try
		{
			PmcData playerProfile = GetPlayerProfile(sessionId);
			BotBaseInventory inventory = ((BotBase)playerProfile).Inventory;
			List<Item> list = ((inventory != null) ? inventory.Items : null);
			if (list == null || list.Count == 0)
			{
				duplicateRecoveryCheckedSessions.Add(text);
				return;
			}
			string playerStashRootId = GetPlayerStashRootId(playerProfile);
			HashSet<string> itemTreeIds = GetItemTreeIds(list, playerStashRootId);
			if (itemTreeIds.Count == 0)
			{
				duplicateRecoveryCheckedSessions.Add(text);
				return;
			}
			string teammateDirectory = GetTeammateDirectory(sessionId);
			if (!fileUtil.DirectoryExists(teammateDirectory))
			{
				duplicateRecoveryCheckedSessions.Add(text);
				return;
			}
			SortedSet<string> sortedSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			int num = 0;
			foreach (string item in fileUtil.GetFiles(teammateDirectory, false, "*").Where(IsTeammateProfileFile))
			{
				BotBase val;
				try
				{
					val = jsonUtil.DeserializeFromFile<BotBase>(item);
				}
				catch (Exception ex)
				{
					logger.Warning($"{"FriendlyTeammateService"}: skipped duplicate item recovery for unreadable teammate profile file '{item}': {ex.Message}", (Exception)null);
					continue;
				}
				if (((val != null) ? val.Id : ((MongoId?)null)).HasValue)
				{
					int num2 = RecoverDuplicateTeammateProfileItems(val, item, itemTreeIds);
					int num3 = RecoverDuplicateDefaultEquipmentItems(sessionId, val, itemTreeIds);
					int num4 = num2 + num3;
					if (num4 > 0)
					{
						sortedSet.Add(GetTeammateDisplayName(val));
						num += num4;
					}
				}
			}
			if (sortedSet.Count > 0)
			{
				startupRecoveryNotices[text] = new FriendlyTeammateStartupRecoveryNotice
				{
					Recovered = true,
					RemovedItemCount = num,
					TeammateNames = sortedSet.ToList()
				};
				logger.Warning($"Recovered duplicate player/teammate item IDs for session '{sessionId}' by removing {num} item(s) from teammate profile(s): {string.Join(", ", sortedSet)}.", (Exception)null);
			}
			duplicateRecoveryCheckedSessions.Add(text);
		}
		catch (Exception ex2)
		{
			logger.Warning($"Failed to run duplicate teammate item recovery for session '{sessionId}': {ex2.Message}", (Exception)null);
		}
	}

	private int RecoverDuplicateTeammateProfileItems(BotBase teammate, string profileFilePath, HashSet<string> playerStashIds)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		BotBaseInventory inventory = teammate.Inventory;
		List<Item> list = ((inventory != null) ? inventory.Items : null);
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		BotBaseInventory inventory2 = teammate.Inventory;
		object protectedRootId;
		if (inventory2 == null)
		{
			protectedRootId = null;
		}
		else
		{
			MongoId? equipment = inventory2.Equipment;
			protectedRootId = (equipment.HasValue ? equipment.GetValueOrDefault().ToString() : null);
		}
		int num = RemoveDuplicatePlayerStashItemTrees(list, (string?)protectedRootId, playerStashIds);
		if (num <= 0)
		{
			return 0;
		}
		BackupFileBeforeRecovery(profileFilePath);
		WriteSerializedFile<BotBase>(profileFilePath, teammate, "teammate profile");
		logger.Warning($"Recovered teammate '{GetTeammateDisplayName(teammate)}' profile by removing {num} duplicate player stash item(s).", (Exception)null);
		return num;
	}

	private int RecoverDuplicateDefaultEquipmentItems(MongoId sessionId, BotBase teammate, HashSet<string> playerStashIds)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		string defaultEquipmentFilePath = GetDefaultEquipmentFilePath(sessionId, teammate);
		if (!fileUtil.FileExists(defaultEquipmentFilePath))
		{
			return 0;
		}
		List<Item> list;
		try
		{
			list = jsonUtil.DeserializeFromFile<List<Item>>(defaultEquipmentFilePath);
		}
		catch (Exception ex)
		{
			logger.Warning($"{"FriendlyTeammateService"}: skipped duplicate item recovery for unreadable teammate default equipment file '{defaultEquipmentFilePath}': {ex.Message}", (Exception)null);
			return 0;
		}
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		BotBaseInventory inventory = teammate.Inventory;
		object obj;
		if (inventory == null)
		{
			obj = null;
		}
		else
		{
			MongoId? equipment = inventory.Equipment;
			obj = (equipment.HasValue ? equipment.GetValueOrDefault().ToString() : null);
		}
		if (obj == null)
		{
			Item? obj2 = list.FirstOrDefault(delegate(Item item)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				if (item == null)
				{
					return false;
				}
				_ = item.Id;
				return true;
			});
			obj = ((obj2 != null) ? obj2.Id.ToString() : null);
		}
		string protectedRootId = (string)obj;
		int num = RemoveDuplicatePlayerStashItemTrees(list, protectedRootId, playerStashIds);
		if (num <= 0)
		{
			return 0;
		}
		if (list.Count == 0)
		{
			logger.Warning("FriendlyTeammateService: skipped duplicate recovery write for teammate " + GetTeammateDisplayName(teammate) + " default equipment because no item remained.", (Exception)null);
			return 0;
		}
		BackupFileBeforeRecovery(defaultEquipmentFilePath);
		WriteSerializedFile(defaultEquipmentFilePath, list, "teammate default equipment");
		logger.Warning($"Recovered teammate '{GetTeammateDisplayName(teammate)}' default equipment by removing {num} duplicate player stash item(s).", (Exception)null);
		return num;
	}

	private static int RemoveDuplicatePlayerStashItemTrees(List<Item> items, string? protectedRootId, HashSet<string> playerStashIds)
	{
		if (items == null || items.Count == 0 || playerStashIds == null || playerStashIds.Count == 0)
		{
			return 0;
		}
		HashSet<string> hashSet = (from item in items.Where(delegate(Item item)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				if (item == null)
				{
					return false;
				}
				_ = item.Id;
				return true;
			})
			select item.Id.ToString() into id
			where playerStashIds.Contains(id) && !string.Equals(id, protectedRootId, StringComparison.OrdinalIgnoreCase)
			select id).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (hashSet.Count == 0)
		{
			return 0;
		}
		HashSet<string> removeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in hashSet)
		{
			foreach (string itemTreeId in GetItemTreeIds(items, item))
			{
				if (!string.Equals(itemTreeId, protectedRootId, StringComparison.OrdinalIgnoreCase))
				{
					removeIds.Add(itemTreeId);
				}
			}
		}
		return items.RemoveAll(delegate(Item item)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				return removeIds.Contains(item.Id.ToString());
			}
			return false;
		});
	}

	private void RecoverTeammateProfileIfNeeded(MongoId sessionId, BotBase teammate, string profileFilePath)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (teammate != null && teammate.Aid.HasValue)
		{
			int num = RecoverTeammateInventoryItems(teammate);
			if (num > 0)
			{
				BackupFileBeforeRecovery(profileFilePath);
				WriteSerializedFile<BotBase>(profileFilePath, teammate, "teammate profile");
				logger.Warning($"Recovered teammate '{GetTeammateDisplayName(teammate)}' profile by removing {num} bad item(s).", (Exception)null);
			}
			int num2 = RecoverDefaultEquipmentSnapshotIfNeeded(sessionId, teammate);
			int num3 = num + num2;
			if (num3 > 0)
			{
				profileRecoveryNotices[GetRecoveryNoticeKey(sessionId, teammate)] = new FriendlyTeammateProfileRecoveryNotice
				{
					Recovered = true,
					RemovedItemCount = num3,
					Message = "The profile of this teammate has been recovered from a bad state. Some items from his inventory may have been deleted in the process."
				};
			}
		}
	}

	private int RecoverTeammateInventoryItems(BotBase teammate)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		object obj;
		if (teammate == null)
		{
			obj = null;
		}
		else
		{
			BotBaseInventory inventory = teammate.Inventory;
			obj = ((inventory != null) ? inventory.Items : null);
		}
		if (obj == null || teammate.Inventory.Items.Count == 0)
		{
			return 0;
		}
		List<Item> items = teammate.Inventory.Items;
		MongoId? equipment = teammate.Inventory.Equipment;
		int removedCount;
		List<Item> list = RecoverEquipmentItems(items, equipment.HasValue ? equipment.GetValueOrDefault().ToString() : null, out removedCount);
		if (removedCount <= 0 || list.Count == 0)
		{
			return 0;
		}
		teammate.Inventory.Items = list;
		teammate.Inventory.Equipment = list.First().Id;
		return removedCount;
	}

	private int RecoverDefaultEquipmentSnapshotIfNeeded(MongoId sessionId, BotBase teammate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		string defaultEquipmentFilePath = GetDefaultEquipmentFilePath(sessionId, teammate);
		if (!fileUtil.FileExists(defaultEquipmentFilePath))
		{
			return 0;
		}
		List<Item> list = jsonUtil.DeserializeFromFile<List<Item>>(defaultEquipmentFilePath);
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		Item val = list.FirstOrDefault((Item item) => item != (Item)null);
		string preferredRootId = ((teammate.Inventory != (BotBaseInventory)null) ? teammate.Inventory.Equipment.ToString() : ((val != null) ? val.Id.ToString() : null));
		int removedCount;
		List<Item> list2 = RecoverEquipmentItems(list, preferredRootId, out removedCount);
		if (removedCount <= 0)
		{
			return 0;
		}
		if (list2.Count == 0)
		{
			logger.Warning("FriendlyTeammateService: skipped recovery write for teammate " + GetTeammateDisplayName(teammate) + " default equipment because no valid root item remained.", (Exception)null);
			return 0;
		}
		BackupFileBeforeRecovery(defaultEquipmentFilePath);
		WriteSerializedFile(defaultEquipmentFilePath, list2, "teammate default equipment");
		logger.Warning($"Recovered teammate '{GetTeammateDisplayName(teammate)}' default equipment by removing {removedCount} bad item(s).", (Exception)null);
		return removedCount;
	}

	private List<Item> RecoverEquipmentItems(List<Item> items, string? preferredRootId, out int removedCount)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		removedCount = 0;
		if (items == null || items.Count == 0)
		{
			return new List<Item>();
		}
		Dictionary<string, Item> dictionary = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
		foreach (Item item2 in items)
		{
			if (item2 == null)
			{
				continue;
			}
			_ = item2.Id;
			if (0 == 0)
			{
				string key = item2.Id.ToString();
				if (!dictionary.ContainsKey(key) && HasKnownTemplate(item2))
				{
					dictionary[key] = item2;
				}
			}
		}
		if (dictionary.Count == 0)
		{
			removedCount = items.Count;
			return new List<Item>();
		}
		string item = ((!string.IsNullOrWhiteSpace(preferredRootId) && dictionary.ContainsKey(preferredRootId)) ? preferredRootId : dictionary.Keys.First());
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { item };
		bool flag;
		do
		{
			flag = false;
			foreach (Item value in dictionary.Values)
			{
				if (value != null)
				{
					_ = value.Id;
					if (0 == 0 && !string.IsNullOrWhiteSpace(value.ParentId) && hashSet.Contains(value.ParentId) && hashSet.Add(value.Id.ToString()))
					{
						flag = true;
					}
				}
			}
		}
		while (flag);
		List<Item> list = new List<Item>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Item item3 in items)
		{
			if (item3 == null)
			{
				continue;
			}
			_ = item3.Id;
			if (0 == 0)
			{
				string text = item3.Id.ToString();
				if (hashSet.Contains(text) && dictionary.ContainsKey(text) && hashSet2.Add(text))
				{
					list.Add(dictionary[text]);
				}
			}
		}
		removedCount = items.Count - list.Count;
		return list;
	}

	private bool HasKnownTemplate(Item item)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (item != null)
		{
			_ = item.Template;
			if (0 == 0)
			{
				try
				{
					return itemHelper.GetItem(item.Template).Key;
				}
				catch
				{
					return false;
				}
			}
		}
		return false;
	}

	private FriendlyTeammateProfileRecoveryNotice? ConsumeProfileRecoveryNotice(MongoId sessionId, BotBase teammate)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		string recoveryNoticeKey = GetRecoveryNoticeKey(sessionId, teammate);
		if (!profileRecoveryNotices.TryGetValue(recoveryNoticeKey, out FriendlyTeammateProfileRecoveryNotice value))
		{
			return null;
		}
		profileRecoveryNotices.Remove(recoveryNoticeKey);
		return value;
	}

	private static string GetRecoveryNoticeKey(MongoId sessionId, BotBase teammate)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return $"{sessionId}:{((teammate == null) ? null : teammate.Aid?.ToString()) ?? string.Empty}";
	}

	private void BackupFileBeforeRecovery(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			return;
		}
		string directoryName = Path.GetDirectoryName(filePath);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
		string extension = Path.GetExtension(filePath);
		string text = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
		string text2 = Path.Combine(directoryName ?? string.Empty, fileNameWithoutExtension + ".recovery-backup-" + text + extension);
		try
		{
			File.Copy(filePath, text2, overwrite: false);
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to create teammate recovery backup '" + text2 + "': " + ex.Message, (Exception)null);
		}
	}

	private void WriteSerializedFile<T>(string filePath, T value, string description)
	{
		string text = jsonUtil.Serialize<T>(value, true);
		if (text == null)
		{
			throw new FriendlyTeammateException("Unable to serialize recovered " + description);
		}
		fileUtil.WriteFile(filePath, text);
	}

	private List<BotBase> LoadTeammates(MongoId sessionId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		string teammateDirectory = GetTeammateDirectory(sessionId);
		if (!fileUtil.DirectoryExists(teammateDirectory))
		{
			return new List<BotBase>();
		}
		List<BotBase> list = new List<BotBase>();
		foreach (string item in fileUtil.GetFiles(teammateDirectory, false, "*").Where(IsTeammateProfileFile))
		{
			BotBase val;
			try
			{
				val = jsonUtil.DeserializeFromFile<BotBase>(item);
			}
			catch (Exception ex)
			{
				logger.Warning($"{"FriendlyTeammateService"}: skipped unreadable teammate profile file '{item}': {ex.Message}", (Exception)null);
				continue;
			}
			if (((val != null) ? val.Id : ((MongoId?)null)).HasValue)
			{
				RecoverTeammateProfileIfNeeded(sessionId, val, item);
				list.Add(val);
			}
		}
		return list.OrderBy(delegate(BotBase profile)
		{
			Info info = profile.Info;
			return ((info != null) ? info.RegistrationDate : ((int?)null)) ?? int.MaxValue;
		}).ThenBy((BotBase profile) => profile.Aid ?? int.MaxValue).ToList();
	}

	private static int GetCurrentUnixTimestampSeconds()
	{
		return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}

	private BotBase FindByAccountId(MongoId sessionId, string? accountId)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(accountId))
		{
			throw new FriendlyTeammateException("Missing teammate accountId");
		}
		if (!int.TryParse(accountId, out var aid))
		{
			throw new FriendlyTeammateException("Invalid teammate accountId '" + accountId + "'");
		}
		BotBase val = LoadTeammates(sessionId).FirstOrDefault((BotBase profile) => profile.Aid == aid);
		return val ?? throw new FriendlyTeammateException("Unable to find teammate with accountId '" + accountId + "'");
	}

	private bool TryFindByAccountId(MongoId sessionId, string? accountId, out BotBase? teammate)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		teammate = null;
		if (string.IsNullOrWhiteSpace(accountId) || !int.TryParse(accountId, out var aid))
		{
			return false;
		}
		teammate = LoadTeammates(sessionId).FirstOrDefault((BotBase profile) => profile.Aid == aid);
		return teammate != (BotBase)null;
	}

	private void SaveTeammate(MongoId sessionId, BotBase teammate)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		EnsureFollowerHasPockets(teammate);
		if (!IsCurrentLoadoutManagementModeExtreme() && RemoveSecureContainerTree(teammate))
		{
			logger.Info($"Removed non-Realistic secure container tree before saving teammate '{teammate.Aid}'.", (Exception)null);
		}
		PruneUnreachableEquipmentItems(teammate);
		string teammateFilePath = GetTeammateFilePath(sessionId, teammate);
		string text = jsonUtil.Serialize<BotBase>(teammate, true);
		if (text == null)
		{
			throw new FriendlyTeammateException("Unable to serialize teammate profile");
		}
		fileUtil.WriteFile(teammateFilePath, text);
	}

	private unsafe string GetTeammateDirectory(MongoId sessionId)
	{
		return Path.Combine(fileUtil.GetModPath("pitFireTeam-ServerMod"), "Resources", "teammates", sessionId.ToString());
	}

	private string GetTeammateFilePath(MongoId sessionId, BotBase teammate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Path.Combine(GetTeammateDirectory(sessionId), $"{teammate.Aid}.json");
	}

	private string GetTeammateSettingsFilePath(MongoId sessionId, BotBase teammate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Path.Combine(GetTeammateDirectory(sessionId), $"{teammate.Aid}-settings.json");
	}

	private string GetDefaultEquipmentFilePath(MongoId sessionId, BotBase teammate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Path.Combine(GetTeammateDirectory(sessionId), $"{teammate.Aid}-equipment.json");
	}

	private string NormalizeRequiredValue(string? value, string fieldName)
	{
		string text = value?.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new FriendlyTeammateException("Missing teammate " + fieldName);
		}
		return text;
	}

	private int GetUniqueAccountId(MongoId sessionId)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		HashSet<int> hashSet = new HashSet<int>(from num3 in saveServer.GetProfiles().Values.Select(delegate(SptProfile profile)
			{
				SPTarkov.Server.Core.Models.Eft.Profile.Info profileInfo = profile.ProfileInfo;
				return ((profileInfo != null) ? profileInfo.Aid : ((int?)null)).GetValueOrDefault();
			})
			where num3 > 0
			select num3);
		foreach (BotBase item in LoadTeammates(sessionId))
		{
			int? aid = item.Aid;
			if (aid.HasValue && aid.GetValueOrDefault() > 0)
			{
				hashSet.Add(item.Aid.Value);
			}
		}
		string text = Path.Combine(fileUtil.GetModPath("pitFireTeam-ServerMod"), "Resources", "teammates");
		if (fileUtil.DirectoryExists(text))
		{
			foreach (int item2 in from num3 in fileUtil.GetFiles(text, true, "*.json").Where(IsTeammateProfileFile).Select(delegate(string path)
				{
					BotBase obj = jsonUtil.DeserializeFromFile<BotBase>(path);
					return ((obj != null) ? obj.Aid : ((int?)null)).GetValueOrDefault();
				})
				where num3 > 0
				select num3)
			{
				hashSet.Add(item2);
			}
		}
		for (int num = 0; num < 1024; num++)
		{
			int num2 = hashUtil.GenerateAccountId();
			if (!hashSet.Contains(num2))
			{
				return num2;
			}
		}
		throw new FriendlyTeammateException("Unable to allocate a unique teammate account id");
	}

	public int GetRecruitAccountIdOrUnique(MongoId sessionId, string? accountId)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (int.TryParse(accountId, out var result) && result > 0 && !IsAccountIdInUse(sessionId, result))
		{
			return result;
		}
		return GetUniqueAccountId(sessionId);
	}

	private bool IsAccountIdInUse(MongoId sessionId, int aid)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (saveServer.GetProfiles().Values.Any(delegate(SptProfile profile)
		{
			SPTarkov.Server.Core.Models.Eft.Profile.Info profileInfo = profile.ProfileInfo;
			return profileInfo != null && profileInfo.Aid == aid;
		}))
		{
			return true;
		}
		if (LoadTeammates(sessionId).Any((BotBase teammate) => teammate.Aid == aid))
		{
			return true;
		}
		string text = Path.Combine(fileUtil.GetModPath("pitFireTeam-ServerMod"), "Resources", "teammates");
		if (!fileUtil.DirectoryExists(text))
		{
			return false;
		}
		return fileUtil.GetFiles(text, true, "*.json").Where(IsTeammateProfileFile).Any(delegate(string path)
		{
			BotBase obj = jsonUtil.DeserializeFromFile<BotBase>(path);
			return obj != null && obj.Aid == aid;
		});
	}

	private SearchFriendResponse ToFriendSummary(BotBase teammate)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_00c2: Expected O, but got Unknown
		Info val = teammate.Info ?? throw new FriendlyTeammateException("Teammate profile is missing Info");
		return new SearchFriendResponse
		{
			Id = (teammate.Id ?? throw new FriendlyTeammateException("Teammate profile is missing Id")),
			Aid = teammate.Aid,
			Info = new UserDialogDetails
			{
				Nickname = val.Nickname,
				Side = val.Side,
				Level = val.Level,
				MemberCategory = (MemberCategory)1024,
				SelectedMemberCategory = (MemberCategory)1024
			}
		};
	}

	private object ToTeammateSummary(BotBase teammate, FriendlyTeammateSettings? settings = null)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		Info val = teammate.Info ?? throw new FriendlyTeammateException("Teammate profile is missing Info");
		if ((object)settings == null)
		{
			settings = CreateDefaultTeammateSettings();
		}
		return new
		{
			Id = (teammate.Id ?? throw new FriendlyTeammateException("Teammate profile is missing Id")),
			Aid = teammate.Aid,
			Info = new UserDialogDetails
			{
				Nickname = val.Nickname,
				Side = val.Side,
				Level = val.Level,
				MemberCategory = (MemberCategory)1024,
				SelectedMemberCategory = (MemberCategory)1024
			},
			AutoJoinEnabled = settings.AutoJoinEnabled,
			HasProperRaidKit = HasProperRaidKit(PrepareTeammateForFetch(teammate))
		};
	}

	private UserDialogInfo ToFriendDialog(BotBase teammate)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		SearchFriendResponse val = ToFriendSummary(teammate);
		return new UserDialogInfo
		{
			Id = val.Id,
			Aid = val.Aid,
			Info = val.Info
		};
	}

	private GetOtherProfileResponse ToOtherProfileResponse(BotBase teammate)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_0100: Expected O, but got Unknown
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Expected O, but got Unknown
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Expected O, but got Unknown
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected O, but got Unknown
		//IL_02b1: Expected O, but got Unknown
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Expected O, but got Unknown
		//IL_0300: Expected O, but got Unknown
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Expected O, but got Unknown
		Info val = teammate.Info ?? throw new FriendlyTeammateException("Teammate profile is missing Info");
		Customization val2 = teammate.Customization ?? new Customization();
		BotBaseInventory val3 = teammate.Inventory ?? new BotBaseInventory
		{
			Items = new List<Item>()
		};
		Stats val4 = teammate.Stats ?? new Stats
		{
			Eft = new EftStats()
		};
		GetOtherProfileResponse val5 = new GetOtherProfileResponse
		{
			Id = teammate.Id,
			Aid = teammate.Aid,
			Info = new OtherProfileInfo
			{
				Nickname = val.Nickname,
				Side = val.Side,
				Experience = val.Experience,
				MemberCategory = 1024,
				BannedState = val.BannedState,
				BannedUntil = val.BannedUntil,
				RegistrationDate = val.RegistrationDate
			}
		};
		OtherProfileCustomization val6 = new OtherProfileCustomization();
		MongoId? head = val2.Head;
		val6.Head = (head.HasValue ? (head.GetValueOrDefault()) : null);
		head = val2.Body;
		val6.Body = (head.HasValue ? (head.GetValueOrDefault()) : null);
		head = val2.Feet;
		val6.Feet = (head.HasValue ? (head.GetValueOrDefault()) : null);
		head = val2.Hands;
		val6.Hands = (head.HasValue ? (head.GetValueOrDefault()) : null);
		head = val2.DogTag;
		val6.Dogtag = (head.HasValue ? (head.GetValueOrDefault()) : null);
		head = val2.Voice;
		val6.Voice = (head.HasValue ? (head.GetValueOrDefault()) : null);
		val5.Customization = val6;
		val5.Skills = teammate.Skills;
		OtherProfileEquipment val7 = new OtherProfileEquipment();
		head = val3.Equipment;
		val7.Id = head.HasValue ? head.GetValueOrDefault().ToString() : null;
		val7.Items = val3.Items ?? new List<Item>();
		val5.Equipment = val7;
		val5.Achievements = teammate.Achievements;
		val5.FavoriteItems = new List<Item>();
		OtherProfileStats val8 = new OtherProfileStats();
		OtherProfileSubStats val9 = new OtherProfileSubStats();
		EftStats eft = val4.Eft;
		val9.TotalInGameTime = ((eft != null) ? eft.TotalInGameTime : ((long?)null));
		EftStats eft2 = val4.Eft;
		val9.OverAllCounters = ((eft2 != null) ? eft2.OverallCounters : null);
		val8.Eft = val9;
		val5.PmcStats = val8;
		OtherProfileStats val10 = new OtherProfileStats();
		OtherProfileSubStats val11 = new OtherProfileSubStats();
		EftStats eft3 = val4.Eft;
		val11.TotalInGameTime = ((eft3 != null) ? eft3.TotalInGameTime : ((long?)null));
		EftStats eft4 = val4.Eft;
		val11.OverAllCounters = ((eft4 != null) ? eft4.OverallCounters : null);
		val10.Eft = val11;
		val5.ScavStats = val10;
		val5.Hideout = teammate.Hideout ?? new Hideout();
		head = val3.HideoutCustomizationStashId;
		val5.CustomizationStash = (head.HasValue ? head.GetValueOrDefault().ToString() : null) ?? string.Empty;
		val5.HideoutAreaStashes = val3.HideoutAreaStashes ?? new Dictionary<string, MongoId>();
		val5.Items = new List<Item>();
		return val5;
	}

	private GroupCharacter ToGroupCharacter(BotBase teammate)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Expected O, but got Unknown
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Expected O, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Expected O, but got Unknown
		//IL_0250: Expected O, but got Unknown
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Expected O, but got Unknown
		Info val = teammate.Info ?? throw new FriendlyTeammateException("Teammate profile is missing Info");
		Customization val2 = teammate.Customization ?? new Customization();
		BotBaseInventory val3 = teammate.Inventory ?? new BotBaseInventory
		{
			Items = new List<Item>()
		};
		GroupCharacter val4 = new GroupCharacter();
		MongoId? id = teammate.Id;
		val4.Id = id.HasValue ? id.GetValueOrDefault().ToString() : null;
		val4.Aid = teammate.Aid;
		val4.Info = new CharacterInfo
		{
			Nickname = val.Nickname,
			SavageNickname = val.Nickname,
			Side = val.Side,
			Level = val.Level,
			MemberCategory = (MemberCategory)1024,
			GameVersion = val.GameVersion,
			HasCoopExtension = val.HasCoopExtension
		};
		PlayerVisualRepresentation val5 = new PlayerVisualRepresentation
		{
			Info = new VisualInfo
			{
				Nickname = val.Nickname,
				Side = val.Side,
				Level = val.Level,
				MemberCategory = (MemberCategory)1024,
				GameVersion = val.GameVersion
			}
		};
		SPTarkov.Server.Core.Models.Eft.Match.Customization val6 = new SPTarkov.Server.Core.Models.Eft.Match.Customization();
		id = val2.Head;
		val6.Head = (id.HasValue ? id.GetValueOrDefault().ToString() : null);
		id = val2.Body;
		val6.Body = (id.HasValue ? id.GetValueOrDefault().ToString() : null);
		id = val2.Feet;
		val6.Feet = (id.HasValue ? id.GetValueOrDefault().ToString() : null);
		id = val2.Hands;
		val6.Hands = (id.HasValue ? id.GetValueOrDefault().ToString() : null);
		val5.Customization = val6;
		Equipment val7 = new Equipment();
		id = val3.Equipment;
		val7.Id = (id.HasValue ? id.GetValueOrDefault().ToString() : null);
		val7.Items = val3.Items ?? new List<Item>();
		val5.Equipment = val7;
		val4.VisualRepresentation = val5;
		val4.IsLeader = false;
		val4.IsReady = true;
		val4.LookingGroup = false;
		val4.Region = string.Empty;
		return val4;
	}

	private void ApplyTemporaryHealthMultiplier(BotBase teammate, double? healthMultiplier)
	{
		if (!healthMultiplier.HasValue || Math.Abs(healthMultiplier.Value - 1.0) < 0.0001)
		{
			return;
		}
		BotBaseHealth health = teammate.Health;
		Dictionary<string, BodyPartHealth> dictionary = ((health != null) ? health.BodyParts : null);
		if (dictionary == null)
		{
			return;
		}
		foreach (BodyPartHealth value in dictionary.Values)
		{
			CurrentMinMax val = ((value != null) ? value.Health : null);
			if (val != null && val.Maximum.HasValue)
			{
				val.Maximum *= healthMultiplier.Value;
				val.Current = val.Maximum;
			}
		}
	}

	private bool IsTeammateProfileFile(string path)
	{
		if (!string.Equals(fileUtil.GetFileExtension(path), "json", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string fileName = Path.GetFileName(path);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
		int result;
		return int.TryParse(fileNameWithoutExtension, out result);
	}

	private FriendlyTeammateSettings GetTeammateSettings(MongoId sessionId, BotBase teammate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		string teammateSettingsFilePath = GetTeammateSettingsFilePath(sessionId, teammate);
		if (!fileUtil.FileExists(teammateSettingsFilePath))
		{
			return CreateDefaultTeammateSettings();
		}
		FriendlyTeammateSettings friendlyTeammateSettings = jsonUtil.DeserializeFromFile<FriendlyTeammateSettings>(teammateSettingsFilePath);
		FriendlyTeammateSettings friendlyTeammateSettings2 = friendlyTeammateSettings ?? CreateDefaultTeammateSettings();
		if (string.IsNullOrWhiteSpace(friendlyTeammateSettings2.SelectedLoadoutId))
		{
			friendlyTeammateSettings2.SelectedLoadoutId = "000000000000000000000000";
		}
		friendlyTeammateSettings2.Aggression = NormalizeAggression(friendlyTeammateSettings2.Aggression);
		friendlyTeammateSettings2.CombatTactic = NormalizeCombatTactic(friendlyTeammateSettings2.CombatTactic);
		return friendlyTeammateSettings2;
	}

	private static FriendlyTeammateSettings CreateDefaultTeammateSettings()
	{
		return new FriendlyTeammateSettings
		{
			SelectedLoadoutId = "000000000000000000000000",
			AutoJoinEnabled = false,
			Aggression = 50f,
			CombatTactic = "Rifleman"
		};
	}

	private void SaveTeammateSettings(MongoId sessionId, BotBase teammate, FriendlyTeammateSettings settings)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		string teammateSettingsFilePath = GetTeammateSettingsFilePath(sessionId, teammate);
		string text = jsonUtil.Serialize<FriendlyTeammateSettings>(settings, true);
		if (text == null)
		{
			throw new FriendlyTeammateException("Unable to serialize teammate settings");
		}
		fileUtil.WriteFile(teammateSettingsFilePath, text);
	}

	private void SaveDefaultEquipmentSnapshot(MongoId sessionId, BotBase teammate, bool overwrite = false, bool includeSecureContainer = false)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_0023: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		if (teammate.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			BotBaseInventory val2 = val;
			teammate.Inventory = val;
		}
		string defaultEquipmentFilePath = GetDefaultEquipmentFilePath(sessionId, teammate);
		if (overwrite || !fileUtil.FileExists(defaultEquipmentFilePath))
		{
			List<Item> list = cloner.Clone<List<Item>>(teammate.Inventory.Items ?? new List<Item>()) ?? new List<Item>();
			if (!includeSecureContainer)
			{
				RemoveSecureContainerTree(list);
			}
			BotBaseInventory inventory = teammate.Inventory;
			object preferredRootId;
			if (inventory == null)
			{
				preferredRootId = null;
			}
			else
			{
				MongoId? equipment = inventory.Equipment;
				preferredRootId = (equipment.HasValue ? equipment.GetValueOrDefault().ToString() : null);
			}
			PruneUnreachableEquipmentItems(list, (string?)preferredRootId);
			string text = jsonUtil.Serialize<List<Item>>(list, true);
			if (text == null)
			{
				throw new FriendlyTeammateException("Unable to serialize teammate default equipment");
			}
			fileUtil.WriteFile(defaultEquipmentFilePath, text);
		}
	}

	private void RestoreDefaultEquipment(MongoId sessionId, BotBase teammate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_005c: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		string defaultEquipmentFilePath = GetDefaultEquipmentFilePath(sessionId, teammate);
		if (!fileUtil.FileExists(defaultEquipmentFilePath))
		{
			throw new FriendlyTeammateException("Teammate default equipment snapshot is missing");
		}
		List<Item> list = jsonUtil.DeserializeFromFile<List<Item>>(defaultEquipmentFilePath);
		if (list == null || list.Count == 0)
		{
			throw new FriendlyTeammateException("Unable to load teammate default equipment");
		}
		if (teammate.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory();
			BotBaseInventory val2 = val;
			teammate.Inventory = val;
		}
		teammate.Inventory.Items = cloner.Clone<List<Item>>(list) ?? list;
		teammate.Inventory.Equipment = teammate.Inventory.Items.First().Id;
	}

	private List<EquipmentBuild> GetCustomEquipmentBuilds(SptProfile profile)
	{
		UserBuilds userBuildData = profile.UserBuildData;
		return ((userBuildData == null) ? null : (from build in userBuildData.EquipmentBuilds?.Where((EquipmentBuild build) => (int)build.BuildType == 0)
			where !string.IsNullOrWhiteSpace(((UserBuild)build).Name)
			select build).ToList()) ?? new List<EquipmentBuild>();
	}

	private string NormalizeCurrentLoadoutId(SptProfile profile, string? selectedLoadoutId)
	{
		if (string.IsNullOrWhiteSpace(selectedLoadoutId) || string.Equals(selectedLoadoutId, "000000000000000000000000", StringComparison.OrdinalIgnoreCase))
		{
			return "000000000000000000000000";
		}
		if (!GetCustomEquipmentBuilds(profile).Any((EquipmentBuild build) => string.Equals(((UserBuild)build).Id.ToString(), selectedLoadoutId, StringComparison.OrdinalIgnoreCase)))
		{
			return "000000000000000000000000";
		}
		return selectedLoadoutId;
	}

	private static float NormalizeAggression(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return 50f;
		}
		return Math.Clamp(value, 0f, 100f);
	}

	private static float GetDefaultAggressionForTactic(string tactic)
	{
		if (!string.Equals(tactic, "Marksman", StringComparison.OrdinalIgnoreCase))
		{
			return 50f;
		}
		return 30f;
	}

	private static string NormalizeCombatTactic(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "Rifleman";
		}
		return value.Trim().ToLowerInvariant() switch
		{
			"marksman" => "Marksman", 
			"protector" => "Rifleman", 
			"guard" => "Rifleman", 
			"holder" => "Rifleman", 
			"support" => "Rifleman", 
			"assist" => "Rifleman", 
			"rifleman" => "Rifleman", 
			"balanced" => "Rifleman", 
			"default" => "Rifleman", 
			"pusher" => "Rifleman", 
			_ => "Rifleman", 
		};
	}

	private void ApplyEquipmentBuild(BotBase teammate, EquipmentBuild equipmentBuild, PmcData playerPmc)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_007f: Expected O, but got Unknown
		if (equipmentBuild.Items == null || equipmentBuild.Items.Count == 0)
		{
			throw new FriendlyTeammateException("Teammate equipment build has no items");
		}
		List<Item> list = cloner.Clone<List<Item>>(equipmentBuild.Items) ?? equipmentBuild.Items;
		List<Item> list2 = itemHelper.ReplaceIDs((IEnumerable<Item>)list, playerPmc, (IEnumerable<InsuredItem>)null).ToList();
		MongoId id = list2.First().Id;
		if (teammate.Inventory == null)
		{
			BotBaseInventory val = new BotBaseInventory
			{
				Items = new List<Item>()
			};
			BotBaseInventory val2 = val;
			teammate.Inventory = val;
		}
		teammate.Inventory.Items = MergeEquipmentWithPreservedSpecialItems(teammate.Inventory.Items, list2);
		teammate.Inventory.Equipment = id;
	}

	private unsafe List<Item> MergeEquipmentWithPreservedSpecialItems(List<Item>? existingItems, List<Item> replacementItems, bool useReplacementSecureContainer = false)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		if (replacementItems == null || replacementItems.Count == 0)
		{
			throw new FriendlyTeammateException("Replacement teammate equipment items are missing");
		}
		List<Item> list = replacementItems.ToList();
		if (!useReplacementSecureContainer)
		{
			RemoveSecureContainerTree(list);
		}
		MongoId id = list.First().Id;
		List<Item> list2 = (from item in existingItems ?? new List<Item>()
			where !string.IsNullOrWhiteSpace(item.SlotId)
			where item.SlotId.Contains("Dogtag", StringComparison.OrdinalIgnoreCase) || (!useReplacementSecureContainer && item.SlotId.Contains("SecuredContainer", StringComparison.OrdinalIgnoreCase))
			select cloner.Clone<Item>(item) ?? item).ToList();
		List<Item> list3 = list.Where((Item item) => string.IsNullOrWhiteSpace(item.SlotId) || ((useReplacementSecureContainer || !item.SlotId.Contains("SecuredContainer", StringComparison.OrdinalIgnoreCase)) && !item.SlotId.Contains("Dogtag", StringComparison.OrdinalIgnoreCase))).ToList();
		Item? obj = list3.FirstOrDefault((Item item) => string.Equals(item.SlotId, "Pockets", StringComparison.OrdinalIgnoreCase));
		MongoId? val = ((obj != null) ? new MongoId?(obj.Id) : ((MongoId?)null));
		foreach (Item item in list2)
		{
			item.ParentId = ((item.SlotId != null && item.SlotId.Contains("SpecialSlot", StringComparison.OrdinalIgnoreCase)) ? (val ?? id) : id);
			list3.Add(item);
		}
		PruneUnreachableEquipmentItems(list3, id.ToString());
		return list3;
	}

	private static int PruneUnreachableEquipmentItems(BotBase teammate)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		object inventoryItems;
		if (teammate == null)
		{
			inventoryItems = null;
		}
		else
		{
			BotBaseInventory inventory = teammate.Inventory;
			inventoryItems = ((inventory != null) ? inventory.Items : null);
		}
		object preferredRootId;
		if (teammate == null)
		{
			preferredRootId = null;
		}
		else
		{
			BotBaseInventory inventory2 = teammate.Inventory;
			if (inventory2 == null)
			{
				preferredRootId = null;
			}
			else
			{
				MongoId? equipment = inventory2.Equipment;
				preferredRootId = (equipment.HasValue ? equipment.GetValueOrDefault().ToString() : null);
			}
		}
		return PruneUnreachableEquipmentItems((List<Item>?)inventoryItems, (string?)preferredRootId);
	}

	private static int PruneUnreachableEquipmentItems(List<Item>? inventoryItems, string? preferredRootId)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		if (inventoryItems == null || inventoryItems.Count == 0)
		{
			return 0;
		}
		object obj2;
		if (string.IsNullOrWhiteSpace(preferredRootId))
		{
			Item? obj = inventoryItems.FirstOrDefault(delegate(Item item)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				if (item == null)
				{
					return false;
				}
				_ = item.Id;
				return true;
			});
			obj2 = ((obj != null) ? obj.Id.ToString() : null);
		}
		else
		{
			obj2 = preferredRootId;
		}
		string text = (string)obj2;
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		HashSet<string> keepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { text };
		bool flag = true;
		while (flag)
		{
			flag = false;
			foreach (Item inventoryItem in inventoryItems)
			{
				if (inventoryItem != null)
				{
					_ = inventoryItem.Id;
					if (0 == 0 && !string.IsNullOrWhiteSpace(inventoryItem.ParentId) && keepIds.Contains(inventoryItem.ParentId) && keepIds.Add(inventoryItem.Id.ToString()))
					{
						flag = true;
					}
				}
			}
		}
		return inventoryItems.RemoveAll(delegate(Item item)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				if (0 == 0)
				{
					return !keepIds.Contains(item.Id.ToString());
				}
			}
			return true;
		});
	}
}
