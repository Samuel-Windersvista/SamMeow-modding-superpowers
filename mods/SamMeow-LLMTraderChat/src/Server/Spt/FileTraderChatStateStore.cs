using System.Text.Json;
using SamMeow.LLMTraderChat.Server.Engine;
using SPTarkov.Common.Models.Logging;

namespace SamMeow.LLMTraderChat.Server.Spt;

/// <summary>
/// 开场一次性状态的持久化实现：把「已开场」的 (sessionId, traderId) 集合存到 mod 目录下的
/// <c>state/openings.json</c>，跨服务器重启保持 at-most-once 语义。
///
/// 读失败按「未开场」处理（可能重发一次）并告警；写失败仅告警、不影响本次运行。
/// 内存缓存 + 锁，避免每条消息反复读盘。
/// </summary>
public sealed class FileTraderChatStateStore : ITraderChatStateStore
{
    private const string StateFileName = "openings.json";

    private readonly string _filePath;
    private readonly ISptLogger<FileTraderChatStateStore>? _logger;
    private readonly object _gate = new();
    private HashSet<string>? _opened;

    /// <summary>以状态目录构造；<paramref name="logger"/> 可选（测试可不传）。</summary>
    public FileTraderChatStateStore(string storageDirectory, ISptLogger<FileTraderChatStateStore>? logger = null)
    {
        _filePath = Path.Combine(storageDirectory, StateFileName);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> TryClaimOpeningAsync(string sessionId, string traderId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var opened = Load();
            if (!opened.Add(Key(sessionId, traderId)))
            {
                return Task.FromResult(false);
            }

            Persist(opened);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsOpeningSentAsync(string sessionId, string traderId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(Load().Contains(Key(sessionId, traderId)));
        }
    }

    /// <inheritdoc />
    public Task MarkOpeningSentAsync(string sessionId, string traderId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var opened = Load();
            if (opened.Add(Key(sessionId, traderId)))
            {
                Persist(opened);
            }
        }

        return Task.CompletedTask;
    }

    private static string Key(string sessionId, string traderId) => $"{sessionId}|{traderId}";

    private HashSet<string> Load()
    {
        if (_opened is not null)
        {
            return _opened;
        }

        try
        {
            _opened = File.Exists(_filePath)
                ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(_filePath)) ?? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger?.Warning($"[LLMTC][state] 读取开场状态失败，按未开场处理：{ex.Message}");
            _opened = new HashSet<string>(StringComparer.Ordinal);
        }

        return _opened;
    }

    private void Persist(HashSet<string> opened)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_filePath, JsonSerializer.Serialize(opened));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger?.Warning($"[LLMTC][state] 写入开场状态失败（不影响本次运行）：{ex.Message}");
        }
    }
}
