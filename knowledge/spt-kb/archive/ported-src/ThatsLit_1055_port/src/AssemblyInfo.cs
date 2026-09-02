namespace ThatsLit;

public static class AssemblyInfo
{
	public const string Title = "That's Lit";

	public const string Description = "One step closer to fair gameplay, by giving AIs non-perfect vision and reactions. Because we too deserve grasses, bushes and nights.";

	public const string Configuration = "4.1.0";

	public const string Company = "";

	public const string Product = "That's Lit";

	public const string Copyright = "Copyright © 2024 BA";

	public const string Trademark = "";

	public const string Culture = "";

	public const int TarkovVersion = 40743;

	public const string EscapeFromTarkov = "EscapeFromTarkov.exe";

	public const string ModName = "That's Lit";

	public const string ModVersion = "1.3100.3";

	public const string SPTGUID = "com.SPT.core";

	public const string SPTVersion = "4.1.0";

	private static long modVersionComparable;

	public static long ModVersionComparable
	{
		get
		{
			if (modVersionComparable == 0L)
			{
				string[] array = "1.3100.3".Split('.');
				modVersionComparable = int.Parse(array[0]) * 1000000000 + int.Parse(array[1]) * 1000000 + int.Parse(array[2]);
			}
			return modVersionComparable;
		}
	}
}
