using System;
using System.Collections;
using System.Reflection;
using EFT;
using EFT.Ballistics;
using EFT.EnvironmentEffect;
using EFT.HealthSystem;
using EFT.UI;
using EFT.Weather;
using Unity.Collections;
using UnityEngine;

using FirearmController = EFT.Player.FirearmController;
using AbstractHandsController = EFT.Player.AbstractHandsController;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ThatsLit;

public class ThatsLitPlayer : MonoBehaviour
{
	internal delegate bool CheckStimEffectProxy(EStimulatorBuffType buffType);

	public const int DEBUG_INTERVAL = 19;

	private readonly int RESOLUTION = 32 * ThatsLitPlugin.ResLevel.Value;

	public const int POWER = 3;

	public CustomRenderTexture rt;

	private Texture slowRT;

	public Camera cam;

	public Camera envCam;

	private NativeArray<Color32> observed;

	public RawImage display;

	private float setupTime;

	private float lastCheckedLights;

	public float fog;

	public float rain;

	public float cloud;

	private AsyncGPUReadbackRequest gquReq;

	private bool _gpuReadbackPending;

	private bool _disposeRequested;

	internal float lastOutside;

	internal float surroundingRating;

	internal float ambienceShadownRating;

	internal float bunkerTimeClamped;

	internal float lastInBunkerTime;

	internal float lastOutBunkerTime;

	internal Vector3 lastInBunderPos;

	internal PlayerTerrainDetailsProfile TerrainDetails;

	private float terrainScoreHintProne;

	private float terrainScoreHintRegular;

	internal PlayerFoliageProfile Foliage;

	internal Vector3 lastShotVector;

	internal float lastShotTime;

	internal static readonly LayerMask ambienceRaycastMask = (int)((1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("HighPolyCollider")) | (1 << LayerMask.NameToLayer("Grass")) | (1 << LayerMask.NameToLayer("Foliage")));

	private CheckStimEffectProxy checkEffectDelegate;

	private static float canLoadTime = 0f;

	internal RaycastHit flashLightHit;

	internal BotOwner lastNearest;

	private ThatsLitGameworld gameworld;

	private static Camera prefab;

	private int cameraThrottleFrequency = 1440;

	internal float overheadHaxRating;

	private float litFactorSample;

	private float ambScoreSample;

	private static float benchmarkSampleSeenCoef;

	private static float benchmarkSampleEncountering;

	private static float benchmarkSampleExtraVisDis;

	private static float benchmarkSampleScoreCalculator;

	private static float benchmarkSampleUpdate;

	private static float benchmarkSampleFUpdate;

	private static float benchmarkSampleFoliageCheck;

	private static float benchmarkSampleTerrainCheck;

	private static float benchmarkSampleGUI;

	private static float benchmarkSampleNoBushOverride;

	private static float benchmarkSampleBlindFire;

	private int guiFrame;

	private string infoCache1;

	private string infoCache2;

	private string infoCacheBenchmark;

	private GUIStyle style;

	private string infoCache;

	public static bool IsDebugSampleFrame
	{
		get
		{
			if (ThatsLitPlugin.DebugInfo.Value)
			{
				return Time.frameCount % 19 == 0;
			}
			return false;
		}
	}

	public PlayerDebugInfo DebugInfo { get; internal set; }

	public LightAndLaserState LightAndLaserState { get; internal set; }

	public RaidSettings ActiveRaidSettings => gameworld.activeRaidSettings;

	public Player Player { get; internal set; }

	internal float AmbienceShadowFactor => Mathf.Pow(ambienceShadownRating / 10f, 2f);

	internal PlayerLitScoreProfile PlayerLitScoreProfile { get; set; }

	internal CheckStimEffectProxy CheckEffectDelegate
	{
		get
		{
			if (checkEffectDelegate == null)
			{
				MethodInfo methodInfo = ReflectionHelper.FindMethodByArgTypes(typeof(ActiveHealthController), new Type[1] { typeof(EStimulatorBuffType) }, BindingFlags.Instance | BindingFlags.Public);
				checkEffectDelegate = (CheckStimEffectProxy)methodInfo.CreateDelegate(typeof(CheckStimEffectProxy), Player.ActiveHealthController);
			}
			return checkEffectDelegate;
		}
	}

	internal float OverheadHaxRatingFactor => overheadHaxRating / 10f;

