using SPTarkov.Server.Core.Models.Eft.Hideout;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// 配方唯一性兜底（缺陷清单 B3）：检测 mod 自带配方资源内的重复 endProduct，告警并去重
/// （保留首条）。
/// <para>
/// 范围说明：仅约束本 mod 的配方资源，不作用于 SPT5 原版配方表——原版同一 endProduct
/// 存在合法的多配方（例如同一物品在厨房/营养站各有一条、同站不同耗时两条）。
/// </para>
/// </summary>
internal static class CraftingRecipeGuard
{
    public static List<HideoutProduction> DedupeByEndProduct(
        IEnumerable<HideoutProduction> recipes,
        SoftcoreChangeLog log)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unique = new List<HideoutProduction>();
        foreach (var recipe in recipes)
        {
            var endProduct = (string)recipe.EndProduct;
            if (!seen.Add(endProduct))
            {
                log.Warn($"新增配方 endProduct 重复（{endProduct}，Id={recipe.Id}），已去重");
                continue;
            }

            unique.Add(recipe);
        }

        return unique;
    }
}
