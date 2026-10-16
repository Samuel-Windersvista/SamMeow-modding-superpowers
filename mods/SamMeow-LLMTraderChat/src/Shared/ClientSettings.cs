using System;
using System.Collections.Generic;
using System.IO;

namespace SamMeow.LLMTraderChat.Shared;

/// <summary>
/// 客户端运行时设置（纯逻辑，放 Shared 以便单测）：输入解锁总开关（BepInEx <c>Config.Bind</c> 提供）
/// + 可聊天商人 id 列表（JSON 文件）。新增商人只需改 JSON 文件（无需重编译）。
/// </summary>
public sealed class ClientSettings
{
    /// <summary>输入解锁总开关默认值。</summary>
    public bool Enabled { get; private set; } = true;

    /// <summary>可解锁输入闸门的商人 id 列表（默认试点商人）。</summary>
    public IReadOnlyList<string> EnabledTraderIds { get; private set; } = SharedConstants.DefaultEnabledTraderIds;

    /// <summary>默认设置（总开关开、默认名单）。</summary>
    public static ClientSettings Default { get; } = new();

    /// <summary>给定商人对话是否应解锁输入（id 比较大小写不敏感）。</summary>
    public bool ShouldUnlock(string traderId) => Enabled && ClientTraderList.Contains(EnabledTraderIds, traderId);

    /// <summary>
    /// 加载：总开关由调用方（BepInEx <c>Config.Bind</c>）提供；商人名单来自 JSON 文件（缺失则按默认写出）。
    /// 读盘失败（IO / 权限）一律回退默认名单，绝不抛异常。
    /// </summary>
    public static ClientSettings Load(bool enabled, string traderListPath)
    {
        var ids = SharedConstants.DefaultEnabledTraderIds;
        try
        {
            if (!File.Exists(traderListPath))
            {
                var directory = Path.GetDirectoryName(traderListPath);
                if (directory is { Length: > 0 })
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(traderListPath, ClientTraderList.BuildDefault(ids));
            }

            ids = ClientTraderList.Parse(File.ReadAllText(traderListPath), SharedConstants.DefaultEnabledTraderIds);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ids = SharedConstants.DefaultEnabledTraderIds;
        }

        return new ClientSettings { Enabled = enabled, EnabledTraderIds = ids };
    }
}
