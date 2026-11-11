using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BestClip
{
    /// <summary>Creates a believable, non-sensitive history for screenshots and demos.</summary>
    public static class DemoData
    {
        public static void Seed(HistoryStore store)
        {
            if (store == null || store.Items.Count > 0) return;

            DateTime now = DateTime.UtcNow;

            AddText(store, "BestClip", "欢迎使用 BestClip 👋\n\n按 Ctrl + Shift + V 随时呼出，↑↓ 选择，Enter 粘贴。\n收藏的内容会固定在顶部，不会被自动清理。", now.AddMinutes(-3), true);

            AddText(store, "Visual Studio Code",
                "private void OnDebounceTick(object sender, EventArgs e)\n{\n    _debounce.Stop();\n    CaptureNow();\n}",
                now.AddMinutes(-12), false);

            AddText(store, "Google Chrome", "https://github.com/langlimumu/bestClip", now.AddMinutes(-26), false);

            AddText(store, "Outlook",
                "你好，附件是本周的产品评审纪要，麻烦在周五前确认一下～",
                now.AddMinutes(-48), false);

            AddImage(store, now.AddMinutes(-74), "Figma");

            AddFiles(store, now.AddHours(-2), "文件资源管理器",
                @"C:\Users\wulang\Documents\季度报告.docx",
                @"C:\Users\wulang\Documents\产品路线图.xlsx",
                @"C:\Users\wulang\Documents\品牌规范.pdf");

            AddText(store, "微信", "明天下午三点，老地方见。", now.AddHours(-3), false);

            AddText(store, "Figma", "#7C6CFF", now.AddHours(-5), false);

            AddText(store, "Windows Terminal",
                "git status && git add . && git commit -m \"polish clipboard UI\"",
                now.AddHours(-8), false);

            AddText(store, "Notion",
                "好的设计是尽可能少的设计。—— Dieter Rams",
                now.AddHours(-26), false);

            store.SaveNow();
        }

        private static void AddText(HistoryStore store, string app, string text, DateTime time, bool pinned)
        {
            var item = new ClipItem();
            item.Id = Guid.NewGuid().ToString("N");
            item.Kind = ClipKind.Text;
            item.Text = text;
            item.AppName = app;
            item.CreatedUtc = time;
            item.LastUsedUtc = time;
            item.Hash = "demo-" + Guid.NewGuid().ToString("N");
            item.Pinned = pinned;
            item.SizeBytes = System.Text.Encoding.UTF8.GetByteCount(text);
            store.Add(item);
        }

        private static void AddFiles(HistoryStore store, DateTime time, string app, params string[] files)
        {
            var item = new ClipItem();
            item.Id = Guid.NewGuid().ToString("N");
            item.Kind = ClipKind.Files;
            item.Files = files;
            item.Text = string.Join("\r\n", files);
            item.AppName = app;
            item.CreatedUtc = time;
            item.LastUsedUtc = time;
            item.Hash = "demo-" + Guid.NewGuid().ToString("N");
            store.Add(item);
        }

        private static void AddImage(HistoryStore store, DateTime time, string app)
        {
            string fileName = "demo-" + Guid.NewGuid().ToString("N") + ".png";
            string path = AppPaths.ImagePath(fileName);
            int width = 960;
            int height = 600;
            GenerateImage(path, width, height);

            var item = new ClipItem();
            item.Id = Guid.NewGuid().ToString("N");
            item.Kind = ClipKind.Image;
            item.AppName = app;
            item.CreatedUtc = time;
            item.LastUsedUtc = time;
            item.Hash = "demo-" + Guid.NewGuid().ToString("N");
            item.ImageFile = fileName;
            item.PixelWidth = width;
            item.PixelHeight = height;
            try { item.SizeBytes = new FileInfo(path).Length; } catch { }
            item.LoadThumbnail();
            store.Add(item);
        }

        private static void GenerateImage(string path, int width, int height)
        {
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                var background = new LinearGradientBrush(
                    Color.FromRgb(0x1A, 0x14, 0x3D),
                    Color.FromRgb(0x0B, 0x1B, 0x3A),
                    new Point(0, 0), new Point(1, 1));
                dc.DrawRectangle(background, null, new Rect(0, 0, width, height));

                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(0x66, 0x8B, 0x7C, 0xFF)), null,
                    new Point(width * 0.78, height * 0.25), 230, 230);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(0x55, 0x4D, 0xA3, 0xFF)), null,
                    new Point(width * 0.18, height * 0.82), 270, 270);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)), null,
                    new Point(width * 0.55, height * 0.62), 150, 150);

                var typeface = new Typeface(new FontFamily("Microsoft YaHei UI"),
                    FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

                var title = new FormattedText("灵感 · 设计稿", CultureInfo.GetCultureInfo("zh-CN"),
                    FlowDirection.LeftToRight, typeface, 64, Brushes.White, 1.0);
                dc.DrawText(title, new Point(70, height - 190));

                var sub = new FormattedText("BestClip  /  clipboard, beautifully organized",
                    CultureInfo.GetCultureInfo("en-US"), FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    22, new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)), 1.0);
                dc.DrawText(sub, new Point(74, height - 112));
            }

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var fs = File.Create(path))
            {
                encoder.Save(fs);
            }
        }
    }
}
