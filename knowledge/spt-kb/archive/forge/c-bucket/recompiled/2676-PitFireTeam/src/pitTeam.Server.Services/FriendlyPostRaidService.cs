using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Models.Spt.Inventory;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Models;

namespace pitTeam.Server.Services;

[Injectable(InjectionType.Singleton)]
public class FriendlyPostRaidService(MailSendService mailSendService, NotificationSendHelper notificationSendHelper, DialogueHelper dialogueHelper, FriendlyLanguageService languageService, FriendlyTeammateService teammateService, TimeUtil timeUtil, SaveServer saveServer, InventoryHelper inventoryHelper, ItemHelper itemHelper, ISptLogger<FriendlyPostRaidService> logger)
{
	private sealed record KillMessageRecord(string Kind, string MessageText);

	private const string KillMessageKindTraitor = "traitor";

	private const string KillMessageKindJerk = "jerk";

	private static readonly ConcurrentDictionary<string, Dictionary<string, KillMessageRecord>> KillMessageRecords = new ConcurrentDictionary<string, Dictionary<string, KillMessageRecord>>();

	private static readonly ConcurrentDictionary<string, HashSet<string>> ProtectedRaidItemIds = new ConcurrentDictionary<string, HashSet<string>>();

	private static readonly string[] ReturnItemsMessages = new string[6] { "Items received from your teammate. Ready for you to claim.", "Your teammate transferred these. Awaiting your confirmation.", "Your items are attached. Select 'Receive all' to collect them.", "Transfer from your teammate completed. Items are ready to be claimed.", "All items from your teammate are prepared. Confirm to receive.", "These were handed over by your teammate. Ready for pickup." };

	private static readonly string[] TeamEscapedMessages = new string[3] { "Nice! We managed to get out.", "And that's a wrap! We made it out.", "Good run. Everyone made extract." };

	private static readonly string[] TeamSomeEscapedMessages = new string[2] { "Well it's a shame about {0}, but at least the rest of us made it.", "A few of us got clipped, but some managed to get out alive." };

	private static readonly string[] FriendlyEscapedMessages = new string[3] { "Glad we made it. Thanks for letting me tag along.", "Whew, glad I found you. Thanks for the help.", "Thanks for the help. I hauled what I could back." };

