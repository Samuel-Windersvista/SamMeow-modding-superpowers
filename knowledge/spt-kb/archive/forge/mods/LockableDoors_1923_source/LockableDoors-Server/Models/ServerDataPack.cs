using SPTarkov.Server.Core.Models.Utils;

namespace LockableDoorsServer.Models;

public class ServerDataPack : IRequestData
{
    public required string ProfileId { get; set; }
    public required string MapId { get; set; }
    public required List<string> LockedDoorIds { get; set; }

    public static ServerDataPack GetEmpty(string profileId, string mapId)
    {
        return new ServerDataPack
        {
            ProfileId = profileId,
            MapId = mapId,
            LockedDoorIds = []
        };
    }
}