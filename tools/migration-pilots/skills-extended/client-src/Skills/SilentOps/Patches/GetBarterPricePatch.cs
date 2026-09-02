using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.Trading;
using HarmonyLib;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

namespace SkillsExtended.Skills.SilentOps.Patches;

/// <summary>
/// 4.1 迁移版：GetBarterPrice patch。
/// 3.11 -> 4.1 映射：
///   TraderAssortmentControllerClass -> EFT.Trading.Assortment
///   TraderClass.GStruct264 -> EFT.Trading.Trader.ItemPrice (MongoID? CurrencyId, int Amount)
///   Class1930.class1930_0.method_0 求和 -> barterScheme.Sum(v => v.Sum(bt => bt.count))
///   BarterScheme = List&lt;BarterVariant&gt;；BarterVariant = List&lt;BarterTemplate&gt;（_tpl / count 在 BarterTemplate 上）
/// </summary>
public class GetBarterPricePatch : ModulePatch
{
    public static Item Selecteditem;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EFT.Trading.Assortment),
            nameof(EFT.Trading.Assortment.GetBarterPrice));
    }

    [PatchPostfix]
    private static void Postfix(EFT.Trading.Assortment __instance, ref EFT.Trading.Trader.ItemPrice? __result, Item[] items)
    {
        if (items.IsNullOrEmpty()) return;
        if (!SkillsPlugin.SkillData.SilentOps.Enabled) return;

        var scheme = __instance.GetSchemeForItem(items[0]);

        if (scheme is null) return;

        float price = 0;
        foreach (var item in items)
        {
            var barterScheme = __instance.GetSchemeForItem(item);

            if (barterScheme is null) continue;

            // 求和 barter scheme 各项的 count（原 Class1930 委托）
            // 4.1: BarterScheme -> List<BarterVariant>；BarterVariant -> List<BarterTemplate>
            var num2 = Mathf.Ceil((float)barterScheme.Sum(variant => variant.Sum(bt => bt.count)));

            var bonus = 1f - SkillManagerExt.Instance(EPlayerSide.Usec).SilentOpsSilencerCostRedBuff;

            // Silencer Type
            if (item is EFT.InventoryLogic.Silencer)
            {
                num2 *= bonus;
            }

            price += num2;
        }

        Selecteditem = __instance.SelectedItem;

        __result = new EFT.Trading.Trader.ItemPrice(scheme[0][0]._tpl, (int)Mathf.Ceil(price));
    }
}

public class RequiredItemsCountPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var type = PatchConstants.EftTypes.SingleCustom(t => t.GetProperty("RequiredItemsCount") != null);

        return AccessTools.PropertyGetter(type, "RequiredItemsCount");
    }

    [PatchPostfix]
    private static void Postfix(EFT.Trading.Requisite __instance, ref int __result)
    {
        // Suppressor type
        if (GetBarterPricePatch.Selecteditem is not EFT.InventoryLogic.Silencer) return;
        if (!SkillsPlugin.SkillData.SilentOps.Enabled) return;

        var bonus = 1f - SkillManagerExt.Instance(EPlayerSide.Usec).SilentOpsSilencerCostRedBuff;

        __result = (int)Mathf.Ceil(__result * bonus);
    }
}
