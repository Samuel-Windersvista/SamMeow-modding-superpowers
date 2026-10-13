using InescapableTarkovsSoftcore.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Hideout;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// G3 藏身处建造/升级免 FIR。遍历各藏身处区域阶段（含 improvements 子结构）的需求，
/// 凡带 <c>isSpawnedInSession</c> 键者置 <c>false</c>；无该键者保持不变（对应 D9 / 用户故事 13）。
/// 仅统计实际发生 true→false 的项；已是 false 的为无操作。
/// 顺序 Order=600（D5：softcore=100 … noFirHideout=600 → raidDuration=700）。
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class NoFirHideoutModule(ISptLogger<NoFirHideoutModule> logger)
    : FeatureModule<NoFirHideoutConfig>
{
    public override string Id => "noFirHideout";

    public override int Order => 600;

    protected override NoFirHideoutConfig Section(SoftcoreConfig root) => root.NoFirHideout;

    protected override ModuleReport Apply(ModContext context, NoFirHideoutConfig config)
    {
        var changed = 0;

        foreach (var area in context.Tables.HideoutTable.Areas)
        {
            if (area?.Stages is null)
            {
                continue;
            }

            foreach (var stage in area.Stages.Values)
            {
                if (stage is null)
                {
                    continue;
                }

                changed += ClearFir(stage.Requirements);

                if (stage.Improvements is null)
                {
                    continue;
                }

                foreach (var improvement in stage.Improvements)
                {
                    changed += ClearFir(improvement?.Requirements);
                }
            }
        }

        logger.Info($"[ITS] noFirHideout: 已清除 {changed} 项藏身处 FIR 要求");
        return ModuleReport.Ok(Id, changed);
    }

    /// <summary>阶段需求：<c>IsSpawnedInSession</c> 为可空布尔，键存在且为 true 才置 false。</summary>
    private static int ClearFir(List<StageRequirement>? requirements)
    {
        if (requirements is null)
        {
            return 0;
        }

        var changed = 0;
        foreach (var requirement in requirements)
        {
            if (requirement?.IsSpawnedInSession == true)
            {
                requirement.IsSpawnedInSession = false;
                changed++;
            }
        }

        return changed;
    }

    /// <summary>升级需求（improvements）：<c>IsSpawnedInSession</c> 为可空布尔；仅 true 才置 false 并计数。</summary>
    private static int ClearFir(List<StageImprovementRequirement>? requirements)
    {
        if (requirements is null)
        {
            return 0;
        }

        var changed = 0;
        foreach (var requirement in requirements)
        {
            if (requirement is { IsSpawnedInSession: true })
            {
                requirement.IsSpawnedInSession = false;
                changed++;
            }
        }

        return changed;
    }
}
