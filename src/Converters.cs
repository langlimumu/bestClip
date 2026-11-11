using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BestClip
{
    public class KindGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string kind = value as string;
            if (kind == ClipKind.Image) return "\uEB9F";
            if (kind == ClipKind.Files) return "\uE8B7";
            return "\uE8A5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class KindBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush TextBrush = Freeze("#7FB0FF");
        private static readonly SolidColorBrush ImageBrush = Freeze("#C79BFF");
        private static readonly SolidColorBrush FileBrush = Freeze("#F3C06B");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string kind = value as string;
            if (kind == ClipKind.Image) return ImageBrush;
            if (kind == ClipKind.Files) return FileBrush;
            return TextBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static SolidColorBrush Freeze(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class KindBackgroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush TextBg = Freeze("#1F7FB0FF");
        private static readonly SolidColorBrush ImageBg = Freeze("#24C79BFF");
        private static readonly SolidColorBrush FileBg = Freeze("#24F3C06B");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string kind = value as string;
            if (kind == ClipKind.Image) return ImageBg;
            if (kind == ClipKind.Files) return FileBg;
            return TextBg;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static SolidColorBrush Freeze(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class ThumbnailBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapSource source = value as BitmapSource;
            if (source == null) return Brushes.Transparent;
            var brush = new ImageBrush(source)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class TimeAgoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime utc;
            if (value is DateTime)
                utc = ((DateTime)value).ToUniversalTime();
            else
                return "";

            if (utc.Year < 2000) return "";
            TimeSpan span = DateTime.UtcNow - utc;
            if (span.TotalSeconds < 45) return "刚刚";
            if (span.TotalSeconds < 3600) return Math.Max(1, (int)(span.TotalSeconds / 60)) + " 分钟前";
            if (span.TotalHours < 24) return (int)span.TotalHours + " 小时前";
            if (span.TotalDays < 7) return (int)span.TotalDays + " 天前";
            return utc.ToLocalTime().ToString("M月d日");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool && (bool)value;
            bool invert = parameter as string == "invert";
            if (invert) flag = !flag;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool has = !string.IsNullOrEmpty(value as string);
            bool invert = parameter as string == "invert";
            if (invert) has = !has;
            return has ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class PinOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && (bool)value ? 1.0 : 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
