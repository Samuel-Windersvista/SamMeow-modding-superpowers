using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EFT.ScenesPreset;

namespace FontReplace
{
    public partial class FontReplacePlugin : BaseUnityPlugin
    {
        private void LoadFontAsset(string bundleName)
        {
            var picked = string.IsNullOrEmpty(bundleName) ? DefaultBundleName : bundleName;
            if (string.IsNullOrEmpty(picked))
            {
                Logger.LogWarning("[FontReplace] 未选择任何字体资源，跳过加载");
                return;
            }

            var fontDir = Path.Combine(pluginDir, FontDirName);
            var bundlePath = Path.Combine(fontDir, picked);
            AssetBundle ab = AssetBundle.LoadFromFile(bundlePath);
            if (ab == null)
            {
                Logger.LogError("[FontReplace] 加载 AssetBundle 失败: " + bundlePath);
                return;
            }

            var fontAssetName = Path.GetFileNameWithoutExtension(picked);
            TMP_FontAsset asset = ab.LoadAsset<TMP_FontAsset>(fontAssetName);
            if (asset == null)
            {
                var assets = ab.LoadAllAssets<TMP_FontAsset>();
                if (assets == null || assets.Length == 0)
                {
                    Logger.LogError("[FontReplace] AssetBundle 中未找到任何 TMP_FontAsset");
                    ab.Unload(false);
                    return;
                }

                asset = assets[0];
            }

            if (asset == null)
            {
                Logger.LogError("[FontReplace] AssetBundle 中未找到任何 TMP_FontAsset");
                ab.Unload(false);
                return;
            }

            _chineseFontAsset = asset;
            _chineseUnityFont = asset.sourceFontFile;

            Logger.LogInfo("[FontReplace] 已加载字体资源: " + asset.sourceFontFile + " (" + picked + ")");
            Logger.LogInfo("[FontReplace] 字体名=" + asset.name + ", 字体家族=" + asset.faceInfo.familyName + ", 样式=" + asset.faceInfo.styleName);
            Logger.LogInfo("[FontReplace] " + asset.atlasPopulationMode);
            Logger.LogInfo("[FontReplace] " + asset.atlasRenderMode);

            ab.Unload(false);
        }

        private void RegisterLocaleListener()
        {
            var localeManager = LocaleManagerCompat.GetInstance(Logger);
            if (localeManager == null)
            {
                Logger.LogWarning("[FontReplace] 未找到 LocaleManager；仅在场景加载时生效。");
                return;
            }

            _unsubscribeLocaleUpdate = LocaleManagerCompat.TrySubscribeLocaleUpdate(localeManager, OnLocaleUpdated, Logger);
            _hasLocaleListener = _unsubscribeLocaleUpdate != null;

            if (!_hasLocaleListener)
            {
                _unsubscribeLocaleUpdate = delegate { };
            }

            ConfigureFallbacks(localeManager);
            TryApplyChineseFont(localeManager, "initial");
        }

        private void RegisterSceneListener()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneListenerRegistered = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var localeManager = LocaleManagerCompat.GetInstance(Logger);
            if (localeManager != null)
            {
                TryApplyChineseFont(localeManager, "sceneLoaded");
            }
            else
            {
                // 没有 LocaleManager 的情况下，仍然尝试刷新 TMP 默认字体
                ApplyDefaultFontAndRefresh("sceneLoaded(noLocaleManager)");
            }
        }

        private void OnLocaleUpdated()
        {
            var localeManager = LocaleManagerCompat.GetInstance(Logger);
            if (localeManager == null)
            {
                return;
            }

            ConfigureFallbacks(localeManager);
            TryApplyChineseFont(localeManager, "update");
        }

        private void TryApplyChineseFont(LocaleManagerClass localeManager, string reason)
        {
            if (_modEnabled != null && !_modEnabled.Value)
            {
                _isChineseLocaleActive = false;
                Logger.LogInfo("[FontReplace] 模组已禁用，跳过字体覆盖 (" + reason + ")");
                return;
            }

            if (_isApplying)
            {
                Logger.LogInfo("[FontReplace] 跳过应用（重新申请）: " + reason);
                return;
            }

            if (_chineseFontAsset == null)
            {
                Logger.LogWarning("[FontReplace] 字体资源尚未加载成功 (" + reason + ").");
                return;
            }

            var currentLang = LocaleManagerCompat.GetCurrentLanguage(localeManager);
            var appliedLang = LocaleManagerCompat.GetAppliedLanguage(localeManager);
            Logger.LogInfo("[FontReplace] 当前语言=" + currentLang + ", 生效语言=" + appliedLang + " (" + reason + ")");

            if (!string.Equals(currentLang, ChineseLocaleKey, StringComparison.OrdinalIgnoreCase))
            {
                // 不是中文语言时不做字体覆盖
                _isChineseLocaleActive = false;
                Logger.LogInfo("[FontReplace] 当前语言不是中文，已关闭字体覆盖（保持原版字体）。");
                return;
            }

            // 中文语言：启用字体覆盖逻辑
            _isChineseLocaleActive = true;

            _isApplying = true;
            try
            {
                // 1) 尝试把 ch 对应的字体写回 LocaleManager 的字体映射（旧/新版本字段名可能不同）
                LocaleManagerCompat.TrySetLocaleFont(localeManager, ChineseLocaleKey, _chineseFontAsset, Logger);

                // 2) 尝试同步 "已应用语言"，避免 UpdateApplicationLanguage 逻辑被短路
                LocaleManagerCompat.TrySetAppliedLanguage(localeManager, currentLang, Logger);

                // 3) 让 LocaleManager 按当前 locale 重新构建 fallback（如果该版本支持）
                LocaleManagerCompat.TryApplyLocaleInternal(localeManager, currentLang, Logger);

                // 4) 兜底：把 TMP 默认字体替换 + 扫一遍现有 Text
                ApplyDefaultFontAndRefresh(reason);

                // 5) 初次应用时，尽量触发一次 Locale 更新事件（如果该版本存在 BindableEvent）
                if (string.Equals(reason, "initial", StringComparison.OrdinalIgnoreCase))
                {
                    LocaleManagerCompat.TryInvokeLocaleUpdated(localeManager, Logger);
                }

                Logger.LogInfo("[FontReplace] 中文字体覆盖已应用完成。");
                LogSampleTextFonts(reason);
            }
            finally
            {
                _isApplying = false;
            }
        }

