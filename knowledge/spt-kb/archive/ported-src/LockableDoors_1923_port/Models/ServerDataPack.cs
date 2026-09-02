using LockableDoors.Components;
using LockableDoors.Fika;
using System.Collections.Generic;

namespace LockableDoors.Models
{
    internal class ServerDataPack
    {
        public string ProfileId;
        public string MapId;
        public List<string> LockedDoorIds = [];

        public ServerDataPack(string profileId, string mapId, List<string> lockedDoorIds = null)
        {
            ProfileId = profileId;
            MapId = mapId;
            if (lockedDoorIds != null)
            {
                LockedDoorIds = lockedDoorIds;
            }
        }

        public static ServerDataPack GetRequestPack()
        {
            return new ServerDataPack(FikaBridge.GetRaidId(), LDSession.Instance.GameWorld.LocationId.ToLower());
        }
    }
}
