namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 态度档位——商人与玩家关系的五个递进阶段（工单 14）。
/// 仅影响商人对话语气与谈资，不改变交易/任务逻辑；对话中不得向玩家报出内部数值。
/// 数值语义：<see cref="Hostile"/> 敌意/戒备、<see cref="Cold"/> 冷淡/生疏、
/// <see cref="Familiar"/> 熟稔/认可、<see cref="Trusted"/> 信任/器重、<see cref="Ally"/> 核心盟友/过命交情。
/// </summary>
public enum AttitudeTier
{
    /// <summary>敌意/戒备：未解锁或好感度为负。</summary>
    Hostile = 1,

    /// <summary>冷淡/生疏：默认档（无记录 / 忠诚等级 &lt;= 1）。</summary>
    Cold = 2,

    /// <summary>熟稔/认可：忠诚等级 == 2。</summary>
    Familiar = 3,

    /// <summary>信任/器重：忠诚等级 == 3。</summary>
    Trusted = 4,

    /// <summary>核心盟友/过命交情：忠诚等级 &gt;= 4。</summary>
    Ally = 5,
}
