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
        private void SetupTextMonitoring()
        {
            if (_tmpTextChangedHooked || _pollCoroutine != null)
            {
                return;
            }

            TryHookTmpTextChangedEvent();

            if (!_tmpTextChangedHooked && _pollCoroutine == null)
            {
                _pollCoroutine = StartCoroutine(PollTextLoop());
                Logger.LogInfo("[FontReplace] 未能订阅 TMP 文本变化事件，已启用轮询模式（每秒检查一次）。");
            }
        }


        private void TeardownTextMonitoring()
        {
            // 解除 TMP 文本变化事件
            try
            {
                if (_tmpOnTextChangedEvent != null && _tmpOnTextChangedHandler != null)
                {
                    _tmpOnTextChangedEvent.RemoveEventHandler(null, _tmpOnTextChangedHandler);
                }
            }
            catch
            {
                // 忽略
            }
            finally
            {
                _tmpOnTextChangedEvent = null;
                _tmpOnTextChangedHandler = null;
                _tmpTextChangedHooked = false;
            }

            // 停止轮询
            try
            {
                if (_pollCoroutine != null)
                {
                    StopCoroutine(_pollCoroutine);
                    _pollCoroutine = null;
                }
            }
            catch
            {
                // 忽略
            }
        }

        private void TryHookTmpTextChangedEvent()
        {
            try
            {
                var evt = typeof(TMP_Text).GetEvent("onTextChanged", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (evt == null || evt.EventHandlerType == null)
                {
                    return;
                }

                // 根据事件委托签名，选择能匹配的回调方法
                var invoke = evt.EventHandlerType.GetMethod("Invoke");
                if (invoke == null)
                {
                    return;
                }

                var ps = invoke.GetParameters();
                if (ps == null || ps.Length != 1)
                {
                    return;
                }

                var pType = ps[0].ParameterType;
                MethodInfo mi = GetType().GetMethod("OnAnyTmpTextChanged", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { pType }, null);
                if (mi == null)
                {
                    // 再兜底一次：尝试 UnityEngine.Object 参数版本
                    mi = GetType().GetMethod("OnAnyTmpTextChanged", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(UnityEngine.Object) }, null);
                }

                if (mi == null)
                {
                    return;
                }

                var del = Delegate.CreateDelegate(evt.EventHandlerType, this, mi, false);
                if (del == null)
                {
                    return;
                }

                evt.AddEventHandler(null, del);

                _tmpOnTextChangedEvent = evt;
                _tmpOnTextChangedHandler = del;
                _tmpTextChangedHooked = true;

                Logger.LogInfo("[FontReplace] 已订阅 TMP_Text.onTextChanged（用于英文字母/数字保留原版）。");
            }
            catch (Exception e)
            {
                Logger.LogWarning("[FontReplace] 订阅 TMP_Text.onTextChanged 失败: " + e);
            }
        }

        private void OnAnyTmpTextChanged(UnityEngine.Object obj)
        {
            if (_isHandlingTextChanged)
            {
                return;
            }

            if (_modEnabled != null && !_modEnabled.Value)
            {
                return;
            }

            if (!_isChineseLocaleActive)
            {
                return;
            }

            if (_chineseFontAsset == null)
            {
                return;
            }

            if (_keepOriginalLatin == null || _keepOriginalDigits == null)
            {
                return;
            }

            if (!_keepOriginalLatin.Value && !_keepOriginalDigits.Value)
            {
                return;
            }

            var text = obj as TMP_Text;
            if (text == null)
            {
                return;
            }

            _isHandlingTextChanged = true;
            try
            {
                CacheOriginalFontIfNeeded(text);

                var targetFont = ShouldKeepOriginalFont(text.text) ? GetOriginalFont(text) : _chineseFontAsset;
                if (targetFont != null && text.font != targetFont)
                {
                    text.font = targetFont;
                    text.havePropertiesChanged = true;
                }
            }
            finally
            {
                _isHandlingTextChanged = false;
            }
        }

        private void OnAnyTmpTextChanged(TMP_Text text)
        {
            OnAnyTmpTextChanged((UnityEngine.Object)text);
        }

        private IEnumerator PollTextLoop()
        {
            var wait = new WaitForSeconds(1f);

            while (true)
            {
                try
                {
                    PollAndApplyTextOnce();
                }
                catch (Exception e)
                {
                    Logger.LogDebug("[FontReplace] PollTextLoop 异常: " + e);
                }

                yield return wait;
            }
        }


        private void PollAndApplyTextOnce()
        {
            if (_modEnabled != null && !_modEnabled.Value)
            {
                return;
            }

            if (!_isChineseLocaleActive)
            {
                return;
            }

            if (_chineseFontAsset == null)
            {
                return;
            }

            if (_keepOriginalLatin == null || _keepOriginalDigits == null)
            {
                return;
            }

            if (!_keepOriginalLatin.Value && !_keepOriginalDigits.Value)
            {
                return;
            }

            // 避免极端情况下缓存无限增长
            if (_lastTmpTextContent.Count > 8000)
            {
                _lastTmpTextContent.Clear();
            }
            if (_lastUnityTextContent.Count > 8000)
            {
                _lastUnityTextContent.Clear();
            }

            var tmpTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            if (tmpTexts != null)
            {
                for (int i = 0; i < tmpTexts.Length; i++)
                {
                    var t = tmpTexts[i];
                    if (t == null)
                    {
                        continue;
                    }

                    int id = t.GetInstanceID();
                    string curr = t.text ?? string.Empty;

                    string last;
                    if (!_lastTmpTextContent.TryGetValue(id, out last) || !string.Equals(last, curr, StringComparison.Ordinal))
                    {
                        _lastTmpTextContent[id] = curr;

                        CacheOriginalFontIfNeeded(t);

                        var targetFont = ShouldKeepOriginalFont(curr) ? GetOriginalFont(t) : _chineseFontAsset;
                        if (targetFont != null && t.font != targetFont)
                        {
                            t.font = targetFont;
                            t.havePropertiesChanged = true;
                        }
                    }
                }
            }

            // UnityEngine.UI.Text：没有 onTextChanged 事件，所以只在轮询模式下顺便处理
            var uiTexts = Resources.FindObjectsOfTypeAll<Text>();
            if (uiTexts != null && _chineseUnityFont != null)
            {
                for (int i = 0; i < uiTexts.Length; i++)
                {
                    var t = uiTexts[i];
                    if (t == null)
                    {
                        continue;
                    }

                    int id = t.GetInstanceID();
                    string curr = t.text ?? string.Empty;

                    string last;
                    if (!_lastUnityTextContent.TryGetValue(id, out last) || !string.Equals(last, curr, StringComparison.Ordinal))
                    {
                        _lastUnityTextContent[id] = curr;

                        CacheOriginalFontIfNeeded(t);

                        var targetFont = ShouldKeepOriginalFont(curr) ? GetOriginalFont(t) : _chineseUnityFont;
                        if (targetFont != null && t.font != targetFont)
                        {
                            t.font = targetFont;
                        }
                    }
                }
            }
        }

    }
}
