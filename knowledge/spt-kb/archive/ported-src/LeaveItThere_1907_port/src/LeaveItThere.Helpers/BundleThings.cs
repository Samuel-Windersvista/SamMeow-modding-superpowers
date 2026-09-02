using System;
using System.IO;
using UnityEngine;

namespace LeaveItThere.Helpers;

public class BundleThings
{
	public static GameObject MoveModeUIPrefab;

	public static void LoadBundles()
	{
		string text = Path.Combine(Plugin.AssemblyFolderPath, "bundles", "editplaceditemmenu.menu");
		AssetBundle obj = AssetBundle.LoadFromFile(text);
		if ((UnityEngine.Object)(object)obj == (UnityEngine.Object)null)
		{
			throw new Exception("Error loading bundle: " + text);
		}
		MoveModeUIPrefab = BundleThings.LoadAsset<GameObject>(obj, "EditPlacedItemMenu");
	}

	public static T LoadAsset<T>(AssetBundle bundle, string assetPath) where T : UnityEngine.Object
	{
		T val = bundle.LoadAsset<T>(assetPath);
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			throw new Exception("Error loading asset " + assetPath);
		}
		UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)val);
		return val;
	}
}
