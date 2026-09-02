using SPT.Reflection.Utils;

namespace LockableDoors.Fika;

internal class FikaBridge
{
    public delegate void SimpleEvent();
    public delegate bool SimpleBoolReturnEvent();
    public delegate string SimpleStringReturnEvent();

    public static event SimpleEvent PluginEnableEmitted;
    public static void PluginEnable() => PluginEnableEmitted?.Invoke();


    public static event SimpleBoolReturnEvent IAmHostEmitted;
    public static bool IAmHost()
    {
        bool? eventResponse = IAmHostEmitted?.Invoke();

        if (eventResponse == null)
        {
            return true;
        }
        else
        {
            return eventResponse.Value;
        }
    }


    public static event SimpleStringReturnEvent GetRaidIdEmitted;
    public static string GetRaidId()
    {
        string eventResponse = GetRaidIdEmitted?.Invoke();

        if (eventResponse == null)
        {
            return ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.ProfileId;
        }
        else
        {
            return eventResponse;
        }
    }

    public delegate void SendDoorLockedStatePacketEvent(string doorId, bool isLocked);
    public static event SendDoorLockedStatePacketEvent SendDoorLockedStatePacketEmitted;
    public static void SendDoorLockedStatePacket(string doorId, bool isLocked)
    { SendDoorLockedStatePacketEmitted?.Invoke(doorId, isLocked); }
}
