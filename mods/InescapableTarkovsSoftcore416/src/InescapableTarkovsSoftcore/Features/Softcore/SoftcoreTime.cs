namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// 时间缩放共享助手：统一「倍率守卫 + 取整策略」。
/// 源 TS 使用 <c>Math.round</c>（half-up）与 <c>Math.ceil</c>；C# <see cref="Math.Round(double)"/>
/// 默认银行家舍入，故 round 场景一律用 <see cref="JsRound"/> 对齐 JS 语义。
/// </summary>
internal static class SoftcoreTime
{
    /// <summary>JS <c>Math.round</c> 语义（half-up，正数）。</summary>
    public static double JsRound(double value) => Math.Floor(value + 0.5);

    /// <summary>ceil(时间 / 倍率)。</summary>
    public static double ScaleCeil(double time, double multiplier) => Math.Ceiling(time / multiplier);

    /// <summary>JS round(时间 / 倍率)。</summary>
    public static double ScaleRound(double time, double multiplier) => JsRound(time / multiplier);

    /// <summary>JS round 后的整型（供 int 字段赋值，避免散落的 (int) 转换）。</summary>
    public static int RoundInt(double value) => (int)JsRound(value);

    /// <summary>JS round(时间 / 倍率) 后的整型。</summary>
    public static int ScaleRoundInt(double time, double multiplier) => (int)JsRound(time / multiplier);

    /// <summary>倍率守卫：&lt;= 0 时告警并返回 false。</summary>
    public static bool TryMultiplier(double multiplier, string label, SoftcoreChangeLog log)
    {
        if (multiplier > 0)
        {
            return true;
        }

        log.Warn($"倍率 {label}={multiplier} 非法（须 > 0），跳过");
        return false;
    }
}
