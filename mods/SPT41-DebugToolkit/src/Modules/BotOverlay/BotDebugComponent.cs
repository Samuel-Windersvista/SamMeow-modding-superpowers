using System;
using System.Collections.Generic;
using System.Text;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using UnityEngine;

namespace SamMeow.DebugToolkit.Modules.BotOverlay
{
    /// <summary>
    /// Bot 头顶调试覆盖层组件。移植自 BotDebug <c>Components/BotDebugComponent.cs</c>（1.8.0）。
    /// </summary>
    /// <remarks>
    /// 4.1.6 / Mono 形态要点：
    /// <list type="bullet">
    /// <item>宿主是 <c>GameWorld.gameObject</c>：所有失败/禁用路径只能 <c>Destroy(this)</c>，
    /// 绝不 <c>Destroy(gameObject)</c>（会摧毁游戏世界）。</item>
    /// <item><c>DebugBotStruct</c> 为值类型 struct，无 null 解引用；仅引用型成员做空判。</item>
    /// <item><c>CameraManager.Instance</c> 永不为 null，但其 <c>SSAA</c> 属性在相机装配前可能为 null，
    /// 取宽度比前必须空判（见 <see cref="ApplyScreenScale"/>）。</item>
    /// <item><c>AllAlivePlayersList</c> 是托管 <c>List&lt;Player&gt;</c>，可安全索引遍历。</item>
    /// </list>
    /// </remarks>
    internal class BotDebugComponent : MonoBehaviour
    {
        private GameWorld gameWorld;
        private BotSpawner botSpawner;
        private Player localPlayer;

        private GUIStyle guiStyle;
        private float nextUpdateTime;
        private bool updateGuiPending;

        private readonly Dictionary<string, BotOverlayEntry> botMap = new Dictionary<string, BotOverlayEntry>();
        private readonly List<string> deadList = new List<string>();

        private float screenScale = 1.0f;
        private EventHandler fontChangedHandler;
        private bool cleanedUp;

        public void Awake()
        {
            try
            {
                Plugin.Logger?.LogInfo("[BotOverlay] BotDebugComponent enabled");

                ApplyScreenScale();

                // FontSize 变更时重建样式；保存 handler 以便 Dispose/OnDestroy 退订，避免跨局泄漏。
                fontChangedHandler = OnFontSizeChanged;
                if (BotOverlaySettings.FontSize != null)
                {
                    BotOverlaySettings.FontSize.SettingChanged += fontChangedHandler;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BotOverlay] 组件初始化失败（覆盖层可能不可用）：{ex}");
            }
        }

        /// <summary>
        /// 惰性解析战局引用（botSpawner / gameWorld / localPlayer）。
        /// 与 InvincibilityRaidComponent / QoLRaidComponent 的惰性模式一致：未就绪返回 false，
        /// 后续帧可重试；避免 Awake 一次性解析失败后整局静默失效。
        /// </summary>
        private bool TryResolveReferences()
        {
            try
            {
                if (gameWorld == null)
                {
                    gameWorld = Singleton<GameWorld>.Instance;
                }

                if (gameWorld != null && localPlayer == null)
                {
                    localPlayer = gameWorld.MainPlayer;
                }

                if (botSpawner == null && Singleton<IBotGame>.Instantiated)
                {
                    botSpawner = Singleton<IBotGame>.Instance.BotsController?.BotSpawner;
                }

                return gameWorld != null && localPlayer != null && botSpawner != null;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] 战局引用解析失败（后续帧重试）：{ex.Message}");
                return false;
            }
        }

        /// <summary>手动禁用入口（配置关闭 / 模块停用）。只销毁本组件，不触碰宿主对象。</summary>
        public void Dispose()
        {
            Plugin.Logger?.LogInfo("[BotOverlay] BotDebugComponent disabled");
            Cleanup();
            Destroy(this);
        }

        private void OnDestroy()
        {
            // Destroy(this) 触发；与 Dispose 幂等。
            Cleanup();
        }

