using System.Collections.Generic;

namespace SeparateHostility.Extensions;

internal class BotsGroupManager : GClass555
{
    internal readonly Dictionary<BotSpawnParams, BotsGroup> _spawnGroups = [];
}