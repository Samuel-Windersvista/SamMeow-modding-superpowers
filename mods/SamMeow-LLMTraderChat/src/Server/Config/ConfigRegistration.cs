using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SamMeow.LLMTraderChat.Server.Chatbots;
using SamMeow.LLMTraderChat.Server.Engine;
using SamMeow.LLMTraderChat.Server.Knowledge;
using SamMeow.LLMTraderChat.Server.Llm;
using SamMeow.LLMTraderChat.Server.Spt;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Services.Commerce;

namespace SamMeow.LLMTraderChat.Server.Config;

/// <summary>
/// DI 注册器：在 SPT 构建容器前读盘并注册配置，以及整套引擎生产依赖与 per-trader chatbot。
///
/// STD-CFG-003：实现 IOnDIConstruct，读盘后 AddSingleton；消费者（ModEntry / chatbot）构造注入。
/// STD-CFG-001：mod 根目录经程序集位置取得，配置/知识/状态路径均相对之。
/// STD-CFG-005：首启（或 config.jsonc 被删）时从 defaultConfig.jsonc 复制，绝不覆盖玩家已有改动。
/// 路线 A：每个启用商人注册一个 <see cref="IDialogueChatBot"/>（DI 收编，SendMessage 按 dialogId 路由）。
/// </summary>
public sealed class ConfigRegistration : IOnDIConstruct
{
    /// <summary>
    /// 4.1 接口签名（源码实读 <c>SPTarkov.Server.Core/DI/IOnDIConstruct.cs</c>）：
    /// <c>static abstract Task OnDIConstructAsync(IServiceCollection, CancellationToken)</c>。
    /// </summary>
    public static Task OnDIConstructAsync(
        IServiceCollection serviceCollection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
        var configDirectory = Path.Combine(modFolder, ConfigLoader.ConfigFolderName);
        var configPath = Path.Combine(configDirectory, ConfigLoader.ConfigFileName);
        var defaultPath = Path.Combine(configDirectory, ConfigLoader.DefaultConfigFileName);

        if (!File.Exists(configPath) && File.Exists(defaultPath))
        {
            Directory.CreateDirectory(configDirectory);
            File.Copy(defaultPath, configPath);
        }

        var result = File.Exists(configPath)
            ? ConfigLoader.LoadFromFile(configPath)
            : ConfigLoader.LoadFromDirectory(modFolder);
        var config = result.Config;

        serviceCollection.AddSingleton(config);
        serviceCollection.AddSingleton(result);

        // 引擎生产依赖。
        serviceCollection.AddSingleton<ILlmClient>(new OpenAiCompatibleLlmClient(config.Llm));
        serviceCollection.AddSingleton<IKnowledgeRetrieverFactory>(new KeywordKnowledgeRetrieverFactory(modFolder));
        serviceCollection.AddSingleton<IPromptTemplateProvider>(new FilePromptTemplateProvider(modFolder));
        serviceCollection.AddSingleton<IChatHistoryProvider, SptChatHistoryProvider>();
        serviceCollection.AddSingleton<ITraderRelationshipProvider, SptTraderRelationshipProvider>();
        serviceCollection.AddSingleton<ITraderChatStateStore>(serviceProvider =>
            new FileTraderChatStateStore(
                Path.Combine(modFolder, "state"),
                serviceProvider.GetService<ISptLogger<FileTraderChatStateStore>>()));
        serviceCollection.AddSingleton<TraderChatEngine>();

        // 每个启用商人一个 chatbot（路线 A：DI 收编为 IEnumerable<IDialogueChatBot>）。
        foreach (var trader in config.Traders.Where(candidate => candidate.Enabled))
        {
            var traderId = trader.TraderId;
            var traderName = string.IsNullOrWhiteSpace(trader.Name) ? traderId : trader.Name;
            serviceCollection.AddSingleton<IDialogueChatBot>(serviceProvider => new TraderChatBot(
                traderId,
                traderName,
                serviceProvider.GetRequiredService<TraderChatEngine>(),
                serviceProvider.GetRequiredService<MailSendService>(),
                serviceProvider.GetRequiredService<DialogueHelper>(),
                serviceProvider.GetRequiredService<ISptLogger<TraderChatBot>>()));
        }

        return Task.CompletedTask;
    }
}
