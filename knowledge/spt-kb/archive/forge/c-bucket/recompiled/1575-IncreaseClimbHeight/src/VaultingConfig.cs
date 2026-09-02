// VaultingConfig：SPT 4.0 -> 4.1.2 迁移
// VaultingSettings 命名空间：SPTarkov.Server.Core.Models.Eft.Common -> SPTarkov.Server.Core.Models.Spt.Tables
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace IncreaseClimbHeight;

public class VaultingConfig
{
	public VaultingSettings VaultingSettings { get; set; } = new VaultingSettings();
}
