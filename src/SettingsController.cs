using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WinForms = System.Windows.Forms;

namespace BestClip
{
    public class SettingsController
    {
        private readonly BestClipApp _app;
        private readonly Window _window;
        private readonly CheckBox _autoPaste;
        private readonly CheckBox _autoStart;
        private readonly CheckBox _ignorePassword;
        private readonly Border _hotkeyBox;
        private readonly TextBlock _hotkeyText;
        private readonly TextBlock _hotkeyHint;
        private readonly TextBlock _hotkeyError;
        private readonly TextBlock _saveError;
        private readonly TextBlock _version;
        private readonly StackPanel _maxItemsPanel;
        private readonly Border _card;

        private uint _candidateModifiers;
        private uint _candidateKey;
        private int _maxItems;
        private bool _recording;

        private static readonly Brush HotkeyNormalBorder = Freeze("#1FFFFFFF");
        private static readonly Brush HotkeyActiveBorder = Freeze("#7C6CFF");
        private static readonly Brush HotkeyNormalBackground = Freeze("#0FFFFFFF");
        private static readonly Brush HotkeyActiveBackground = Freeze("#1F7C6CFF");

        public bool ClearRequested { get; private set; }

        public SettingsController(BestClipApp app)
        {
            _app = app;
            _window = XamlLoader.LoadWindow("SettingsWindow.xaml");
            XamlLoader.ApplyWindowIcon(_window);

            _autoPaste = (CheckBox)_window.FindName("AutoPasteSwitch");
            _autoStart = (CheckBox)_window.FindName("AutoStartSwitch");
            _ignorePassword = (CheckBox)_window.FindName("IgnorePasswordSwitch");
            _hotkeyBox = (Border)_window.FindName("HotkeyBox");
            _hotkeyText = (TextBlock)_window.FindName("HotkeyText");
            _hotkeyHint = (TextBlock)_window.FindName("HotkeyHint");
            _hotkeyError = (TextBlock)_window.FindName("HotkeyError");
            _saveError = (TextBlock)_window.FindName("SaveError");
            _version = (TextBlock)_window.FindName("VersionText");
            _maxItemsPanel = (StackPanel)_window.FindName("MaxItemsPanel");
            _card = (Border)_window.FindName("Card");

            _candidateModifiers = app.Settings.HotkeyModifiers;
            _candidateKey = app.Settings.HotkeyKey;
            _maxItems = app.Settings.MaxItems;

            _autoPaste.IsChecked = app.Settings.AutoPaste;
            _autoStart.IsChecked = AutoStart.IsEnabled();
            _ignorePassword.IsChecked = app.Settings.IgnorePasswordManagers;
            _hotkeyText.Text = app.Settings.HotkeyText;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            _version.Text = "BestClip " + version.Major + "." + version.Minor + "." + version.Build;

            BuildMaxItems();
            WireEvents();
        }

        public Window Window { get { return _window; } }

        private void WireEvents()
        {
            var header = (Grid)_window.FindName("HeaderArea");
            header.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState != MouseButtonState.Pressed) return;
                try { _window.DragMove(); } catch { }
            };

            ((Button)_window.FindName("CloseButton")).Click += delegate { _window.Close(); };
            _hotkeyBox.MouseLeftButtonUp += delegate { BeginRecording(); };
            _window.PreviewKeyDown += Window_PreviewKeyDown;
            ((Button)_window.FindName("SaveButton")).Click += delegate { Save(); };
            ((Button)_window.FindName("ClearAllButton")).Click += delegate
            {
                ClearRequested = true;
                _window.Close();
            };

