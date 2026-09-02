using System;
using EFT;
using KmyTarkovReflection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace KmyTarkovApi.Helpers
{
    public class LocaleManagerClassHelper
    {
        private static readonly Lazy<LocaleManagerClassHelper> Lazy =
            new Lazy<LocaleManagerClassHelper>(() => new LocaleManagerClassHelper());

        public static LocaleManagerClassHelper Instance => Lazy.Value;

        public LocalizationManager LocaleManagerClass => RefLocaleManagerClass.GetValue(null);

        public string CurrentLanguage => LocaleManagerClass.Culture;

        public readonly RefHelper.PropertyRef<LocalizationManager, LocalizationManager> RefLocaleManagerClass;

        private LocaleManagerClassHelper()
        {
            RefLocaleManagerClass =
                RefHelper.PropertyRef<LocalizationManager, LocalizationManager>.Create("Instance");
        }
    }
}