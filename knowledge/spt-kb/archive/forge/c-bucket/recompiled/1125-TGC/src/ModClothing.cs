using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace TGC.Models;
public class ModClothing
{
	[JsonPropertyName("clone")]
	[field: CompilerGenerated]
	public MongoId Clone
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}
	[JsonPropertyName("customization")]
	[field: CompilerGenerated]
	public CustomizationWrapper CustomizationProperties
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}
	[JsonPropertyName("locales")]
	[field: CompilerGenerated]
	public Dictionary<string, LocaleDetails> Locales
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}

	[Obsolete("Constructors of types with required members are not supported in this version of your compiler.", true)]
	public ModClothing()
	{
	}
}
