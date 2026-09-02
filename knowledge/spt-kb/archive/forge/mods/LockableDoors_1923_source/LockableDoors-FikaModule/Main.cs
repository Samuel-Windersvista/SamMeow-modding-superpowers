using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using LockableDoors.Fika;
using LockableDoors.FikaModule.Common;

namespace LockableDoors.FikaModule;

internal class Main
{
    // called by the core dll via reflection
    public static void Init()
    {
        PluginAwake();
        FikaBridge.PluginEnableEmitted += PluginEnable;

        FikaBridge.IAmHostEmitted += IAmHost;
        FikaBridge.GetRaidIdEmitted += GetRaidId;

        FikaBridge.SendDoorLockedStatePacketEmitted += FikaMethods.SendDoorLockedStatePacket;
    }

    public static void PluginAwake()
    {

    }

    public static void PluginEnable()
    {
        FikaMethods.InitOnPluginEnabled();
    }

    public static bool IAmHost()
    {
        return Singleton<FikaServer>.Instantiated;
    }

    public static string GetRaidId()
    {
        return FikaBackendUtils.GroupId;
    }
}
