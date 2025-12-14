using System;
using System.IO;
using System.Windows.Media;

namespace RandomWheel.Services
{
    public class SoundService
    {
        private MediaPlayer? _mediaPlayer;

        public SoundService()
        {
        }

        /// <summary>
        /// Plays the winner sound if a custom sound path is configured.
        /// </summary>
        public void PlayWinnerSound(string? customSoundPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(customSoundPath) || !File.Exists(customSoundPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Sound file not found or not configured: {customSoundPath}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Playing sound: {customSoundPath}");

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
                
                _mediaPlayer.Open(new Uri(customSoundPath, UriKind.Absolute));
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
