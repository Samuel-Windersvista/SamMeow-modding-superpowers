using InescapableTarkovsSoftcore.Features;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>测试用空表载体：不含真实 SPT 表；fake 模块不得触碰表。</summary>
internal static class TestModTables
{
    public static ModTables Empty { get; } = new(null!, null!, null!, null!, null!, null!);

    /// <summary>仅挂载模板表（其余表不需要），供真实变换模块测试使用。</summary>
    public static ModTables With(TemplateTable templateTable) => new(templateTable, null!, null!, null!, null!, null!);
}
