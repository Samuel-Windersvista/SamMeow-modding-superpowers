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
        private static class LocaleManagerCompat
        {
            private static readonly BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            private static readonly BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            private static PropertyInfo s_singletonProp;
            private static FieldInfo s_singletonField;

            private static MemberInfo s_currentLangMember;
            private static MemberInfo s_appliedLangMember;

            private static MemberInfo s_fontMapMember;
            private static MemberInfo s_bindableEventMember;

            public static LocaleManagerClass GetInstance(BepInEx.Logging.ManualLogSource logger)
            {
                try
                {
                    var t = typeof(LocaleManagerClass);

                    // 1) 优先按常见名字取单例
                    if (s_singletonProp == null)
                    {
                        s_singletonProp = t.GetProperty("LocaleManagerClass", AnyStatic);
                        if (s_singletonProp == null || s_singletonProp.PropertyType != t)
                        {
                            // 2) 兜底：找到任意一个返回 LocaleManagerClass 的静态属性
                            var props = t.GetProperties(AnyStatic);
                            for (int i = 0; i < props.Length; i++)
                            {
                                var p = props[i];
                                if (p.PropertyType == t && p.GetIndexParameters().Length == 0)
                                {
                                    s_singletonProp = p;
                                    break;
                                }
                            }
                        }
                    }

                    if (s_singletonProp != null)
                    {
                        var v = s_singletonProp.GetValue(null, null) as LocaleManagerClass;
                        if (v != null)
                        {
                            return v;
                        }
                    }

                    // 3) 再兜底：静态字段
                    if (s_singletonField == null)
                    {
                        var fields = t.GetFields(AnyStatic);
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var f = fields[i];
                            if (f.FieldType == t)
                            {
                                s_singletonField = f;
                                break;
                            }
                        }
                    }

                    if (s_singletonField != null)
                    {
                        return s_singletonField.GetValue(null) as LocaleManagerClass;
                    }
                }
                catch (Exception e)
                {
                    if (logger != null)
                    {
                        logger.LogWarning("[FontReplace] GetInstance(LocaleManager) 失效: " + e);
                    }
                }

                return null;
            }

            public static string GetCurrentLanguage(LocaleManagerClass lm)
            {
                // 源码属性 String_0 (默认 en)
                string v;
                if (TryGetString(lm, ref s_currentLangMember, new[] { "String_0", "CurrentLanguage", "Language", "Locale" }, out v))
                {
                    return v;
                }
                return "en";
            }

            public static string GetAppliedLanguage(LocaleManagerClass lm)
            {
                // 源码字段 String_1
                string v;
                if (TryGetString(lm, ref s_appliedLangMember, new[] { "String_1", "AppliedLanguage", "CurrentAppliedLanguage" }, out v))
                {
                    return v;
                }
                return string.Empty;
            }

            public static void TrySetAppliedLanguage(LocaleManagerClass lm, string lang, BepInEx.Logging.ManualLogSource logger)
            {
                if (!TrySetString(lm, ref s_appliedLangMember, new[] { "String_1", "AppliedLanguage", "CurrentAppliedLanguage" }, lang))
                {
                    // 没有也不算致命
                    if (logger != null)
                    {
                        logger.LogDebug("[FontReplace] 未找到 AppliedLanguage 成员；跳过应用。");
                    }
                }
            }

            public static TMP_FontAsset TryGetLocaleFont(LocaleManagerClass lm, string locale)
            {
                var map = GetLocaleFontMap(lm);
                if (map == null)
                {
                    return null;
                }

                TMP_FontAsset font;
                if (map.TryGetValue(locale, out font))
                {
                    return font;
                }

                return null;
            }

            public static void TrySetLocaleFont(LocaleManagerClass lm, string locale, TMP_FontAsset font, BepInEx.Logging.ManualLogSource logger)
            {
                var map = GetLocaleFontMap(lm);
                if (map == null)
                {
                    if (logger != null)
                    {
                        logger.LogWarning("[FontReplace] 未找到本地化字体映射；仅回退到 TMP_Settings。");
                    }
                    return;
                }

                map[locale] = font;

                // 打印一下确认
                if (logger != null)
                {
                    logger.LogInfo("[FontReplace] 设置本地化字体： " + locale + " -> " + (font != null ? font.name : "(null)"));
                }
            }

            public static void TryApplyLocaleInternal(LocaleManagerClass lm, string locale, BepInEx.Logging.ManualLogSource logger)
            {
                try
                {
                    // 你提供的源码里是 public void method_1(string localeType)
                    var m = lm.GetType().GetMethod("method_1", AnyInstance, null, new[] { typeof(string) }, null);
                    if (m != null)
                    {
                        m.Invoke(lm, new object[] { locale });
                        return;
                    }

                    // 兜底：有些版本叫 UpdateApplicationLanguage
                    var m2 = lm.GetType().GetMethod("UpdateApplicationLanguage", AnyInstance, null, Type.EmptyTypes, null);
                    if (m2 != null)
                    {
                        m2.Invoke(lm, null);
                    }
                }
                catch (Exception e)
                {
                    if (logger != null)
                    {
                        logger.LogWarning("[FontReplace] TryApplyLocaleInternal 失败: " + e);
                    }
                }
            }

            public static void TryInvokeLocaleUpdated(LocaleManagerClass lm, BepInEx.Logging.ManualLogSource logger)
            {
                try
                {
                    var evt = GetBindableEvent(lm);
                    if (evt == null)
                    {
                        return;
                    }

                    // BindableEvent.Invoke()
                    var invoke = evt.GetType().GetMethod("Invoke", AnyInstance, null, Type.EmptyTypes, null);
                    if (invoke != null)
                    {
                        invoke.Invoke(evt, null);
                    }
                }
                catch (Exception e)
                {
                    if (logger != null)
                    {
                        logger.LogDebug("[FontReplace] TryInvokeLocaleUpdated 失败: " + e);
                    }
                }
            }

            public static Action TrySubscribeLocaleUpdate(LocaleManagerClass lm, Action callback, BepInEx.Logging.ManualLogSource logger)
            {
                try
                {
                    // 你提供的源码里：public Action AddLocaleUpdateListener(Action callback)
                    var m = lm.GetType().GetMethod("AddLocaleUpdateListener", AnyInstance, null, new[] { typeof(Action) }, null);
                    if (m != null)
                    {
                        var ret = m.Invoke(lm, new object[] { callback }) as Action;
                        if (ret != null)
                        {
                            return ret;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (logger != null)
                    {
                        logger.LogWarning("[FontReplace] TrySubscribeLocaleUpdate 失败: " + e);
                    }
                }

                return null;
            }

            private static IDictionary<string, TMP_FontAsset> GetLocaleFontMap(LocaleManagerClass lm)
            {
                if (lm == null)
                {
                    return null;
                }

                // 你提供的源码里是 public Dictionary<string, TMP_FontAsset> Dictionary_1
                if (s_fontMapMember == null)
                {
                    var t = lm.GetType();

                    // 1) 常见名字
                    var fNamed = t.GetField("Dictionary_1", AnyInstance);
                    if (fNamed != null && typeof(IDictionary<string, TMP_FontAsset>).IsAssignableFrom(fNamed.FieldType))
                    {
                        s_fontMapMember = fNamed;
                    }

                    if (s_fontMapMember == null)
                    {
                        var pNamed = t.GetProperty("Dictionary_1", AnyInstance);
                        if (pNamed != null && typeof(IDictionary<string, TMP_FontAsset>).IsAssignableFrom(pNamed.PropertyType) && pNamed.GetIndexParameters().Length == 0)
                        {
                            s_fontMapMember = pNamed;
                        }
                    }

                    // 2) 兜底：按类型找 Dictionary<string, TMP_FontAsset>
                    if (s_fontMapMember == null)
                    {
                        var fields = t.GetFields(AnyInstance);
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var f = fields[i];
                            if (typeof(IDictionary<string, TMP_FontAsset>).IsAssignableFrom(f.FieldType))
                            {
                                s_fontMapMember = f;
                                break;
                            }
                        }
                    }

                    if (s_fontMapMember == null)
                    {
                        var props = t.GetProperties(AnyInstance);
                        for (int i = 0; i < props.Length; i++)
                        {
                            var p = props[i];
                            if (p.GetIndexParameters().Length != 0 || !p.CanRead)
                            {
                                continue;
                            }

                            if (typeof(IDictionary<string, TMP_FontAsset>).IsAssignableFrom(p.PropertyType))
                            {
                                s_fontMapMember = p;
                                break;
                            }
                        }
                    }
                }

                if (s_fontMapMember is FieldInfo)
                {
                    var fi = (FieldInfo)s_fontMapMember;
                    return fi.GetValue(lm) as IDictionary<string, TMP_FontAsset>;
                }

                if (s_fontMapMember is PropertyInfo)
                {
                    var pi = (PropertyInfo)s_fontMapMember;
                    return pi.GetValue(lm, null) as IDictionary<string, TMP_FontAsset>;
                }

                return null;
            }

            private static object GetBindableEvent(LocaleManagerClass lm)
            {
                if (lm == null)
                {
                    return null;
                }

                // 你提供的源码里是 public BindableEvent BindableEvent_0
                if (s_bindableEventMember == null)
                {
                    var t = lm.GetType();

                    // 1) 常见名字
                    var fNamed = t.GetField("BindableEvent_0", AnyInstance);
                    if (fNamed != null)
                    {
                        s_bindableEventMember = fNamed;
                    }

                    if (s_bindableEventMember == null)
                    {
                        var pNamed = t.GetProperty("BindableEvent_0", AnyInstance);
                        if (pNamed != null && pNamed.GetIndexParameters().Length == 0)
                        {
                            s_bindableEventMember = pNamed;
                        }
                    }

                    // 2) 兜底：按类型名包含 BindableEvent 的字段/属性
                    if (s_bindableEventMember == null)
                    {
                        var fields = t.GetFields(AnyInstance);
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var f = fields[i];
                            if (f.FieldType != null && f.FieldType.Name.IndexOf("BindableEvent", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                s_bindableEventMember = f;
                                break;
                            }
                        }
                    }

                    if (s_bindableEventMember == null)
                    {
                        var props = t.GetProperties(AnyInstance);
                        for (int i = 0; i < props.Length; i++)
                        {
                            var p = props[i];
                            if (p.GetIndexParameters().Length != 0 || !p.CanRead)
                            {
                                continue;
                            }

                            if (p.PropertyType != null && p.PropertyType.Name.IndexOf("BindableEvent", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                s_bindableEventMember = p;
                                break;
                            }
                        }
                    }
                }

                if (s_bindableEventMember is FieldInfo)
                {
                    return ((FieldInfo)s_bindableEventMember).GetValue(lm);
                }

                if (s_bindableEventMember is PropertyInfo)
                {
                    return ((PropertyInfo)s_bindableEventMember).GetValue(lm, null);
                }

                return null;
            }

            private static bool TryGetString(object obj, ref MemberInfo cachedMember, string[] names, out string value)
            {
                value = null;
                if (obj == null)
                {
                    return false;
                }

                try
                {
                    var t = obj.GetType();

                    if (cachedMember == null)
                    {
                        for (int i = 0; i < names.Length; i++)
                        {
                            var n = names[i];
                            var p = t.GetProperty(n, AnyInstance);
                            if (p != null && p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0 && p.CanRead)
                            {
                                cachedMember = p;
                                break;
                            }

                            var f = t.GetField(n, AnyInstance);
                            if (f != null && f.FieldType == typeof(string))
                            {
                                cachedMember = f;
                                break;
                            }
                        }

                        // 兜底：找第一个 string 的属性/字段（风险较大，所以仅作为最后手段）
                        if (cachedMember == null)
                        {
                            var props = t.GetProperties(AnyInstance);
                            for (int i = 0; i < props.Length; i++)
                            {
                                var p = props[i];
                                if (p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0 && p.CanRead)
                                {
                                    cachedMember = p;
                                    break;
                                }
                            }
                        }

                        if (cachedMember == null)
                        {
                            var fields = t.GetFields(AnyInstance);
                            for (int i = 0; i < fields.Length; i++)
                            {
                                var f = fields[i];
                                if (f.FieldType == typeof(string))
                                {
                                    cachedMember = f;
                                    break;
                                }
                            }
                        }
                    }

                    if (cachedMember is PropertyInfo)
                    {
                        value = (string)((PropertyInfo)cachedMember).GetValue(obj, null);
                        return true;
                    }

                    if (cachedMember is FieldInfo)
                    {
                        value = (string)((FieldInfo)cachedMember).GetValue(obj);
                        return true;
                    }
                }
                catch
                {
                    // 忽略
                }

                return false;
            }

            private static bool TrySetString(object obj, ref MemberInfo cachedMember, string[] names, string value)
            {
                if (obj == null)
                {
                    return false;
                }

                try
                {
                    var t = obj.GetType();

                    if (cachedMember == null)
                    {
                        for (int i = 0; i < names.Length; i++)
                        {
                            var n = names[i];

                            var p = t.GetProperty(n, AnyInstance);
                            if (p != null && p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0 && p.CanWrite)
                            {
                                cachedMember = p;
                                break;
                            }

                            var f = t.GetField(n, AnyInstance);
                            if (f != null && f.FieldType == typeof(string) && !f.IsInitOnly)
                            {
                                cachedMember = f;
                                break;
                            }
                        }
                    }

                    if (cachedMember is PropertyInfo)
                    {
                        ((PropertyInfo)cachedMember).SetValue(obj, value, null);
                        return true;
                    }

                    if (cachedMember is FieldInfo)
                    {
                        ((FieldInfo)cachedMember).SetValue(obj, value);
                        return true;
                    }
                }
                catch
                {
                    // 忽略
                }

                return false;
            }
        }
    }
}
