using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace TGC.Models;

public class ModPresets
{
	[JsonPropertyName("ItemPresets")]
	[field: CompilerGenerated]
	public Dictionary<MongoId, Preset> ModPreset
	{
		[CompilerGenerated]
		get;
		[CompilerGenerated]
		set;
	}
}
