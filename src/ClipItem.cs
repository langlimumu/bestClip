using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Media.Imaging;

namespace BestClip
{
    public static class ClipKind
    {
        public const string Text = "text";
        public const string Image = "image";
        public const string Files = "files";
    }

    [DataContract]
    public class ClipItem : INotifyPropertyChanged
    {
        private bool _pinned;
        private BitmapSource _thumbnail;

        [DataMember(Order = 1)] public string Id { get; set; }
        [DataMember(Order = 2)] public string Kind { get; set; }
        [DataMember(Order = 3)] public string Text { get; set; }
        [DataMember(Order = 4)] public string[] Files { get; set; }
        [DataMember(Order = 5)] public string AppName { get; set; }
        [DataMember(Order = 6)] public string AppTitle { get; set; }
        [DataMember(Order = 7)] public DateTime CreatedUtc { get; set; }
        [DataMember(Order = 8)] public DateTime LastUsedUtc { get; set; }
        [DataMember(Order = 9)] public string Hash { get; set; }
        [DataMember(Order = 10)] public string ImageFile { get; set; }
        [DataMember(Order = 11)] public int PixelWidth { get; set; }
        [DataMember(Order = 12)] public int PixelHeight { get; set; }
        [DataMember(Order = 13)] public long SizeBytes { get; set; }

        [DataMember(Order = 14)]
        public bool Pinned
        {
            get { return _pinned; }
            set
            {
                if (_pinned == value) return;
                _pinned = value;
                Raise("Pinned");
                Raise("PinGlyphOpacity");
            }
        }

        public BitmapSource Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
                Raise("Thumbnail");
                Raise("HasThumbnail");
            }
        }

        public bool HasThumbnail { get { return _thumbnail != null; } }

        public string KindLabel
        {
            get
            {
                if (Kind == ClipKind.Image) return "图片";
                if (Kind == ClipKind.Files) return "文件";
                return "文本";
            }
        }

        public string Title
        {
            get
            {
                if (Kind == ClipKind.Image)
                    return PixelWidth > 0 && PixelHeight > 0
                        ? string.Format("图片  {0} × {1}", PixelWidth, PixelHeight)
                        : "图片";
                if (Kind == ClipKind.Files)
                {
                    string first = Files != null && Files.Length > 0
                        ? Path.GetFileName(Files[0])
                        : "文件";
                    if (string.IsNullOrEmpty(first)) first = Files[0];
                    return Files != null && Files.Length > 1
                        ? string.Format("{0}  等 {1} 个文件", first, Files.Length)
                        : first;
                }

                string t = Normalize(Text);
                if (t.Length > 160) t = t.Substring(0, 160);
                return string.IsNullOrEmpty(t) ? "(空文本)" : t;
            }
        }

        public string Preview
        {
            get
            {
                if (Kind == ClipKind.Image)
                {
                    if (PixelWidth > 0 && PixelHeight > 0)
                        return string.Format("图片 · {0} × {1}", PixelWidth, PixelHeight);
                    return "图片";
                }
                if (Kind == ClipKind.Files)
                {
                    if (Files == null || Files.Length == 0) return "文件";
                    var sb = new StringBuilder();
                    int max = Math.Min(Files.Length, 3);
                    for (int i = 0; i < max; i++)
                    {
                        if (i > 0) sb.Append("   ");
                        sb.Append(Path.GetFileName(Files[i]));
                    }
                    if (Files.Length > max) sb.Append("   …");
                    return sb.ToString();
                }

                string text = Normalize(Text);
                if (text.Length > 220) text = text.Substring(0, 220) + "…";
                return text;
            }
        }

        public string MetaText
        {
            get
            {
                if (Kind == ClipKind.Image)
                {
                    if (SizeBytes <= 0) return "";
                    return FormatSize(SizeBytes);
                }
                if (Kind == ClipKind.Files)
                {
                    int n = Files == null ? 0 : Files.Length;
                    return n + " 项";
                }
                if (Text == null) return "";
                return Text.Length.ToString("N0") + " 字符";
            }
        }

        public string SourceLabel
        {
            get
            {
                string name = AppName;
                if (string.IsNullOrEmpty(name)) name = "未知来源";
                if (name.Length > 22) name = name.Substring(0, 21) + "…";
                return name;
            }
        }

        public string ToolTipText
        {
            get
            {
                if (Kind == ClipKind.Files && Files != null)
                    return string.Join("\r\n", Files);
                if (Kind == ClipKind.Image)
                    return string.Format("{0} × {1}", PixelWidth, PixelHeight);
                string t = Text ?? "";
                if (t.Length > 1500) t = t.Substring(0, 1500) + "…";
                return t;
            }
        }

        public string SearchText
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(KindLabel).Append(' ');
                if (!string.IsNullOrEmpty(Text)) sb.Append(Text).Append(' ');
                if (Files != null)
                {
                    foreach (string f in Files)
                    {
                        sb.Append(f).Append(' ');
                    }
                }
                sb.Append(AppName).Append(' ');
                return sb.ToString().ToLowerInvariant();
            }
        }

        public bool IsImage { get { return Kind == ClipKind.Image; } }

        public void LoadThumbnail()
        {
            if (Kind != ClipKind.Image || string.IsNullOrEmpty(ImageFile)) return;
            string path = AppPaths.ImagePath(ImageFile);
            if (!File.Exists(path)) return;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bmp.DecodePixelWidth = 140;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                Thumbnail = bmp;
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
        }

        internal static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!space && sb.Length > 0) sb.Append(' ');
                    space = true;
                }
                else
                {
                    sb.Append(c);
                    space = false;
                }
            }
            return sb.ToString().Trim();
        }

        internal static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.#") + " KB";
            return (bytes / (1024.0 * 1024.0)).ToString("0.#") + " MB";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Raise(string name)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
