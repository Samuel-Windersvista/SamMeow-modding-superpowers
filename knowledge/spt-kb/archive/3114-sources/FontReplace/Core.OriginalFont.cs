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
        private void CacheOriginalDefaultFonts()
        {
            if (_originalDefaultTmpFont != null)
            {
                return;
            }

            try
            {
                var settings = TMP_Settings.instance;
                if (settings == null)
                {
                    return;
                }

                var field = typeof(TMP_Settings).GetField("m_defaultFontAsset", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    _originalDefaultTmpFont = field.GetValue(settings) as TMP_FontAsset;
                }
                else
                {
                    var prop = typeof(TMP_Settings).GetProperty("defaultFontAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(TMP_FontAsset) && prop.CanRead)
                    {
                        _originalDefaultTmpFont = prop.GetValue(settings, null) as TMP_FontAsset;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogDebug("[FontReplace] CacheOriginalDefaultFonts 失败: " + e);
            }
        }

        private void OnKeepOriginalSettingChanged(object sender, EventArgs e)
        {
            if (_modEnabled != null && !_modEnabled.Value)
            {
                return;
            }

            if (_chineseFontAsset == null)
            {
                return;
            }

            // 只有在“中文字体覆盖”处于启用状态时才刷新，避免在非中文语言下误改字体
            if (_isChineseLocaleActive)
            {
                ApplyDefaultFontAndRefresh("configChanged");
            }
        }


        private void OnModEnabledSettingChanged(object sender, EventArgs e)
        {
            if (_modEnabled == null)
            {
                return;
            }

            if (_modEnabled.Value)
            {
                Logger.LogInfo("[FontReplace] 模组已启用，开始尝试应用字体覆盖。");

                var localeManager = LocaleManagerCompat.GetInstance(Logger);
                if (localeManager != null)
                {
                    ConfigureFallbacks(localeManager);
                    TryApplyChineseFont(localeManager, "modEnabled");
                }
                else
                {
                    ApplyDefaultFontAndRefresh("modEnabled(noLocaleManager)");
                }

                SetupTextMonitoring();
            }
            else
            {
                Logger.LogInfo("[FontReplace] 模组已禁用，恢复原版字体。");
                RestoreOriginalFonts();
                TeardownTextMonitoring();
            }
        }


        private void RestoreOriginalFonts()
        {
            _isChineseLocaleActive = false;

            // 还原 TMP 默认字体
            var settings = TMP_Settings.instance;
            if (settings != null && _originalDefaultTmpFont != null)
            {
                var field = typeof(TMP_Settings).GetField("m_defaultFontAsset", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(settings, _originalDefaultTmpFont);
                }
                else
                {
                    var prop = typeof(TMP_Settings).GetProperty("defaultFontAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(TMP_FontAsset) && prop.CanWrite)
                    {
                        prop.SetValue(settings, _originalDefaultTmpFont, null);
                    }
                }
            }

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

                    TMP_FontAsset original;
                    if (_originalTmpFonts.TryGetValue(text.GetInstanceID(), out original) && original != null)
                    {
                        text.font = original;
                    }
                    else if (_originalDefaultTmpFont != null)
                    {
                        text.font = _originalDefaultTmpFont;
                    }

                    text.havePropertiesChanged = true;
                }
            }

            var uiTexts = Resources.FindObjectsOfTypeAll<Text>();
            if (uiTexts != null)
            {
                for (int i = 0; i < uiTexts.Length; i++)
                {
                    var text = uiTexts[i];
                    if (text == null)
                    {
                        continue;
                    }

                    Font original;
                    if (_originalUnityFonts.TryGetValue(text.GetInstanceID(), out original) && original != null)
                    {
                        text.font = original;
                    }
                }
            }
        }

        private bool ShouldKeepOriginalFont(string s)
        {
            if (_keepOriginalLatin == null || _keepOriginalDigits == null)
            {
                return false;
            }

            bool checkLatin = _keepOriginalLatin.Value;
            bool checkDigits = _keepOriginalDigits.Value;

            if (!checkLatin && !checkDigits)
            {
                return false;
            }

            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            bool inTag = false;
            bool foundLatin = false;
            bool foundDigit = false;
            bool foundNonAscii = false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                // 忽略 TMP 富文本标签内容
                if (c == '<')
                {
                    inTag = true;
                    continue;
                }
                if (inTag)
                {
                    if (c == '>')
                    {
                        inTag = false;
                    }
                    continue;
                }

                if (c > 127 && !char.IsWhiteSpace(c))
                {
                    foundNonAscii = true;
                    // 不需要继续细分，直接标记即可
                    continue;
                }

                if (checkLatin)
                {
                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                    {
                        foundLatin = true;
                    }
                }

                if (checkDigits)
                {
                    if (c >= '0' && c <= '9')
                    {
                        foundDigit = true;
                    }
                }
            }

            // 含中文/俄文/全角/Emoji 等非 ASCII 内容：不要保留原版（避免“中文句子里夹着 AK-74”导致整段回退到原版字体）
            if (foundNonAscii)
            {
                return false;
            }

            if (checkLatin && foundLatin)
            {
                return true;
            }

            if (checkDigits && foundDigit)
            {
                return true;
            }

            return false;
        }


        private void CacheOriginalFontIfNeeded(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            int id = text.GetInstanceID();
            if (_originalTmpFonts.ContainsKey(id))
            {
                return;
            }

            // 只缓存“非覆盖字体”，避免把中文覆盖字体当成原版缓存
            if (text.font != null && text.font != _chineseFontAsset)
            {
                _originalTmpFonts[id] = text.font;
            }
        }


        private void CacheOriginalFontIfNeeded(Text text)
        {
            if (text == null)
            {
                return;
            }

            int id = text.GetInstanceID();
            if (_originalUnityFonts.ContainsKey(id))
            {
                return;
            }

            if (text.font != null && text.font != _chineseUnityFont)
            {
                _originalUnityFonts[id] = text.font;
            }
        }


        private TMP_FontAsset GetOriginalFont(TMP_Text text)
        {
            TMP_FontAsset cached;
            if (text != null && _originalTmpFonts.TryGetValue(text.GetInstanceID(), out cached) && cached != null)
            {
                return cached;
            }

            if (_originalDefaultTmpFont != null)
            {
                return _originalDefaultTmpFont;
            }

            // 最后兜底：返回当前字体（可能已经是覆盖字体）
            return text != null ? text.font : null;
        }


        private Font GetOriginalFont(Text text)
        {
            Font cached;
            if (text != null && _originalUnityFonts.TryGetValue(text.GetInstanceID(), out cached) && cached != null)
            {
                return cached;
            }

            return text != null ? text.font : null;
        }

    }
}
