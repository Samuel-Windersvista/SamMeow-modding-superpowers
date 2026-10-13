using System.Reflection;
using System.Runtime.CompilerServices;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// 构造 SPT <see cref="TemplateTable"/> 假表的测试工具：仅用于功能模块单测。
/// TemplateTable 含多个 <c>required</c> 属性且 <c>Items</c> 为 init-only，
/// 无法用对象初始值设定项简易构造，故用 <see cref="RuntimeHelpers.GetUninitializedObject"/>
/// 绕开 required 校验，再经反射写入 Items 后备字段。
/// </summary>
internal static class TestItemTables
{
    private static readonly FieldInfo ItemsBackingField =
        typeof(TemplateTable).GetField("<Items>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("找不到 TemplateTable.Items 后备字段");

    public static TemplateTable Items(params (string Id, string Parent, int Stack, int? MaxHp)[] items)
    {
        var table = (TemplateTable)RuntimeHelpers.GetUninitializedObject(typeof(TemplateTable));
        var dictionary = new Dictionary<MongoId, TemplateItem>();
        foreach (var (id, parent, stack, maxHp) in items)
        {
            dictionary[new MongoId(id)] = new TemplateItem
            {
                Id = id,
                Parent = parent,
                Properties = new TemplateItemProperties { StackMaxSize = stack, MaxHpResource = maxHp }
            };
        }

        ItemsBackingField.SetValue(table, dictionary);
        return table;
    }

    public static TemplateItemProperties Props(TemplateTable table, string id) =>
        table.Items[new MongoId(id)].Properties!;
}
