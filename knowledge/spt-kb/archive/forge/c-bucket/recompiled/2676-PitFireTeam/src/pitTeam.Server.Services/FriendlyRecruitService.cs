using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Models;
using Info = SPTarkov.Server.Core.Models.Eft.Common.Tables.Info;
using Path = System.IO.Path;

namespace pitTeam.Server.Services;

[Injectable(InjectionType.Singleton)]
public class FriendlyRecruitService(FileUtil fileUtil, JsonUtil jsonUtil, ProfileHelper profileHelper, FriendlyTeammateService teammateService, ISptLogger<FriendlyRecruitService> logger)
{
	private const string ModFolderName = "pitFireTeam-ServerMod";

	private const string RecruitRequestsFileName = "recruit-requests.json";

	private const bool ForceRecruitPickupInviteForTesting = false;

	public void QueueRecruitPickups(MongoId sessionId, List<FriendlyRecruitPickupCandidate>? candidates)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		if (candidates == null || candidates.Count == 0)
		{
			logger.Warning($"Recruit pickup request for session '{sessionId}' contained no candidates.", (Exception)null);
			return;
		}
		PmcData pmcProfile = profileHelper.GetPmcProfile(sessionId);
		int? obj;
		if (pmcProfile == null)
		{
			obj = null;
		}
		else
		{
			Info info = ((BotBase)pmcProfile).Info;
			obj = ((info != null) ? info.Level : ((int?)null));
		}
		int playerLevel = obj ?? 1;
		List<FriendlyRecruitRequestEntry> list = new List<FriendlyRecruitRequestEntry>();
		foreach (FriendlyRecruitPickupCandidate candidate in candidates)
		{
			if (!IsValidCandidate(candidate))
			{
				logger.Warning($"Skipped invalid recruit pickup candidate for session '{sessionId}'. profileId='{candidate?.ProfileId ?? string.Empty}' nickname='{candidate?.Nickname ?? string.Empty}' voice='{candidate?.Voice ?? string.Empty}' head='{candidate?.Head ?? string.Empty}'", (Exception)null);
			}
			else if (Random.Shared.Next(0, 101) <= CalculateRecruitChance(playerLevel, Math.Max(1, candidate.Level)))
			{
				list.Add(new FriendlyRecruitRequestEntry
				{
					ProfileId = candidate.ProfileId,
					AccountId = (candidate.AccountId?.Trim() ?? string.Empty),
					Nickname = candidate.Nickname.Trim(),
					Level = Math.Max(1, candidate.Level),
					Side = (candidate.Side?.Trim() ?? string.Empty),
					Voice = candidate.Voice.Trim(),
					Head = candidate.Head.Trim(),
					ProfileJson = (candidate.ProfileJson?.Trim() ?? string.Empty),
					CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
				});
			}
		}
		if (list.Count == 0)
		{
			logger.Warning($"Recruit pickup request for session '{sessionId}' had {candidates.Count} candidate(s), but none passed validation/chance.", (Exception)null);
			return;
		}
		List<FriendlyRecruitRequestEntry> list2 = LoadRecruitRequests(sessionId);
		FriendlyRecruitRequestEntry picked = list[Random.Shared.Next(list.Count)];
		if (list2.Any((FriendlyRecruitRequestEntry entry) => string.Equals(entry.ProfileId, picked.ProfileId, StringComparison.Ordinal)))
		{
			logger.Info($"Skipped duplicate recruit pickup request '{picked.Nickname}' for session '{sessionId}'", (Exception)null);
		}
		else
		{
			EnsureRecruitRequestAccountId(sessionId, list2, picked);
			list2.Add(picked);
			SaveRecruitRequests(sessionId, list2);
			logger.Info($"Queued recruit pickup request '{picked.Nickname}' for session '{sessionId}'", (Exception)null);
		}
	}

	public unsafe List<FriendlySocialFriendRequestEntry> ListRecruitFriendRequests(MongoId sessionId)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		List<FriendlyRecruitRequestEntry> list = LoadRecruitRequests(sessionId);
		if (list.Count == 0)
		{
			return new List<FriendlySocialFriendRequestEntry>();
		}
		if (EnsureRecruitRequestAccountIds(sessionId, list))
		{
			SaveRecruitRequests(sessionId, list);
		}
		string toId = sessionId.ToString();
		return list.Select((FriendlyRecruitRequestEntry entry) => new FriendlySocialFriendRequestEntry
		{
			Id = "pitfireteam-recruit-" + entry.ProfileId,
			From = entry.ProfileId,
			To = toId,
			Date = entry.CreatedAt,
			Profile = CreateRecruitRequestMember(entry)
		}).ToList();
	}

	public bool TryGetRecruitProfile(MongoId sessionId, string? accountId, out GetOtherProfileResponse? profile)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		profile = null;
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return false;
		}
		List<FriendlyRecruitRequestEntry> list = LoadRecruitRequests(sessionId);
		if (EnsureRecruitRequestAccountIds(sessionId, list))
		{
			SaveRecruitRequests(sessionId, list);
		}
		FriendlyRecruitRequestEntry friendlyRecruitRequestEntry = list.FirstOrDefault((FriendlyRecruitRequestEntry entry) => IsRecruitIdentityMatch(entry, accountId));
		if (friendlyRecruitRequestEntry == null)
		{
			return false;
		}
		return teammateService.TryGetRecruitCandidateProfile(sessionId, friendlyRecruitRequestEntry, out profile);
	}

	public bool AcceptRecruitRequest(MongoId sessionId, string? profileId)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(profileId))
		{
			return false;
		}
		List<FriendlyRecruitRequestEntry> list = LoadRecruitRequests(sessionId);
		FriendlyRecruitRequestEntry friendlyRecruitRequestEntry = list.FirstOrDefault((FriendlyRecruitRequestEntry entry) => string.Equals(entry.ProfileId, profileId, StringComparison.Ordinal));
		if (friendlyRecruitRequestEntry == null)
		{
			return false;
		}
		teammateService.CreateTeammateFromRecruitCandidate(sessionId, friendlyRecruitRequestEntry);
		list.Remove(friendlyRecruitRequestEntry);
		SaveRecruitRequests(sessionId, list);
		return true;
	}

	public bool AcceptAllRecruitRequests(MongoId sessionId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		List<FriendlyRecruitRequestEntry> list = LoadRecruitRequests(sessionId);
		if (list.Count == 0)
		{
			return false;
		}
		foreach (FriendlyRecruitRequestEntry item in list.ToList())
		{
			teammateService.CreateTeammateFromRecruitCandidate(sessionId, item);
		}
		SaveRecruitRequests(sessionId, new List<FriendlyRecruitRequestEntry>());
		return true;
	}

	public bool DeclineRecruitRequest(MongoId sessionId, string? profileId)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(profileId))
		{
			return false;
		}
		List<FriendlyRecruitRequestEntry> list = LoadRecruitRequests(sessionId);
		bool flag = list.RemoveAll((FriendlyRecruitRequestEntry entry) => string.Equals(entry.ProfileId, profileId, StringComparison.Ordinal)) > 0;
		if (flag)
		{
			SaveRecruitRequests(sessionId, list);
		}
		return flag;
	}

	private List<FriendlyRecruitRequestEntry> LoadRecruitRequests(MongoId sessionId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		string recruitRequestsFilePath = GetRecruitRequestsFilePath(sessionId);
		if (!fileUtil.FileExists(recruitRequestsFilePath))
		{
			return new List<FriendlyRecruitRequestEntry>();
		}
		return jsonUtil.DeserializeFromFile<List<FriendlyRecruitRequestEntry>>(recruitRequestsFilePath) ?? new List<FriendlyRecruitRequestEntry>();
	}

	private void SaveRecruitRequests(MongoId sessionId, List<FriendlyRecruitRequestEntry> requests)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		string text = jsonUtil.Serialize<List<FriendlyRecruitRequestEntry>>(requests, true);
		if (text != null)
		{
			fileUtil.WriteFile(GetRecruitRequestsFilePath(sessionId), text);
		}
	}

	private string GetRecruitRequestsFilePath(MongoId sessionId)
	{
		return Path.Combine(fileUtil.GetModPath("pitFireTeam-ServerMod"), "Resources", "teammates", sessionId.ToString(), "recruit-requests.json");
	}

	private static bool IsValidCandidate(FriendlyRecruitPickupCandidate candidate)
	{
		if (candidate != null && !string.IsNullOrWhiteSpace(candidate.ProfileId) && !string.IsNullOrWhiteSpace(candidate.Nickname) && !string.IsNullOrWhiteSpace(candidate.Voice))
		{
			return !string.IsNullOrWhiteSpace(candidate.Head);
		}
		return false;
	}

	private static bool IsRecruitIdentityMatch(FriendlyRecruitRequestEntry entry, string accountId)
	{
		if (!string.Equals(entry.ProfileId, accountId, StringComparison.Ordinal) && !string.Equals(entry.AccountId, accountId, StringComparison.Ordinal))
		{
			return string.Equals("pitfireteam-recruit-" + entry.ProfileId, accountId, StringComparison.Ordinal);
		}
		return true;
	}

	private bool EnsureRecruitRequestAccountIds(MongoId sessionId, List<FriendlyRecruitRequestEntry> requests)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		foreach (FriendlyRecruitRequestEntry request in requests)
		{
			flag |= EnsureRecruitRequestAccountId(sessionId, requests, request);
		}
		return flag;
	}

	private bool EnsureRecruitRequestAccountId(MongoId sessionId, List<FriendlyRecruitRequestEntry> pending, FriendlyRecruitRequestEntry request)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if (int.TryParse(request.AccountId, out var result) && result > 0)
		{
			return false;
		}
		HashSet<int> hashSet = (from entry in pending
			where (object)entry != request
			select int.TryParse(entry.AccountId, out var result2) ? result2 : 0 into aid
			where aid > 0
			select aid).ToHashSet();
		int recruitAccountIdOrUnique;
		do
		{
			recruitAccountIdOrUnique = teammateService.GetRecruitAccountIdOrUnique(sessionId, request.AccountId);
		}
		while (hashSet.Contains(recruitAccountIdOrUnique));
		request.AccountId = recruitAccountIdOrUnique.ToString();
		return true;
	}

	private FriendlySocialFriendProfile CreateRecruitRequestMember(FriendlyRecruitRequestEntry entry)
	{
		return new FriendlySocialFriendProfile
		{
			Id = entry.ProfileId,
			Aid = entry.AccountId,
			Info = new FriendlySocialFriendInfo
			{
				Nickname = entry.Nickname,
				Side = (string.Equals(entry.Side, "Bear", StringComparison.OrdinalIgnoreCase) ? "Bear" : "Usec"),
				Level = entry.Level,
				MemberCategory = (MemberCategory)1024,
				SelectedMemberCategory = (MemberCategory)1024,
				Ignored = false,
				Banned = false
			}
		};
	}

	private static int CalculateRecruitChance(int playerLevel, int botLevel)
	{
		int num = botLevel - playerLevel;
		if (num >= 10)
		{
			return 0;
		}
		if (num <= -10)
		{
			return 100;
		}
		return (int)Math.Round((1.0 - (double)(num + 10) / 20.0) * 100.0);
	}
}
