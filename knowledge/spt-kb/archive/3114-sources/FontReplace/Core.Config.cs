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
        private void InitConfig()
        {
            _modEnabled = Config.Bind(
                ModSection,
                "启用模组",
                true,
                new ConfigDescription(
                    "关闭后：不进行任何字体覆盖（保持游戏原版字体）。",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        DispName = "启用模组",
                        HideDefaultButton = false
                    }));

            _fontBundleName = Config.Bind(
                FontSection,
                "字体切换",
                DefaultBundleName,
                new ConfigDescription(
                    "从 BepInEx\\plugins\\FontReplace\\Font 读取字体文件并切换应用。",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        DispName = "字体切换\n",
                        CustomDrawer = DrawFontBundlePicker,
                        HideDefaultButton = true
                    }));

            _keepOriginalLatin = Config.Bind(
                KeepOriginalSection,
                "显示原版字母",
                false,
                new ConfigDescription(
                    "开启后：当文本内容仅包含 ASCII 字符且出现 A-Z/a-z 时，该文本将保持游戏原版字体，不使用中文字体覆盖。\n（包含非 ASCII 字符的文本不受影响）\n\n且包含中文的语句会失效",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        DispName = "显示原版字母 （需重启游戏）",
                        HideDefaultButton = false
                    }));

            _keepOriginalDigits = Config.Bind(
                KeepOriginalSection,
                "显示原版数字",
                false,
                new ConfigDescription(
                    "开启后：当文本内容仅包含 ASCII 字符且出现 0-9 时，该文本将保持游戏原版字体，不使用中文字体覆盖。\n（包含非 ASCII 字符的文本不受影响）\n\n且包含中文的语句会失效",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        DispName = "显示原版数字 （需重启游戏）",
                        HideDefaultButton = false
                    }));

            _modEnabled.SettingChanged += OnModEnabledSettingChanged;
            // 当开关改变时，立刻刷新一次已有文本
            _keepOriginalLatin.SettingChanged += OnKeepOriginalSettingChanged;
            _keepOriginalDigits.SettingChanged += OnKeepOriginalSettingChanged;

            // 启动时扫描字体资源
            ScanFontBundles(true);
        }

        private void DrawFontBundlePicker(ConfigEntryBase entry)
        {
            if (!s_FontListLoaded)
            {
                ScanFontBundles(true);
            }

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<", GUILayout.Width(28)))
            {
                if (s_FontBundleNames.Count > 0)
                {
                    s_SelectedFontIndex = (s_SelectedFontIndex - 1 + s_FontBundleNames.Count) % s_FontBundleNames.Count;
                }
            }

            var label = (s_FontBundleNames.Count > 0 && s_SelectedFontIndex >= 0 && s_SelectedFontIndex < s_FontBundleNames.Count)
                ? s_FontBundleNames[s_SelectedFontIndex]
                : "(无任何字体资源)";
            GUILayout.Label(label, GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", GUILayout.Width(28)))
            {
                if (s_FontBundleNames.Count > 0)
                {
                    s_SelectedFontIndex = (s_SelectedFontIndex + 1) % s_FontBundleNames.Count;
                }
            }

            if (GUILayout.Button("刷新", GUILayout.Width(64)))
            {
                ScanFontBundles(true);
                Logger.LogInfo("[FontReplace] 已重新扫描字体目录，发现字体数量=" + s_FontBundleNames.Count);
                ShowFontUiHint("已刷新到: " + s_FontBundleNames.Count + " 个字体资源");
            }

            if (GUILayout.Button("应用", GUILayout.Width(64)))
            {
                if (s_FontBundleNames.Count > 0)
                {
                    var pick = s_FontBundleNames[s_SelectedFontIndex];
                    _fontBundleName.Value = pick;
                    ApplyFontBundleByName(pick, "ui");
                    ShowFontUiHint("已成功应用： " + pick);
                }
            }

            GUILayout.EndHorizontal();

            if (s_FontHintStyle == null)
            {
                s_FontHintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
                s_FontHintStyle.normal.textColor = new Color(0.7f, 1f, 0.7f, 1f);
            }

            GUILayout.Space(4);
            string hintMsg = (s_FontUiHintUntil > 0f && Time.realtimeSinceStartup < s_FontUiHintUntil)
                ? ("> " + s_FontUiHint)
                : " ";
            GUILayout.Label(hintMsg, s_FontHintStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(FontHintMinHeight));

            GUILayout.EndVertical();
        }

        private void ShowFontUiHint(string msg, float seconds = 2f)
        {
            s_FontUiHint = msg ?? "";
            s_FontUiHintUntil = Time.realtimeSinceStartup + Mathf.Max(0.5f, seconds);
        }

        private void ScanFontBundles(bool resetSelectionToCurrent)
        {
            try
            {
                var list = new List<string>();
                // 扫描 Font 目录下的所有字体 AssetBundle 文件（用于配置界面的“字体切换”列表）
                var fontDir = Path.Combine(pluginDir, FontDirName);
                if (Directory.Exists(fontDir))
                {
                    var files = Directory.GetFiles(fontDir, "*", SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < files.Length; i++)
                    {
                        var name = Path.GetFileName(files[i]);
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        // .NET Framework 下 List.Contains 没有带 comparer 的重载，用 Any 替代
                        if (!list.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Add(name);
                        }
                    }
                }

                s_FontBundleNames = list;
                s_FontListLoaded = true;

                if (resetSelectionToCurrent)
                {
                    var current = _fontBundleName != null ? (_fontBundleName.Value ?? DefaultBundleName) : DefaultBundleName;
                    int idx = s_FontBundleNames.FindIndex(n => string.Equals(n, current, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        s_SelectedFontIndex = idx;
                    }
                    else if (s_FontBundleNames.Count > 0)
                    {
                        s_SelectedFontIndex = 0;
                        if (_fontBundleName != null)
                        {
                            _fontBundleName.Value = s_FontBundleNames[0];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning("[FontReplace] 扫描字体目录时发生异常" + e);
                s_FontBundleNames = new List<string>();
                s_SelectedFontIndex = 0;
                s_FontListLoaded = true;
            }
        }

        private void ApplyFontBundleByName(string bundleName, string reason)
        {
            LoadFontAsset(bundleName);
            var localeManager = LocaleManagerCompat.GetInstance(Logger);
            if (localeManager != null)
            {
                ConfigureFallbacks(localeManager);
                TryApplyChineseFont(localeManager, reason);
            }
            else
            {
                ApplyDefaultFontAndRefresh(reason + "(noLocaleManager)");
            }
        }

    }
}
