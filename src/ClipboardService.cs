using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BestClip
{
    /// <summary>Watches the Windows clipboard, turns changes into ClipItems and writes items back.</summary>
    public class ClipboardService
    {
        private readonly HistoryStore _store;
        private readonly AppSettings _settings;
        private readonly DispatcherTimer _debounce;
        private ForegroundInfo _pendingForeground;
        private DateTime _ignoreUntil = DateTime.MinValue;
        private bool _enabled = true;

        public event EventHandler<ClipItem> Captured;

        public ClipboardService(HistoryStore store, AppSettings settings)
        {
            _store = store;
            _settings = settings;
            _debounce = new DispatcherTimer(DispatcherPriority.Background);
            _debounce.Interval = TimeSpan.FromMilliseconds(170);
            _debounce.Tick += OnDebounceTick;
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (!value) _debounce.Stop();
            }
        }

        public void ScheduleCapture()
        {
            _pendingForeground = ForegroundInfo.Capture();
            _debounce.Stop();
            _debounce.Start();
        }

        public void CaptureCurrentClipboard()
        {
            _pendingForeground = ForegroundInfo.Capture();
            CaptureNow();
        }

        public void IgnoreChangesFor(TimeSpan duration)
        {
            _ignoreUntil = DateTime.UtcNow + duration;
        }

        private void OnDebounceTick(object sender, EventArgs e)
        {
            _debounce.Stop();
            CaptureNow();
        }

        private void CaptureNow()
        {
            if (!_enabled) return;
            if (DateTime.UtcNow < _ignoreUntil) return;

            ForegroundInfo fg = _pendingForeground ?? ForegroundInfo.Capture();
            if (fg != null && fg.ProcessId == Process.GetCurrentProcess().Id) return;
            if (fg != null && _settings.IgnorePasswordManagers && fg.IsPasswordManager) return;

            ClipItem item = ReadClipboard(fg);
            if (item == null) return;

            ClipItem added = _store.Add(item);
            var handler = Captured;
            if (handler != null) handler(this, added);
        }

        private ClipItem ReadClipboard(ForegroundInfo fg)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    if (Clipboard.ContainsFileDropList())
                    {
                        StringCollection files = Clipboard.GetFileDropList();
                        if (files != null && files.Count > 0)
                        {
                            return BuildFilesItem(files, fg);
                        }
                    }

                    if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                    {
                        string text = Clipboard.GetText(TextDataFormat.UnicodeText);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (text.Length > 4000000) return null;
                            return BuildTextItem(text, fg);
                        }
                    }

                    if (Clipboard.ContainsImage())
                    {
                        BitmapSource image = Clipboard.GetImage();
                        if (image != null)
                        {
                            return BuildImageItem(image, fg);
                        }
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    if (attempt == 5)
                    {
                        AppPaths.Log(ex);
                        return null;
                    }
                    Thread.Sleep(35);
                }
            }
            return null;
        }

        private ClipItem BuildTextItem(string text, ForegroundInfo fg)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            var item = new ClipItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Kind = ClipKind.Text,
                Text = text,
                Hash = HashBytes(bytes, "text:"),
                SizeBytes = bytes.Length,
                CreatedUtc = DateTime.UtcNow,
                LastUsedUtc = DateTime.UtcNow
            };
            ApplyForeground(item, fg);
            return item;
        }

        private ClipItem BuildFilesItem(StringCollection files, ForegroundInfo fg)
        {
            var list = new string[files.Count];
            files.CopyTo(list, 0);
            var sb = new StringBuilder();
            foreach (string f in list) sb.Append(f).Append('|');
            var item = new ClipItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Kind = ClipKind.Files,
                Files = list,
                Text = string.Join("\r\n", list),
                Hash = HashBytes(Encoding.UTF8.GetBytes(sb.ToString()), "files:"),
                CreatedUtc = DateTime.UtcNow,
                LastUsedUtc = DateTime.UtcNow
            };
            ApplyForeground(item, fg);
            return item;
        }

        private ClipItem BuildImageItem(BitmapSource image, ForegroundInfo fg)
        {
            if (image.CanFreeze) image.Freeze();

            byte[] png;
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                png = ms.ToArray();
            }

            if (png.Length > 30 * 1024 * 1024) return null;

            var item = new ClipItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Kind = ClipKind.Image,
                PixelWidth = image.PixelWidth,
                PixelHeight = image.PixelHeight,
                Hash = HashBytes(png, "image:"),
                SizeBytes = png.Length,
                CreatedUtc = DateTime.UtcNow,
                LastUsedUtc = DateTime.UtcNow,
                ImageFile = Guid.NewGuid().ToString("N") + ".png"
            };

            AppPaths.EnsureCreated();
            string path = AppPaths.ImagePath(item.ImageFile);
            File.WriteAllBytes(path, png);

            var thumb = new BitmapImage();
            thumb.BeginInit();
            thumb.CacheOption = BitmapCacheOption.OnLoad;
            thumb.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            thumb.DecodePixelWidth = 140;
            thumb.StreamSource = new MemoryStream(png);
            thumb.EndInit();
            thumb.Freeze();
            item.Thumbnail = thumb;

            ApplyForeground(item, fg);
            return item;
        }

        private static void ApplyForeground(ClipItem item, ForegroundInfo fg)
        {
            if (fg == null) return;
            item.AppName = fg.AppName;
            item.AppTitle = fg.Title;
        }

        private static string HashBytes(byte[] bytes, string prefix)
        {
            using (var sha = SHA1.Create())
            {
                byte[] prefixBytes = Encoding.UTF8.GetBytes(prefix);
                byte[] all = new byte[prefixBytes.Length + bytes.Length];
                Buffer.BlockCopy(prefixBytes, 0, all, 0, prefixBytes.Length);
                Buffer.BlockCopy(bytes, 0, all, prefixBytes.Length, bytes.Length);
                return Convert.ToBase64String(sha.ComputeHash(all));
            }
        }

        /// <summary>The last clipboard write failure, kept for diagnostics.</summary>
        public Exception LastWriteError { get; private set; }

        /// <summary>Puts the item back on the clipboard. Returns false when the clipboard is locked.</summary>
        public bool WriteToClipboard(ClipItem item)
        {
            return WriteToClipboard(item, 1);
        }

        /// <summary>Puts the item back on the clipboard, retrying a bounded number of times.</summary>
        public bool WriteToClipboard(ClipItem item, int attempts)
        {
            if (item == null) return false;
            if (attempts < 1) attempts = 1;
            LastWriteError = null;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                try
                {
                    if (item.Kind == ClipKind.Text)
                    {
                        var data = new DataObject();
                        data.SetData(DataFormats.UnicodeText, item.Text ?? "");
                        data.SetData(DataFormats.Text, item.Text ?? "");
                        Clipboard.SetDataObject(data, true);
                    }
                    else if (item.Kind == ClipKind.Files)
                    {
                        var files = new StringCollection();
                        if (item.Files != null) files.AddRange(item.Files);
                        Clipboard.SetFileDropList(files);
                    }
                    else if (item.Kind == ClipKind.Image)
                    {
                        string path = AppPaths.ImagePath(item.ImageFile);
                        if (!File.Exists(path)) return false;
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(path, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        Clipboard.SetImage(bmp);
                    }
                    LastWriteError = null;
                    return true;
                }
                catch (Exception ex)
                {
                    LastWriteError = ex;
                    if (attempt + 1 < attempts) Thread.Sleep(45);
                }
            }
            return false;
        }
    }

    public class ForegroundInfo
    {
        public IntPtr Handle;
        public uint ProcessId;
        public string AppName;
        public string Title;
        public bool IsPasswordManager;

        private static readonly Dictionary<string, string> FriendlyNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "chrome", "Google Chrome" },
                { "msedge", "Microsoft Edge" },
                { "firefox", "Firefox" },
                { "code", "Visual Studio Code" },
                { "devenv", "Visual Studio" },
                { "explorer", "文件资源管理器" },
                { "notepad", "记事本" },
                { "notepad++", "Notepad++" },
                { "windowsterminal", "Windows Terminal" },
                { "cmd", "命令提示符" },
                { "powershell", "PowerShell" },
                { "pwsh", "PowerShell" },
                { "wechat", "微信" },
                { "weixin", "微信" },
                { "qq", "QQ" },
                { "winword", "Word" },
                { "excel", "Excel" },
                { "powerpnt", "PowerPoint" },
                { "outlook", "Outlook" },
                { "acrobat", "Acrobat" },
                { "photoshop", "Photoshop" },
                { "figma", "Figma" },
                { "idea64", "IntelliJ IDEA" },
                { "pycharm64", "PyCharm" }
            };

        private static readonly string[] PasswordManagers =
        {
            "keepass", "1password", "bitwarden", "lastpass", "dashlane", "enpass", "roboform"
        };

        public static ForegroundInfo Capture()
        {
            try
            {
                IntPtr hwnd = Native.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;

                uint pid;
                Native.GetWindowThreadProcessId(hwnd, out pid);
                if (pid == 0) return null;

                string title = Native.GetWindowTitle(hwnd);
                string processName = null;
                string appName = null;

                try
                {
                    using (Process p = Process.GetProcessById((int)pid))
                    {
                        processName = p.ProcessName;
                        appName = processName;
                        try
                        {
                            string desc = p.MainModule.FileVersionInfo.FileDescription;
                            if (!string.IsNullOrWhiteSpace(desc)) appName = desc.Trim();
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                string friendly;
                if (!string.IsNullOrEmpty(processName) && FriendlyNames.TryGetValue(processName, out friendly))
                {
                    appName = friendly;
                }

                bool passwordManager = false;
                if (!string.IsNullOrEmpty(processName))
                {
                    string lower = processName.ToLowerInvariant();
                    foreach (string p in PasswordManagers)
                    {
                        if (lower.Contains(p)) { passwordManager = true; break; }
                    }
                }

                if (title != null && title.Length > 90) title = title.Substring(0, 89) + "…";

                return new ForegroundInfo
                {
                    Handle = hwnd,
                    ProcessId = pid,
                    AppName = appName,
                    Title = title,
                    IsPasswordManager = passwordManager
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