	public void HandleReturnItems(MongoId sessionId, FriendlyPostRaidReturnItemsRequest request)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		List<Item> list = request.Items ?? new List<Item>();
		StripProtectedTeammateItemsFromReturnItems(sessionId, list);
		if (list.Count != 0)
		{
			RemoveReturnedItemsFromInsurance(sessionId, list);
			UserDialogInfo deliverySender = GetDeliverySender();
			SendMessageDetails val = new SendMessageDetails
			{
				RecipientId = sessionId,
				Sender = (MessageType)2,
				DialogType = (MessageType)2,
				SenderDetails = deliverySender,
				Trader = "67d3a28a3d6f4f7dbd09ed13",
				MessageText = PickRandom(ReturnItemsMessages),
				Items = list,
				ItemsMaxStorageLifetimeSeconds = 86400L
			};
			mailSendService.SendMessageToPlayer(val);
			EnsureDialogHasSender(sessionId, deliverySender);
		}
	}

	private unsafe void StripProtectedTeammateItemsFromReturnItems(MongoId sessionId, List<Item> items)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (items.Count == 0)
		{
			return;
		}
		HashSet<string> protectedTeammateItemIdsForExtraction = teammateService.GetProtectedTeammateItemIdsForExtraction(sessionId);
		string key = sessionId.ToString();
		if (ProtectedRaidItemIds.TryGetValue(key, out HashSet<string> value))
		{
			lock (value)
			{
				protectedTeammateItemIdsForExtraction.UnionWith(value);
			}
		}
		if (protectedTeammateItemIdsForExtraction.Count != 0)
		{
			int num = RemoveItemTreesById(items, protectedTeammateItemIdsForExtraction);
			if (num > 0)
			{
				logger.Info($"Removed {num} protected teammate item(s) from returned follower-loot delivery.");
			}
		}
	}

	private void RemoveReturnedItemsFromInsurance(MongoId sessionId, List<Item> returnedItems)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		HashSet<MongoId> returnedItemIds = (from item in returnedItems
			where item != (Item)null
			select item.Id).ToHashSet();
		if (returnedItemIds.Count == 0)
		{
			return;
		}
		try
		{
			SptProfile profile = saveServer.GetProfile(sessionId);
			object obj;
			if (profile == null)
			{
				obj = null;
			}
			else
			{
				Characters characterData = profile.CharacterData;
				obj = ((characterData != null) ? characterData.PmcData : null);
			}
			PmcData val = (PmcData)obj;
			int num = 0;
			int num2 = 0;
			if (((val != null) ? ((BotBase)val).InsuredItems : null) != null)
			{
				int count = ((BotBase)val).InsuredItems.Count;
				((BotBase)val).InsuredItems = ((BotBase)val).InsuredItems.Where((InsuredItem insuredItem) => insuredItem == null || !insuredItem.ItemId.HasValue || !returnedItemIds.Contains(insuredItem.ItemId.Value)).ToList();
				num = count - ((BotBase)val).InsuredItems.Count;
			}
			if (((profile != null) ? profile.InsuranceList : null) != null)
			{
				foreach (Insurance item in profile.InsuranceList.Where((Insurance package) => ((package != null) ? package.Items : null) != null))
				{
					HashSet<MongoId> packageRemovalIds = BuildReturnedInsuranceRemovalIds(item.Items, returnedItemIds);
					if (packageRemovalIds.Count != 0)
					{
						int count2 = item.Items.Count;
						item.Items = item.Items.Where((Item item) => item != (Item)null && !packageRemovalIds.Contains(item.Id)).ToList();
						num2 += count2 - item.Items.Count;
					}
				}
			}
			if (num > 0 || num2 > 0)
			{
				logger.Info($"Removed pitFireTeam-returned item(s) from insurance tracking: active={num}, scheduled={num2}.");
			}
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to remove returned pitFireTeam item(s) from insurance tracking: " + ex.Message);
		}
	}

	private unsafe static HashSet<MongoId> BuildReturnedInsuranceRemovalIds(List<Item> insuranceItems, HashSet<MongoId> returnedItemIds)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		HashSet<MongoId> hashSet = (from val in insuranceItems
			where val != (Item)null && returnedItemIds.Contains(val.Id)
			select val.Id).ToHashSet();
		bool flag;
		do
		{
			flag = false;
			foreach (Item item in insuranceItems)
			{
				if (!(item == (Item)null) && !hashSet.Contains(item.Id) && !string.IsNullOrWhiteSpace(item.ParentId) && hashSet.Any((MongoId parentId) => string.Equals(item.ParentId, parentId.ToString(), StringComparison.Ordinal)))
				{
					flag |= hashSet.Add(item.Id);
				}
			}
		}
		while (flag);
		return hashSet;
	}

	public void HandleTeamEscaped(MongoId sessionId, FriendlyPostRaidTeamEscapedRequest request)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		FriendlyPostRaidMember member = request.Member;
		if (member == null)
		{
			return;
		}
		UserDialogInfo val = ToSenderInfo(member);
		FriendlyPostRaidSquadInfo friendlyPostRaidSquadInfo = member.SquadInfo ?? new FriendlyPostRaidSquadInfo();
		string text = PickRandom(FriendlyEscapedMessages);
		if (friendlyPostRaidSquadInfo.Mate)
		{
			text = PickRandom(TeamEscapedMessages);
			if (friendlyPostRaidSquadInfo.Partial)
			{
				string arg = "the others";
				if (friendlyPostRaidSquadInfo.Lost.Count > 0 && friendlyPostRaidSquadInfo.Lost.Count < 3)
				{
					arg = JoinNames(friendlyPostRaidSquadInfo.Lost);
				}
				text = string.Format(PickRandom(TeamSomeEscapedMessages), arg);
			}
		}
		notificationSendHelper.SendMessageToPlayerAsync(sessionId, val, text, (MessageType)1);
	}

	public unsafe void RecordKillMessage(MongoId sessionId, FriendlyPostRaidKillMessageRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.VictimProfileId) || string.IsNullOrWhiteSpace(request.MessageKind))
		{
			return;
		}
		string text = NormalizeKillMessageKind(request.MessageKind);
		if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(request.MessageText))
		{
			return;
		}
		Dictionary<string, KillMessageRecord> orAdd = KillMessageRecords.GetOrAdd(sessionId.ToString(), (string _) => new Dictionary<string, KillMessageRecord>(StringComparer.Ordinal));
		lock (orAdd)
		{
			KillMessageRecord value = new KillMessageRecord(text, request.MessageText);
			orAdd[request.VictimProfileId] = value;
			if (!string.IsNullOrWhiteSpace(request.VictimAccountId))
			{
				orAdd["aid:" + request.VictimAccountId] = value;
			}
		}
	}

	public void RegisterProtectedRaidItems(MongoId sessionId, FriendlyPostRaidProtectedItemsRequest request)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (!(request == null))
		{
			string context = request.Context ?? "client registration";
			RemoveProtectedRaidItemIds(sessionId, request.RemoveItemIds, context);
			RegisterProtectedRaidItemIds(sessionId, request.ItemIds, context);
		}
	}

	public unsafe void RegisterProtectedRaidItemIds(MongoId sessionId, IEnumerable<string>? itemIds, string context)
	{
		string[] array = itemIds?.Where((string itemId) => !string.IsNullOrWhiteSpace(itemId)).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
		if (array.Length == 0)
		{
			return;
		}
		HashSet<string> orAdd = ProtectedRaidItemIds.GetOrAdd(sessionId.ToString(), (string _) => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		lock (orAdd)
		{
			string[] array2 = array;
			foreach (string item in array2)
			{
				orAdd.Add(item);
			}
		}
		logger.Info($"Registered {array.Length} protected teammate raid item id(s). context='{context}'.");
	}

	private unsafe void RemoveProtectedRaidItemIds(MongoId sessionId, IEnumerable<string>? itemIds, string context)
	{
		string[] array = itemIds?.Where((string itemId) => !string.IsNullOrWhiteSpace(itemId)).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
		if (array.Length == 0)
		{
			return;
		}
		string key = sessionId.ToString();
		if (!ProtectedRaidItemIds.TryGetValue(key, out HashSet<string> value))
		{
			return;
		}
		int num = 0;
		lock (value)
		{
			string[] array2 = array;
			foreach (string item in array2)
			{
				if (value.Remove(item))
				{
					num++;
				}
			}
			if (value.Count == 0)
			{
				ProtectedRaidItemIds.TryRemove(key, out HashSet<string> _);
			}
		}
		if (num > 0)
		{
			logger.Info($"Unregistered {num} protected teammate raid item id(s). context='{context}'.");
		}
	}

	public unsafe void RemoveProtectedTeammateItemsFromExtractedProfile(MongoId sessionId, EndLocalRaidRequestData request)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		EndRaidResult results = request.Results;
		object obj;
		if (results == null)
		{
			obj = null;
		}
		else
		{
			PmcData profile = results.Profile;
			if (profile == null)
			{
				obj = null;
			}
			else
			{
				BotBaseInventory inventory = ((BotBase)profile).Inventory;
				obj = ((inventory != null) ? inventory.Items : null);
			}
		}
		List<Item> list = (List<Item>)obj;
		if (list == null || list.Count == 0)
		{
			ProtectedRaidItemIds.TryRemove(sessionId.ToString(), out HashSet<string> _);
			return;
		}
		string key = sessionId.ToString();
		HashSet<string> protectedTeammateItemIdsForExtraction = teammateService.GetProtectedTeammateItemIdsForExtraction(sessionId);
		if (ProtectedRaidItemIds.TryRemove(key, out HashSet<string> value2))
		{
			lock (value2)
			{
				protectedTeammateItemIdsForExtraction.UnionWith(value2);
			}
		}
		if (protectedTeammateItemIdsForExtraction.Count != 0)
		{
			int num = RemoveItemTreesById(list, protectedTeammateItemIdsForExtraction);
			int num2 = RemoveProtectedItemsFromSavedProfile(sessionId, protectedTeammateItemIdsForExtraction);
			int num3 = num + num2;
			if (num3 > 0)
			{
				logger.Info($"Removed protected teammate item(s) from extracted player inventory: request={num}, savedProfile={num2}.");
			}
		}
	}

	public void HandleEndLocalRaidKillMessages(MongoId sessionId, EndLocalRaidRequestData request)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		EndRaidResult results = request.Results;
		object obj;
		if (results == null)
		{
			obj = null;
		}
		else
		{
			PmcData profile = results.Profile;
			if (profile == null)
			{
				obj = null;
			}
			else
			{
				Stats stats = ((BotBase)profile).Stats;
				if (stats == null)
				{
					obj = null;
				}
				else
				{
					EftStats eft = stats.Eft;
					obj = ((eft == null) ? null : (from victim in eft.Victims?.Where(IsPmcVictim)
						where ((victim != null) ? victim.ProfileId : ((MongoId?)null)).HasValue
						select victim).Cast<Victim>().ToList());
				}
			}
		}
		if (obj == null)
		{
			obj = new List<Victim>();
		}
		List<Victim> list = (List<Victim>)obj;
		if (list.Count == 0)
		{
			ClearKillMessageRecords(sessionId);
			return;
		}
		long recentMessageThreshold = timeUtil.GetTimeStamp() - 60;
		foreach (Victim item in list)
		{
			KillMessageRecord postRaidKillMessageRecord = GetPostRaidKillMessageRecord(sessionId, item);
			if (!(postRaidKillMessageRecord == null))
			{
				RemoveRecentVanillaPmcResponse(sessionId, item, recentMessageThreshold);
				if (postRaidKillMessageRecord.Kind == "traitor" || postRaidKillMessageRecord.Kind == "jerk")
				{
					SendVictimMessage(sessionId, item, postRaidKillMessageRecord.MessageText);
				}
			}
		}
		ClearKillMessageRecords(sessionId);
	}

	private int RemoveItemTreesById(List<Item> inventoryItems, IEnumerable<string> rootItemIds)
	{
		if (inventoryItems.Count == 0)
		{
			return 0;
		}
		HashSet<string> hashSet = rootItemIds.Where((string id) => !string.IsNullOrWhiteSpace(id)).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (hashSet.Count == 0)
		{
			return 0;
		}
		Dictionary<string, Item> byId = inventoryItems.Where(delegate(Item item)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (item == null)
			{
				return false;
			}
			_ = item.Id;
			return true;
		}).GroupBy<Item, string>((Item item) => item.Id.ToString(), StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, Item>, string, Item>((IGrouping<string, Item> group) => group.Key, (IGrouping<string, Item> group) => group.First(), StringComparer.OrdinalIgnoreCase);
		int num = AssignFreshIdsToExtractedProtectedCopies(inventoryItems, byId, hashSet);
		if (num > 0)
		{
			logger.Info($"Assigned fresh item ids to {num} extracted protected ammo stack(s).");
		}
		if (hashSet.Count == 0)
		{
			return 0;
		}
		HashSet<string> removeIds = BuildRemovalIdClosure(inventoryItems, hashSet);
		SalvageUnprotectedChildrenIntoBackpack(inventoryItems, hashSet, removeIds);
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
		});
	}

	private bool IsAmmoItem(Item? item)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (item != null)
		{
			_ = item.Template;
			MongoId template = item.Template;
			if (!template.IsEmpty)
			{
				return itemHelper.IsOfBaseclass(item.Template, BaseClasses.AMMO);
			}
		}
		return false;
	}

	private int AssignFreshIdsToExtractedProtectedCopies(List<Item> inventoryItems, Dictionary<string, Item> byId, HashSet<string> protectedIds)
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = protectedIds.Where((string id) => byId.TryGetValue(id, out Item value4) && IsCopyableExtractedProtectedItem(value4) && !HasProtectedAncestor(value4, byId, protectedIds)).ToList();
		if (list.Count == 0)
		{
			return 0;
		}
		HashSet<string> hashSet = (from item in inventoryItems.Where(delegate(Item item)
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
		int num = 0;
		foreach (string item in list)
		{
			if (!byId.TryGetValue(item, out Item value) || value == null)
			{
				continue;
			}
			_ = value.Id;
			if (false || !string.Equals(value.Id.ToString(), item, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			HashSet<string> hashSet2 = BuildItemTreeIds(inventoryItems, item);
			if (hashSet2.Count == 0)
			{
				continue;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (string item2 in hashSet2)
			{
				if (byId.ContainsKey(item2))
				{
					string text;
					do
					{
						text = new MongoId().ToString();
					}
					while (!hashSet.Add(text));
					dictionary[item2] = text;
				}
			}
			if (dictionary.Count == 0)
			{
				continue;
			}
			foreach (var (key, text4) in dictionary)
			{
				if (byId.TryGetValue(key, out Item value2) && value2 != (Item)null)
				{
					value2.Id = new MongoId(text4);
				}
			}
			foreach (Item inventoryItem in inventoryItems)
			{
				if (!string.IsNullOrWhiteSpace((inventoryItem != null) ? inventoryItem.ParentId : null) && dictionary.TryGetValue(inventoryItem.ParentId, out var value3))
				{
					inventoryItem.ParentId = value3;
				}
			}
			foreach (string key2 in dictionary.Keys)
			{
				protectedIds.Remove(key2);
			}
			num++;
		}
		return num;
	}

	private bool IsCopyableExtractedProtectedItem(Item? item)
	{
		return IsAmmoItem(item);
	}

	private static bool HasProtectedAncestor(Item? item, Dictionary<string, Item> byId, HashSet<string> protectedIds)
	{
		string text = ((item != null) ? item.ParentId : null);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
		{
			if (protectedIds.Contains(text))
			{
				return true;
			}
			text = (byId.TryGetValue(text, out Item value) ? value.ParentId : null);
		}
		return false;
	}

	private static HashSet<string> BuildItemTreeIds(List<Item> inventoryItems, string rootId)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddItemTreeIds(inventoryItems, rootId, hashSet);
		return hashSet;
	}

	private static void AddItemTreeIds(List<Item> inventoryItems, string itemId, HashSet<string> treeIds)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (!treeIds.Add(itemId))
		{
			return;
		}
		foreach (Item item in inventoryItems.Where(delegate(Item item)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				return string.Equals(item.ParentId, itemId, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}).ToList())
		{
			AddItemTreeIds(inventoryItems, item.Id.ToString(), treeIds);
		}
	}

	private static HashSet<string> BuildRemovalIdClosure(List<Item> inventoryItems, HashSet<string> rootItemIds)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		HashSet<string> hashSet = new HashSet<string>(rootItemIds, StringComparer.OrdinalIgnoreCase);
		bool flag = true;
		while (flag)
		{
			flag = false;
			foreach (Item inventoryItem in inventoryItems)
			{
				if (inventoryItem != null)
				{
					_ = inventoryItem.Id;
					if (0 == 0 && !string.IsNullOrWhiteSpace(inventoryItem.ParentId) && hashSet.Contains(inventoryItem.ParentId))
					{
						flag |= hashSet.Add(inventoryItem.Id.ToString());
					}
				}
			}
		}
		return hashSet;
	}

	private int SalvageUnprotectedChildrenIntoBackpack(List<Item> inventoryItems, HashSet<string> protectedIds, HashSet<string> removeIds)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		if (inventoryItems.Count == 0 || protectedIds.Count == 0 || removeIds.Count == 0)
		{
			return 0;
		}
		Item val = FindPlayerBackpack(inventoryItems);
		if (val != null)
		{
			_ = val.Id;
			if (0 == 0 && !removeIds.Contains(val.Id.ToString()))
			{
				Dictionary<string, Item> byId = inventoryItems.Where(delegate(Item item)
				{
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					if (item == null)
					{
						return false;
					}
					_ = item.Id;
					return true;
				}).GroupBy<Item, string>((Item item) => item.Id.ToString(), StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, Item>, string, Item>((IGrouping<string, Item> group) => group.Key, (IGrouping<string, Item> group) => group.First(), StringComparer.OrdinalIgnoreCase);
				Dictionary<string, List<Item>> byParent = inventoryItems.Where(delegate(Item item)
				{
					//IL_0004: Unknown result type (might be due to invalid IL or missing references)
					if (item != null)
					{
						_ = item.Id;
						return !string.IsNullOrWhiteSpace(item.ParentId);
					}
					return false;
				}).GroupBy<Item, string>((Item item) => item.ParentId, StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, Item>, string, List<Item>>((IGrouping<string, Item> group) => group.Key, (IGrouping<string, Item> group) => group.ToList(), StringComparer.OrdinalIgnoreCase);
				int num = 0;
				{
					foreach (Item item in inventoryItems.ToList())
					{
						if (item == null)
						{
							continue;
						}
						_ = item.Id;
						if (false || string.IsNullOrWhiteSpace(item.ParentId) || protectedIds.Contains(item.Id.ToString()) || !removeIds.Contains(item.Id.ToString()) || !removeIds.Contains(item.ParentId) || IsAmmoItem(item) || HasUnprotectedRemovedAncestor(item, byId, protectedIds, removeIds))
						{
							continue;
						}
						List<Item> list = BuildSalvageTree(item, byParent, protectedIds);
						if (list.Count == 0 || !TryPlaceItemTreeInBackpack(inventoryItems, val, list, removeIds))
						{
							continue;
						}
						foreach (Item item2 in list)
						{
							if (item2 != null)
							{
								_ = item2.Id;
								removeIds.Remove(item2.Id.ToString());
							}
						}
						num++;
					}
					return num;
				}
			}
		}
		return 0;
	}

	private static Item? FindPlayerBackpack(List<Item> inventoryItems)
	{
		Dictionary<string, Item> byId = inventoryItems.Where(delegate(Item item)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (item == null)
			{
				return false;
			}
			_ = item.Id;
			return true;
		}).GroupBy<Item, string>((Item item) => item.Id.ToString(), StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, Item>, string, Item>((IGrouping<string, Item> group) => group.Key, (IGrouping<string, Item> group) => group.First(), StringComparer.OrdinalIgnoreCase);
		return inventoryItems.FirstOrDefault(delegate(Item item)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				_ = item.Id;
				if (string.Equals(item.SlotId, "Backpack", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.ParentId) && byId.TryGetValue(item.ParentId, out Item value))
				{
					return string.IsNullOrWhiteSpace(value.ParentId);
				}
			}
			return false;
		});
	}

	private static bool HasUnprotectedRemovedAncestor(Item item, Dictionary<string, Item> byId, HashSet<string> protectedIds, HashSet<string> removeIds)
	{
		string text = item.ParentId;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (!string.IsNullOrWhiteSpace(text) && removeIds.Contains(text) && hashSet.Add(text))
		{
			if (!protectedIds.Contains(text))
			{
				return true;
			}
			text = (byId.TryGetValue(text, out Item value) ? value.ParentId : null);
		}
		return false;
	}

	private static List<Item> BuildSalvageTree(Item root, Dictionary<string, List<Item>> byParent, HashSet<string> protectedIds)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		List<Item> list = new List<Item>();
		Queue<Item> queue = new Queue<Item>();
		queue.Enqueue(root);
		while (queue.Count > 0)
		{
			Item val = queue.Dequeue();
			if (val == null)
			{
				continue;
			}
			_ = val.Id;
			if (false || protectedIds.Contains(val.Id.ToString()))
			{
				continue;
			}
			list.Add(val);
			if (!byParent.TryGetValue(val.Id.ToString(), out List<Item> value))
			{
				continue;
			}
			foreach (Item item in value)
			{
				queue.Enqueue(item);
			}
		}
		return list;
	}

	private bool TryPlaceItemTreeInBackpack(List<Item> inventoryItems, Item backpack, List<Item> itemTree, HashSet<string> removeIds)
	{
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (backpack != null)
			{
				_ = backpack.Id;
				if (0 == 0 && itemTree.Count != 0)
				{
					int[,] containerSlotMap = inventoryHelper.GetContainerSlotMap(backpack.Template);
					int length = containerSlotMap.GetLength(1);
					int length2 = containerSlotMap.GetLength(0);
					List<Item> list = inventoryItems.Where(delegate(Item item)
					{
						//IL_0004: Unknown result type (might be due to invalid IL or missing references)
						//IL_0011: Unknown result type (might be due to invalid IL or missing references)
						//IL_0016: Unknown result type (might be due to invalid IL or missing references)
						if (item != null)
						{
							_ = item.Id;
							return !removeIds.Contains(item.Id.ToString());
						}
						return false;
					}).ToList();
					int[,] containerMap = inventoryHelper.GetContainerMap(length, length2, (IEnumerable<Item>)list, backpack.Id);
					FindSlotResult val = inventoryHelper.PlaceItemInContainer(containerMap, itemTree, backpack.Id.ToString(), "main");
					return val.Success ?? false;
				}
			}
			return false;
		}
		catch (Exception ex)
		{
			ISptLogger<FriendlyPostRaidService> obj = logger;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(63, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Failed to salvage teammate-linked child item '");
			Item? obj2 = itemTree.FirstOrDefault();
			defaultInterpolatedStringHandler.AppendFormatted((obj2 != null) ? new MongoId?(obj2.Id) : ((MongoId?)null));
			defaultInterpolatedStringHandler.AppendLiteral("' into backpack: ");
			defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
			obj.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
			return false;
		}
	}

	private int RemoveProtectedItemsFromSavedProfile(MongoId sessionId, HashSet<string> protectedIds)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			SptProfile profile = saveServer.GetProfile(sessionId);
			object obj;
			if (profile == null)
			{
				obj = null;
			}
			else
			{
				Characters characterData = profile.CharacterData;
				if (characterData == null)
				{
					obj = null;
				}
				else
				{
					PmcData pmcData = characterData.PmcData;
					if (pmcData == null)
					{
						obj = null;
					}
					else
					{
						BotBaseInventory inventory = ((BotBase)pmcData).Inventory;
						obj = ((inventory != null) ? inventory.Items : null);
					}
				}
			}
			List<Item> list = (List<Item>)obj;
			return (list != null) ? RemoveItemTreesById(list, protectedIds) : 0;
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to strip protected teammate item(s) from saved profile fallback: " + ex.Message);
			return 0;
		}
	}

	public void HandleDeathEscapeSummary(MongoId sessionId, FriendlyTeammateDeathEscapeSummary summary)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Expected O, but got Unknown
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		if (summary.EscapedNames.Count != 0 || summary.LostNames.Count != 0)
		{
			Dictionary<string, string> stringMap = languageService.GetStringMap(sessionId, "deathEscape");
			string languageValue = GetLanguageValue(stringMap, "MadeItOut");
			string languageValue2 = GetLanguageValue(stringMap, "Lost");
			string languageValue3 = GetLanguageValue(stringMap, "ExtractRoute");
			List<string> list = new List<string>();
			if (summary.EscapedNames.Count > 0)
			{
				list.Add(string.Format(languageValue, JoinNames(summary.EscapedNames)));
			}
			if (summary.LostNames.Count > 0)
			{
				list.Add(string.Format(languageValue2, JoinNames(summary.LostNames)));
			}
			if (!string.IsNullOrWhiteSpace(summary.ExtractName))
			{
				list.Add(string.Format(languageValue3, summary.ExtractName));
			}
			string[] stringArray = languageService.GetStringArray(sessionId, "deathEscapeMessages");
			string format = ((stringArray.Length != 0) ? PickRandom(stringArray) : "{0}");
			string messageText = string.Format(format, string.Join("\n", list));
			UserDialogInfo deliverySender = GetDeliverySender();
			SendMessageDetails val = new SendMessageDetails
			{
				RecipientId = sessionId,
				Sender = (MessageType)2,
				DialogType = (MessageType)2,
				SenderDetails = deliverySender,
				Trader = "67d3a28a3d6f4f7dbd09ed13",
				MessageText = messageText
			};
			mailSendService.SendMessageToPlayer(val);
			EnsureDialogHasSender(sessionId, deliverySender);
		}
	}

	private KillMessageRecord? GetPostRaidKillMessageRecord(MongoId sessionId, Victim victim)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (teammateService.IsTeammateIdentity(sessionId, victim.ProfileId, victim.AccountId))
		{
			return new KillMessageRecord("teammate", string.Empty);
		}
		return GetRecordedKillMessageRecord(sessionId, victim);
	}

	private unsafe KillMessageRecord? GetRecordedKillMessageRecord(MongoId sessionId, Victim victim)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if (!KillMessageRecords.TryGetValue(sessionId.ToString(), out Dictionary<string, KillMessageRecord> value))
		{
			return null;
		}
		lock (value)
		{
			if (victim.ProfileId.HasValue && value.TryGetValue(victim.ProfileId.Value.ToString(), out var value2))
			{
				return value2;
			}
			if (!string.IsNullOrWhiteSpace(victim.AccountId) && value.TryGetValue("aid:" + victim.AccountId, out var value3))
			{
				return value3;
			}
		}
		return null;
	}

	private void RemoveRecentVanillaPmcResponse(MongoId sessionId, Victim victim, long recentMessageThreshold)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (!victim.ProfileId.HasValue)
		{
			return;
		}
		try
		{
			Dictionary<MongoId, Dialogue> dialogsForProfile = dialogueHelper.GetDialogsForProfile(sessionId);
			if (!dialogsForProfile.TryGetValue(victim.ProfileId.Value, out var value) || value.Messages == null)
			{
				return;
			}
			int num = value.Messages.RemoveAll((Message message) => message.UserId == victim.ProfileId.Value && (int)message.MessageType.GetValueOrDefault() == 1 && message.Items == null && message.DateTime >= recentMessageThreshold);
			if (num > 0)
			{
				int? num2 = value.New;
				if (num2.HasValue && num2.GetValueOrDefault() > 0)
				{
					value.New = Math.Max(0, value.New.Value - num);
				}
			}
		}
		catch (Exception ex)
		{
			logger.Warning($"Failed to remove vanilla PMC response for victim '{victim.ProfileId}': {ex.Message}");
		}
	}

	private void SendVictimMessage(MongoId sessionId, Victim victim, string message)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (victim.ProfileId.HasValue && !string.IsNullOrWhiteSpace(message))
		{
			notificationSendHelper.SendMessageToPlayerAsync(sessionId, ToSenderInfo(victim), message, (MessageType)1);
		}
	}

	private static UserDialogInfo ToSenderInfo(Victim victim)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_00d0: Expected O, but got Unknown
		int result = 0;
		if (!string.IsNullOrWhiteSpace(victim.AccountId))
		{
			int.TryParse(victim.AccountId, out result);
		}
		return new UserDialogInfo
		{
			Id = victim.ProfileId.Value,
			Aid = result,
			Info = new UserDialogDetails
			{
				Nickname = (string.IsNullOrWhiteSpace(victim.Name) ? "PMC" : victim.Name),
				Side = (string.IsNullOrWhiteSpace(victim.Side) ? "Usec" : victim.Side),
				Level = (victim.Level ?? 1.0),
				MemberCategory = (MemberCategory)1024,
				SelectedMemberCategory = (MemberCategory)1024
			}
		};
	}

	private static bool IsPmcVictim(Victim? victim)
	{
		if (!string.Equals((victim != null) ? victim.Role : null, "pmcBEAR", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals((victim != null) ? victim.Role : null, "pmcUSEC", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string NormalizeKillMessageKind(string? kind)
	{
		string text = kind?.Trim().ToLowerInvariant();
		if (!(text == "traitor"))
		{
			if (text == "jerk")
			{
				return "jerk";
			}
			return string.Empty;
		}
		return "traitor";
	}

	private unsafe static void ClearKillMessageRecords(MongoId sessionId)
	{
		KillMessageRecords.TryRemove(sessionId.ToString(), out Dictionary<string, KillMessageRecord> _);
	}

	private void EnsureDialogHasSender(MongoId sessionId, UserDialogInfo sender)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Dictionary<MongoId, Dialogue> dialogsForProfile = dialogueHelper.GetDialogsForProfile(sessionId);
			if (dialogsForProfile.TryGetValue(sender.Id, out var value) && value != null)
			{
				Dialogue val = value;
				if (val.Users == null)
				{
					List<UserDialogInfo> list = (val.Users = new List<UserDialogInfo>());
				}
				if (value.Users.All((UserDialogInfo user) => user.Id != sender.Id))
				{
					value.Users.Add(sender);
				}
			}
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to ensure sender details in post-raid dialog: " + ex.Message);
		}
	}

	private static UserDialogInfo GetDeliverySender()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_0067: Expected O, but got Unknown
		return new UserDialogInfo
		{
			Id = FriendlyCourierTraderProfile.CourierTraderId,
			Aid = 1113680,
			Info = new UserDialogDetails
			{
				Nickname = "-P|T- Comms",
				Side = "Usec",
				Level = 1.0,
				MemberCategory = (MemberCategory)4,
				SelectedMemberCategory = (MemberCategory)4
			}
		};
	}

	private static UserDialogInfo ToSenderInfo(FriendlyPostRaidMember member)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		int result = 0;
		if (!string.IsNullOrWhiteSpace(member.Aid))
		{
			int.TryParse(member.Aid, out result);
		}
		if (!TryParseMongoId(member.Id, out var mongoId))
		{
			mongoId = new MongoId("67b0f29e151899410b04aacb");
		}
		return new UserDialogInfo
		{
			Id = mongoId,
			Aid = result,
			Info = (UserDialogDetails)(((object)member.Info) ?? ((object)new UserDialogDetails
			{
				Nickname = "Squadmate",
				Side = "Usec",
				Level = 1.0,
				MemberCategory = (MemberCategory)1024,
				SelectedMemberCategory = (MemberCategory)1024
			}))
		};
	}

	private static bool TryParseMongoId(string? value, out MongoId mongoId)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrWhiteSpace(value))
		{
			try
			{
				mongoId = new MongoId(value);
				return true;
			}
			catch
			{
			}
		}
		mongoId = default(MongoId);
		return false;
	}

	private static string JoinNames(List<string> names)
	{
		if (names.Count == 0)
		{
			return string.Empty;
		}
		if (names.Count == 1)
		{
			return names[0];
		}
		if (names.Count == 2)
		{
			return names[0] + " and " + names[1];
		}
		return string.Join(", ", names.Take(names.Count - 1)) + ", and " + names[names.Count - 1];
	}

	private static string PickRandom(string[] values)
	{
		if (values.Length == 0)
		{
			return string.Empty;
		}
		int num = Random.Shared.Next(values.Length);
		return values[num];
	}

	private static string GetLanguageValue(Dictionary<string, string> values, string key)
	{
		if (!values.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
		{
			return "{0}";
		}
		return value;
	}
}



