    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;

    namespace ContactManagerApp.Services
    {
        public class SettingService
        {
            private static readonly string SettingsFilePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", "Settings", "settings.json");
            private Dictionary<string, string> _settings;

            public SettingService()
            {
                _settings = LoadSettings();
            }

            public Dictionary<string, string> LoadSettings()
            {
                try
                {
                    if (!File.Exists(SettingsFilePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Settings file not found at {SettingsFilePath}");
                        return new Dictionary<string, string>();
                    }

                    var encryptedJson = File.ReadAllBytes(SettingsFilePath);
                    var jsonBytes = ProtectedData.Unprotect(encryptedJson, null, DataProtectionScope.CurrentUser);
                    var json = Encoding.UTF8.GetString(jsonBytes);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                    System.Diagnostics.Debug.WriteLine($"Loaded settings with {settings.Count} entries");
                    return settings;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading settings: {e.Message}");
                    return new Dictionary<string, string>();
                }
            }

            public void SaveSettings(Dictionary<string, string> settings)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    var jsonBytes = Encoding.UTF8.GetBytes(json);
                    var encryptedJson = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(SettingsFilePath, encryptedJson);
                    System.Diagnostics.Debug.WriteLine($"Saved settings to {SettingsFilePath}");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving settings: {e.Message}");
                }
            }

            public string GetSetting(string key)
            {
                return _settings.TryGetValue(key, out var value) ? value : null;
            }

            public void SetSetting(string key, string value)
            {
                if (value == null)
                {
                    _settings.Remove(key);
                }
                else
                {
                    _settings[key] = value;
                }
                SaveSettings(_settings);
            }

            public DateTime? GetCurrentUserIdLastUsed()
            {
                string lastUsedStr = GetSetting("CurrentUserIdLastUsed");
                if (DateTime.TryParse(lastUsedStr, out DateTime lastUsed))
                {
                    return lastUsed;
                }
                return null;
            }

            public void SetCurrentUserIdLastUsed(DateTime? dateTime)
            {
                if (dateTime.HasValue)
                {
                    SetSetting("CurrentUserIdLastUsed", dateTime.Value.ToString("o"));
                }
                else
                {
                    SetSetting("CurrentUserIdLastUsed", null);
                }
            }

            public bool GetSessionExpiredShown()
            {
                string value = GetSetting("SessionExpiredShown");
                return bool.TryParse(value, out bool shown) && shown;
            }

            public void SetSessionExpiredShown(bool shown)
            {
                SetSetting("SessionExpiredShown", shown.ToString());
            }

            public DateTime? GetAppLastClosed()
            {
                string lastClosedStr = GetSetting("AppLastClosed");
                if (DateTime.TryParse(lastClosedStr, out DateTime lastClosed))
                {
                    return lastClosed;
                }
                return null;
            }

            public void SetAppLastClosed(DateTime? dateTime)
            {
                if (dateTime.HasValue)
                {
                    SetSetting("AppLastClosed", dateTime.Value.ToString("o"));
                }
                else
                {
                    SetSetting("AppLastClosed", null);
                }
            }
        }
    }