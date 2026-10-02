using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace MonitorAltTab.Core
{
    public class AppSettings
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "AltTabCanavari";

        public bool IsEnabled { get; set; } = true;
        public bool AutoStartWithWindows { get; set; } = false;
        public bool MinimizeToTrayOnClose { get; set; } = true;
        public bool StartMinimized { get; set; } = false;

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AltTabCanavari");
        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        // Sync auto-start with actual registry state
                        settings.AutoStartWithWindows = IsAutoStartEnabled();
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] Load failed: {ex.Message}");
            }

            var defaultSettings = new AppSettings();
            defaultSettings.AutoStartWithWindows = IsAutoStartEnabled();
            return defaultSettings;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] Save failed: {ex.Message}");
            }
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public void SetAutoStart(bool enable)
        {
            AutoStartWithWindows = enable;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\" --minimized");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] SetAutoStart failed: {ex.Message}");
            }
            Save();
        }
    }
}
