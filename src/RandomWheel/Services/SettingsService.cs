using System;
using System.IO;
using System.Text.Json;

namespace RandomWheel.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings _settings;

        public SettingsService()
        {
            _settingsFilePath = Path.Combine(Paths.AppFolder, "settings.json");
            _settings = Load();
        }

        public string? CustomWinnerSoundPath
        {
            get => _settings.CustomWinnerSoundPath;
            set
            {
                _settings.CustomWinnerSoundPath = value;
                Save();
            }
        }

        public bool WinnerSoundEnabled
        {
            get => _settings.WinnerSoundEnabled;
            set
            {
                _settings.WinnerSoundEnabled = value;
                Save();
            }
        }

        public string? BrandingLogoPath
        {
            get => _settings.BrandingLogoPath;
            set
            {
                _settings.BrandingLogoPath = value;
                Save();
            }
        }

        public double BrandingLogoOffsetX
        {
            get => _settings.BrandingLogoOffsetX;
            set
            {
                _settings.BrandingLogoOffsetX = value;
                Save();
            }
        }

        public double BrandingLogoOffsetY
        {
            get => _settings.BrandingLogoOffsetY;
            set
            {
                _settings.BrandingLogoOffsetY = value;
                Save();
            }
        }

        public double BrandingLogoScale
        {
            get => _settings.BrandingLogoScale;
            set
            {
                _settings.BrandingLogoScale = value;
                Save();
            }
        }

        private AppSettings Load()
        {
            try
            {
                Paths.EnsureAppFolder();
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            return new AppSettings();
        }

        private void Save()
        {
            try
            {
                Paths.EnsureAppFolder();
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private class AppSettings
        {
            public string? CustomWinnerSoundPath { get; set; }
            public bool WinnerSoundEnabled { get; set; } = true;
            public string? BrandingLogoPath { get; set; }
            public double BrandingLogoOffsetX { get; set; } = 0;
            public double BrandingLogoOffsetY { get; set; } = 0;
            public double BrandingLogoScale { get; set; } = 1.0;
        }
    }
}
