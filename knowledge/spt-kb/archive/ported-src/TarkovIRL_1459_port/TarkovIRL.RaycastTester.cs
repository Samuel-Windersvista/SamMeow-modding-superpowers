using EFT;
using UnityEngine;

namespace TarkovIRL;

internal static class RaycastTester
{
	public static void CheckRaycast(Player player)
	{
		Vector3 position = player.MainParts[BodyPartType.body].Position;
		float magnitude = (position + player.HeadRotation * 1000f - position).magnitude;
	}
}
