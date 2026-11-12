using System;
using System.IO;

namespace BestClip
{
    /// <summary>All on-disk locations used by BestClip.</summary>
    public static class AppPaths
    {
        private static string _dataDir;

        static AppPaths()
        {
            string overrideDir = Environment.GetEnvironmentVariable("BESTCLIP_DATA");
            if (!string.IsNullOrEmpty(overrideDir))
            {
                Configure(overrideDir);
                return;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string temp = Path.GetTempPath();

            // Locked-down company PCs sometimes deny writes to %APPDATA%.
            // Try the standard location first, then portable/fallback locations.
            var candidates = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(appData)) candidates.Add(Path.Combine(appData, "BestClip"));
            if (!string.IsNullOrEmpty(localAppData)) candidates.Add(Path.Combine(localAppData, "BestClip"));
            if (!string.IsNullOrEmpty(exeDir)) candidates.Add(Path.Combine(exeDir, "BestClip-data"));
            if (!string.IsNullOrEmpty(temp)) candidates.Add(Path.Combine(temp, "BestClip"));

            foreach (string candidate in candidates)
            {
                try
                {
                    Directory.CreateDirectory(candidate);
                    string probe = Path.Combine(candidate, ".write-test-" + Guid.NewGuid().ToString("N"));
                    File.WriteAllText(probe, "");
                    File.Delete(probe);
                    Configure(candidate);
                    return;
                }
                catch
                {
                    // Try the next writable location.
                }
            }

            // Nothing worked; keep the standard path so the error message is meaningful.
            Configure(Path.Combine(appData ?? "", "BestClip"));
        }

        private static void Configure(string dataDir)
        {
            _dataDir = dataDir;
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
