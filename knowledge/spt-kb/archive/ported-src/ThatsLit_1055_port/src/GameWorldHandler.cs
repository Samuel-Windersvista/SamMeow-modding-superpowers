using Comfort.Common;
using EFT;
using UnityEngine;

namespace ThatsLit;

public class GameWorldHandler
{
	public static ThatsLitGameworld ThatsLitGameWorld { get; private set; }

	public static void Update()
	{
		GameWorld instance = Singleton<GameWorld>.Instance;
		if ((Object)(object)instance == (Object)null && (Object)(object)ThatsLitGameWorld != (Object)null)
		{
			Object.Destroy((Object)(object)ThatsLitGameWorld);
		}
		else if ((Object)(object)instance != (Object)null && (Object)(object)ThatsLitGameWorld == (Object)null)
		{
			ThatsLitGameWorld = GameObjectExtensions.GetOrAddComponent<ThatsLitGameworld>((MonoBehaviour)(object)instance);
		}
	}
}
