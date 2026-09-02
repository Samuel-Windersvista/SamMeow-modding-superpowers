// 反编译补全：ModMetadata（record 类型，ILSpy 无法反编译，基于 cecil 属性结构手写）
// 注意：属性值与原始 DLL 可能不完全一致（编译时按需修正）
using SPTarkov.Common.Models.Spt.Config;

namespace Painter;

public record ModMetadata(
    string ModGuid,
    string Name,
    string Author,
    Version Version,
    string SptVersion,
    string Url = "",
    bool IsBundleMod = false)
    : SPTarkov.Common.Interfaces.IModMetadata;
