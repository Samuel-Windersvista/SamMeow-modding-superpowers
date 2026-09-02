// EnergyDrinkBuffs（4.0 → 4.1.2）
// 4.1.2 变更：Buff 类型命名空间 SPTarkov.Server.Core.Models.Eft.Common → SPTarkov.Server.Core.Models.Spt.Tables
using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace HoodsEnergyDrinks_CSharp;

public class EnergyDrinkBuffs
{
    public required Dictionary<string, IEnumerable<Buff>> buffs { get; set; }
}
