using System;
using System.Collections.Generic;
using System.Linq;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace RZCustomProfiles;

[Injectable(InjectionType.Scoped)]
public class AssortUtilities(TemplateTable templateTable, ConfigLoader configLoader)
{
	private Dictionary<MongoId, TemplateItem>? _itemTemplates;

	private Dictionary<MongoId, TemplateItem>? ItemTemplates => _itemTemplates ??= templateTable.Items;

	public List<BarterScheme> BuildPayment(int priceRoubles, List<ItemEntry> barterItems, bool devMode = false)
	{
		if (devMode)
		{
			return new List<BarterScheme>
			{
				new BarterScheme
				{
					Template = ItemTpl.MONEY_ROUBLES,
					Count = 1.0
				}
			};
		}
		List<BarterScheme> list2 = new List<BarterScheme>();
		if (priceRoubles > 0)
		{
			list2.Add(new BarterScheme
			{
				Template = ItemTpl.MONEY_ROUBLES,
				Count = priceRoubles
			});
		}
		foreach (ItemEntry barterItem in barterItems)
		{
			list2.Add(new BarterScheme
			{
				Template = (MongoId)barterItem.Tpl,
				Count = barterItem.Count
			});
		}
		if (list2.Count <= 0)
		{
			return new List<BarterScheme>
			{
				new BarterScheme
				{
					Template = ItemTpl.MONEY_ROUBLES,
					Count = 0.0
				}
			};
		}
		return list2;
	}

	public List<Item> CreateRootItem(string tpl, int stackCount = 1, bool resolveChildren = true)
	{
		Item val = new Item
		{
			Id = new MongoId(),
			Template = (MongoId)tpl,
			ParentId = "hideout",
			SlotId = "hideout",
			Upd = new Upd
			{
				StackObjectsCount = stackCount
			}
		};
		List<Item> list = new List<Item> { val };
		if (resolveChildren)
		{
			ResolveRequiredChildren(list, val.Id, (MongoId)tpl, 100, new HashSet<string>());
		}
		return list;
	}

	public void ResolveRequiredChildren(List<Item> items, MongoId parentId, MongoId parentTpl, int durability, HashSet<string> skipSlots, int depth = 0)
	{
		if (depth > 5 || !ItemTemplates.TryGetValue(parentTpl, out TemplateItem value))
		{
			return;
		}
		IEnumerable<Slot>? enumerable = value.Properties?.Slots;
		if (enumerable == null)
		{
			return;
		}
		foreach (Slot item in enumerable)
		{
			if (item.Required != true || item.Name == null || skipSlots.Contains(item.Name))
			{
				continue;
			}
			MongoId? val = ResolveSlotDefaultTpl(item);
			if (!val.HasValue)
			{
				continue;
			}
			MongoId val2 = new MongoId();
			UpdRepairable? val3 = null;
			if (ItemTemplates.TryGetValue(val.Value, out TemplateItem value2))
			{
				int? num = (int?)value2.Properties?.Durability;
				if (num.HasValue && num.GetValueOrDefault() > 0)
				{
					double num2 = durability <= 0 ? 1.0 : Math.Clamp((double)durability / 100.0, 0.0, 1.0);
					val3 = new UpdRepairable
					{
						MaxDurability = num.Value,
						Durability = (int)Math.Round((double)num.Value * num2)
					};
				}
			}
			items.Add(new Item
			{
				Id = val2,
				Template = val.Value,
				ParentId = parentId,
				SlotId = item.Name,
				Upd = val3 == null ? null : new Upd
				{
					Repairable = val3
				}
			});
			ResolveRequiredChildren(items, val2, val.Value, durability, new HashSet<string>(), depth + 1);
		}
	}

	private static MongoId? ResolveSlotDefaultTpl(Slot slot)
	{
		IEnumerable<SlotFilter>? enumerable = slot.Properties?.Filters;
		if (enumerable == null)
		{
			return null;
		}
		foreach (SlotFilter item in enumerable)
		{
			if (item.Plate is { } plate && plate != default)
			{
				return plate;
			}
			if (item.Filter is { Count: > 0 })
			{
				return item.Filter.First();
			}
		}
		return null;
	}
}
