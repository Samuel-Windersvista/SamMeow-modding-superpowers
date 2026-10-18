using EFT;

namespace DoorBreach.Fika;

/// <summary>
/// Fika 无依赖事件桥：本 fork 保留为未来接入位（上游 BackdoorBanditFika 子工程未并入）。
/// 无订阅者时所有事件调用为空操作，不影响单机运行。
/// </summary>
public class FikaBridge
{
    public delegate void SimpleEvent();
    public static event SimpleEvent PluginEnableEmitted;
    public static void PluginEnable()
    {
        PluginEnableEmitted?.Invoke();
    }

    public delegate void SendPlantC4PacketEvent(int playerId, string doorId, int c4Timer);
    public static event SendPlantC4PacketEvent SendPlantC4PacketEmitted;
    public static void SendPlantC4PacketPacket(Player player, string doorId, int c4Timer)
    {
        //Plugin.LogSource.LogDebug("Sending player position packet");
        SendPlantC4PacketEmitted?.Invoke(player.Id, doorId, c4Timer);
    }

    public delegate void SendSyncOpenStatePacketEvent(int playerId, string objectId, int objectType);
    public static event SendSyncOpenStatePacketEvent SendSyncOpenStatePacketEmitted;
    public static void SendSyncOpenStatePacket(Player player, string objectId, int objectType)
    {
        //Plugin.LogSource.LogDebug("Sending player position packet");
        SendSyncOpenStatePacketEmitted?.Invoke(player.Id, objectId, objectType);
    }
}
