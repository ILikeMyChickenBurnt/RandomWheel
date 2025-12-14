using System;
using System.IO;
using System.Windows.Media;

namespace RandomWheel.Services
{
    public class SoundService
    {
        private MediaPlayer? _mediaPlayer;
        private readonly string _defaultSoundPath;

        public SoundService()
        {
            _defaultSoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "winner_sound.mp3");
        }

        /// <summary>
        /// Plays the winner sound. Uses custom sound if set, otherwise uses default.
        /// </summary>
        public void PlayWinnerSound(string? customSoundPath = null)
        {
            try
            {
                string soundPath = GetSoundPath(customSoundPath);
                
                if (string.IsNullOrEmpty(soundPath) || !File.Exists(soundPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Sound file not found: {soundPath}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Playing sound: {soundPath}");

                // Stop any currently playing sound
                Stop();

                // Use MediaPlayer for more format support (.wav, .mp3, etc.)
                _mediaPlayer = new MediaPlayer();
                
                // Subscribe to MediaOpened to ensure we play after the file is loaded
                _mediaPlayer.MediaOpened += (s, e) =>
                {
                    _mediaPlayer?.Play();
                    System.Diagnostics.Debug.WriteLine("Sound playback started");
                };
                
                _mediaPlayer.MediaFailed += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorException?.Message}");
                };
                
                _mediaPlayer.Open(new Uri(soundPath, UriKind.Absolute));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops any currently playing sound.
        /// </summary>
        public void Stop()
        {
            try
            {
                _mediaPlayer?.Stop();
                _mediaPlayer?.Close();
                _mediaPlayer = null;
            }
            catch
            {
                // Ignore errors when stopping
            }
        }

        /// <summary>
        /// Gets the sound path to use, preferring custom path if valid.
        /// </summary>
        private string GetSoundPath(string? customSoundPath)
        {
            // Use custom path if provided and file exists
            if (!string.IsNullOrEmpty(customSoundPath) && File.Exists(customSoundPath))
            {
                return customSoundPath;
            }

            // Fall back to default sound
            if (File.Exists(_defaultSoundPath))
            {
                return _defaultSoundPath;
            }

            System.Diagnostics.Debug.WriteLine($"Default sound not found at: {_defaultSoundPath}");
            return string.Empty;
        }

        /// <summary>
        /// Checks if a sound file is valid (exists and is a supported format).
        /// </summary>
        public bool IsValidSoundFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".wav" || extension == ".mp3" || extension == ".wma" || extension == ".aac";
        }
    }
}
