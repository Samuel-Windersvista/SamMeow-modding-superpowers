using LeaveItThere.Fika;

namespace LeaveItThere.Addon;

public static class LITFikaTools
{
	public static bool IAmHost()
	{
		return FikaBridge.IAmHost();
	}

	public static string GetRaidId()
	{
		return FikaBridge.GetRaidId();
	}
}
