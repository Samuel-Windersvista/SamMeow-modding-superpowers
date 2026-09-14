using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Server.Core.DI;

namespace {{ROOT_NAMESPACE}}.Server.Config;

/// <summary>
/// 配置注册器：在 SPT 构建 DI 容器前读盘，并把配置注册为单例。
/// STD-CFG-003：实现 IOnDIConstruct，在 OnDIConstructAsync 中读盘后 AddSingleton；
///              消费方（服务 / 路由 / 入口）通过构造函数参数注入 {{MOD_CLASS_NAME}}Config。
/// STD-CFG-001：配置随 mod 部署在 user/mods/&lt;ModName&gt;/config/ 内，用相对 mod 根目录的路径读取，
///              禁止硬编码绝对路径或依赖进程工作目录。静态注册方法里用程序集位置取 mod 根
///              （与 EV-GAP-CFG 参考实现一致）；运行期消费方若要自取路径，用
///              ModHelper.GetAbsolutePathToModFolder(Assembly)。
/// STD-CFG-005：首启（或 config.jsonc 被删）时从 defaultConfig.jsonc 复制，绝不覆盖玩家已有改动。
/// STD-LOG-005：取消不是错误，让 OperationCanceledException 正常传播。
/// </summary>
public class {{MOD_CLASS_NAME}}ConfigRegistration : IOnDIConstruct
{
    /// <summary>STD-CFG-002：容忍 .jsonc 的注释与尾随逗号。</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// 4.1 接口签名（源码实读）：
    /// static abstract Task OnDIConstructAsync(IServiceCollection serviceCollection, CancellationToken cancellationToken)。
    /// </summary>
    public static async Task OnDIConstructAsync(
        IServiceCollection serviceCollection,
        CancellationToken cancellationToken)
    {
        // STD-CFG-001：相对 mod 根目录定位 config/，不依赖进程工作目录。
        string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("无法定位 mod 目录。");
        string configPath = Path.Combine(modFolder, "config", "config.jsonc");
        string defaultPath = Path.Combine(modFolder, "config", "defaultConfig.jsonc");

        // STD-CFG-005：仅在目标不存在时复制默认副本，绝不覆盖玩家已有改动。
        if (!File.Exists(configPath) && File.Exists(defaultPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.Copy(defaultPath, configPath);
        }

        {{MOD_CLASS_NAME}}Config config;
        if (File.Exists(configPath))
        {
            // STD-CFG-003 + STD-LOG-005：把 CancellationToken 传给反序列化。
            await using FileStream stream = File.OpenRead(configPath);
            config = await JsonSerializer.DeserializeAsync<{{MOD_CLASS_NAME}}Config>(
                stream, JsonOptions, cancellationToken) ?? new {{MOD_CLASS_NAME}}Config();
        }
        else
        {
            // config.jsonc 与 defaultConfig.jsonc 都缺失：退回内置默认值，保证容器仍能构建。
            config = new {{MOD_CLASS_NAME}}Config();
        }

        serviceCollection.AddSingleton(config);
    }
}
