// ModMetadata: SPT 4.0 -> 4.1.2 迁移
// 4.1.2 将 AbstractModMetadata 抽象类改为 IModMetadata 接口：
//   - 去掉 override 关键字
//   - 接口无 IsBundleMod 属性（删除），新增 HasPrepatcher（必填，默认 false）
//   - 必填项 ModGuid/Name/Author/License 均非空
// SptVersion 已由 ldstr 补丁改为 ~4.1.0，此处保持
using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SemanticVersioning;

namespace TarkovCraftLoader;

public record ModMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "com.vinihns.tarkovcraftloader";

	public string Name { get; init; } = "TarkovCraft Loader";

	public string Author { get; init; } = "ViniHNS";

	public SemanticVersioning.Version Version { get; init; } = new SemanticVersioning.Version("1.0.0", false);

	public SemanticVersioning.Range SptVersion { get; init; } = new SemanticVersioning.Range("~4.1.0", false);

	public bool HasPrepatcher { get; init; } = false;

	public List<string>? Contributors { get; init; }

	public List<string>? Incompatibilities { get; init; }

	public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }

	public string? Url { get; init; } = "https://github.com/viniHNS/TarkovCraft-Loader";

	public string License { get; init; } = "MIT";
}
