using System.Collections;
using EFT;
using HomeComforts.Components;
using UnityEngine;

namespace HomeComforts.Helpers;

// Renamed from Utils -> HomeComfortsUtils: the 4.1 Assembly-CSharp exposes a global
// top-level `Utils` class (90 methods) that shadows any `HomeComforts.Helpers.Utils`.
// See KB client-mod-311-to-41.md 5.1 "全局命名空间冲突陷阱".
internal class HomeComfortsUtils
{
	public static void ForceUpdatePlayerCollisions()
	{
		EFT.StaticManager.BeginCoroutine(ForceUpdatePlayerCollisionsRoutine());
	}

	private static IEnumerator ForceUpdatePlayerCollisionsRoutine()
	{
		Transform transform = HCSession.Instance.Player.gameObject.transform;
		transform.position += new Vector3(0f, 1E-05f, 0f);
		yield return null;
		transform.position -= new Vector3(0f, 1E-05f, 0f);
	}
}
