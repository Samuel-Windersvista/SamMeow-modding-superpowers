using EFT;
using SPT.Reflection.Utils;

namespace ImmersiveDaylightCycle.Fika;

public class FikaBridge
{
	public delegate bool SimpleBoolReturnEvent();

	public delegate string SimpleStringReturnEvent();

	public static event SimpleBoolReturnEvent IAmHostEmitted;

	public static event SimpleStringReturnEvent GetRaidIdEmitted;

	public static bool IAmHost()
	{
		bool? flag = FikaBridge.IAmHostEmitted?.Invoke();
		if (!flag.HasValue)
		{
			return true;
		}
		return flag.Value;
	}

	public static string GetRaidId()
	{
		string text = FikaBridge.GetRaidIdEmitted?.Invoke();
		if (text == null)
		{
			return ClientAppUtils.GetMainApp().Session.Profile.ProfileId;
		}
		return text;
	}
}
