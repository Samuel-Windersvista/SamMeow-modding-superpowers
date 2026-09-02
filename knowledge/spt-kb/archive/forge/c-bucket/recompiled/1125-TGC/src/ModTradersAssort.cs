using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace TGC.Models;
public class ModTradersAssort
{
	public class TgcTraderItemsClass
	{
		[JsonPropertyName("_id")]
		[field: CompilerGenerated]
		public MongoId Id
		{
			[CompilerGenerated]
			get;
			[CompilerGenerated]
			set;
		}

		[JsonPropertyName("_tpl")]
		[field: CompilerGenerated]
		public MongoId Tpl
		{
			[CompilerGenerated]
			get;
			[CompilerGenerated]
			set;
		}
		[JsonPropertyName("parentId")]
		[field: CompilerGenerated]
		public string ParentId
		{
			[CompilerGenerated]
			get;
			[CompilerGenerated]
			set;
		}
		[JsonPropertyName("slotId")]
		[field: CompilerGenerated]
		public string SlotId
		{
			[CompilerGenerated]
			get;
			[CompilerGenerated]
			set;
		}

		[JsonPropertyName("upd")]
		[field: CompilerGenerated]
		public Upd Upd
		{
			[CompilerGenerated]
			get;
			[CompilerGenerated]
			set;
		} = new Upd();

		[Obsolete("Constructors of types with required members are not supported in this version of your compiler.", true)]
		public TgcTraderItemsClass()
		{
		}//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown

	}
	[JsonPropertyName("items")]
	[field: CompilerGenerated]
	public List<TgcTraderItemsClass> TraderItems
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}
	[JsonPropertyName("barter_scheme")]
	[field: CompilerGenerated]
	public Dictionary<MongoId, List<List<BarterScheme>>> BarterSchemes
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}

	[JsonPropertyName("loyal_level_items")]
	[field: CompilerGenerated]
	public Dictionary<MongoId, int> LoyalLevelItems
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}

	[Obsolete("Constructors of types with required members are not supported in this version of your compiler.", true)]
	public ModTradersAssort()
	{
	}
}
