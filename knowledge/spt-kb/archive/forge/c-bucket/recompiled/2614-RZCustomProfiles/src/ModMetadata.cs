// ModMetadata（反编译补全）：RZCustomProfiles（新增开局角色）
// 4.0 record : AbstractModMetadata -> 4.1.2 class : IModMetadata（set->init，补 HasPrepatcher）
// 原始值从 DLL 构造函数 IL 提取：Version 1.1.0 / ModGuid com.rz.customprofiles / Name RZCustomProfiles /
// Author RemzDNB / SptVersion ~4.1.0（原 DLL 已补丁）/ License MIT / ModDependencies 空字典
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace RZCustomProfiles;

public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.rz.customprofiles";
    public string Name { get; init; } = "RZCustomProfiles";
    public string Author { get; init; } = "RemzDNB";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.1.0", false);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0", false);
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new();
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}
