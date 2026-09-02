using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TGC.Models;

public class ModConfig
{
	[JsonPropertyName("PouchesInSecureContainer")]
	[field: CompilerGenerated]
	public bool PouchesInSecureContainer
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}
}
