using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using Requirement = SPTarkov.Server.Core.Models.Eft.Hideout.Requirement;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// 自定义安全容器升级配方（源 TS assets/recipes.ts 的 alpha/beta/epsilon/gamma 四条）。
/// 区域为 Workbench（areaType 10），每条 1 件成品，需 5×2 件材料。
/// </summary>
internal static class ContainerRecipes
{
    public static IReadOnlyList<HideoutProduction> All { get; } = [Alpha(), Beta(), Epsilon(), Gamma()];

    private static HideoutProduction Alpha() => Recipe(
        "63da4dbee8fa73e22500001a",
        level: 1,
        ItemTpl.SECURE_CONTAINER_ALPHA,
        ["567143bf4bdc2d1a0f8b4567", "5783c43d2459774bbe137486", "5c093e3486f77430cb02e593", "590c621186f774138d11ea29"],
        productionTime: 5600);

    private static HideoutProduction Beta() => Recipe(
        "63da4dbee8fa73e22500001b",
        level: 1,
        ItemTpl.SECURE_CONTAINER_BETA,
        [ItemTpl.SECURE_CONTAINER_ALPHA, "5aafbde786f774389d0cbc0f", "590c60fc86f77412b13fddcf", "62a0a16d0b9d3c46de5b6e97"],
        productionTime: 10800);

    private static HideoutProduction Epsilon() => Recipe(
        "63da4dbee8fa73e22500001c",
        level: 2,
        ItemTpl.SECURE_CONTAINER_EPSILON,
        [ItemTpl.SECURE_CONTAINER_BETA, "5c127c4486f7745625356c13", "59fafd4b86f7745ca07e1232", "619cbf9e0a7c3a1a2731940a", "61bf7c024770ee6f9c6b8b53"],
        productionTime: 35000);

    private static HideoutProduction Gamma() => Recipe(
        "63da4dbee8fa73e22500001d",
        level: 3,
        ItemTpl.SECURE_CONTAINER_GAMMA,
        [ItemTpl.SECURE_CONTAINER_EPSILON, "5e2af55f86f7746d4159f07c", "59fb016586f7746d0d4b423a", "5d235bb686f77443f4331278", "619cbf7d23893217ec30b689", "6389c7750ef44505c87f5996"],
        productionTime: 61200);

    private static HideoutProduction Recipe(
        string id,
        int level,
        string endProduct,
        string[] materials,
        double productionTime)
    {
        var requirements = new List<Requirement> { Area(level) };
        requirements.AddRange(materials.Select(Item));

        return new HideoutProduction
        {
            Id = id,
            AreaType = HideoutAreas.Workbench,
            Requirements = requirements,
            ProductionTime = productionTime,
            EndProduct = endProduct,
            Count = 1,
            IsEncoded = false,
            Locked = false,
            NeedFuelForAllProductionTime = true,
            Continuous = false,
            ProductionLimitCount = 0,
            IsCodeProduction = false
        };
    }

    private static Requirement Area(int level) =>
        new() { AreaType = (int)HideoutAreas.Workbench, RequiredLevel = level, Type = "Area" };

    private static Requirement Item(string templateId) =>
        new() { TemplateId = templateId, Count = 2, IsFunctional = false, Type = "Item" };
}
