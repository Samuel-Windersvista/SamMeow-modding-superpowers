using System.Collections.Generic;

namespace SeparateHostility.Extensions;

internal class BotsGroupManager : BotZoneGroups
{
    internal readonly Dictionary<BotSpawnParams, BotsGroup> _spawnGroups = [];
}