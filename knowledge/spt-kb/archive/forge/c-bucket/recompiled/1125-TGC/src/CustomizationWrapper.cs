using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace TGC.Models;
public class CustomizationWrapper
{
	[JsonPropertyName("_props")]
	[field: CompilerGenerated]
	public CustomizationProperties Props
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}

	[Obsolete("Constructors of types with required members are not supported in this version of your compiler.", true)]
	public CustomizationWrapper()
	{
	}
}
