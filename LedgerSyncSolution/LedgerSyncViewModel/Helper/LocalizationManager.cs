using System;
using System.Globalization;
using System.Windows;

namespace LedgerSyncViewModel.Helper
{
    /// <summary>
    /// Centralized localization manager.
    /// Previously there were two conflicting systems:
    ///   1. ResourceManager (.resx) used by LocalizationManager/LocExtension
    ///   2. ResourceDictionary (.xaml) used by ShellViewModel.SwitchLanguage
    /// FIX: Unified into one system - ResourceDictionary (.xaml) is the source of truth.
    /// LocalizationManager.Get() now reads from Application.Current.Resources (XAML dictionaries),
    /// and ChangeCulture() updates both CurrentCulture and fires OnLanguageChanged
    /// so LocExtension bindings update correctly.
    /// ShellViewModel.SwitchLanguage() should call LocalizationManager.ChangeCulture() after
    /// swapping the ResourceDictionary.
    /// </summary>
    public static class LocalizationManager
    {
        public static CultureInfo CurrentCulture { get; private set; } = new CultureInfo("zh-CN");

        public static event EventHandler OnLanguageChanged;

        /// <summary>
        /// Get localized string by key from current XAML ResourceDictionary.
        /// </summary>
        public static string Get(string key)
        {
            if (Application.Current?.Resources?.Contains(key) == true)
                return Application.Current.Resources[key]?.ToString() ?? key;
            return key;
        }

        /// <summary>
        /// Call this after swapping the ResourceDictionary in ShellViewModel.SwitchLanguage()
        /// to update CurrentCulture and notify all LocExtension bindings.
        /// </summary>
        public static void ChangeCulture(string cultureName)
        {
            CurrentCulture = new CultureInfo(cultureName);
            OnLanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
