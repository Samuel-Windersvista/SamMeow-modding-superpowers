using System.IO;
using BepInEx;
using HarmonyLib;
using SamMeow.LLMTraderChat.Shared;

namespace SamMeow.LLMTraderChat.Client;

/// <summary>
/// 客户端半入口（SPT 4.1.6 / Mono / BepInEx 5）。
/// M2：输入解锁改为配置驱动——BepInEx <c>Config.Bind</c> 总开关 + 外部 JSON 商人名单；
/// 应用 Harmony 补丁（输入解锁 + 中文标点放行）；不新建任何 UI。
/// </summary>
[BepInPlugin(SharedConstants.ModGuid, SharedConstants.ModName, ModVersion.Value)]
public class LLMTraderChatClientPlugin : BaseUnityPlugin
{
    private Harmony _harmony = null!;

    /// <summary>当前设置；补丁经此判断是否解锁。</summary>
    internal static ClientSettings Settings { get; private set; } = ClientSettings.Default;

    private void Awake()
    {
        // STD-CFG-006：客户端配置经 BepInEx Config.Bind 声明，落 BepInEx/config/<guid>.cfg。
        var enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "输入解锁总开关：false 时本 mod 客户端不改变任何行为。");

        var traderListPath = Config.Bind(
            "General",
            "TraderListPath",
            Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath) ?? ".", "SamMeow.LLMTraderChat.traders.json"),
            "可聊天商人 id 列表 JSON 的路径（{\"enabledTraderIds\":[\"...\"]}）；新增商人只改此文件、无需重编译，重启生效。");

        Settings = ClientSettings.Load(enabled.Value, traderListPath.Value);

        // STD-CLI-007：Harmony 创建 + PatchAll + 退出 UnpatchSelf 生命周期。
        // 备注（工单 12 NIT-3）：PatchAll 为「全有或全无」——单个补丁目标缺失会使全部补丁不生效；
        // 曾评估改为逐类 CreateClassProcessor(...).Patch() + try/catch，但会违反 STD-CLI-007 的 PatchAll 契约，
        // 且本 mod 各补丁目标在当前 4.1.6 上均存在（已实机验证），故**维持 PatchAll**。
        _harmony = new Harmony(SharedConstants.ModGuid);
        _harmony.PatchAll();

        Logger.LogInfo(
            $"[LLMTC] {SharedConstants.ModName} v{ModVersion.Value} 客户端已加载"
            + $"（协议版本 {SharedConstants.ProtocolVersion}；输入解锁={Settings.Enabled}；"
            + $"可聊天商人={string.Join(",", Settings.EnabledTraderIds)}）");
    }

    private void OnDestroy()
    {
        // 退出/热重载时撤销补丁，避免残留。
        _harmony?.UnpatchSelf();
    }
}
