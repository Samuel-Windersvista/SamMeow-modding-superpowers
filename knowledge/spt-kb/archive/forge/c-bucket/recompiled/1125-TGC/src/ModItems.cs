using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace TGC.Models;

public class ModItems
{
	[JsonPropertyName("itemTplToClone")]
	[field: CompilerGenerated]
	public MongoId ItemTplToClone
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}

	[JsonPropertyName("PutInArmband")]
	[field: CompilerGenerated]
	public bool? PutInArmband
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}

	[JsonPropertyName("putInSecureContainer")]
	[field: CompilerGenerated]
	public bool? PutInSecureContainer
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}

	[JsonPropertyName("addToThisItemsFilters")]
	[field: CompilerGenerated]
	public Dictionary<string, List<MongoId>>? AddToThisItemsFilters
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}

	[JsonPropertyName("addToExistingItemFilters")]
	[field: CompilerGenerated]
	public Dictionary<string, List<MongoId>>? AddToExistingItemFilters
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}
}
