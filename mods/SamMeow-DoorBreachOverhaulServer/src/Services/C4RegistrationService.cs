using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace SamMeow.DoorBreachOverhaulServer.Services;

/// <summary>
/// C4 服务端注册服务：把 C4 物品模板写入物品表（TemplateTable），并把它挂到工作台配方（HideoutTable）。
///
/// 4.1 数据契约（反编译 SPTarkov.Server.Core 4.1.6 实测）：
///   CustomItemService.CreateItem(NewItemDetails, Assembly?) —— 直接提交一个完整的 TemplateItem
///   （不像旧世代需要 mod 目录 db/CustomItems/*.json 自动扫描；4.1 无此类自动加载器）。
///   物品 bundle 不经过本服务：BundleLoader 在启动时扫描 mod 根目录的 bundles.json
///   （key 一一对应 bundles/&lt;key&gt; 文件），并把 CRC 经 /singleplayer/bundles 下发客户端。
///   本服务只负责物品与配方的数据注册，bundle 由上述机制分发。
///
/// STD-BND-006：数据库覆盖由 mod 代码显式加载，不依赖服务器自动合并。
/// </summary>
[Injectable(InjectionType.Singleton)]
public class C4RegistrationService(
    ISptLogger<C4RegistrationService> logger,
    ModHelper modHelper,
    CustomItemService customItemService,
    HideoutTable hideoutTable)
{
    /// <summary>客户端硬编码契约 ID（mods/SamMeow-DoorBreachOverhaul 的 ExplosiveBreachComponent.C4ExplosiveId），不得变更。</summary>
    public const string C4ItemId = "6636606320e842b50084e51a";

    /// <summary>上游 BB-NG 发行物的手册父节点（炸药类），与 2.0.4 发行一致。</summary>
    private const string C4HandbookParentId = "5b47574386f77428ca22b2f2";

    private const double C4HandbookPriceRoubles = 40000;
    private const double C4FleaPriceRoubles = 45000;

    /// <summary>
    /// 注册 C4 物品与工作台配方。任何契约违背（ID 不匹配、数据缺失、注册失败）都就地抛异常。
    /// </summary>
    public C4RegistrationResult Register(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TemplateItem template = LoadC4Template();
        List<HideoutProduction> recipes = LoadC4Recipes();

        MongoId expectedId = new(C4ItemId);

        // 契约校验：模板 ID 必须是客户端硬编码值（改数据即在此崩，不静默错配）。
        if (template.Id != expectedId)
        {
            throw new InvalidOperationException(
                $"C4 模板 ID 契约违背：期望 {C4ItemId}，数据文件给出 {template.Id}。");
        }

        // 契约校验：配方产物必须是同一个 C4 ID。
        foreach (HideoutProduction recipe in recipes)
        {
            if (recipe.EndProduct != expectedId)
            {
                throw new InvalidOperationException(
                    $"C4 配方 {recipe.Id} 的 endProduct={recipe.EndProduct} 与客户端契约 {C4ItemId} 不符。");
            }
        }

        // 1) 注册物品：物品表 + locale + 手册价 + 跳蚤价（CustomItemService.CreateItem 一次完成）。
        NewItemDetails details = new()
        {
            NewItem = template,
            // 手册分类与价格：让物品出现在图鉴（handbookParentId 为炸药类节点）。
            AddToHandbook = true,
            HandbookParentId = C4HandbookParentId,
            HandbookPriceRoubles = C4HandbookPriceRoubles,
            // 跳蚤价：AddToFleaPriceDb=true 时 FleaPriceRoubles 必须 > 0，否则 CreateItem 抛异常。
            AddToFleaPriceDb = true,
            FleaPriceRoubles = C4FleaPriceRoubles,
            // C4 非武器，无需进武器架白名单。
            AddToWeaponShelf = false,
            Locales = BuildLocales(),
        };

        CreateItemResult createResult = customItemService.CreateItem(details, Assembly.GetExecutingAssembly());
        if (!createResult.Success)
        {
            throw new InvalidOperationException(
                $"注册 C4 物品失败：{string.Join("; ", createResult.Errors)}");
        }

        // 2) 注册工作台配方：追加到 hideout.production.recipes（与上游 TS 行为一致）。
        List<HideoutProduction>? productionRecipes = hideoutTable.Production.Recipes
            ?? throw new InvalidOperationException("HideoutTable.Production.Recipes 为 null，数据库未按 4.1 契约加载。");

        foreach (HideoutProduction recipe in recipes)
        {
            if (productionRecipes.Any(existing => existing.Id == recipe.Id))
            {
                throw new InvalidOperationException($"C4 配方 ID 冲突：{recipe.Id} 已存在于 hideout.production.recipes。");
            }

            productionRecipes.Add(recipe);
        }

        logger.Info(
            $"[DoorBreachOverhaulServer] C4 模板与 {recipes.Count} 条工作台配方已注入（item={createResult.ItemId}）。");

        // 单条配方的 recipeId（本单只注册 1 条）。
        return new C4RegistrationResult(createResult.ItemId, recipes[0].Id);
    }

    /// <summary>从 mod 自身目录 db/customitems/c4explosive.jsonc 读取 C4 的 TemplateItem。</summary>
    private TemplateItem LoadC4Template()
    {
        string itemDir = System.IO.Path.Combine(GetModRootPath(), "db", "customitems");
        return modHelper.GetJsonDataFromFile<TemplateItem>(itemDir, "c4explosive.jsonc")
            ?? throw new InvalidOperationException("读取 C4 物品模板失败（db/customitems/c4explosive.jsonc）。");
    }

    /// <summary>从 mod 自身目录 db/hideout/c4explosive_recipe.jsonc 读取工作台配方数组。</summary>
    private List<HideoutProduction> LoadC4Recipes()
    {
        string recipeDir = System.IO.Path.Combine(GetModRootPath(), "db", "hideout");
        List<HideoutProduction> recipes =
            modHelper.GetJsonDataFromFile<List<HideoutProduction>>(recipeDir, "c4explosive_recipe.jsonc")
            ?? throw new InvalidOperationException("读取 C4 配方失败（db/hideout/c4explosive_recipe.jsonc）。");

        if (recipes.Count == 0)
        {
            throw new InvalidOperationException("C4 配方文件为空（db/hideout/c4explosive_recipe.jsonc）。");
        }

        return recipes;
    }

    /// <summary>
    /// STD-CFG-001 变体：用 mod 程序集位置定位 mod 根目录，不依赖进程工作目录。
    /// </summary>
    private string GetModRootPath()
    {
        return modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// locale：en 与 ch（SPT 简体中文键为 "ch"，见 languages.json）。
    /// 4.1 CustomItemService 会把缺失语言的条目回退到首个语言，故只需这两种。
    /// </summary>
    private static Dictionary<string, LocaleDetails> BuildLocales()
    {
        return new Dictionary<string, LocaleDetails>
        {
            ["en"] = new()
            {
                Name = "C4 Explosive",
                ShortName = "C4",
                Description =
                    "This C4 Explosive is used for breaching reinforced doors. It is a powerful explosive " +
                    "used in the military and law enforcement. A stable plastic explosive triggered after a set timer.",
            },
            ["ch"] = new()
            {
                Name = "C4 炸药",
                ShortName = "C4",
                Description =
                    "用于爆破加固门的 C4 塑性炸药。军用与执法部门使用的强力炸药，" +
                    "性质稳定、可安全携带，设置计时器后起爆。",
            },
        };
    }
}

/// <summary>注册结果：物品 tpl 与配方 id，供入口日志断言。</summary>
public readonly record struct C4RegistrationResult(MongoId ItemId, MongoId RecipeId);