        private void ConfigureFallbacks(LocaleManagerClass localeManager)
        {
            if (_chineseFontAsset == null)
            {
                return;
            }

            if (_chineseFontAsset.fallbackFontAssetTable == null)
            {
                _chineseFontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            var existing = new HashSet<int>();
            foreach (var fb in _chineseFontAsset.fallbackFontAssetTable)
            {
                if (fb != null)
                {
                    existing.Add(fb.GetInstanceID());
                }
            }

            int added = 0;

            // 优先把 en/ru 的主字体加入 fallback（如果能从 LocaleManager 里读到的话）
            var en = LocaleManagerCompat.TryGetLocaleFont(localeManager, "en");
            if (en != null && en != _chineseFontAsset && existing.Add(en.GetInstanceID()))
            {
                _chineseFontAsset.fallbackFontAssetTable.Add(en);
                added++;
            }

            var ru = LocaleManagerCompat.TryGetLocaleFont(localeManager, "ru");
            if (ru != null && ru != _chineseFontAsset && existing.Add(ru.GetInstanceID()))
            {
                _chineseFontAsset.fallbackFontAssetTable.Add(ru);
                added++;
            }

            // 再把 UI/Fonts 目录里所有 TMP_FontAsset 加入 fallback
            var uiFonts = Resources.LoadAll<TMP_FontAsset>("UI/Fonts");
            if (uiFonts != null)
            {
                for (int i = 0; i < uiFonts.Length; i++)
                {
                    var font = uiFonts[i];
                    if (font == null || font == _chineseFontAsset)
                    {
                        continue;
                    }

                    if (existing.Add(font.GetInstanceID()))
                    {
                        _chineseFontAsset.fallbackFontAssetTable.Add(font);
                        added++;
                    }
                }
            }

            if (added > 0)
            {
                Logger.LogInfo("[FontReplace] 返回字体添加: " + added);
            }
        }

        private void ApplyDefaultFontAndRefresh(string reason)
        {
            if (_modEnabled != null && !_modEnabled.Value)
            {
                return;
            }

            if (_chineseFontAsset == null)
            {
                return;
            }

            // 只要执行到这里，说明我们正在进行“中文字体覆盖”逻辑
            _isChineseLocaleActive = true;

            // 替换 TMP 默认字体（部分版本字段名不同，所以用反射）
            var settings = TMP_Settings.instance;
            if (settings != null)
            {
                var field = typeof(TMP_Settings).GetField("m_defaultFontAsset", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(settings, _chineseFontAsset);
                }
                else
                {
                    Logger.LogWarning("[FontReplace] TMP_Settings 中未找到 m_defaultFontAsset 字段，无法替换默认字体");
                }
            }
            else
            {
                Logger.LogWarning("[FontReplace] TMP_Settings 实例为空，无法替换默认字体");
            }

            int updated = 0;

            var texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            if (texts != null)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    var text = texts[i];
                    if (text == null)
                    {
                        continue;
                    }

                    // 先记录一次“原版字体”（只记录非覆盖字体，避免把中文覆盖字体当成原版缓存）
                    CacheOriginalFontIfNeeded(text);

                    // 根据内容决定是否保留原版字体（仅 ASCII 文本：英文字母/数字）
                    var targetFont = ShouldKeepOriginalFont(text.text) ? GetOriginalFont(text) : _chineseFontAsset;

                    if (targetFont != null && text.font != targetFont)
                    {
                        text.font = targetFont;
                        text.havePropertiesChanged = true;
                        updated++;
                    }
                }
            }

            var unityTexts = Resources.FindObjectsOfTypeAll<Text>();
            if (unityTexts != null)
            {
                if (_chineseUnityFont != null)
                {
                    for (int i = 0; i < unityTexts.Length; i++)
                    {
                        var text = unityTexts[i];
                        if (text == null)
                        {
                            continue;
                        }

                        CacheOriginalFontIfNeeded(text);

                        var targetFont = ShouldKeepOriginalFont(text.text) ? GetOriginalFont(text) : _chineseUnityFont;
                        if (targetFont != null && text.font != targetFont)
                        {
                            text.font = targetFont;
                            updated++;
                        }
                    }
                }
                else
                {
                    Logger.LogWarning("[FontReplace] sourceFontFile 失效 UnityEngine.UI.Text");
                }
            }

            Logger.LogInfo("[FontReplace] 已刷新文本组件数量： " + updated + " (" + reason + ")");
        }

        private void LogSampleTextFonts(string reason)
        {
            var texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            if (texts == null || texts.Length == 0)
            {
                Logger.LogInfo("[FontReplace] 未找到任何 TMP_Text（原因：" + reason + ").");
                return;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            int logged = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null || text.font == null)
                {
                    continue;
                }

                var key = text.font.name;
                if (seen.Add(key))
                {
                    Logger.LogInfo("[FontReplace] 示例 TMP_Text 使用字体：" + text.font.name + " (示例, " + reason + ")");
                    logged++;
                    if (logged >= 5)
                    {
                        break;
                    }
                }
            }
        }

    }
}
