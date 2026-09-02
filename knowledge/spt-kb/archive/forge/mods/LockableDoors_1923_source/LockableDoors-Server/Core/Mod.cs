using LockableDoorsServer.Models;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

namespace LockableDoorsServer.Core;

[Injectable]
public class Callbacks(
    JsonUtil jsonUtil,
    HttpResponseUtil httpResponseUtil,
    ISptLogger<Callbacks> logger,
    ModHelper modHelper)
{
    public ValueTask<string> OnDataToServer(string url, ServerDataPack? info, MongoId sessionId)
    {
        if (info is not null)
        {
            string filePath = GetFilePath(info.ProfileId, info.MapId);
            logger.Success(filePath);
            File.WriteAllText(filePath, jsonUtil.Serialize(info));
        }

        return new ValueTask<string>(httpResponseUtil.NullResponse());
    }

    public ValueTask<string> OnDataToClient(string url, ServerDataPack? info, MongoId sessionId)
    {
        string? jsonData = null;

        if (info is not null)
        {
            string filePath = GetFilePath(info.ProfileId, info.MapId);

            jsonData = File.Exists(filePath)
                ? File.ReadAllText(filePath, System.Text.Encoding.UTF8)
                : jsonUtil.Serialize(ServerDataPack.GetEmpty(info.ProfileId, info.MapId));
        }

        return new ValueTask<string>(jsonData ?? httpResponseUtil.NullResponse());
    }

    private string GetFilePath(string profileId, string mapId)
    {
        string pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        ModConfig config = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config.json");

        string mapIdAdjusted = mapId;

        if (mapId == "factory4_day" || mapId == "factory4_night")
        {
            mapIdAdjusted = "factory";
        }
        if (mapId == "sandbox_high")
        {
            mapIdAdjusted = "sandbox";
        }

        string profileIdAdjusted = profileId;
        if (config.GlobalDoorDataProfile)
        {
            profileIdAdjusted = "global";
        }

        string folderPath = Path.Combine(pathToMod, "LockedDoorData", profileIdAdjusted);
        string filePath = Path.Combine(folderPath, $"{mapIdAdjusted}.json");

        Directory.CreateDirectory(folderPath);
        return filePath;
    }
}

[Injectable]
public class CustomStaticRouter(JsonUtil jsonUtil, Callbacks callbacks)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<ServerDataPack>(
                "/jehree/lockabledoors/data_to_server",
                async (url, info, sessionId, output) => await callbacks.OnDataToServer(url, info, sessionId)
            ),
            new RouteAction<ServerDataPack>(
                "/jehree/lockabledoors/data_to_client",
                async (url, info, sessionId, output) => await callbacks.OnDataToClient(url, info, sessionId)
            )
        ]
    )
{ }
