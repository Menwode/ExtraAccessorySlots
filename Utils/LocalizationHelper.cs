using Newtonsoft.Json;
using SodaCraft.Localizations;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using System;
using System.Linq;

#nullable disable

namespace ExtraAccessorySlots.Utils
{
    public static class LocalizationHelper
    {
        public const string KeyPrefix = "ExtraAccessorySlots.";
        public static readonly Dictionary<string, string> BaseEnglishDict = new Dictionary<string, string>
        {
            [$"{KeyPrefix}ModSettingButton"] = "[Mod] ExtraAccessorySlots",
            [$"{KeyPrefix}Option_On"] = "On",
            [$"{KeyPrefix}Option_Off"] = "Off",
        };

        public static void Init()
        {
            EnsureBaseLanguageFileExists();
            LoadLanguageFile(LocalizationManager.CurrentLanguage);
            LocalizationManager.OnSetLanguage += OnLanguageChanged;
        }
        public static void Release()
        {
            LocalizationManager.OnSetLanguage -= OnLanguageChanged;
        }

        public static void AddOrUpdateLocalization(string key, string value)
        {
            LocalizationManager.SetOverrideText(key, value);
        }

        // Register localization for a Tag by name, matching Tag.cs rules
        public static void RegisterTagLocalization(string tagName, string displayName, string description = null)
        {
            string key = "Tag_" + tagName;
            string descKey = key + "_Desc";

            // Try group-based registration first (group = "Tags")
            if (!TrySetGroupText("Tags", key, displayName))
            {
                LocalizationManager.SetOverrideText(key, displayName);
            }
            if (!string.IsNullOrEmpty(description))
            {
                if (!TrySetGroupText("Tags", descKey, description))
                {
                    LocalizationManager.SetOverrideText(descKey, description);
                }
            }
        }

        // Central place to register all mod-specific tag localizations
        public static void RegisterTagsForMod()
        {
            RegisterTagLocalization("WeaponChip", "武器芯片", "允许安装武器芯片配件");
        }

        private static void OnLanguageChanged(SystemLanguage language)
        {
            LoadLanguageFile(language);
            // Re-apply tag localizations on language change
            RegisterTagsForMod();
        }

        private static void LoadLanguageFile(SystemLanguage language)
        {
            string languageName = language.ToString();
            string path = GetLocalizationFilePath(languageName);
            if (!File.Exists(path))
            {
                languageName = "English";
                path = GetLocalizationFilePath(languageName);
                if (!File.Exists(path))
                {
                    return;
                }
            }

            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
            foreach (var kv in dict)
            {
                LocalizationManager.SetOverrideText(kv.Key, kv.Value);
            }
        }

        private static void EnsureBaseLanguageFileExists()
        {
            var path = GetLocalizationFilePath("English");
            if (File.Exists(path)) return;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonConvert.SerializeObject(BaseEnglishDict, Formatting.Indented));
        }

        private static string GetLocalizationFilePath(string languageName)
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (directoryName == null) return null;
            return Path.Combine(directoryName, "Localization", $"{languageName}.json");
        }

        private static bool TrySetGroupText(string group, string key, string value)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var candidates = assemblies.SelectMany(a => a.GetTypes())
                    .Where(t => t.Namespace != null &&
                                (t.Namespace.Contains("SodaCraft") || t.Namespace.Contains("MiniLocalizor")) &&
                                (t.Name.Contains("Localization") || t.Name.Contains("Localizor")));

                foreach (var t in candidates)
                {
                    var ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach (var m in ms)
                    {
                        var ps = m.GetParameters();
                        if (ps.Length == 3 && ps.All(p => p.ParameterType == typeof(string)) &&
                            (m.Name.Contains("Set") || m.Name.Contains("Add") || m.Name.Contains("Register")))
                        {
                            m.Invoke(null, new object[] { group, key, value });
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
            return false;
        }
    }
}