            var link = (System.Windows.Documents.Hyperlink)_window.FindName("GitHubLink");
            link.RequestNavigate += delegate
            {
                try { Process.Start("https://github.com/langlimumu/bestClip"); }
                catch (Exception ex) { AppPaths.Log(ex); }
            };
        }

        private void BuildMaxItems()
        {
            Style segment = (Style)_window.FindResource("Segment");
            int[] options = { 100, 200, 300, 500, 1000 };
            bool matched = false;

            foreach (int option in options)
            {
                var radio = new RadioButton();
                radio.Content = option.ToString();
                radio.Style = segment;
                radio.GroupName = "MaxItems";
                radio.Tag = option;
                radio.Checked += MaxItem_Checked;
                if (option == _maxItems)
                {
                    radio.IsChecked = true;
                    matched = true;
                }
                _maxItemsPanel.Children.Add(radio);
            }

            if (!matched)
            {
                var radio = new RadioButton();
                radio.Content = _maxItems.ToString();
                radio.Style = segment;
                radio.GroupName = "MaxItems";
                radio.Tag = _maxItems;
                radio.Checked += MaxItem_Checked;
                radio.IsChecked = true;
                _maxItemsPanel.Children.Add(radio);
            }
        }

        private void MaxItem_Checked(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio != null && radio.Tag is int) _maxItems = (int)radio.Tag;
        }

        private void BeginRecording()
        {
            _recording = true;
            _hotkeyBox.BorderBrush = HotkeyActiveBorder;
            _hotkeyBox.Background = HotkeyActiveBackground;
            _hotkeyText.Text = "请按下快捷键…";
            _hotkeyText.Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xE1, 0xFF));
            _hotkeyHint.Text = "按 Esc 取消录制；至少需要 Ctrl、Alt 或 Shift 中的一个";
            _hotkeyError.Visibility = Visibility.Collapsed;
            _window.Activate();
            Keyboard.Focus(_window);
        }

        private void CancelRecording()
        {
            _recording = false;
            ResetHotkeyVisual();
            _hotkeyText.Text = HotkeyUtil.ToText(_candidateModifiers, _candidateKey);
            _hotkeyHint.Text = "点击右侧快捷键可重新录制，按 Esc 取消";
        }

        private void ResetHotkeyVisual()
        {
            _hotkeyBox.BorderBrush = HotkeyNormalBorder;
            _hotkeyBox.Background = HotkeyNormalBackground;
            _hotkeyText.Foreground = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEE));
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_recording) return;

            if (e.Key == Key.Escape)
            {
                CancelRecording();
                e.Handled = true;
                return;
            }

            Key key = e.Key;
            if (key == Key.System) key = e.SystemKey;
            if (key == Key.ImeProcessed) key = e.ImeProcessedKey;
            if (IsModifierKey(key))
            {
                e.Handled = true;
                return;
            }

            ModifierKeys modifiers = Keyboard.Modifiers;
            uint mods = 0;
            if ((modifiers & ModifierKeys.Control) != 0) mods |= Native.MOD_CONTROL;
            if ((modifiers & ModifierKeys.Alt) != 0) mods |= Native.MOD_ALT;
            if ((modifiers & ModifierKeys.Shift) != 0) mods |= Native.MOD_SHIFT;
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) mods |= Native.MOD_WIN;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            if (vk == 0) return;

            if (mods == 0)
            {
                _hotkeyError.Text = "快捷键需要至少一个修饰键（Ctrl / Alt / Shift）";
                _hotkeyError.Visibility = Visibility.Visible;
                e.Handled = true;
                return;
            }

            _candidateModifiers = mods;
            _candidateKey = vk;
            _recording = false;
            ResetHotkeyVisual();
            _hotkeyText.Text = HotkeyUtil.ToText(mods, vk);
            _hotkeyHint.Text = "点击右侧快捷键可重新录制，按 Esc 取消";
            _hotkeyError.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }

        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LWin || key == Key.RWin;
        }

        private void Save()
        {
            if (_recording) CancelRecording();
            if (_candidateModifiers == 0 || _candidateKey == 0)
            {
                ShowHotkeyError("请先设置一个有效的快捷键");
                return;
            }

            bool hotkeyChanged = _candidateModifiers != _app.Settings.HotkeyModifiers ||
                                 _candidateKey != _app.Settings.HotkeyKey;
            if (hotkeyChanged)
            {
                string error;
                if (!_app.TryChangeHotkey(_candidateModifiers, _candidateKey, out error))
                {
                    ShowHotkeyError(error);
                    return;
                }
            }

            bool wantAutoStart = _autoStart.IsChecked == true;
            if (wantAutoStart != AutoStart.IsEnabled())
            {
                if (!AutoStart.Set(wantAutoStart))
                {
                    ShowSaveError("无法修改开机启动项，请检查系统权限");
                    return;
                }
            }

            _app.Settings.AutoPaste = _autoPaste.IsChecked == true;
            _app.Settings.IgnorePasswordManagers = _ignorePassword.IsChecked == true;
            _app.Settings.MaxItems = _maxItems;
            _app.Settings.Save();
            _app.Store.SetMaxItems(_maxItems);
            _app.SaveSoon();
            _app.OnSettingsSaved();
            _window.Close();
        }

        private void ShowHotkeyError(string message)
        {
            _hotkeyError.Text = message;
            _hotkeyError.Visibility = Visibility.Visible;
        }

        private void ShowSaveError(string message)
        {
            _saveError.Text = message;
            _saveError.Visibility = Visibility.Visible;
        }

        public void PositionOverMain(Window main)
        {
            try
            {
                IntPtr handle = new WindowInteropHelper(_window).EnsureHandle();
                uint dpi = Native.GetDpiForWindow(handle);
                double scale = dpi > 0 ? dpi / 96.0 : 1.0;

                int width = (int)Math.Round(_window.Width * scale);
                int height = (int)Math.Round(_window.Height * scale);
                int x = 0;
                int y = 0;
                bool positioned = false;

                if (main != null && main.IsVisible)
                {
                    Native.RECT bounds;
                    IntPtr mainHandle = new WindowInteropHelper(main).Handle;
                    if (Native.GetWindowRect(mainHandle, out bounds))
                    {
                        x = bounds.Left + (bounds.Right - bounds.Left - width) / 2;
                        y = bounds.Top + (bounds.Bottom - bounds.Top - height) / 2;
                        positioned = true;
                    }
                }

                if (!positioned)
                {
                    WinForms.Screen screen = WinForms.Screen.FromPoint(WinForms.Cursor.Position);
                    System.Drawing.Rectangle wa = screen.WorkingArea;
                    x = wa.Left + (wa.Width - width) / 2;
                    y = wa.Top + (wa.Height - height) / 2;
                }

                WinForms.Screen targetScreen = WinForms.Screen.FromPoint(
                    new System.Drawing.Point(x + width / 2, y + height / 2));
                System.Drawing.Rectangle area = targetScreen.WorkingArea;
                x = Math.Max(area.Left + 8, Math.Min(x, area.Right - width - 8));
                y = Math.Max(area.Top + 8, Math.Min(y, area.Bottom - height - 8));

                Native.SetWindowPos(handle, Native.HWND_TOPMOST, x, y, 0, 0,
                    Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
        }

        private static Brush Freeze(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