        private void Cleanup()
        {
            if (cleanedUp)
            {
                return;
            }

            cleanedUp = true;
            try
            {
                if (fontChangedHandler != null && BotOverlaySettings.FontSize != null)
                {
                    BotOverlaySettings.FontSize.SettingChanged -= fontChangedHandler;
                }

                fontChangedHandler = null;
                botMap.Clear();
                deadList.Clear();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] 组件清理异常（已忽略）：{ex.Message}");
            }
        }

        private void OnFontSizeChanged(object sender, EventArgs e)
        {
            try
            {
                CreateGuiStyle();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] 重建 GUI 样式失败：{ex.Message}");
            }
        }

        /// <summary>DLSS/FSR 开启时按 输出/输入 宽度比补偿屏幕坐标。</summary>
        private void ApplyScreenScale()
        {
            try
            {
                var cameraManager = CameraManager.Instance;
                // SSAA 属性（=> _sSAA）在相机装配前可能为 null，必须空判。
                var ssaa = cameraManager != null ? cameraManager.SSAA : null;
                if (ssaa != null && ssaa.isActiveAndEnabled)
                {
                    int inputWidth = ssaa.GetInputWidth();
                    if (inputWidth > 0)
                    {
                        screenScale = (float)ssaa.GetOutputWidth() / inputWidth;
                        Plugin.Logger?.LogDebug($"[BotOverlay] DLSS or FSR is enabled, scale screen offsets by {screenScale}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 缩放探测失败无碍绘制：回退 1.0。
                screenScale = 1.0f;
                Plugin.Logger?.LogWarning($"[BotOverlay] SSAA 探测失败（回退 1.0）：{ex.Message}");
            }
        }

        private void CreateGuiStyle()
        {
            guiStyle = new GUIStyle(GUI.skin.box);
            guiStyle.alignment = TextAnchor.MiddleLeft;
            guiStyle.fontSize = BotOverlaySettings.FontSize != null ? BotOverlaySettings.FontSize.Value : 24;
            guiStyle.margin = new RectOffset(3, 3, 3, 3);
            guiStyle.richText = true;
        }

        private void Update()
        {
            try
            {
                if (!BotOverlaySettings.Initialized || !BotOverlaySettings.Enable.Value || !TryResolveReferences())
                {
                    return;
                }

                if (BotOverlaySettings.NextModeKey.Value.IsDown())
                {
                    BotOverlaySettings.SetActiveMode(BotOverlaySettings.GetActiveMode().Next());
                    updateGuiPending = true;
                }
                else if (BotOverlaySettings.PrevModeKey.Value.IsDown())
                {
                    BotOverlaySettings.SetActiveMode(BotOverlaySettings.GetActiveMode().Previous());
                    updateGuiPending = true;
                }

                // 数据每秒轮询一次。
                if (Time.time < nextUpdateTime)
                {
                    return;
                }

                nextUpdateTime = Time.time + 1.0f;

                var players = gameWorld.AllAlivePlayersList;
                if (players == null)
                {
                    return;
                }

                for (int i = 0; i < players.Count; i++)
                {
                    Player player = players[i];
                    if (player == null)
                    {
                        continue;
                    }

                    string profileId = player.ProfileId;
                    if (string.IsNullOrEmpty(profileId))
                    {
                        continue;
                    }

                    DebugBotStruct data;
                    try
                    {
                        data = botSpawner.GetBotDebugData(localPlayer, profileId);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogWarning($"[BotOverlay] GetBotDebugData 失败（跳过 {profileId}）：{ex.Message}");
                        continue;
                    }

                    if (!botMap.TryGetValue(profileId, out var botData))
                    {
                        botData = new BotOverlayEntry();
                        botMap.Add(profileId, botData);
                    }

                    botData.SetData(data);
                }

                updateGuiPending = true;
            }
            catch (Exception ex)
            {
                // fail-open：Update 异常不得穿透 MonoBehaviour 消息链。
                Plugin.Logger?.LogWarning($"[BotOverlay] Update 异常（已忽略）：{ex.Message}");
            }
        }

        private void OnGUI()
        {
            if (!BotOverlaySettings.Initialized || !BotOverlaySettings.Enable.Value)
            {
                return;
            }

            try
            {
                if (guiStyle == null)
                {
                    CreateGuiStyle();
                }

                if (guiStyle == null || Camera.main == null)
                {
                    return;
                }

                foreach (var bot in botMap)
                {
                    // DebugBotStruct 为值类型：以 InitedBotData 判定可用性，而非 null 比较。
                    var debugBotStruct = bot.Value.Data;
                    if (!debugBotStruct.InitedBotData)
                    {
                        continue;
                    }

                    var playerOwner = debugBotStruct.PlayerOwner;
                    if (playerOwner == null)
                    {
                        deadList.Add(bot.Key);
                        continue;
                    }

                    IAIData aiData = playerOwner.AIData;
                    if (aiData?.BotOwner == null)
                    {
                        deadList.Add(bot.Key);
                        continue;
                    }

                    // 超过 3 秒未刷新视为死亡，移除条目。
                    if (Time.time - bot.Value.LastUpdate >= 3f)
                    {
                        deadList.Add(bot.Key);
                        continue;
                    }

                    if (bot.Value.GuiContent == null)
                    {
                        bot.Value.GuiContent = new GUIContent();
                    }

                    // GuiRect 为值类型（Rect），无需判空；上游的 null 比较是无意残留。

                    IPlayer iPlayer = playerOwner.iPlayer;
                    if (iPlayer == null || localPlayer == null)
                    {
                        deadList.Add(bot.Key);
                        continue;
                    }

                    Vector3 aboveBotHeadPos = iPlayer.Position + (Vector3.up * 1.5f);
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(aboveBotHeadPos);
                    if (screenPos.z > 0)
                    {
                        if (updateGuiPending)
                        {
                            StringBuilder botInfoStringBuilder =
                                BotInfo.GetInfoText(debugBotStruct, localPlayer, BotOverlaySettings.GetActiveMode());
                            bot.Value.GuiContent.text = botInfoStringBuilder != null
                                ? botInfoStringBuilder.ToString().TrimEnd('\r', '\n')
                                : "";
                        }

                        int dist = Mathf.RoundToInt((iPlayer.Position - localPlayer.Transform.position).magnitude);
                        if (bot.Value.GuiContent.text.Length > 0 && dist < BotOverlaySettings.MaxDrawDistance.Value)
                        {
                            Vector2 guiSize = guiStyle.CalcSize(bot.Value.GuiContent);
                            bot.Value.GuiRect.x = (screenPos.x * screenScale) - (guiSize.x / 2);
                            bot.Value.GuiRect.y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
                            bot.Value.GuiRect.size = guiSize;

                            GUI.Box(bot.Value.GuiRect, bot.Value.GuiContent, guiStyle);
                        }
                    }
                }

                updateGuiPending = false;

                // 延迟删除，避免遍历中改集合。
                for (int i = 0; i < deadList.Count; i++)
                {
                    botMap.Remove(deadList[i]);
                }

                deadList.Clear();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] OnGUI 绘制异常（已忽略）：{ex.Message}");
            }
        }

        /// <summary>战局开始时挂载组件（由 <see cref="GameStartPatch"/> 调用）。</summary>
        public static void Enable()
        {
            try
            {
                if (!Singleton<IBotGame>.Instantiated)
                {
                    return;
                }

                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld == null || gameWorld.gameObject == null)
                {
                    return;
                }

                if (gameWorld.gameObject.GetComponent<BotDebugComponent>() == null)
                {
                    gameWorld.gameObject.AddComponent<BotDebugComponent>();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] 挂载组件失败（已忽略）：{ex.Message}");
            }
        }

        /// <summary>移除组件（配置关闭 / 模块停用）。</summary>
        public static void Disable()
        {
            try
            {
                if (!Singleton<IBotGame>.Instantiated)
                {
                    return;
                }

                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld == null)
                {
                    return;
                }

                var component = gameWorld.GetComponent<BotDebugComponent>();
                if (component != null)
                {
                    component.Dispose();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] 移除组件失败（已忽略）：{ex.Message}");
            }
        }

        /// <summary>覆盖层条目（避免与 EFT <c>DebugBotStruct.BotData</c> 同名异义）。</summary>
        internal class BotOverlayEntry
        {
            public void SetData(DebugBotStruct botData)
            {
                LastUpdate = Time.time;
                Data = botData;
            }

            public float LastUpdate;
            public DebugBotStruct Data;
            public GUIContent GuiContent;
            public Rect GuiRect;
        }
    }
}
