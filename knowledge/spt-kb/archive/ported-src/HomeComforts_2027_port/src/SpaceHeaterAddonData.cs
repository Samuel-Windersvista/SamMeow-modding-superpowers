internal class SpaceHeaterAddonData
{
	public bool HeaterEnabled;

	public SpaceHeaterAddonData()
	{
	}

	public SpaceHeaterAddonData(bool enabled)
	{
		HeaterEnabled = enabled;
	}

	public static SpaceHeaterAddonData CreateData(bool enabled)
	{
		return new SpaceHeaterAddonData(enabled);
	}
}