	public static bool CanLoad()
	{
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)EFT.CameraControl.CameraManager.Instance.OpticCameraManager.Camera != (Object)null && (Object)(object)prefab == (Object)null)
		{
			((Component)EFT.CameraControl.CameraManager.Instance.OpticCameraManager.Camera).gameObject.SetActive(false);
			prefab = Object.Instantiate<Camera>(EFT.CameraControl.CameraManager.Instance.OpticCameraManager.Camera);
			((Component)EFT.CameraControl.CameraManager.Instance.OpticCameraManager.Camera).gameObject.SetActive(true);
			((Object)((Component)prefab).gameObject).name = "That's Lit Camera (Prefab)";
			MonoBehaviour[] components = ((Component)prefab).gameObject.GetComponents<MonoBehaviour>();
			foreach (MonoBehaviour val in components)
			{
				VolumetricLightRenderer val2 = (VolumetricLightRenderer)(object)((val is VolumetricLightRenderer) ? val : null);
				if (val2 == null)
				{
					if (val is AreaLightManager)
					{
						Object.Destroy((Object)(object)val);
					}
					else
					{
						Object.Destroy((Object)(object)val);
					}
				}
				else if (ThatsLitPlugin.VolumetricLightRenderer.Value)
				{
					val2.IsOptic = false;
				}
			}
			prefab.clearFlags = (CameraClearFlags)2;
			prefab.backgroundColor = new Color(0f, 0f, 0f, 0f);
			prefab.nearClipPlane = 0.001f;
			prefab.farClipPlane = 3.5f;
			prefab.cullingMask = (int)(LayersMaskController.PlayerMask);
			prefab.fieldOfView = 44f;
			canLoadTime = Time.realtimeSinceStartup;
			Logger.LogWarning($"[That's Lit] Can load players. Time: {canLoadTime}");
			return false;
		}
		if ((Object)(object)prefab != (Object)null)
		{
			return canLoadTime + 10f < Time.realtimeSinceStartup;
		}
		return false;
	}

	internal void Setup(ThatsLitGameworld gameworld)
	{
		setupTime = Time.time;
		if (Player.IsYourPlayer)
		{
			DebugInfo = new PlayerDebugInfo();
		}
		this.gameworld = gameworld;
		MaybeEnableBrightness();
	}

	internal void MaybeEnableBrightness()
	{
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Expected O, but got Unknown
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		if (!ThatsLitPlugin.EnabledLighting.Value || gameworld.ScoreCalculator == null || Application.isBatchMode)
		{
			return;
		}
		if (PlayerLitScoreProfile == null)
		{
			PlayerLitScoreProfile = new PlayerLitScoreProfile(this);
		}
		if (PlayerLitScoreProfile.IsProxy)
		{
			return;
		}
		if ((Object)(object)rt == (Object)null)
		{
			int num = RESOLUTION;
			if (!Player.IsYourPlayer)
			{
				Vector3 position = Player.Position;
				ThatsLitPlayer mainThatsLitPlayer = gameworld.MainThatsLitPlayer;
				Vector3? obj;
				if (mainThatsLitPlayer == null)
				{
					obj = null;
				}
				else
				{
					Player player = mainThatsLitPlayer.Player;
					obj = ((player != null) ? new Vector3?(player.Position) : ((Vector3?)null));
				}
				float num2 = Vector3.Distance(position, (Vector3)(obj ?? Player.Position));
				if (num2 > 100f)
				{
					num = 16;
				}
				else if (num2 > 50f)
				{
					num = 24;
				}
			}
			rt = new CustomRenderTexture(num, num, (RenderTextureFormat)0, (RenderTextureReadWrite)0)
			{
				depth = 0,
				doubleBuffered = true,
				useMipMap = false,
				filterMode = (FilterMode)0
			};
			((RenderTexture)rt).Create();
		}
		if ((Object)(object)cam == (Object)null)
		{
			cam = Object.Instantiate<Camera>(prefab);
			((Object)((Component)cam).gameObject).name = "That's Lit Camera";
			((Component)cam).transform.SetParent(Player.Transform.Original);
			cam.targetTexture = (RenderTexture)(object)rt;
			((Component)cam).gameObject.SetActive(true);
		}
		else
		{
			((Behaviour)cam).enabled = true;
		}
		if (Player.IsYourPlayer)
		{
			ThatsLitPlugin.DebugTexture.SettingChanged += HandleDebugTextureSettingChanged;
		}
		if (Player.IsYourPlayer && ThatsLitPlugin.DebugTexture.Value)
		{
			if ((Object)(object)slowRT == (Object)null)
			{
				slowRT = (Texture)new Texture2D(RESOLUTION, RESOLUTION, (TextureFormat)4, false);
			}
			if ((Object)(object)display == (Object)null)
			{
				display = new GameObject().AddComponent<RawImage>();
				((Component)display).transform.SetParent((Transform)(object)RectTransformExtensions.RectTransform((Component)(object)MonoBehaviourSingleton<GameUI>.Instance));
				RectTransformExtensions.RectTransform((Component)(object)display).sizeDelta = new Vector2(160f, 160f);
				display.texture = slowRT;
				RectTransformExtensions.RectTransform((Component)(object)display).anchoredPosition = new Vector2(-720f, -360f);
			}
			else
			{
				((Behaviour)display).enabled = true;
			}
		}
	}

	internal void DisableBrightness()
	{
		if ((bool)((Object)(object)cam))
		{
			((Behaviour)cam).enabled = false;
		}
		if ((bool)((Object)(object)display))
		{
			((Behaviour)display).enabled = false;
		}
		PlayerLitScoreProfile = null;
		ThatsLitPlugin.DebugTexture.SettingChanged -= HandleDebugTextureSettingChanged;
	}

	internal void ToggleBrightnessProxy(bool toggle)
	{
		if ((bool)((Object)(object)cam))
		{
			((Behaviour)cam).enabled = !toggle;
		}
		if ((bool)((Object)(object)display))
		{
			((Behaviour)display).enabled = !toggle;
		}
		PlayerLitScoreProfile.IsProxy = toggle;
		if (toggle)
		{
			PlayerLitScoreProfile.frame0 = default(FrameStats);
		}
	}

	private void Update()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_048d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a23: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a51: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a63: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a68: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a7b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a80: Unknown result type (might be due to invalid IL or missing references)
		//IL_064e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0653: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a94: Unknown result type (might be due to invalid IL or missing references)
		//IL_0624: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b5a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b65: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b67: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b72: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b74: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b80: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8c: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_081b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0840: Unknown result type (might be due to invalid IL or missing references)
		//IL_0845: Unknown result type (might be due to invalid IL or missing references)
		//IL_084c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0851: Unknown result type (might be due to invalid IL or missing references)
		//IL_0930: Unknown result type (might be due to invalid IL or missing references)
		//IL_0955: Unknown result type (might be due to invalid IL or missing references)
		//IL_095a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0961: Unknown result type (might be due to invalid IL or missing references)
		//IL_0966: Unknown result type (might be due to invalid IL or missing references)
		//IL_0984: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b94: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b9b: Unknown result type (might be due to invalid IL or missing references)
		//IL_077f: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0734: Unknown result type (might be due to invalid IL or missing references)
		//IL_0759: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0909: Unknown result type (might be due to invalid IL or missing references)
		//IL_090e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0887: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ac: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)Player == (Object)null)
		{
			return;
		}
		Player player = Player;
		if (player != null)
		{
			IHealthController healthController = player.HealthController;
			if (((healthController != null) ? new bool?(healthController.IsAlive) : ((bool?)null)) == false)
			{
				Object.Destroy((Object)(object)this);
				return;
			}
		}
		if (!ThatsLitPlugin.EnabledMod.Value)
		{
			DisableBrightness();
			return;
		}
		ThatsLitPlugin.swUpdate.MaybeResume();
		Vector3 position = Player.MainParts[(BodyPartType)1].Position;
		if (!Player.AIData.IsInside)
		{
			lastOutside = Time.time;
		}
		if (EnvironmentManager.Instance.InBunker && lastOutBunkerTime >= lastInBunkerTime)
		{
			lastInBunkerTime = Time.time;
			lastInBunderPos = position;
		}
		if (!EnvironmentManager.Instance.InBunker && lastOutBunkerTime < lastInBunkerTime)
		{
			lastOutBunkerTime = Time.time;
		}
		if (lastOutBunkerTime < lastInBunkerTime && MyExtensions.SqrDistance(position, lastInBunderPos) > 2.25f)
		{
			bunkerTimeClamped += Time.deltaTime;
		}
		else
		{
			bunkerTimeClamped -= Time.deltaTime * 5f;
		}
		bunkerTimeClamped = Mathf.Clamp(bunkerTimeClamped, 0f, 10f);
		ThatsLitPlugin.swTerrain.MaybeResume();
		if (ThatsLitPlugin.EnabledGrasses.Value && !gameworld.terrainDetailsUnavailable && TerrainDetails == null && !Application.isBatchMode)
		{
			TerrainDetails = new PlayerTerrainDetailsProfile();
		}
		if (TerrainDetails != null && Time.time > TerrainDetails.LastCheckedTime + 0.41f)
		{
			gameworld.CheckTerrainDetails(position, TerrainDetails);
			ThatsLitAPI.OnPlayerSurroundingTerrainSampledDirect?.Invoke(this);
			ThatsLitAPI.OnPlayerSurroundingTerrainSampled?.Invoke(Player);
			if (ThatsLitPlugin.TerrainInfo.Value)
			{
				TerrainDetailScore terrainScore = gameworld.CalculateDetailScore(TerrainDetails, Vector3.zero, 0f, 0f);
				terrainScoreHintProne = terrainScore.prone;
				float poseFactor = Utility.GetPoseFactor(Player.PoseLevel, Player.Physical.MaxPoseLevel, Player.IsInPronePose);
				terrainScoreHintRegular = Utility.GetPoseWeightedRegularTerrainScore(poseFactor, terrainScore);
			}
		}
		ThatsLitPlugin.swTerrain.Stop();
		ThatsLitPlugin.swFoliage.MaybeResume();
		if (ThatsLitPlugin.EnabledFoliage.Value && !gameworld.foliageUnavailable && Foliage == null && !Application.isBatchMode)
		{
			Foliage = new PlayerFoliageProfile(new FoliageInfo[16], (Collider[])(object)new Collider[16]);
		}
		if (Foliage != null)
		{
			if (!Foliage.IsFoliageSorted)
			{
				Foliage.IsFoliageSorted = SlicedBubbleSort(Foliage.Foliage, Foliage.FoliageCount * 2, Foliage.FoliageCount);
			}
			gameworld.UpdateFoliageScore(position, Foliage);
		}
		ThatsLitPlugin.swFoliage.Stop();
		if (ThatsLitPlugin.EnableEquipmentCheck.Value && Time.time > lastCheckedLights + 0.41f && PlayerLitScoreProfile != null)
		{
			lastCheckedLights = Time.time;
			LightAndLaserState lightAndLaserState = LightAndLaserState;
			ref ThatsLitCompat.DeviceMode deviceStateCache = ref lightAndLaserState.deviceStateCache;
			ref ThatsLitCompat.DeviceMode deviceStateCacheSub = ref lightAndLaserState.deviceStateCacheSub;
			(ThatsLitCompat.DeviceMode, ThatsLitCompat.DeviceMode) tuple = Utility.DetermineShiningEquipments(Player);
			deviceStateCache = tuple.Item1;
			deviceStateCacheSub = tuple.Item2;
			lightAndLaserState.VisibleLight = lightAndLaserState.deviceStateCache.light > 0f;
			lightAndLaserState.VisibleLaser = lightAndLaserState.deviceStateCache.laser > 0f;
			lightAndLaserState.IRLight = lightAndLaserState.deviceStateCache.irLight > 0f;
			lightAndLaserState.IRLaser = lightAndLaserState.deviceStateCache.irLaser > 0f;
			lightAndLaserState.VisibleLightSub = lightAndLaserState.deviceStateCacheSub.light > 0f;
			lightAndLaserState.VisibleLaserSub = lightAndLaserState.deviceStateCacheSub.laser > 0f;
			lightAndLaserState.IRLightSub = lightAndLaserState.deviceStateCacheSub.irLight > 0f;
			lightAndLaserState.IRLaserSub = lightAndLaserState.deviceStateCacheSub.irLaser > 0f;
			LightAndLaserState = lightAndLaserState;
		}
		CastFlashlight();
		overheadHaxRating = UpdateOverheadHaxCastRating(position, overheadHaxRating);
		if (PlayerLitScoreProfile == null && ThatsLitPlugin.EnabledLighting.Value)
		{
			MaybeEnableBrightness();
			ThatsLitPlugin.swUpdate.Stop();
			return;
		}
		if (PlayerLitScoreProfile != null && !PlayerLitScoreProfile.IsProxy && !ThatsLitPlugin.EnabledLighting.Value)
		{
			DisableBrightness();
			ThatsLitPlugin.swUpdate.Stop();
			return;
		}
		if (PlayerLitScoreProfile == null || PlayerLitScoreProfile.IsProxy || gameworld?.ScoreCalculator == null)
		{
			ThatsLitPlugin.swUpdate.Stop();
			return;
		}
		if (ThatsLitPlugin.EnabledCameraThrottling.Value && PlayerLitScoreProfile != null)
		{
			float ambienceScore = PlayerLitScoreProfile.frame0.ambienceScore;
			int num = (int)Mathf.Lerp(6f, 1f, Mathf.InverseLerp(-0.5f, 0.5f, ambienceScore));
			num = Mathf.Clamp(num, 1, 6);
			if (Time.frameCount % num != 0)
			{
				((Behaviour)cam).enabled = false;
			}
			else
			{
				((Behaviour)cam).enabled = true;
			}
		}
		else if (!ThatsLitPlugin.EnabledCameraThrottling.Value)
		{
			((Behaviour)cam).enabled = true;
		}
		if ((gquReq.done || !_gpuReadbackPending) && (Object)(object)rt != (Object)null)
		{
			_gpuReadbackPending = true;
			Camera obj = cam;
			if (obj != null && !((Behaviour)obj).enabled)
			{
				_disposeRequested = true;
				if (observed.IsCreated)
				{
					observed.Dispose();
				}
				observed = default(NativeArray<Color32>);
				_gpuReadbackPending = false;
			}
			else
			{
				_disposeRequested = false;
				gquReq = AsyncGPUReadback.Request((Texture)(object)rt, 0, (Action<AsyncGPUReadbackRequest>)delegate(AsyncGPUReadbackRequest req)
				{
					//IL_003f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0044: Unknown result type (might be due to invalid IL or missing references)
					//IL_0067: Unknown result type (might be due to invalid IL or missing references)
					if (_disposeRequested)
					{
						_disposeRequested = false;
					}
					else
					{
						if (observed.IsCreated)
						{
							observed.Dispose();
						}
						if (!req.hasError)
						{
							ThatsLitPlugin.swScoreCalc.MaybeResume();
							observed = req.GetData<Color32>(0);
							gameworld?.ScoreCalculator?.PreCalculate(PlayerLitScoreProfile, observed, Utility.GetInGameDayTime());
							ThatsLitPlugin.swScoreCalc.Stop();
						}
					}
				});
			}
		}
		Camera obj2 = cam;
		if (obj2 != null && ((Behaviour)obj2).enabled)
		{
			_ = Time.frameCount % 6;
			float num2 = (Player.IsInPronePose ? 0.45f : (2.2f * (0.6f + 0.4f * Player.PoseLevel)));
			float num3 = (Player.IsInPronePose ? 0.2f : 0.7f);
			float num4 = (Player.IsInPronePose ? 1.2f : 0.8f);
			switch (Time.frameCount % 6)
			{
			case 0:
				if (Player.IsInPronePose)
				{
					((Component)cam).transform.localPosition = new Vector3(0f, 2f, 0f);
					((Component)cam).transform.LookAt(Player.Transform.Original.position);
				}
				else
				{
					((Component)cam).transform.localPosition = new Vector3(0f, num2, 0f);
					((Component)cam).transform.LookAt(Player.Transform.Original.position);
				}
				break;
			case 1:
				((Component)cam).transform.localPosition = new Vector3(num4, num2, num4);
				((Component)cam).transform.LookAt(Player.Transform.Original.position + Vector3.up * num3);
				break;
			case 2:
				((Component)cam).transform.localPosition = new Vector3(num4, num2, 0f - num4);
				((Component)cam).transform.LookAt(Player.Transform.Original.position + Vector3.up * num3);
				break;
			case 3:
				if (Player.IsInPronePose)
				{
					((Component)cam).transform.localPosition = new Vector3(0f, 2f, 0f);
					((Component)cam).transform.LookAt(Player.Transform.Original.position);
				}
				else
				{
					((Component)cam).transform.localPosition = new Vector3(0f, -0.5f, 0.35f);
					((Component)cam).transform.LookAt(Player.Transform.Original.position + Vector3.up * 1f);
				}
				break;
			case 4:
				((Component)cam).transform.localPosition = new Vector3(0f - num4, num2, 0f - num4);
				((Component)cam).transform.LookAt(Player.Transform.Original.position + Vector3.up * num3);
				break;
			case 5:
				((Component)cam).transform.localPosition = new Vector3(0f - num4, num2, num4);
				((Component)cam).transform.LookAt(Player.Transform.Original.position + Vector3.up * num3);
				break;
			}
			if (ThatsLitPlugin.DebugTexture.Value && Time.frameCount % 61 == 0)
			{
				RawImage obj3 = display;
				if (obj3 != null && ((Behaviour)obj3).enabled && (Object)(object)rt != (Object)null)
				{
					Graphics.CopyTexture((Texture)(object)rt, slowRT);
				}
			}
		}
		Vector3 position2 = Player.MainParts[(BodyPartType)0].Position;
		Vector3 position3 = Player.MainParts[(BodyPartType)2].Position;
		Vector3 position4 = Player.MainParts[(BodyPartType)3].Position;
		Vector3 position5 = Player.MainParts[(BodyPartType)4].Position;
		Vector3 position6 = Player.MainParts[(BodyPartType)5].Position;
		if ((Object)(object)TOD_Sky.Instance != (Object)null)
		{
			Ray ray = default(Ray);
			float? num5 = gameworld.ScoreCalculator?.CalculateSunLightTimeFactor(ActiveRaidSettings.LocationId, Utility.GetInGameDayTime());
			if (!num5.HasValue)
			{
				num5 = (TOD_Sky.Instance.IsDay ? 1f : 0f);
			}
			Vector3 val = ((num5 > 0.05f) ? TOD_Sky.Instance.LocalSunDirection : TOD_Sky.Instance.LightDirection);
			switch (Time.frameCount % 5)
			{
			case 0:
				ray = new Ray(position2, val);
				break;
			case 1:
				ray = new Ray(position5, val);
				break;
			case 2:
				ray = new Ray(position6, val);
				break;
			case 3:
				ray = new Ray(position3, val);
				break;
			case 4:
				ray = new Ray(position4, val);
				break;
			}
			RaycastHit hit;
			RaycastHit lastPenetrated;
			bool num6 = RaycastIgnoreGlass(ray, 2000f, ambienceRaycastMask, out hit, out lastPenetrated);
			bool flag = false;
			if (num6 || flag)
			{
				ambienceShadownRating += 10f * Time.deltaTime;
			}
			else
			{
				ambienceShadownRating -= 25f * Time.deltaTime;
			}
			ambienceShadownRating = Mathf.Clamp(ambienceShadownRating, 0f, 10f);
		}
		ThatsLitPlugin.swUpdate.Stop();
	}

	private void FixedUpdate()
	{
		//IL_0452: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04de: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_050a: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0544: Unknown result type (might be due to invalid IL or missing references)
		//IL_054f: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0578: Unknown result type (might be due to invalid IL or missing references)
		ThatsLitPlugin.swFUpdate.MaybeResume();
		if (ThatsLitPlugin.BotLookDirectionTweaks.Value)
		{
			Player player = Player;
			if (player != null)
			{
				IHealthController healthController = player.HealthController;
				if (((healthController != null) ? new bool?(healthController.IsAlive) : ((bool?)null)) == false)
				{
					goto IL_0072;
				}
			}
			if (!((Object)(object)gameworld == (Object)null) && ThatsLitPlugin.EnabledMod.Value)
			{
				BotOwner obj = lastNearest;
				Player val = ((obj != null) ? obj.GetPlayer : null);
				if (ThatsLitPlugin.EnableNearestBotSteering.Value && (Object)(object)val != (Object)null && ((Behaviour)val).isActiveAndEnabled && val.MainParts != null && val.MainParts.ContainsKey((BodyPartType)0) && lastNearest.Steering != null && lastNearest.Mover != null)
				{
					EFT.BotMemory memory = lastNearest.Memory;
					EnemyInfo val2 = ((memory != null) ? memory.GoalEnemy : null);
					gameworld.singleIdThrottlers.TryGetValue(lastNearest.ProfileId, out var value);
					if ((object)((val2 != null) ? val2.Person : null) == Player && val2 != null && val2.HaveSeen && Time.time - value.lastForceLook > 1f && lastNearest.Mover.IsMoving && !val.IsSprintEnabled && Vector3.Distance(Player.Position, lastNearest.Position) < 5f && Random.Range(0f, 1f) < 0.6f * Mathf.InverseLerp(15f, 5f, Time.time - val2.TimeLastSeenReal) + 0.5f * Mathf.InverseLerp(7.5f, 1f, Vector3.Distance(Player.Position, val2.EnemyLastPositionReal)))
					{
						lastNearest.Steering.LookToPoint(Player.Position);
						if (DebugInfo != null)
						{
							DebugInfo.forceLooks++;
						}
						value.lastForceLook = Time.time;
						gameworld.singleIdThrottlers[lastNearest.ProfileId] = value;
						ThatsLitPlugin.swFUpdate.Stop();
						return;
					}
					if ((object)((val2 != null) ? val2.Person : null) == Player && val2 != null && val2.HaveSeen && Time.time - val2.TimeLastSeen < 10f && lastNearest.Mover.IsMoving && !val.IsSprintEnabled && Time.time - lastOutside > 1f && Time.time - value.lastForceLook > 1f && Random.Range(0f, 1f) < 0.05f * Mathf.InverseLerp(5f, 1f, val2.Distance))
					{
						BotSteering steering = lastNearest.Steering;
						if (steering != null)
						{
							steering.LookToPoint(Player.Position);
						}
						if (DebugInfo != null)
						{
							DebugInfo.forceLooks++;
						}
						value.lastForceLook = Time.time;
						gameworld.singleIdThrottlers[lastNearest.ProfileId] = value;
						ThatsLitPlugin.swFUpdate.Stop();
						return;
					}
					if (!val.IsSprintEnabled && (val2 == null || !(Time.time - val2.TimeLastSeenReal < 10f)) && Time.time - value.lastSideLook > 10f)
					{
						((MonoBehaviour)lastNearest).StartCoroutine(MakeBotPeekSide(lastNearest));
						if (DebugInfo != null)
						{
							DebugInfo.sideLooks++;
						}
						value.lastSideLook = Time.time;
						gameworld.singleIdThrottlers[lastNearest.ProfileId] = value;
						ThatsLitPlugin.swFUpdate.Stop();
						return;
					}
					Vector3 position = val.MainParts[(BodyPartType)0].Position;
					if ((Object)(object)flashLightHit.collider != (Object)null && Time.time - value.lastForceLook > 1f && ((object)((val2 != null) ? val2.Person : null) == Player || val2 == null) && Vector3.Angle(lastNearest.LookDirection, flashLightHit.normal) > 90f && Vector3.Angle(lastNearest.LookDirection, flashLightHit.point - position) < 90f && Vector3.Angle(lastNearest.LookDirection, Player.Position - position) > 30f && Random.Range(0f, 1f) < 0.05f * Mathf.InverseLerp(20f, 2f, Vector3.Distance(lastNearest.Position, Player.Position)))
					{
						BotSteering steering2 = lastNearest.Steering;
						if (steering2 != null)
						{
							steering2.LookToPoint(Player.Position);
						}
						if (DebugInfo != null)
						{
							DebugInfo.forceLooks++;
						}
						value.lastForceLook = Time.time;
						gameworld.singleIdThrottlers[lastNearest.ProfileId] = value;
						ThatsLitPlugin.swFUpdate.Stop();
						return;
					}
				}
				ThatsLitPlugin.swFUpdate.Stop();
				return;
			}
		}
		goto IL_0072;
		IL_0072:
		ThatsLitPlugin.swFUpdate.Stop();
	}

	private IEnumerator MakeBotPeekSide(BotOwner bot)
	{
		Vector3 val = MyExtensions.RotateAroundPivot(bot.LookDirection, Vector3.up, Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f));
		BotSteering steering = bot.Steering;
		if (steering != null)
		{
			steering.LookToDirection(val);
		}
		yield return (object)new WaitForSeconds(Random.Range(1f, 4f));
		BotSteering steering2 = bot.Steering;
		if (steering2 != null)
		{
			steering2.LookToMovingDirection();
		}
	}

	private float UpdateOverheadHaxCastRating(Vector3 bodyPos, float currentRating)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		currentRating = ((!OverheadHaxCast(bodyPos, out var hit)) ? (currentRating - Time.timeScale * 1.75f) : (currentRating + Time.timeScale * (Mathf.InverseLerp(10f, 1f, hit.distance) - 0.01f)));
		return Mathf.Clamp(currentRating, 0f, 10f);
	}

	private float UpdateSurroundingCastRating(Vector3 bodyPos, float currentRating)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		currentRating = ((!SurroundingCast(bodyPos, out var hit)) ? (currentRating - Time.timeScale * 1.75f) : (currentRating + Time.timeScale * (Mathf.InverseLerp(5f, 1f, hit.distance) - 0.01f)));
		return Mathf.Clamp(currentRating, 0f, 10f);
	}

	private void LateUpdate()
	{
		if ((Object)(object)Player == (Object)null)
		{
			return;
		}
		gameworld.GetWeatherStats(out fog, out rain, out cloud);
		if (PlayerLitScoreProfile != null)
		{
			ThatsLitPlugin.swScoreCalc.MaybeResume();
			gameworld.ScoreCalculator?.CalculateMultiFrameScore(cloud, fog, rain, gameworld, PlayerLitScoreProfile, Utility.GetInGameDayTime(), ActiveRaidSettings.LocationId);
			ThatsLitPlugin.swScoreCalc.Stop();
			if (!PlayerLitScoreProfile.IsProxy)
			{
				ThatsLitAPI.OnPlayerBrightnessScoreCalculatedDirect?.Invoke(this, PlayerLitScoreProfile.frame0.multiFrameLitScore, PlayerLitScoreProfile.frame0.ambienceScore);
				ThatsLitAPI.OnPlayerBrightnessScoreCalculated?.Invoke(Player, PlayerLitScoreProfile.frame0.multiFrameLitScore, PlayerLitScoreProfile.frame0.ambienceScore);
			}
		}
	}

	private bool OverheadHaxCast(Vector3 from, out RaycastHit hit)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		val = new Vector3(0f, 1f, 0f);
		int num = Time.frameCount % 6;
		val = Quaternion.Euler((float)((Time.frameCount % 24 / 6 + 1) * 10), 0f, 0f) * val;
		val = Quaternion.Euler(0f, (float)num * -60f, 0f) * val;
		Ray ray = default(Ray);
		ray = new Ray(from, val);
		RaycastHit lastPenetrated;
		return RaycastIgnoreGlass(ray, 30f, ambienceRaycastMask, out hit, out lastPenetrated);
	}

	private bool SurroundingCast(Vector3 from, out RaycastHit hit)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		val = new Vector3(1f, 0f, 0f);
		int num = Time.frameCount % 60;
		val = Quaternion.Euler(0f, (float)(num * 6), 0f) * val;
		return Physics.Raycast(new Ray(from, val), out hit, 5f, (int)(ambienceRaycastMask));
	}

	private void CastFlashlight()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		flashLightHit = default(RaycastHit);
		if (LightAndLaserState.AnyLightMain)
		{
			AbstractHandsController handsController = Player.HandsController;
			FirearmController val = (FirearmController)(object)((handsController is FirearmController) ? handsController : null);
			if (val != null)
			{
				Physics.Raycast(new Ray(val.FireportPosition, val.WeaponDirection + Random.insideUnitSphere / 7.5f), out flashLightHit, 20f, (int)(ambienceRaycastMask));
			}
		}
	}

	private bool RaycastIgnoreGlass(Ray ray, float distance, LayerMask mask, out RaycastHit hit, out RaycastHit lastPenetrated, int depth = 0, int maxDepth = 10)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Invalid comparison between Unknown and I4
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Invalid comparison between Unknown and I4
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		lastPenetrated = default(RaycastHit);
		hit = default(RaycastHit);
		if (distance < 0f || depth++ >= maxDepth)
		{
			return false;
		}
		if (Physics.Raycast(ray, out hit, distance, (int)(mask)))
		{
			Collider collider = hit.collider;
			object obj;
			if (collider == null)
			{
				obj = null;
			}
			else
			{
				GameObject gameObject = ((Component)collider).gameObject;
				obj = ((gameObject != null) ? gameObject.GetComponent<BallisticCollider>() : null);
			}
			BallisticCollider val = (BallisticCollider)obj;
			if ((Object)(object)val == (Object)null)
			{
				Collider collider2 = hit.collider;
				if (collider2 != null)
				{
					Transform transform = ((Component)collider2).transform;
					if (transform != null)
					{
						Transform parent = transform.parent;
						if (parent != null)
						{
							((Component)parent).GetComponent<BallisticCollider>();
						}
					}
				}
			}
			if (((Object)(object)val == (Object)null || val == null || (int)val.TypeOfMaterial != 10) && (val == null || (int)val.TypeOfMaterial != 11))
			{
				return true;
			}
			lastPenetrated = hit;
			Vector3 point = hit.point;
			Vector3 direction = ray.direction;
			ray.origin = point + direction.normalized * 0.1f;
			hit.distance = hit.distance + 0.1f;
			int layer = ((Component)hit.collider).gameObject.layer;
			Collider collider3 = hit.collider;
			if ((bool)(Object)(object)((collider3 != null) ? ((Component)collider3).gameObject : null))
			{
				((Component)hit.collider).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
			}
			RaycastHit hit2 = default(RaycastHit);
			if (RaycastIgnoreGlass(ray, distance - hit.distance, mask, out hit2, out lastPenetrated, depth))
			{
				Collider collider4 = hit.collider;
				if ((bool)(Object)(object)((collider4 != null) ? ((Component)collider4).gameObject : null))
				{
					((Component)hit.collider).gameObject.layer = layer;
				}
				hit.distance = hit.distance + hit2.distance;
				return true;
			}
			Collider collider5 = hit.collider;
			if ((bool)(Object)(object)((collider5 != null) ? ((Component)collider5).gameObject : null))
			{
				((Component)hit.collider).gameObject.layer = layer;
			}
			return false;
		}
		return false;
	}

	private bool SlicedBubbleSort<T>(T[] subject, int step, int valid) where T : IComparable<T>
	{
		bool flag = true;
		int num = Mathf.Min(valid - 1, subject.Length - 1);
		while (step > 0)
		{
			for (int i = 0; i < num; i++)
			{
				if (subject[i].CompareTo(subject[i + 1]) > 0)
				{
					T val = subject[i];
					subject[i] = subject[i + 1];
					subject[i + 1] = val;
					flag = false;
					step--;
				}
				if (i == num - 1)
				{
					if (flag)
					{
						return true;
					}
					i = -1;
					flag = true;
				}
			}
		}
		return false;
	}

	private void HandleConfigEvents(bool enable)
	{
		if (enable)
		{
			ThatsLitPlugin.DebugTexture.SettingChanged += HandleDebugTextureSettingChanged;
		}
		else
		{
			ThatsLitPlugin.DebugTexture.SettingChanged -= HandleDebugTextureSettingChanged;
		}
	}

	private void HandleDebugTextureSettingChanged(object sender, EventArgs e)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)slowRT == (Object)null)
		{
			slowRT = (Texture)new Texture2D(RESOLUTION, RESOLUTION, (TextureFormat)4, false);
		}
		if ((Object)(object)display == (Object)null)
		{
			display = new GameObject().AddComponent<RawImage>();
			((Component)display).transform.SetParent((Transform)(object)RectTransformExtensions.RectTransform((Component)(object)MonoBehaviourSingleton<GameUI>.Instance));
			RectTransformExtensions.RectTransform((Component)(object)display).sizeDelta = new Vector2(160f, 160f);
			display.texture = slowRT;
			RectTransformExtensions.RectTransform((Component)(object)display).anchoredPosition = new Vector2(-720f, -360f);
		}
		((Behaviour)display).enabled = ThatsLitPlugin.DebugTexture.Value;
	}

	private void OnDestroy()
	{
		DisableBrightness();
		if ((bool)((Object)(object)display))
		{
			Object.Destroy((Object)(object)((Component)display).gameObject);
		}
		if ((bool)((Object)(object)cam))
		{
			Object.Destroy((Object)(object)((Component)cam).gameObject);
		}
		if ((bool)((Object)(object)rt))
		{
			((RenderTexture)rt).Release();
		}
		if (observed.IsCreated)
		{
			observed.Dispose();
		}
	}

	private void OnGUIInfo()
	{
		if (ThatsLitPlugin.ScoreInfo.Value || ThatsLitPlugin.WeatherInfo.Value || ThatsLitPlugin.EquipmentInfo.Value || ThatsLitPlugin.TerrainInfo.Value || ThatsLitPlugin.FoliageInfo.Value)
		{
			switch (ThatsLitPlugin.InfoOffset.Value)
			{
			case 1:
				GUILayout.Label("\n\n\n\n", style, Array.Empty<GUILayoutOption>());
				break;
			case 2:
				GUILayout.Label("\n\n\n\n\n\n\n\n", style, Array.Empty<GUILayoutOption>());
				break;
			case 3:
				GUILayout.Label("\n\n\n\n\n\n\n\n\n\n\n\n", style, Array.Empty<GUILayoutOption>());
				break;
			case 4:
				GUILayout.Label("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n", style, Array.Empty<GUILayoutOption>());
				break;
			case 5:
				GUILayout.Label("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n", style, Array.Empty<GUILayoutOption>());
				break;
			case 6:
				GUILayout.Label("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n", style, Array.Empty<GUILayoutOption>());
				break;
			case 7:
				GUILayout.Label("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n", style, Array.Empty<GUILayoutOption>());
				break;
			}
		}
		if (ThatsLitPlugin.ScoreInfo.Value && gameworld.ScoreCalculator != null && Time.time < setupTime + 10f)
		{
			GUILayout.Label("  [That's Lit] The HUD can be configured in plugin settings.", style, Array.Empty<GUILayoutOption>());
		}
		if (ThatsLitPlugin.ScoreInfo.Value && PlayerLitScoreProfile != null)
		{
			Utility.GUILayoutDrawAsymetricMeter((int)(PlayerLitScoreProfile.frame0.multiFrameLitScore / 0.0999f), alternative: false, style);
			Utility.GUILayoutDrawAsymetricMeter((int)(Mathf.Pow(PlayerLitScoreProfile.frame0.multiFrameLitScore, 3f) / 0.0999f), alternative: false, style);
		}
		if (ThatsLitPlugin.EquipmentInfo.Value && LightAndLaserState.storage != 0L)
		{
			GUILayout.Label(LightAndLaserState.Format(), style, Array.Empty<GUILayoutOption>());
		}
		if (ThatsLitPlugin.FoliageInfo.Value && Foliage != null && Foliage.FoliageScore > 0f)
		{
			Utility.GUILayoutFoliageMeter((int)(Foliage.FoliageScore / 0.0999f), alternative: false, style);
		}
		if (ThatsLitPlugin.TerrainInfo.Value && TerrainDetails != null && terrainScoreHintProne > 0.0998f)
		{
			if (Player.IsInPronePose)
			{
				Utility.GUILayoutTerrainMeter((int)(terrainScoreHintProne / 0.0999f), alternative: false, style);
			}
			else
			{
				Utility.GUILayoutTerrainMeter((int)(terrainScoreHintRegular / 0.0999f), alternative: false, style);
			}
		}
		if (ThatsLitPlugin.WeatherInfo.Value && PlayerLitScoreProfile != null)
		{
			if (cloud <= -1.1f)
			{
				GUILayout.Label("  CLEAR ☀☀☀", style, Array.Empty<GUILayoutOption>());
			}
			else if (cloud <= -0.7f)
			{
				GUILayout.Label("  CLEAR ☀☀", style, Array.Empty<GUILayoutOption>());
			}
			else if (cloud <= -0.25f)
			{
				GUILayout.Label("  CLEAR ☀", style, Array.Empty<GUILayoutOption>());
			}
			else if (cloud >= 1.1f)
			{
				GUILayout.Label("  CLOUDY ☁☁☁", style, Array.Empty<GUILayoutOption>());
			}
			else if (cloud >= 0.7f)
			{
				GUILayout.Label("  CLOUDY ☁☁", style, Array.Empty<GUILayoutOption>());
			}
			else if (cloud >= 0.25f)
			{
				GUILayout.Label("  CLOUDY ☁", style, Array.Empty<GUILayoutOption>());
			}
		}
	}

	private void OnGUI()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0723: Unknown result type (might be due to invalid IL or missing references)
		//IL_079b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a75: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a6e: Unknown result type (might be due to invalid IL or missing references)
		Player player = Player;
		if (player == null || !player.IsYourPlayer)
		{
			return;
		}
		_ = GUI.skin.label.alignment;
		if (style == null)
		{
			style = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)3
			};
			if (ThatsLitPlugin.InfoFontSizeOverride.Value != 0)
			{
				style.fontSize = ThatsLitPlugin.InfoFontSizeOverride.Value;
			}
		}
		if (style.fontSize != ThatsLitPlugin.InfoFontSizeOverride.Value)
		{
			if (ThatsLitPlugin.InfoFontSizeOverride.Value != 0)
			{
				style.fontSize = ThatsLitPlugin.InfoFontSizeOverride.Value;
			}
			else
			{
				style.fontSize = GUI.skin.label.fontSize;
			}
		}
		bool flag = guiFrame < Time.frameCount;
		ThatsLitPlugin.swGUI.MaybeResume();
		if (PlayerLitScoreProfile == null && Time.time - setupTime < 30f && !ThatsLitPlugin.HideMapTip.Value)
		{
			GUILayout.Label("  [That's Lit] Brightness module is disabled in configs or not supported on this map.", style, Array.Empty<GUILayoutOption>());
		}
		float num = Player.PoseLevel / Player.Physical.MaxPoseLevel * 0.6f + 0.4f;
		if (Player.IsInPronePose)
		{
			num -= 0.4f;
		}
		num += 0.05f;
		OnGUIInfo();
		ThatsLitAPI.OnMainPlayerGUI?.Invoke();
		if (!ThatsLitPlugin.DebugInfo.Value || DebugInfo == null)
		{
			ThatsLitPlugin.swGUI.Stop();
			guiFrame = Time.frameCount;
			return;
		}
		WeatherController instance = WeatherController.Instance;
		float? obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			IWeatherCurve weatherCurve = instance.WeatherCurve;
			obj = ((weatherCurve != null) ? new float?(weatherCurve.Fog) : ((float?)null));
		}
		float? num2 = obj;
		float valueOrDefault = num2.GetValueOrDefault();
		WeatherController instance2 = WeatherController.Instance;
		float? obj2;
		if (instance2 == null)
		{
			obj2 = null;
		}
		else
		{
			IWeatherCurve weatherCurve2 = instance2.WeatherCurve;
			obj2 = ((weatherCurve2 != null) ? new float?(weatherCurve2.Rain) : ((float?)null));
		}
		num2 = obj2;
		float valueOrDefault2 = num2.GetValueOrDefault();
		WeatherController instance3 = WeatherController.Instance;
		float? obj3;
		if (instance3 == null)
		{
			obj3 = null;
		}
		else
		{
			IWeatherCurve weatherCurve3 = instance3.WeatherCurve;
			obj3 = ((weatherCurve3 != null) ? new float?(weatherCurve3.Cloudiness) : ((float?)null));
		}
		num2 = obj3;
		float valueOrDefault3 = num2.GetValueOrDefault();
		if (flag)
		{
			object[] obj4 = new object[69]
			{
				DebugInfo.lastCalcFrom,
				DebugInfo.lastCalcTo0,
				DebugInfo.lastCalcTo1,
				DebugInfo.lastCalcTo2,
				DebugInfo.lastCalcTo3,
				DebugInfo.lastCalcTo4,
				DebugInfo.lastCalcTo5,
				DebugInfo.lastCalcTo6,
				DebugInfo.lastCalcTo7,
				DebugInfo.lastCalcTo8,
				DebugInfo.lastFactor2,
				DebugInfo.lastFactor1,
				DebugInfo.lastScore,
				ambScoreSample,
				litFactorSample,
				DebugInfo.calced,
				DebugInfo.calcedLastFrame,
				DebugInfo.encounter,
				DebugInfo.vagueHint,
				DebugInfo.vagueHintCancel,
				DebugInfo.signalDanger,
				lastShotVector,
				Time.time - lastShotTime,
				DebugInfo.lastEncounterShotAngleDelta,
				DebugInfo.lastEncounteringShotCutoff,
				DebugInfo.lastVisiblePartsFactor,
				DebugInfo.lastGlobalOverlookChance,
				DebugInfo.lastDisCompThermal,
				DebugInfo.lastDisCompNVG,
				DebugInfo.lastDisCompDay,
				DebugInfo.lastDisComp,
				DebugInfo.lastNearestFocusAngleX,
				DebugInfo.lastNearestFocusAngleY,
				terrainScoreHintProne,
				terrainScoreHintRegular,
				TerrainDetails?.RecentDetailCount3x3,
				TerrainDetails?.RecentDetailCount5x5,
				PlayerLitScoreProfile?.detailBonusSmooth,
				Foliage?.FoliageScore,
				Foliage?.FoliageCount,
				Foliage?.Nearest?.dis,
				Foliage?.Nearest?.name,
				DebugInfo.lastBushRat,
				valueOrDefault,
				valueOrDefault2,
				valueOrDefault3,
				Utility.GetInGameDayTime(),
				gameworld.IsWinter,
				num,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			Vector3 velocity = Player.Velocity;
			obj4[49] = velocity.magnitude;
			obj4[50] = Time.time - lastOutside;
			obj4[51] = ambienceShadownRating;
			obj4[52] = overheadHaxRating;
			obj4[53] = bunkerTimeClamped;
			obj4[54] = surroundingRating;
			obj4[55] = DebugInfo.nearestOffset;
			obj4[56] = DebugInfo.nearestCaution;
			obj4[57] = DebugInfo.cancelledSAINNoBush;
			obj4[58] = DebugInfo.attemptToCancelSAINNoBush;
			obj4[59] = DebugInfo.lastInterruptChance;
			obj4[60] = DebugInfo.lastInterruptChanceDis;
			obj4[61] = DebugInfo.sniperHintOffset;
			obj4[62] = DebugInfo.sniperHintChance;
			obj4[63] = flashLightHit.point;
			obj4[64] = DebugInfo.flashLightHint;
			obj4[65] = DebugInfo.forceLooks;
			obj4[66] = DebugInfo.sideLooks;
			obj4[67] = !((Behaviour)cam).enabled;
			obj4[68] = cameraThrottleFrequency;
			infoCache1 = string.Format("  IMPACT: {0:0.000} -> {1:0.000} -> {2:0.000} -> {3:0.000} -> {4:0.000} -> {5:0.000} -> {6:0.000} -> {7:0.000} -> {8:0.000} -> {9:0.000}\n ({10:0.000} <- {11:0.000} <- {12:0.000}) AMB: {13:0.00} LIT: {14:0.00} (SAMPLE)\n  AFFECTED: {15} (+{16})\n  ENCOUNTER: {17}  V.HINT: {18}  V.CANCEL: {19}  SIG.D: {20}\n  LAST SHOT: {21} {22:0.0}s {23}deg  -{24}x\n  LAST_PARTS: {25} (x9)  G.OVL: {26:P1}\n  V.DIS.COMP: {27}(T)  {28}(NVG)  {29}(Day)  {30}  Focus: {31:0.0}degX/{32:0.0}degY\n  TERRAIN: {33:0.000}/{34:0.000}  3x3/5x5: {35}/{36} (score-{37:0.00})  FOLIAGE: {38:0.000} ({39}) (H{40:0.00} to {41}) RAT: {42:0.00}\n  FOG: {43:0.000} / RAIN: {44:0.000} / CLOUD: {45:0.000} / TIME: {46:0.000} / WINTER: {47}\n  POSE: {48} SPEED: {49:0.000}  INSIDE: {50:0.000}  AMB: {51:0.000}  OVH: {52:0.000}  BNKR: {53:0.000}  SRD: {54:0.000}\n  {55}  Caution: {56}  NoBush.Cancel: {57}/{58} {59:P1}  {60:0.0}m SNP: {61} ({62:P1})  FL: {63:000.0}  HINT:{64}\n  F.L:{65}  S.L:{66}\n Throttle: {67} {68}", obj4);
		}
		GUILayout.Label(infoCache1, style, Array.Empty<GUILayoutOption>());
		OnGUIScoreCalc(style);
		if (IsDebugSampleFrame)
		{
			litFactorSample = PlayerLitScoreProfile?.litScoreFactor ?? 0f;
			ambScoreSample = PlayerLitScoreProfile?.frame0.ambienceScore ?? 0f;
		}
		if (Time.frameCount % 19 == 1 && ThatsLitPlugin.EnableBenchmark.Value && flag)
		{
			ConcludeBenchmarks();
		}
		if (ThatsLitPlugin.EnableBenchmark.Value)
		{
			if (flag)
			{
				infoCacheBenchmark = $"  Update:         {benchmarkSampleUpdate,8:0.0000}\n  FUpdate:         {benchmarkSampleFUpdate,8:0.0000}\n  Foliage:        {benchmarkSampleFoliageCheck,8:0.0000}\n  Terrain:        {benchmarkSampleTerrainCheck,8:0.0000}\n  SeenCoef:       {benchmarkSampleSeenCoef,8:0.0000}\n  Encountering:   {benchmarkSampleEncountering,8:0.0000}\n  ExtraVisDis:    {benchmarkSampleExtraVisDis,8:0.0000}\n  ScoreCalculator:{benchmarkSampleScoreCalculator,8:0.0000}\n  Info(+Debug):    {benchmarkSampleGUI,8:0.0000}\n  No Bush OVR:    {benchmarkSampleNoBushOverride,8:0.0000}\n  BlindFire:    {benchmarkSampleBlindFire,8:0.0000} ms";
			}
			GUILayout.Label(infoCacheBenchmark, style, Array.Empty<GUILayoutOption>());
			if (Time.frameCount % 6000 == 0 && flag)
			{
				ConsoleScreen.Log(infoCacheBenchmark);
			}
		}
		gameworld.ScoreCalculator?.OnGUI(PlayerLitScoreProfile, flag);
		if (ThatsLitPlugin.DebugTerrain.Value && TerrainDetails?.Details5x5 != null)
		{
			infoCache2 = $"  DETAIL (SAMPLE): {DebugInfo?.lastFinalDetailScoreNearest:+0.00;-0.00;+0.00} ({DebugInfo?.lastDisFactorNearest:0.000}df) 3x3: {TerrainDetails.RecentDetailCount3x3}\n  {Utility.DetermineDir(DebugInfo?.lastTriggeredDetailCoverDirNearest ?? Vector3.zero)} {DebugInfo?.lastNearest:0.00}m {DebugInfo?.lastTiltAngle} {DebugInfo?.lastRotateAngle}";
			GUILayout.Label(infoCache2, style, Array.Empty<GUILayoutOption>());
			for (int i = TerrainDetails.GetDetailInfoIndex(2, 2, 0, gameworld.MaxDetailTypes); i < TerrainDetails.GetDetailInfoIndex(3, 2, 0, gameworld.MaxDetailTypes); i++)
			{
				if (TerrainDetails.Details5x5[i].casted)
				{
					GUILayout.Label($"  {TerrainDetails.Details5x5[i].count} Detail#{i}({TerrainDetails.Details5x5[i].name}))", style, Array.Empty<GUILayoutOption>());
				}
			}
			Utility.GUILayoutDrawAsymetricMeter((int)(DebugInfo.lastFinalDetailScoreNearest / 0.0999f), alternative: false, style);
		}
		ThatsLitPlugin.swGUI.Stop();
		guiFrame = Time.frameCount;
	}

	internal virtual void OnGUIScoreCalc(GUIStyle style)
	{
		if (PlayerLitScoreProfile == null || DebugInfo == null)
		{
			return;
		}
		if (PlayerLitScoreProfile.IsProxy)
		{
			GUILayout.Label("  [PROXY]", style, Array.Empty<GUILayoutOption>());
			return;
		}
		if (IsDebugSampleFrame)
		{
			DebugInfo.shinePixelsRatioSample = (PlayerLitScoreProfile.frame0.RatioShinePixels + PlayerLitScoreProfile.frame1.RatioShinePixels + PlayerLitScoreProfile.frame2.RatioShinePixels + PlayerLitScoreProfile.frame3.RatioShinePixels + PlayerLitScoreProfile.frame4.RatioShinePixels + PlayerLitScoreProfile.frame5.RatioShinePixels) / 6f;
			DebugInfo.highLightPixelsRatioSample = (PlayerLitScoreProfile.frame0.RatioHighPixels + PlayerLitScoreProfile.frame1.RatioHighPixels + PlayerLitScoreProfile.frame2.RatioHighPixels + PlayerLitScoreProfile.frame3.RatioHighPixels + PlayerLitScoreProfile.frame4.RatioHighPixels + PlayerLitScoreProfile.frame5.RatioHighPixels) / 6f;
			DebugInfo.highMidLightPixelsRatioSample = (PlayerLitScoreProfile.frame0.RatioHighMidPixels + PlayerLitScoreProfile.frame1.RatioHighMidPixels + PlayerLitScoreProfile.frame2.RatioHighMidPixels + PlayerLitScoreProfile.frame3.RatioHighMidPixels + PlayerLitScoreProfile.frame4.RatioHighMidPixels + PlayerLitScoreProfile.frame5.RatioHighMidPixels) / 6f;
			DebugInfo.midLightPixelsRatioSample = (PlayerLitScoreProfile.frame0.RatioMidPixels + PlayerLitScoreProfile.frame1.RatioMidPixels + PlayerLitScoreProfile.frame2.RatioMidPixels + PlayerLitScoreProfile.frame3.RatioMidPixels + PlayerLitScoreProfile.frame4.RatioMidPixels + PlayerLitScoreProfile.frame5.RatioMidPixels) / 6f;
			DebugInfo.midLowLightPixelsRatioSample = (PlayerLitScoreProfile.frame0.RatioMidLowPixels + PlayerLitScoreProfile.frame1.RatioMidLowPixels + PlayerLitScoreProfile.frame2.RatioMidLowPixels + PlayerLitScoreProfile.frame3.RatioMidLowPixels + PlayerLitScoreProfile.frame4.RatioMidLowPixels + PlayerLitScoreProfile.frame5.RatioMidLowPixels) / 6f;
			DebugInfo.lowLightPixelsRatioSample = (PlayerLitScoreProfile.frame0.RatioLowPixels + PlayerLitScoreProfile.frame1.RatioLowPixels + PlayerLitScoreProfile.frame2.RatioLowPixels + PlayerLitScoreProfile.frame3.RatioLowPixels + PlayerLitScoreProfile.frame4.RatioLowPixels + PlayerLitScoreProfile.frame5.RatioLowPixels) / 6f;
			DebugInfo.darkPixelsRatioSample = (PlayerLitScoreProfile.frame0.RatioDarkPixels + PlayerLitScoreProfile.frame1.RatioDarkPixels + PlayerLitScoreProfile.frame2.RatioDarkPixels + PlayerLitScoreProfile.frame3.RatioDarkPixels + PlayerLitScoreProfile.frame4.RatioDarkPixels + PlayerLitScoreProfile.frame5.RatioDarkPixels) / 6f;
		}
		if (guiFrame < Time.frameCount)
		{
			infoCache = $"  PIXELS: {DebugInfo.shinePixelsRatioSample * 100f:000}% - {DebugInfo.highLightPixelsRatioSample * 100f:000}% - {DebugInfo.highMidLightPixelsRatioSample * 100f:000}% - {DebugInfo.midLightPixelsRatioSample * 100f:000}% - {DebugInfo.midLowLightPixelsRatioSample * 100f:000}% - {DebugInfo.lowLightPixelsRatioSample * 100f:000}% | {DebugInfo.darkPixelsRatioSample * 100f:000}% (AVG Sample)\n  AvgLum: {PlayerLitScoreProfile.frame0.avgLum:0.000}  AvgLumMF: {PlayerLitScoreProfile.frame0.avgLumMultiFrames:0.000} / {gameworld.ScoreCalculator.GetMinAmbianceLum():0.000} ~ {gameworld?.ScoreCalculator?.GetMaxAmbianceLum():0.000} ({gameworld?.ScoreCalculator?.GetAmbianceLumRange():0.000})\n   Sun: {gameworld?.ScoreCalculator?.sunLightScore:0.000}/{gameworld?.ScoreCalculator?.GetMaxSunlightScore():0.000}, Moon: {gameworld?.ScoreCalculator?.moonLightScore:0.000}/{gameworld?.ScoreCalculator?.GetMaxMoonlightScore():0.000}\n  SCORE : {DebugInfo.scoreRawBase:＋0.00;－0.00;+0.00} -> {DebugInfo.scoreRaw0:＋0.00;－0.00;+0.00} -> {DebugInfo.scoreRaw1:＋0.00;－0.00;+0.00} -> {DebugInfo.scoreRaw2:＋0.00;－0.00;+0.00} -> {DebugInfo.scoreRaw3:＋0.00;－0.00;+0.00} -> {DebugInfo.scoreRaw4:＋0.00;－0.00;+0.00} (SAMPLE)";
		}
		GUILayout.Label(infoCache, style, Array.Empty<GUILayoutOption>());
		Utility.GUILayoutDrawAsymetricMeter((int)(PlayerLitScoreProfile.frame0.score / 0.0999f), alternative: false, style);
	}

	private void ConcludeBenchmarks()
	{
		benchmarkSampleSeenCoef = ThatsLitPlugin.swSeenCoef.ConcludeMs() / 19f;
		benchmarkSampleEncountering = ThatsLitPlugin.swEncountering.ConcludeMs() / 19f;
		benchmarkSampleExtraVisDis = ThatsLitPlugin.swExtraVisDis.ConcludeMs() / 19f;
		benchmarkSampleScoreCalculator = ThatsLitPlugin.swScoreCalc.ConcludeMs() / 19f;
		benchmarkSampleUpdate = ThatsLitPlugin.swUpdate.ConcludeMs() / 19f;
		benchmarkSampleFUpdate = ThatsLitPlugin.swFUpdate.ConcludeMs() / 19f;
		benchmarkSampleGUI = ThatsLitPlugin.swGUI.ConcludeMs() / 19f;
		benchmarkSampleFoliageCheck = ThatsLitPlugin.swFoliage.ConcludeMs() / 19f;
		benchmarkSampleTerrainCheck = ThatsLitPlugin.swTerrain.ConcludeMs() / 19f;
		benchmarkSampleNoBushOverride = ThatsLitPlugin.swNoBushOverride.ConcludeMs() / 19f;
		benchmarkSampleBlindFire = ThatsLitPlugin.swBlindFireScatter.ConcludeMs() / 19f;
	}
}
