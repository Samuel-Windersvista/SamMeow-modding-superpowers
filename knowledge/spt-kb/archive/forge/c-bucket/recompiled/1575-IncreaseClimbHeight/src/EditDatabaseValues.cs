// EditDatabaseValues：SPT 4.0 -> 4.1.2 迁移
// 4.1.2 API 变更：
//   - IOnLoad.OnLoad() -> IOnLoad.OnLoadAsync(CancellationToken)
//   - DatabaseService（4.0 提供 GetGlobals()）在 4.1.2 中移除，
//     改为直接注入 GlobalTable（表模型），经 Configuration.VaultingSettings 访问
//   - Injectable 特性签名变化：4.1.2 为 (InjectionType, int typePriority)，无 typeOverride
//     （原 4.0 blob 解码：InjectionType=Scoped，TypePriority=400001）
//   - ISptLogger 命名空间：SPTarkov.Server.Core.Models.Utils -> SPTarkov.Common.Models.Logging
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace IncreaseClimbHeight;

[Injectable(InjectionType.Scoped, 400001)]
public class EditDatabaseValues(ISptLogger<EditDatabaseValues> logger, GlobalTable globalTable, ModHelper modHelper) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		string? modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
		if (modFolder == null)
		{
			return Task.CompletedTask;
		}

		VaultingConfig? vaultingConfig = JsonSerializer.Deserialize<VaultingConfig>(File.ReadAllText(Path.Combine(modFolder, "config.json")));
		if (vaultingConfig == null)
		{
			return Task.CompletedTask;
		}

		globalTable.Configuration.VaultingSettings = vaultingConfig.VaultingSettings;
		logger.Info("Increased Climb Height.", null);
		return Task.CompletedTask;
	}
}
