using SPTarkov.Server.Core.Models.Spt.Tables;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// 网格扩容共享助手：定位模板的首个网格并设置 cellsV / cellsH。
/// 仅当模板与网格存在时返回 true；调用方据此计数或告警。
/// </summary>
internal static class SoftcoreGrids
{
    public static bool TrySetCells(TemplateTable templates, string template, int? cellsV, int? cellsH = null)
    {
        if (!templates.Items.TryGetValue(template, out var item)
            || item.Properties?.Grids?.FirstOrDefault()?.Properties is not { } grid)
        {
            return false;
        }

        if (cellsV.HasValue)
        {
            grid.CellsV = cellsV;
        }

        if (cellsH.HasValue)
        {
            grid.CellsH = cellsH;
        }

        return true;
    }
}
