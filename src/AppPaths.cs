using System;
using System.IO;

namespace BestClip
{
    /// <summary>All on-disk locations used by BestClip.</summary>
    public static class AppPaths
    {
        private static readonly string _dataDir;

        static AppPaths()
        {
            string overrideDir = Environment.GetEnvironmentVariable("BESTCLIP_DATA");
            if (!string.IsNullOrEmpty(overrideDir))
            {
                _dataDir = overrideDir;
            }
            else
            {
                _dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BestClip");
            }

            ImagesDir = Path.Combine(_dataDir, "images");
            HistoryFile = Path.Combine(_dataDir, "history.json");
            SettingsFile = Path.Combine(_dataDir, "settings.json");
            LogFile = Path.Combine(_dataDir, "error.log");
        }

        public static string DataDir { get { return _dataDir; } }
        public static string ImagesDir { get; private set; }
        public static string HistoryFile { get; private set; }
        public static string SettingsFile { get; private set; }
        public static string LogFile { get; private set; }

        public static void EnsureCreated()
        {
            Directory.CreateDirectory(_dataDir);
            Directory.CreateDirectory(ImagesDir);
        }

        public static string ImagePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            return Path.Combine(ImagesDir, fileName);
        }

        public static void Log(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(_dataDir);
                string line = string.Format(
                    "[{0:yyyy-MM-dd HH:mm:ss}] {1}\r\n{2}\r\n\r\n",
                    DateTime.Now, ex.Message, ex);
                File.AppendAllText(LogFile, line);
            }
            catch
            {
                // Logging must never take the app down.
            }
        }
    }
}
