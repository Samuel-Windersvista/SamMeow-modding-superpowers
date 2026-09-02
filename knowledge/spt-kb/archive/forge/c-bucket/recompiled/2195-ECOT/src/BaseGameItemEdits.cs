// BaseGameItemEdits（反编译原样 + 修 API）：
// 对 3 个基础物品的槽位过滤器/属性做修正
// 4.1.2 迁移：DatabaseService.GetItems() -> TemplateTable.Items 注入；OnLoad() -> OnLoadAsync(CancellationToken)
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace EukyreECOT.Utilities;

[Injectable(InjectionType.Singleton)]
public class BaseGameItemEdits(ISptLogger<BaseGameItemEdits> logger, TemplateTable templateTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        EditFilters();
        return Task.CompletedTask;
    }

    private void EditFilters()
    {
        Dictionary<MongoId, TemplateItem> items = templateTable.Items;
        foreach (var (key, item) in items)
        {
            switch ((string)key)
            {
                case "689ce8bfd4370ee552a641da":
                    ModifySlotFilters(item, 5, 0, new List<MongoId> { "689d099c264b71b2da14043f" });
                    break;
                case "5c793fc42e221600114ca25d":
                    item.Properties.Accuracy = 0.0;
                    item.Properties.Velocity = 0.0;
                    break;
                case "628a66b41d5e41750e314f34":
                    ModifySlotFilters(item, 0, 0, new List<MongoId> { "695fa0bf748597b4fa1f9f31" });
                    break;
            }
        }
    }

    private void ReplaceSlotFilters(TemplateItem item, int slotIndex, int filterIndex, HashSet<MongoId> ids)
    {
        Slot slotAtIndex = GetSlotAtIndex(item, slotIndex);
        SlotFilter slotFilterAtIndex = GetSlotFilterAtIndex(slotAtIndex, filterIndex);
        slotFilterAtIndex.Filter = ids;
    }

    private void ModifySlotFilters(TemplateItem item, int slotIndex, int filterIndex, List<MongoId> ids, bool isCartridge = false)
    {
        Slot slotAtIndex = GetSlotAtIndex(item, slotIndex, isCartridge);
        SlotFilter slotFilterAtIndex = GetSlotFilterAtIndex(slotAtIndex, filterIndex);
        slotFilterAtIndex.Filter.UnionWith(ids);
    }

    private Slot GetSlotAtIndex(TemplateItem item, int index, bool isCartridge = false)
    {
        Slot[] array = isCartridge
            ? item.Properties?.Cartridges?.ToArray() ?? Array.Empty<Slot>()
            : item.Properties?.Slots?.ToArray() ?? Array.Empty<Slot>();
        if (index >= 0 && index < array.Length)
        {
            return array[index];
        }
        throw new IndexOutOfRangeException("Index on item slot property `" + item.Name + "` is out of range");
    }

    private SlotFilter GetSlotFilterAtIndex(Slot slot, int index)
    {
        SlotFilter[] array = slot.Properties?.Filters?.ToArray() ?? Array.Empty<SlotFilter>();
        if (index >= 0 && index < array.Length)
        {
            return array[index];
        }
        throw new IndexOutOfRangeException("Index on slot property `" + slot.Name + "` is out of range");
    }
}
