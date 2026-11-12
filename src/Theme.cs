using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace BestClip
{
    /// <summary>A complete, immutable color set for one theme.</summary>
    public sealed class ThemePalette
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public bool IsLight;

        public Brush CardBg;
        public Brush WindowBorder;
        public Color ShadowColor;
        public double ShadowOpacity;

        public Brush TextPrimary;
        public Brush TextBody;
        public Brush TextSecondary;
        public Brush TextMuted;
        public Brush TextFaint;
        public Brush TextOnAccent;
        public Brush Icon;

        public Brush ControlBg;
        public Brush ControlBorder;
        public Brush ControlHover;
        public Brush ControlPressed;

        public Brush ChipText;
        public Brush Link;
        public Brush LinkHover;
        public Brush SearchText;
        public Brush SearchCaret;
        public Brush Selection;

        public Brush ListHover;
        public Brush ListSelected;
        public Brush ListSelectedBorder;
        public Brush ScrollThumb;

        public Brush MenuBg;
        public Brush MenuBorder;
        public Brush MenuText;
        public Brush MenuShortcut;
        public Brush MenuItemHover;
        public Brush MenuSeparator;

        public Brush ToastBg;
        public Brush ToastBorder;
        public Brush ToastText;
        public Brush ToastIcon;

        public Brush PausedBg;
        public Brush PausedBorder;
        public Brush PausedText;

        public Brush SearchBoxBg;
        public Brush SearchBoxBorder;
        public Brush HotkeyBadgeBg;
        public Brush HotkeyBadgeText;
        public Brush SearchIcon;
        public Brush SearchPlaceholder;

        public Brush EmptyIconBg;
        public Brush EmptyIconBorder;
        public Brush EmptyIcon;

        public Brush KeycapBg;
        public Brush KeycapText;
        public Brush FooterText;
        public Brush CountText;

        public Brush Pin;
        public Brush Accent;
        public Brush AccentSoft;
        public Brush AccentPale;

        public Brush DangerText;
        public Brush DangerBg;
        public Brush DangerBorder;
        public Brush DangerHover;

        public Brush PanelBg;
        public Brush PanelBorder;
        public Brush Divider;

        public Brush HotkeyNormalBorder;
        public Brush HotkeyActiveBorder;
        public Brush HotkeyNormalBg;
        public Brush HotkeyActiveBg;

        public Brush SegmentBg;
        public Brush SegmentBorder;
        public Brush SegmentHover;
        public Brush SegmentCheckedBg;
        public Brush SegmentCheckedBorder;
        public Brush SegmentCheckedText;

        public Brush SwitchTrackOff;
        public Brush SwitchKnobOff;
        public Brush FooterSeparator;
        public Brush VersionText;

        private static SolidColorBrush S(string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private static Brush G(string top, string bottom)
        {
            var b = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            b.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(top), 0));
            b.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(bottom), 1));
            b.Freeze();
            return b;
        }

        private static Color C(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        public static ThemePalette Dark()
        {
            var t = new ThemePalette();
            t.Id = "dark";
            t.DisplayName = "深色";
            t.Description = "经典深色，适合夜间";
            t.IsLight = false;
            t.CardBg = G("#101219", "#0C0E14");
            t.WindowBorder = S("#1FFFFFFF");
            t.ShadowColor = C("#000000");
            t.ShadowOpacity = 0.55;
            t.TextPrimary = S("#F7F9FC");
            t.TextBody = S("#EDF1F7");
            t.TextSecondary = S("#8D97AA");
            t.TextMuted = S("#5F697E");
            t.TextFaint = S("#4C5568");
            t.TextOnAccent = S("#FFFFFF");
            t.Icon = S("#98A2B5");
            t.ControlBg = S("#0BFFFFFF");
            t.ControlBorder = S("#17FFFFFF");
            t.ControlHover = S("#14FFFFFF");
            t.ControlPressed = S("#22FFFFFF");
            t.ChipText = S("#818CA1");
            t.Link = S("#6F7A8E");
            t.LinkHover = S("#D7DCE6");
            t.SearchText = S("#F2F5FA");
            t.SearchCaret = S("#8B7CFF");
            t.Selection = S("#667C6CFF");
            t.ListHover = S("#0AFFFFFF");
            t.ListSelected = S("#167C6CFF");
            t.ListSelectedBorder = S("#337C6CFF");
            t.ScrollThumb = S("#2EFFFFFF");
            t.MenuBg = S("#171A23");
            t.MenuBorder = S("#26FFFFFF");
            t.MenuText = S("#E6EAF2");
            t.MenuShortcut = S("#697386");
            t.MenuItemHover = S("#1AFFFFFF");
            t.MenuSeparator = S("#14FFFFFF");
            t.ToastBg = S("#F21A1E28");
            t.ToastBorder = S("#337C6CFF");
            t.ToastText = S("#E9EDF5");
            t.ToastIcon = S("#7C6CFF");
            t.PausedBg = S("#1FF3C06B");
            t.PausedBorder = S("#40F3C06B");
            t.PausedText = S("#F3C06B");
            t.SearchBoxBg = S("#0BFFFFFF");
            t.SearchBoxBorder = S("#17FFFFFF");
            t.HotkeyBadgeBg = S("#0FFFFFFF");
            t.HotkeyBadgeText = S("#7F8A9F");
            t.SearchIcon = S("#7A8498");
            t.SearchPlaceholder = S("#5C6679");
            t.EmptyIconBg = S("#0EFFFFFF");
            t.EmptyIconBorder = S("#14FFFFFF");
            t.EmptyIcon = S("#7C6CFF");
            t.KeycapBg = S("#0FFFFFFF");
            t.KeycapText = S("#8D97AA");
            t.FooterText = S("#5E687B");
            t.CountText = S("#667085");
            t.Pin = S("#A99CFF");
            t.Accent = S("#7C6CFF");
            t.AccentSoft = S("#A99CFF");
            t.AccentPale = S("#9A8CFF");
            t.DangerText = S("#FF8093");
            t.DangerBg = S("#14FF6B81");
            t.DangerBorder = S("#40FF6B81");
            t.DangerHover = S("#26FF6B81");
            t.PanelBg = S("#08FFFFFF");
            t.PanelBorder = S("#12FFFFFF");
            t.Divider = S("#0EFFFFFF");
            t.HotkeyNormalBorder = S("#1FFFFFFF");
            t.HotkeyActiveBorder = S("#7C6CFF");
            t.HotkeyNormalBg = S("#0FFFFFFF");
            t.HotkeyActiveBg = S("#1F7C6CFF");
            t.SegmentBg = S("#0AFFFFFF");
            t.SegmentBorder = S("#14FFFFFF");
            t.SegmentHover = S("#0FFFFFFF");
            t.SegmentCheckedBg = S("#227C6CFF");
            t.SegmentCheckedBorder = S("#557C6CFF");
            t.SegmentCheckedText = S("#D6D0FF");
            t.SwitchTrackOff = S("#24FFFFFF");
            t.SwitchKnobOff = S("#A9B2C2");
            t.FooterSeparator = S("#3E4758");
            t.VersionText = S("#5F697B");
            return t;
        }

        public static ThemePalette Light()
        {
            var t = new ThemePalette();
            t.Id = "light";
            t.DisplayName = "浅色";
            t.Description = "清爽白底，白天更清晰";
            t.IsLight = true;
            t.CardBg = G("#FFFFFF", "#F6F8FD");
            t.WindowBorder = S("#DCE3EF");
            t.ShadowColor = C("#263349");
            t.ShadowOpacity = 0.20;
            t.TextPrimary = S("#182033");
            t.TextBody = S("#263043");
            t.TextSecondary = S("#47536A");
            t.TextMuted = S("#68748A");
            t.TextFaint = S("#8A95A8");
            t.TextOnAccent = S("#FFFFFF");
            t.Icon = S("#47536A");
            t.ControlBg = S("#F3F5FA");
            t.ControlBorder = S("#E1E6EF");
            t.ControlHover = S("#E9EDF6");
            t.ControlPressed = S("#DDE3EF");
            t.ChipText = S("#47536A");
            t.Link = S("#526078");
            t.LinkHover = S("#1F2A3D");
            t.SearchText = S("#182033");
            t.SearchCaret = S("#6A5AE0");
            t.Selection = S("#557C6CFF");
            t.ListHover = S("#F1F4FA");
            t.ListSelected = S("#EFEAFF");
            t.ListSelectedBorder = S("#C9BFFF");
            t.ScrollThumb = S("#C4CCDA");
            t.MenuBg = S("#FFFFFF");
            t.MenuBorder = S("#E2E7F0");
            t.MenuText = S("#222B3D");
            t.MenuShortcut = S("#8A94A8");
            t.MenuItemHover = S("#F2F4FA");
            t.MenuSeparator = S("#E8ECF3");
            t.ToastBg = S("#202634");
            t.ToastBorder = S("#4D7C6CFF");
            t.ToastText = S("#F5F7FB");
            t.ToastIcon = S("#B9AEFF");
            t.PausedBg = S("#FFF4E0");
            t.PausedBorder = S("#F0D090");
            t.PausedText = S("#A66A00");
            t.SearchBoxBg = S("#F4F6FA");
            t.SearchBoxBorder = S("#E4E8F0");
            t.HotkeyBadgeBg = S("#EDF0F6");
            t.HotkeyBadgeText = S("#5B667A");
            t.SearchIcon = S("#5B667A");
            t.SearchPlaceholder = S("#7D8798");
            t.EmptyIconBg = S("#F2F4FA");
            t.EmptyIconBorder = S("#E6EAF2");
            t.EmptyIcon = S("#6A5AE0");
            t.KeycapBg = S("#F0F3F8");
            t.KeycapText = S("#5B667A");
            t.FooterText = S("#5B667A");
            t.CountText = S("#68748A");
            t.Pin = S("#6B5BFF");
            t.Accent = S("#6A5AE0");
            t.AccentSoft = S("#7C6CFF");
            t.AccentPale = S("#6A5AE0");
            t.DangerText = S("#D64557");
            t.DangerBg = S("#14D64557");
            t.DangerBorder = S("#40D64557");
            t.DangerHover = S("#26D64557");
            t.PanelBg = S("#F8FAFD");
            t.PanelBorder = S("#E8ECF3");
            t.Divider = S("#E8ECF3");
            t.HotkeyNormalBorder = S("#DDE3EE");
            t.HotkeyActiveBorder = S("#6A5AE0");
            t.HotkeyNormalBg = S("#F4F6FA");
            t.HotkeyActiveBg = S("#EDE9FF");
            t.SegmentBg = S("#F4F6FA");
            t.SegmentBorder = S("#E4E8F0");
            t.SegmentHover = S("#EDF0F6");
            t.SegmentCheckedBg = S("#E9E4FF");
            t.SegmentCheckedBorder = S("#C9BFFF");
            t.SegmentCheckedText = S("#4B3FD0");
            t.SwitchTrackOff = S("#DDE3EE");
            t.SwitchKnobOff = S("#FFFFFF");
            t.FooterSeparator = S("#C9D0DC");
            t.VersionText = S("#68748A");
            return t;
        }

        public static ThemePalette Sepia()
        {
            var t = new ThemePalette();
            t.Id = "sepia";
            t.DisplayName = "护眼";
            t.Description = "暖纸色，长时间使用更舒适";
            t.IsLight = true;
            t.CardBg = G("#FFFCF3", "#F7EFDD");
            t.WindowBorder = S("#E4D7C0");
            t.ShadowColor = C("#3B3125");
            t.ShadowOpacity = 0.18;
            t.TextPrimary = S("#3B3125");
            t.TextBody = S("#463A2C");
            t.TextSecondary = S("#756554");
            t.TextMuted = S("#9A8770");
            t.TextFaint = S("#B3A18B");
            t.TextOnAccent = S("#FFFFFF");
            t.Icon = S("#6E5E4B");
            t.ControlBg = S("#F6EDDC");
            t.ControlBorder = S("#E4D5BC");
            t.ControlHover = S("#EFE2CC");
            t.ControlPressed = S("#E7D8BE");
            t.ChipText = S("#756554");
            t.Link = S("#7A674F");
            t.LinkHover = S("#3B3125");
            t.SearchText = S("#3B3125");
            t.SearchCaret = S("#6B5ACD");
            t.Selection = S("#556B5ACD");
            t.ListHover = S("#F8F0DF");
            t.ListSelected = S("#F0E3C9");
            t.ListSelectedBorder = S("#DCC9A8");
            t.ScrollThumb = S("#D3C2A8");
            t.MenuBg = S("#FFFCF4");
            t.MenuBorder = S("#E4D7C0");
            t.MenuText = S("#3B3125");
            t.MenuShortcut = S("#9A8770");
            t.MenuItemHover = S("#F6EDDC");
            t.MenuSeparator = S("#EADDC7");
            t.ToastBg = S("#3B3125");
            t.ToastBorder = S("#667C6CFF");
            t.ToastText = S("#FFF8EA");
            t.ToastIcon = S("#C9BFFF");
            t.PausedBg = S("#FFF1D2");
            t.PausedBorder = S("#E8C987");
            t.PausedText = S("#9A6200");
            t.SearchBoxBg = S("#F6EDDC");
            t.SearchBoxBorder = S("#E7D9C2");
            t.HotkeyBadgeBg = S("#F0E4CF");
            t.HotkeyBadgeText = S("#8B7962");
            t.SearchIcon = S("#8B7962");
            t.SearchPlaceholder = S("#A8967D");
            t.EmptyIconBg = S("#F5EBD8");
            t.EmptyIconBorder = S("#E8DAC2");
            t.EmptyIcon = S("#6B5ACD");
            t.KeycapBg = S("#F2E7D2");
            t.KeycapText = S("#8B7962");
            t.FooterText = S("#8B7962");
            t.CountText = S("#9A8770");
            t.Pin = S("#6B5ACD");
            t.Accent = S("#6B5ACD");
            t.AccentSoft = S("#7C6CFF");
            t.AccentPale = S("#6B5ACD");
            t.DangerText = S("#C24A4A");
            t.DangerBg = S("#14C24A4A");
            t.DangerBorder = S("#40C24A4A");
            t.DangerHover = S("#26C24A4A");
            t.PanelBg = S("#FBF4E5");
            t.PanelBorder = S("#EADDC7");
            t.Divider = S("#EADDC7");
            t.HotkeyNormalBorder = S("#E4D5BC");
            t.HotkeyActiveBorder = S("#6B5ACD");
            t.HotkeyNormalBg = S("#F5EBD8");
            t.HotkeyActiveBg = S("#EEE5FA");
            t.SegmentBg = S("#F5EBD8");
            t.SegmentBorder = S("#E7D9C2");
            t.SegmentHover = S("#EFE2CC");
            t.SegmentCheckedBg = S("#EDE4FA");
            t.SegmentCheckedBorder = S("#CBBDF0");
            t.SegmentCheckedText = S("#4B3FA8");
            t.SwitchTrackOff = S("#E4D5BC");
            t.SwitchKnobOff = S("#FFFCF4");
            t.FooterSeparator = S("#C9B79B");
            t.VersionText = S("#9A8770");
            return t;
        }

        public static IList<ThemePalette> All()
        {
            return new List<ThemePalette>
            {
                new ThemePalette { Id = "system", DisplayName = "跟随系统", Description = "跟随 Windows 应用深浅色", IsLight = false },
                Dark(),
                Light(),
                Sepia()
            };
        }

        public static ThemePalette Resolve(string id)
        {
            if (id == "light") return Light();
            if (id == "sepia") return Sepia();
            if (id == "system") return IsSystemLight() ? Light() : Dark();
            return Dark();
        }

        public static bool IsSystemLight()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object v = key.GetValue("AppsUseLightTheme");
                        if (v is int && (int)v != 0) return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }
    }

    /// <summary>Applies a palette to application-wide DynamicResource keys.</summary>
    public static class ThemeManager
    {
        public static ThemePalette Current { get; private set; }
        public static string SelectedId { get; private set; }
        public static event EventHandler Changed;

        public static IList<ThemePalette> Themes
        {
            get { return ThemePalette.All(); }
        }

        public static void Apply(string id)
        {
            if (string.IsNullOrEmpty(id)) id = "dark";
            ThemePalette p = ThemePalette.Resolve(id);
            Current = p;
            SelectedId = id;

            if (Application.Current != null)
            {
                ResourceDictionary r = Application.Current.Resources;
                r["CardBackgroundBrush"] = p.CardBg;
                r["WindowBorderBrush"] = p.WindowBorder;
                r["ShadowColor"] = p.ShadowColor;
                r["ShadowOpacity"] = p.ShadowOpacity;
                r["TextPrimaryBrush"] = p.TextPrimary;
                r["TextBodyBrush"] = p.TextBody;
                r["TextSecondaryBrush"] = p.TextSecondary;
                r["TextMutedBrush"] = p.TextMuted;
                r["TextFaintBrush"] = p.TextFaint;
                r["TextOnAccentBrush"] = p.TextOnAccent;
                r["IconBrush"] = p.Icon;
                r["ControlBgBrush"] = p.ControlBg;
                r["ControlBorderBrush"] = p.ControlBorder;
                r["ControlHoverBrush"] = p.ControlHover;
                r["ControlPressedBrush"] = p.ControlPressed;
                r["ChipTextBrush"] = p.ChipText;
                r["LinkBrush"] = p.Link;
                r["LinkHoverBrush"] = p.LinkHover;
                r["SearchTextBrush"] = p.SearchText;
                r["SearchCaretBrush"] = p.SearchCaret;
                r["SelectionBrush"] = p.Selection;
                r["ListHoverBrush"] = p.ListHover;
                r["ListSelectedBrush"] = p.ListSelected;
                r["ListSelectedBorderBrush"] = p.ListSelectedBorder;
                r["ScrollThumbBrush"] = p.ScrollThumb;
                r["MenuBgBrush"] = p.MenuBg;
                r["MenuBorderBrush"] = p.MenuBorder;
                r["MenuTextBrush"] = p.MenuText;
                r["MenuShortcutBrush"] = p.MenuShortcut;
                r["MenuItemHoverBrush"] = p.MenuItemHover;
                r["MenuSeparatorBrush"] = p.MenuSeparator;
                r["ToastBgBrush"] = p.ToastBg;
                r["ToastBorderBrush"] = p.ToastBorder;
                r["ToastTextBrush"] = p.ToastText;
                r["ToastIconBrush"] = p.ToastIcon;
                r["PausedBgBrush"] = p.PausedBg;
                r["PausedBorderBrush"] = p.PausedBorder;
                r["PausedTextBrush"] = p.PausedText;
                r["SearchBoxBgBrush"] = p.SearchBoxBg;
                r["SearchBoxBorderBrush"] = p.SearchBoxBorder;
                r["HotkeyBadgeBgBrush"] = p.HotkeyBadgeBg;
                r["HotkeyBadgeTextBrush"] = p.HotkeyBadgeText;
                r["SearchIconBrush"] = p.SearchIcon;
                r["SearchPlaceholderBrush"] = p.SearchPlaceholder;
                r["EmptyIconBgBrush"] = p.EmptyIconBg;
                r["EmptyIconBorderBrush"] = p.EmptyIconBorder;
                r["EmptyIconBrush"] = p.EmptyIcon;
                r["KeycapBgBrush"] = p.KeycapBg;
                r["KeycapTextBrush"] = p.KeycapText;
                r["FooterTextBrush"] = p.FooterText;
                r["CountTextBrush"] = p.CountText;
                r["PinBrush"] = p.Pin;
                r["AccentBrush"] = p.Accent;
                r["AccentSoftBrush"] = p.AccentSoft;
                r["AccentPaleBrush"] = p.AccentPale;
                r["DangerTextBrush"] = p.DangerText;
                r["DangerBgBrush"] = p.DangerBg;
                r["DangerBorderBrush"] = p.DangerBorder;
                r["DangerHoverBrush"] = p.DangerHover;
                r["PanelBgBrush"] = p.PanelBg;
                r["PanelBorderBrush"] = p.PanelBorder;
                r["DividerBrush"] = p.Divider;
                r["HotkeyNormalBorderBrush"] = p.HotkeyNormalBorder;
                r["HotkeyActiveBorderBrush"] = p.HotkeyActiveBorder;
                r["HotkeyNormalBgBrush"] = p.HotkeyNormalBg;
                r["HotkeyActiveBgBrush"] = p.HotkeyActiveBg;
                r["SegmentBgBrush"] = p.SegmentBg;
                r["SegmentBorderBrush"] = p.SegmentBorder;
                r["SegmentHoverBrush"] = p.SegmentHover;
                r["SegmentCheckedBgBrush"] = p.SegmentCheckedBg;
                r["SegmentCheckedBorderBrush"] = p.SegmentCheckedBorder;
                r["SegmentCheckedTextBrush"] = p.SegmentCheckedText;
                r["SwitchTrackOffBrush"] = p.SwitchTrackOff;
                r["SwitchKnobOffBrush"] = p.SwitchKnobOff;
                r["FooterSeparatorBrush"] = p.FooterSeparator;
                r["VersionTextBrush"] = p.VersionText;
            }

            if (Changed != null) Changed(null, EventArgs.Empty);
        }
    }
}