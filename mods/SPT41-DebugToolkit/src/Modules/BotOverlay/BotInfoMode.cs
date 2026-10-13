namespace SamMeow.DebugToolkit.Modules.BotOverlay
{
    /// <summary>
    /// 覆盖层监视模式。移植自 BotDebug <c>BotInfo.BotInfoMode</c>（1.8.0）。
    /// </summary>
    /// <remarks>
    /// 顺序照上游：<c>Minimized</c>（隐藏覆盖层）→ 5 个核心模式 → BigBrain 两模式。
    /// F10/F11 按枚举顺序循环，故 Minimized 在 Custom 之后、Behaviour 之前可达。
    /// 选中 <c>Minimized</c> 时 <c>BotInfo.GetInfoText</c> 返回 null，组件按「空文本不绘制」隐藏。
    /// BigBrain 两模式以 <c>#if !STANDALONE</c> 保护（本线默认编入，见 csproj 无 STANDALONE 定义）。
    /// </remarks>
    internal enum BotInfoMode
    {
        // 隐藏覆盖层（对齐上游 Minimized = 0）：与 F12 的 Enable 总开关语义独立。
        Minimized = 0,
        Behaviour,
        BattleState,
        Health,
        Specials,
        Custom,
#if !STANDALONE
        BigBrainLayer,
        BigBrainLogic,
#endif
    }
}
