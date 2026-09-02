// ModMetadata: SPT 4.0 -> 4.1.2 迁移
// 4.1.2 将 AbstractModMetadata 抽象类改为 IModMetadata 接口：
//   - 去掉 override 关键字
//   - 接口无 IsBundleMod 属性（删除），新增 HasPrepatcher（必填，默认 false）
//   - 必填项 ModGuid/Name/Author/License 均非空
//   - ModDependencies: com.morebotsapi.tacticaltoaster 已随 MoreBots 依赖移除；
//     com.wtt.commonlib 区间按整合包约束改为 >=3.0.0 <4.0.0（原 >=2.0.15 是 3.11/4.0 时代区间）
// SptVersion 已由 ldstr 补丁改为 ~4.1.0，此处保持
using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SemanticVersioning;

namespace RaidOverhaulMain;

public record ModMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "nameless.raidoverhaul.server";

	public string Name { get; init; } = "Raid Overhaul Server";

	public string Author { get; init; } = "nameless";

	public SemanticVersioning.Version Version { get; init; } = new SemanticVersioning.Version("3.0.3", false);

	public SemanticVersioning.Range SptVersion { get; init; } = new SemanticVersioning.Range("~4.1.0", false);

	public bool HasPrepatcher { get; init; } = false;

	public List<string>? Contributors { get; init; }

	public List<string>? Incompatibilities { get; init; }

	public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new Dictionary<string, SemanticVersioning.Range>
	{
		["com.wtt.commonlib"] = new SemanticVersioning.Range(">=3.0.0 <4.0.0", false)
	};

	public string? Url { get; init; }

	public string License { get; init; } = "CC BY-NC-ND 4.0";
}
