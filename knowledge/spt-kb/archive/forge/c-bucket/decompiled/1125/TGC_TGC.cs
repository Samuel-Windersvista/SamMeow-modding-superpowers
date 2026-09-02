using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Json;
using TGC.Models;
using WTTServerCommonLib;

namespace TGC;

[RequiredMember]
[Injectable(/*Could not decode attribute arguments.*/)]
public class TGC(ISptLogger<TGC> logger, WTTServerCommonLib wttCommon, ModHelper modHelper, DatabaseService databaseService) : IOnLoad
{
	[StructLayout((LayoutKind)3)]
	[CompilerGenerated]
	private struct <OnLoad>d__16 : IAsyncStateMachine
	{
		public int <>1__state;

		public AsyncTaskMethodBuilder <>t__builder;

		public TGC <>4__this;

		private MongoId <armbandId>5__2;

		private MongoId <traderIdPainter>5__3;

		private List<MongoId> <secureContainerIds>5__4;

		private TaskAwaiter <>u__1;

		private void MoveNext()
		{
			//IL_0387: Unknown result type (might be due to invalid IL or missing references)
			//IL_0393: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_020d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0212: Unknown result type (might be due to invalid IL or missing references)
			//IL_0352: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			//IL_035f: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01af: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0218: Unknown result type (might be due to invalid IL or missing references)
			//IL_021d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0221: Unknown result type (might be due to invalid IL or missing references)
			//IL_0226: Unknown result type (might be due to invalid IL or missing references)
			//IL_022f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_0237: Unknown result type (might be due to invalid IL or missing references)
			//IL_0239: Unknown result type (might be due to invalid IL or missing references)
			//IL_0241: Unknown result type (might be due to invalid IL or missing references)
			//IL_024b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0251: Unknown result type (might be due to invalid IL or missing references)
			//IL_025e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0291: Unknown result type (might be due to invalid IL or missing references)
			//IL_0296: Unknown result type (might be due to invalid IL or missing references)
			//IL_029c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_031d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0322: Unknown result type (might be due to invalid IL or missing references)
			//IL_0337: Unknown result type (might be due to invalid IL or missing references)
			//IL_0339: Unknown result type (might be due to invalid IL or missing references)
			int num = <>1__state;
			TGC tGC = <>4__this;
			try
			{
				TaskAwaiter val;
				TaskAwaiter val2;
				if (num != 0)
				{
					if (num == 1)
					{
						val = <>u__1;
						<>u__1 = default(TaskAwaiter);
						num = (<>1__state = -1);
						goto IL_036e;
					}
					tGC.DatabaseService = tGC.databaseService;
					tGC.Items = tGC.DatabaseService.GetItems();
					tGC.Traders = tGC.DatabaseService.GetTraders();
					tGC.CustomizationItems = tGC.DatabaseService.GetTemplates().Customization;
					tGC.GlobalLocales = tGC.DatabaseService.GetLocales().Global;
					Assembly executingAssembly = Assembly.GetExecutingAssembly();
					<armbandId>5__2 = ItemTpl.INVENTORY_DEFAULT;
					<traderIdPainter>5__3 = MongoId.op_Implicit("668aaff35fd574b6dcc4a686");
					List<MongoId> obj = new List<MongoId>(9);
					obj.Add(ItemTpl.SECURE_CONTAINER_ALPHA);
					obj.Add(ItemTpl.SECURE_CONTAINER_BETA);
					obj.Add(ItemTpl.SECURE_CONTAINER_EPSILON);
					obj.Add(ItemTpl.SECURE_CONTAINER_GAMMA);
					obj.Add(ItemTpl.SECURE_CONTAINER_GAMMA_TUE);
					obj.Add(ItemTpl.SECURE_TOURNAMENT_SECURED_CONTAINER);
					obj.Add(ItemTpl.SECURE_CONTAINER_KAPPA);
					obj.Add(ItemTpl.SECURE_CONTAINER_KAPPA_DESECRATED);
					obj.Add(ItemTpl.SECURE_CONTAINER_THETA);
					<secureContainerIds>5__4 = obj;
					string absolutePathToModFolder = tGC.modHelper.GetAbsolutePathToModFolder(executingAssembly);
					tGC.ModPresets = tGC.modHelper.GetJsonDataFromFile<ModPresets>(absolutePathToModFolder, "db/globals.json");
					tGC.ModConfig = tGC.modHelper.GetJsonDataFromFile<ModConfig>(absolutePathToModFolder, "config/config.json");
					tGC.ModItems = tGC.modHelper.GetJsonDataFromFile<Dictionary<MongoId, ModItems>>(absolutePathToModFolder, "db/CustomItems/modTGC_items.json");
					tGC.ModClothing = tGC.modHelper.GetJsonDataFromFile<Dictionary<MongoId, ModClothing>>(absolutePathToModFolder, "db/modTGC_clothes.json");
					tGC.ModTradersAssort = tGC.modHelper.GetJsonDataFromFile<ModTradersAssort>(absolutePathToModFolder, "db/traders/668aaff35fd574b6dcc4a686/assort.json");
					tGC.ModTraderSuitsList = tGC.modHelper.GetJsonDataFromFile<List<Suit>>(absolutePathToModFolder, "db/traders/668aaff35fd574b6dcc4a686/suits.json");
					val2 = tGC.wttCommon.CustomItemServiceExtended.CreateCustomItems(executingAssembly, (string)null).GetAwaiter();
					if (!((TaskAwaiter)(ref val2)).IsCompleted)
					{
						num = (<>1__state = 0);
						<>u__1 = val2;
						((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__16>(ref val2, ref this);
						return;
					}
				}
				else
				{
					val2 = <>u__1;
					<>u__1 = default(TaskAwaiter);
					num = (<>1__state = -1);
				}
				((TaskAwaiter)(ref val2)).GetResult();
				Enumerator<MongoId, ModItems> enumerator = tGC.ModItems.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<MongoId, ModItems> current = enumerator.Current;
						MongoId key = current.Key;
						MongoId itemTplToClone = current.Value.ItemTplToClone;
						tGC.CopyToFilter(itemTplToClone, key);
						tGC.AddToFilter(key);
						tGC.AddToArmband(current.Key, <armbandId>5__2);
						tGC.AddToSecureContainer(current.Key, <secureContainerIds>5__4);
					}
				}
				finally
				{
					if (num < 0)
					{
						((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
					}
				}
				Enumerator<MongoId, ModClothing> enumerator2 = tGC.ModClothing.GetEnumerator();
				try
				{
					while (enumerator2.MoveNext())
					{
						KeyValuePair<MongoId, ModClothing> current2 = enumerator2.Current;
						MongoId key2 = current2.Key;
						MongoId clone = current2.Value.Clone;
						tGC.AddClothing(clone, key2);
						tGC.AddLocales(key2);
					}
				}
				finally
				{
					if (num < 0)
					{
						((global::System.IDisposable)enumerator2/*cast due to constrained. prefix*/).Dispose();
					}
				}
				tGC.AddItemPresets();
				tGC.AddTraderAssort(<traderIdPainter>5__3);
				tGC.AddTraderSuits(<traderIdPainter>5__3);
				tGC.logger.Success("[TGC] Mod loaded successfully.", (global::System.Exception)null);
				val = global::System.Threading.Tasks.Task.CompletedTask.GetAwaiter();
				if (!((TaskAwaiter)(ref val)).IsCompleted)
				{
					num = (<>1__state = 1);
					<>u__1 = val;
					((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__16>(ref val, ref this);
					return;
				}
				goto IL_036e;
				IL_036e:
				((TaskAwaiter)(ref val)).GetResult();
			}
			catch (global::System.Exception exception)
			{
				<>1__state = -2;
				<armbandId>5__2 = default(MongoId);
				<traderIdPainter>5__3 = default(MongoId);
				<secureContainerIds>5__4 = null;
				((AsyncTaskMethodBuilder)(ref <>t__builder)).SetException(exception);
				return;
			}
			<>1__state = -2;
			<armbandId>5__2 = default(MongoId);
			<traderIdPainter>5__3 = default(MongoId);
			<secureContainerIds>5__4 = null;
			((AsyncTaskMethodBuilder)(ref <>t__builder)).SetResult();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			((AsyncTaskMethodBuilder)(ref <>t__builder)).SetStateMachine(stateMachine);
		}
	}

	[RequiredMember]
	public DatabaseService DatabaseService;

	[RequiredMember]
	public ModPresets ModPresets;

	[RequiredMember]
	public Dictionary<MongoId, ModItems> ModItems;

	[RequiredMember]
	public ModConfig ModConfig;

	[RequiredMember]
	public ModTradersAssort ModTradersAssort;

	[RequiredMember]
	public List<Suit> ModTraderSuitsList;

	[RequiredMember]
	public Dictionary<MongoId, ModClothing> ModClothing;

	[RequiredMember]
	public Dictionary<MongoId, TemplateItem> Items;

	[RequiredMember]
	public Dictionary<MongoId, Trader> Traders;

	[RequiredMember]
	public Dictionary<MongoId, CustomizationItem> CustomizationItems;

	[RequiredMember]
	public Dictionary<string, LazyLoad<Dictionary<string, string>>> GlobalLocales;

	[AsyncStateMachine(typeof(<OnLoad>d__16))]
	public global::System.Threading.Tasks.Task OnLoad()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		<OnLoad>d__16 <OnLoad>d__17 = default(<OnLoad>d__16);
		<OnLoad>d__17.<>t__builder = AsyncTaskMethodBuilder.Create();
		<OnLoad>d__17.<>4__this = this;
		<OnLoad>d__17.<>1__state = -1;
		((AsyncTaskMethodBuilder)(ref <OnLoad>d__17.<>t__builder)).Start<<OnLoad>d__16>(ref <OnLoad>d__17);
		return ((AsyncTaskMethodBuilder)(ref <OnLoad>d__17.<>t__builder)).Task;
	}

	private void AddToArmband(MongoId modTgcId, MongoId armbandId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		if (ModItems[modTgcId].PutInArmband.HasValue && ModItems[modTgcId].PutInArmband == true)
		{
			Enumerable.First<SlotFilter>(Enumerable.ToArray<Slot>(Items[armbandId].Properties.Slots)[14].Properties.Filters).Filter.Add(modTgcId);
		}
	}

	private void AddToSecureContainer(MongoId modTgcId, List<MongoId> secureContainerIds)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		if (!ModConfig.PouchesInSecureContainer || !ModItems[modTgcId].PutInArmband.HasValue || ModItems[modTgcId].PutInSecureContainer != true)
		{
			return;
		}
		Enumerator<MongoId> enumerator = secureContainerIds.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				MongoId current = enumerator.Current;
				Enumerable.First<GridFilter>(Enumerable.First<Grid>(Items[current].Properties.Grids).Properties.Filters).Filter.Add(modTgcId);
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private void CopyToFilter(MongoId itemClone, MongoId modTgcId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		Enumerator<MongoId, TemplateItem> enumerator = Items.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<MongoId, TemplateItem> current = enumerator.Current;
				if (ModItems.ContainsKey(current.Key))
				{
					continue;
				}
				ValueTuple<List<Slot>, List<MongoId>> filters = GetFilters(MongoId.op_Implicit(current.Key));
				List<Slot> item = filters.Item1;
				List<MongoId> item2 = filters.Item2;
				Enumerator<Slot> enumerator2 = item.GetEnumerator();
				try
				{
					while (enumerator2.MoveNext())
					{
						Slot current2 = enumerator2.Current;
						if (current2.Properties == (SlotProperties)null)
						{
							continue;
						}
						global::System.Collections.Generic.IEnumerable<SlotFilter> filters2 = current2.Properties.Filters;
						if (((filters2 != null) ? Enumerable.First<SlotFilter>(filters2).Filter : null) == null)
						{
							continue;
						}
						SlotProperties properties = current2.Properties;
						object obj;
						if (properties == null)
						{
							obj = null;
						}
						else
						{
							global::System.Collections.Generic.IEnumerable<SlotFilter> filters3 = properties.Filters;
							obj = ((filters3 != null) ? Enumerable.FirstOrDefault<SlotFilter>(filters3) : null);
						}
						SlotFilter val = (SlotFilter)obj;
						if (((val != null) ? val.Filter : null) != null)
						{
							HashSet<MongoId> filter = val.Filter;
							if (filter.Contains(itemClone))
							{
								filter.Add(modTgcId);
							}
						}
					}
				}
				finally
				{
					((global::System.IDisposable)enumerator2/*cast due to constrained. prefix*/).Dispose();
				}
				Enumerator<MongoId> enumerator3 = Enumerable.ToList<MongoId>((global::System.Collections.Generic.IEnumerable<MongoId>)item2).GetEnumerator();
				try
				{
					while (enumerator3.MoveNext())
					{
						MongoId current3 = enumerator3.Current;
						if (current3 == itemClone)
						{
							item2.Add(modTgcId);
						}
					}
				}
				finally
				{
					((global::System.IDisposable)enumerator3/*cast due to constrained. prefix*/).Dispose();
				}
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private void AddToFilter(MongoId modTgcId)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		logger.Debug("addToFilters: " + MongoId.op_Implicit(modTgcId), (global::System.Exception)null);
		if (ModItems[modTgcId].AddToThisItemsFilters != null)
		{
			List<Slot> item = GetFilters(MongoId.op_Implicit(modTgcId)).Item1;
			List<MongoId> item2 = GetFilters(MongoId.op_Implicit(modTgcId)).Item2;
			Enumerator<string, List<MongoId>> enumerator = ModItems[modTgcId].AddToThisItemsFilters.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<string, List<MongoId>> current = enumerator.Current;
					if (current.Key == "conflicts")
					{
						item2.AddRange((global::System.Collections.Generic.IEnumerable<MongoId>)ModItems[modTgcId].AddToThisItemsFilters["conflicts"]);
						continue;
					}
					for (int i = 0; i < item.Capacity; i++)
					{
						if (!(current.Key != item[i].Name))
						{
							HashSet<MongoId> filter = Enumerable.First<SlotFilter>(item[i].Properties.Filters).Filter;
							filter.UnionWith((global::System.Collections.Generic.IEnumerable<MongoId>)ModItems[modTgcId].AddToThisItemsFilters[current.Key]);
							Enumerable.First<SlotFilter>(item[i].Properties.Filters).Filter = filter;
						}
					}
				}
			}
			finally
			{
				((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
			}
		}
		if (ModItems[modTgcId].AddToExistingItemFilters == null)
		{
			return;
		}
		Enumerator<string, List<MongoId>> enumerator2 = ModItems[modTgcId].AddToExistingItemFilters.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				KeyValuePair<string, List<MongoId>> current2 = enumerator2.Current;
				if (current2.Key == "conflicts")
				{
					Enumerator<MongoId> enumerator3 = ModItems[modTgcId].AddToExistingItemFilters[current2.Key].GetEnumerator();
					try
					{
						while (enumerator3.MoveNext())
						{
							MongoId current3 = enumerator3.Current;
							List<MongoId> item3 = GetFilters(MongoId.op_Implicit(current3)).Item2;
							item3.Add(modTgcId);
						}
					}
					finally
					{
						((global::System.IDisposable)enumerator3/*cast due to constrained. prefix*/).Dispose();
					}
					continue;
				}
				Enumerator<MongoId> enumerator4 = ModItems[modTgcId].AddToExistingItemFilters[current2.Key].GetEnumerator();
				try
				{
					while (enumerator4.MoveNext())
					{
						MongoId current4 = enumerator4.Current;
						List<Slot> item4 = GetFilters(MongoId.op_Implicit(current4)).Item1;
						Enumerator<Slot> enumerator5 = item4.GetEnumerator();
						try
						{
							while (enumerator5.MoveNext())
							{
								Slot current5 = enumerator5.Current;
								if (current2.Key == current5.Name)
								{
									Enumerable.First<SlotFilter>(current5.Properties.Filters).Filter.Add(modTgcId);
								}
							}
						}
						finally
						{
							((global::System.IDisposable)enumerator5/*cast due to constrained. prefix*/).Dispose();
						}
					}
				}
				finally
				{
					((global::System.IDisposable)enumerator4/*cast due to constrained. prefix*/).Dispose();
				}
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator2/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private ValueTuple<List<Slot>, List<MongoId>> GetFilters(string itemId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		global::System.Collections.Generic.IEnumerable<Slot> enumerable = Items[MongoId.op_Implicit(itemId)].Properties.Slots ?? global::System.Array.Empty<Slot>();
		global::System.Collections.Generic.IEnumerable<Slot> enumerable2 = Items[MongoId.op_Implicit(itemId)].Properties.Chambers ?? global::System.Array.Empty<Slot>();
		global::System.Collections.Generic.IEnumerable<Slot> enumerable3 = Items[MongoId.op_Implicit(itemId)].Properties.Cartridges ?? global::System.Array.Empty<Slot>();
		List<Slot> val = Enumerable.ToList<Slot>(Enumerable.Concat<Slot>(Enumerable.Concat<Slot>(enumerable, enumerable2), enumerable3));
		HashSet<MongoId> val2 = Items[MongoId.op_Implicit(itemId)].Properties.ConflictingItems ?? new HashSet<MongoId>();
		return new ValueTuple<List<Slot>, List<MongoId>>(val, Enumerable.ToList<MongoId>((global::System.Collections.Generic.IEnumerable<MongoId>)val2));
	}

	private void AddItemPresets()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		Enumerator<MongoId, Preset> enumerator = ModPresets.ModPreset.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<MongoId, Preset> current = enumerator.Current;
				DatabaseService.GetGlobals().ItemPresets[current.Key] = current.Value;
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private void AddTraderAssort(MongoId traderId)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_0087: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		Enumerator<ModTradersAssort.TgcTraderItemsClass> enumerator = ModTradersAssort.TraderItems.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				ModTradersAssort.TgcTraderItemsClass current = enumerator.Current;
				Traders[traderId].Assort.Items.Add(new Item
				{
					Id = current.Id,
					Template = current.Tpl,
					ParentId = current.ParentId,
					SlotId = current.SlotId,
					Upd = new Upd
					{
						StackObjectsCount = current.Upd.StackObjectsCount
					}
				});
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
		Enumerator<MongoId, List<List<BarterScheme>>> enumerator2 = ModTradersAssort.BarterSchemes.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				KeyValuePair<MongoId, List<List<BarterScheme>>> current2 = enumerator2.Current;
				Traders[traderId].Assort.BarterScheme[current2.Key] = new List<List<BarterScheme>>();
				Enumerator<List<BarterScheme>> enumerator3 = current2.Value.GetEnumerator();
				try
				{
					while (enumerator3.MoveNext())
					{
						List<BarterScheme> current3 = enumerator3.Current;
						Traders[traderId].Assort.BarterScheme[current2.Key].Add(current3);
					}
				}
				finally
				{
					((global::System.IDisposable)enumerator3/*cast due to constrained. prefix*/).Dispose();
				}
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator2/*cast due to constrained. prefix*/).Dispose();
		}
		Enumerator<MongoId, int> enumerator4 = ModTradersAssort.LoyalLevelItems.GetEnumerator();
		try
		{
			while (enumerator4.MoveNext())
			{
				KeyValuePair<MongoId, int> current4 = enumerator4.Current;
				Traders[traderId].Assort.LoyalLevelItems[current4.Key] = current4.Value;
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator4/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private void AddTraderSuits(MongoId traderId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		Traders[traderId].Base.CustomizationSeller = true;
		if (Traders[traderId].Suits == null)
		{
			Traders[traderId].Suits = new List<Suit>();
		}
		Enumerator<Suit> enumerator = ModTraderSuitsList.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				Suit current = enumerator.Current;
				Traders[traderId].Suits.Add(current);
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private void AddClothing(MongoId itemToCloneId, MongoId modTgcId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		CustomizationItem val = CustomizationItems[itemToCloneId];
		CustomizationProperties props = ModClothing[modTgcId].CustomizationProperties.Props;
		CustomizationItem val2 = DeepClone<CustomizationItem>(val);
		val2.Id = modTgcId;
		val2.Name = MongoId.op_Implicit(modTgcId);
		val2.Properties = DeepClone<CustomizationProperties>(val.Properties);
		val2.Properties.Side = props.Side;
		val2.Properties.Prefab = props.Prefab ?? val2.Properties.Prefab;
		val2.Properties.Body = props.Body ?? val2.Properties.Body;
		val2.Properties.Hands = props.Hands ?? val2.Properties.Hands;
		val2.Properties.Feet = props.Feet ?? val2.Properties.Feet;
		CustomizationItems[modTgcId] = val2;
	}

	private void AddLocales(MongoId modTgcId)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		Enumerator<string, LazyLoad<Dictionary<string, string>>> enumerator = GlobalLocales.GetEnumerator();
		try
		{
			string text = default(string);
			LazyLoad<Dictionary<string, string>> val = default(LazyLoad<Dictionary<string, string>>);
			while (enumerator.MoveNext())
			{
				enumerator.Current.Deconstruct(ref text, ref val);
				string localeCode = text;
				LazyLoad<Dictionary<string, string>> val2 = val;
				val2.AddTransformer((Func<Dictionary<string, string>, Dictionary<string, string>>)delegate(Dictionary<string, string>? localeData)
				{
					//IL_001b: Unknown result type (might be due to invalid IL or missing references)
					//IL_004f: Unknown result type (might be due to invalid IL or missing references)
					//IL_007a: Unknown result type (might be due to invalid IL or missing references)
					//IL_007f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0091: Unknown result type (might be due to invalid IL or missing references)
					//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
					//IL_0102: Unknown result type (might be due to invalid IL or missing references)
					if (localeData == null)
					{
						return localeData;
					}
					LocaleDetails val3 = CollectionExtensions.GetValueOrDefault<string, LocaleDetails>((IReadOnlyDictionary<string, LocaleDetails>)(object)ModClothing[modTgcId].Locales, localeCode) ?? CollectionExtensions.GetValueOrDefault<string, LocaleDetails>((IReadOnlyDictionary<string, LocaleDetails>)(object)ModClothing[modTgcId].Locales, "en");
					if (val3 == (LocaleDetails)null)
					{
						return localeData;
					}
					MongoId val4 = modTgcId;
					string text2 = $"{modTgcId} name";
					string text3 = $"{modTgcId} description";
					string text4 = val3.Name ?? "";
					string text5 = val3.Description ?? "";
					localeData[MongoId.op_Implicit(val4)] = text4;
					localeData[text2] = text4;
					localeData[text3] = text5;
					return localeData;
				});
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private static T DeepClone<T>(T obj)
	{
		string text = JsonSerializer.Serialize<T>(obj, (JsonSerializerOptions)null);
		return JsonSerializer.Deserialize<T>(text, (JsonSerializerOptions)null);
	}
}
