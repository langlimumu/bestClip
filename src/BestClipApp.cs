using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BestClip
{
    public class BestClipApp : Application
    {
        private AppSettings _settings;
        private HistoryStore _store;
        private ClipboardService _clipboard;
        private HotkeyWindow _hotkeyWindow;
        private TrayIcon _tray;
        private MainWindowController _main;
        private SettingsController _settingsController;
        private DispatcherTimer _saveTimer;
        private IntPtr _previousForeground = IntPtr.Zero;
        private EventWaitHandle _showEvent;
        private Thread _showThread;
        private bool _exiting;
        private bool _closeHintShown;
        private string _hotkeyError;
        private string[] _args;

        public AppSettings Settings { get { return _settings; } }
        public HistoryStore Store { get { return _store; } }
        public ClipboardService Clipboard { get { return _clipboard; } }
        public MainWindowController Main { get { return _main; } }
        public bool SuppressHide { get; set; }
        public bool SettingsOpen { get; set; }
        public bool ShuttingDown { get; set; }
        public string HotkeyError { get { return _hotkeyError; } }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _args = e.Args ?? new string[0];

            try
            {
                AppPaths.EnsureCreated();
                _settings = AppSettings.Load();
                AutoStart.RefreshIfEnabled();
                _store = new HistoryStore(_settings.MaxItems);
                _store.Load();
                _store.Changed += Store_Changed;

                _saveTimer = new DispatcherTimer();
                _saveTimer.Interval = TimeSpan.FromMilliseconds(700);
                _saveTimer.Tick += SaveTimer_Tick;

                _clipboard = new ClipboardService(_store, _settings);

                _hotkeyWindow = new HotkeyWindow();
                _hotkeyWindow.ClipboardChanged += delegate { _clipboard.ScheduleCapture(); };
                _hotkeyWindow.HotkeyPressed += delegate { ToggleMain(); };

                string hotkeyError;
                if (!_hotkeyWindow.RegisterHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey, out hotkeyError))
                {
                    _hotkeyError = hotkeyError;
                    AppPaths.Log(new InvalidOperationException("Hotkey registration failed: " + hotkeyError));
                }

                _main = new MainWindowController(this);
                _tray = new TrayIcon(this);
                _main.Refresh();

                if (!string.IsNullOrEmpty(_hotkeyError))
                    _tray.ShowBalloon("BestClip", "全局快捷键注册失败：" + _hotkeyError + "。请在设置中更换快捷键。");

                DispatcherUnhandledException += App_DispatcherUnhandledException;
                Application.Current.SessionEnding += App_SessionEnding;

                StartShowListener();

                bool demo = HasArg("--demo");
                if (demo)
                {
                    DemoData.Seed(_store);
                }

                bool firstRun = !_settings.FirstRunDone && !demo;
                if (firstRun)
                {
                    _settings.FirstRunDone = true;
                    _settings.Save();
                    _clipboard.CaptureCurrentClipboard();
                    _main.Refresh();
                }

                if (HasArg("--screenshot"))
                {
                    RunScreenshot(ArgValue("--screenshot"), false);
                    return;
                }
                if (HasArg("--settings-shot"))
                {
                    RunScreenshot(ArgValue("--settings-shot"), true);
                    return;
                }

                if (firstRun)
                {
                    ShowMain();
                    _tray.ShowBalloon("BestClip 已就绪", "按 " + _settings.HotkeyText + " 随时呼出剪贴板历史");
                }
                else if (!HasArg("--autostart"))
                {
                    ShowMain();
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
                MessageBox.Show("BestClip 启动失败：\n" + ex.Message, "BestClip",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void StartShowListener()
        {
            try
            {
                _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\BestClip.Show");
                _showThread = new Thread(ShowListenerLoop);
                _showThread.IsBackground = true;
                _showThread.Name = "BestClip.ShowListener";
                _showThread.Start();
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
        }

        private void ShowListenerLoop()
        {
            while (!ShuttingDown)
            {
                try
                {
                    _showEvent.WaitOne();
                    if (ShuttingDown) return;
                    Dispatcher.BeginInvoke(new Action(ShowMain));
                }
                catch
                {
                    return;
                }
            }
        }

        private bool HasArg(string name)
        {
            foreach (string arg in _args)
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private string ArgValue(string name)
        {
            for (int i = 0; i < _args.Length - 1; i++)
            {
                if (string.Equals(_args[i], name, StringComparison.OrdinalIgnoreCase))
                    return _args[i + 1];
            }
            return null;
        }

        private void DebugLog(string text)
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("BESTCLIP_DEBUG"), "1", StringComparison.Ordinal))
                return;
            try
            {
                File.AppendAllText(AppPaths.LogFile,
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + text + Environment.NewLine);
            }
            catch
            {
            }
        }

        public void ShowMain()
        {
            if (ShuttingDown) return;
            if (SettingsOpen)
            {
                if (_settingsController != null) _settingsController.Window.Activate();
                return;
            }
            if (_main == null) return;
            if (_main.IsVisible)
            {
                _main.Window.Activate();
                _main.FocusSearch();
                return;
            }

            _previousForeground = Native.GetForegroundWindow();
            DebugLog("show, previous=" + _previousForeground);
            _main.ShowForCapture();
        }

        public void HideMain()
        {
            if (_main != null) _main.Hide();
        }

        public void HideMainWithHint()
        {
            HideMain();
            if (_closeHintShown || _tray == null) return;
            _closeHintShown = true;
            _tray.ShowBalloon("BestClip 仍在后台运行",
                "按 " + _settings.HotkeyText + " 可随时呼出，右键托盘图标可退出。");
        }

        public void ToggleMain()
        {
            if (_main != null && _main.IsVisible) HideMain();
            else ShowMain();
        }

        public void PasteItem(ClipItem item)
        {
            if (item == null) return;
            if (!_clipboard.WriteToClipboard(item))
            {
                _main.ShowToast("剪贴板正被其他程序占用，请重试");
                return;
            }

            _store.MarkUsed(item);
            SaveSoon();
            HideMain();

            if (!_settings.AutoPaste) return;
            IntPtr target = _previousForeground;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
            {
                RestoreFocusAndPaste(target);
            }));
        }

        private void RestoreFocusAndPaste(IntPtr target)
        {
            try
            {
                if (target == IntPtr.Zero) return;
                DebugLog("paste target=" + target + " fgBefore=" + Native.GetForegroundWindow() +
                    " valid=" + Native.IsWindow(target));
                if (!Native.IsWindow(target)) return;
                if (Native.IsIconic(target)) Native.ShowWindow(target, 9);

                Native.ForceForeground(target);
                DebugLog("after force fg=" + Native.GetForegroundWindow());

                Thread.Sleep(110);
                Native.SendPaste();
                DebugLog("sent paste");
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
        }

        public void OpenSettings()
        {
            if (SettingsOpen || ShuttingDown) return;

            try
            {
                SettingsOpen = true;
                SuppressHide = true;

                var controller = new SettingsController(this);
                _settingsController = controller;
                controller.PositionOverMain(_main == null ? null : _main.Window);
                controller.Window.ShowDialog();

                bool clearRequested = controller.ClearRequested;
                _settingsController = null;
                SettingsOpen = false;
                SuppressHide = false;

                if (clearRequested)
                {
                    ShowMain();
                    if (_main != null) _main.PromptClearHistory();
                }
                else if (_main != null && _main.IsVisible)
                {
                    _main.Window.Activate();
                }
            }
            catch (Exception ex)
            {
                SettingsOpen = false;
                SuppressHide = false;
                AppPaths.Log(ex);
                MessageBox.Show("无法打开设置：\n" + ex.Message, "BestClip",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OnSettingsSaved()
        {
            if (_main != null)
            {
                _main.Refresh();
                _main.ShowToast("设置已保存");
            }
            SaveSoon();
        }

        public bool TryChangeHotkey(uint modifiers, uint key, out string error)
        {
            error = null;
            uint oldModifiers = _settings.HotkeyModifiers;
            uint oldKey = _settings.HotkeyKey;

            _hotkeyWindow.UnregisterHotkey();
            if (_hotkeyWindow.RegisterHotkey(modifiers, key, out error))
            {
                _settings.HotkeyModifiers = modifiers;
                _settings.HotkeyKey = key;
                _hotkeyError = null;
                if (_main != null) _main.UpdateChrome();
                return true;
            }

            string ignored;
            _hotkeyWindow.RegisterHotkey(oldModifiers, oldKey, out ignored);
            return false;
        }

        public void TogglePause()
        {
            _clipboard.Enabled = !_clipboard.Enabled;
            if (_tray != null) _tray.SetPaused(!_clipboard.Enabled);
            if (_main != null)
            {
                _main.Refresh();
                if (_main.IsVisible)
                    _main.ShowToast(_clipboard.Enabled ? "已恢复记录" : "已暂停记录");
            }
            if (_tray != null && (_main == null || !_main.IsVisible))
            {
                _tray.ShowBalloon("BestClip",
                    _clipboard.Enabled ? "已恢复记录剪贴板内容" : "已暂停记录剪贴板内容");
            }
        }

        public void ToggleAutoStartFromTray()
        {
            bool enable = !AutoStart.IsEnabled();
            if (!AutoStart.Set(enable))
            {
                if (_tray != null) _tray.ShowBalloon("BestClip", "无法修改开机启动项，请检查系统权限");
                return;
            }
            if (_tray != null)
                _tray.ShowBalloon("BestClip", enable ? "已开启开机自动启动" : "已关闭开机自动启动");
        }

        public void RequestClearHistoryFromTray()
        {
            ShowMain();
            if (_main != null) _main.PromptClearHistory();
        }

        public void SaveSoon()
        {
            if (_saveTimer == null || ShuttingDown) return;
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void Store_Changed(object sender, EventArgs e)
        {
            SaveSoon();
        }

        private void SaveTimer_Tick(object sender, EventArgs e)
        {
            _saveTimer.Stop();
            if (_store != null) _store.SaveNow();
        }

        public void ExitApp()
        {
            if (_exiting) return;
            _exiting = true;
            ShuttingDown = true;

            try
            {
                if (_saveTimer != null) _saveTimer.Stop();
                if (_store != null) _store.SaveNow();
                if (_settings != null) _settings.Save();
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }

            DisposeShell();
            Shutdown();
        }

        private void DisposeShell()
        {
            try
            {
                if (_hotkeyWindow != null) _hotkeyWindow.Dispose();
            }
            catch
            {
            }
            try
            {
                if (_tray != null) _tray.Dispose();
            }
            catch
            {
            }
            try
            {
                if (_showEvent != null) _showEvent.Close();
            }
            catch
            {
            }
            try
            {
                if (_main != null) _main.Window.Hide();
            }
            catch
            {
            }
        }

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            try
            {
                if (_store != null) _store.SaveNow();
                if (_settings != null) _settings.Save();
            }
            catch
            {
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppPaths.Log(e.Exception);
            e.Handled = true;
            if (_tray != null)
            {
                _tray.ShowBalloon("BestClip 遇到问题", "错误已记录到 " + AppPaths.LogFile);
            }
        }

        private void RunScreenshot(string path, bool settingsShot)
        {
            if (string.IsNullOrEmpty(path))
            {
                ExitApp();
                return;
            }

            Window target;
            if (settingsShot)
            {
                _settingsController = new SettingsController(this);
                target = _settingsController.Window;
                target.Show();
                target.UpdateLayout();
            }
            else
            {
                _main.ShowForCapture();
                target = _main.Window;
                target.UpdateLayout();
            }

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(900);
            timer.Tick += delegate
            {
                timer.Stop();
                try
                {
                    Screenshot.Save(target, path);
                }
                catch (Exception ex)
                {
                    AppPaths.Log(ex);
                }
                ExitApp();
            };
            timer.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (!_exiting)
            {
                try
                {
                    if (_store != null) _store.SaveNow();
                    if (_settings != null) _settings.Save();
                }
                catch
                {
                }
                DisposeShell();
            }
            base.OnExit(e);
        }
    }

    public static class Screenshot
    {
        public static void Save(Window window, string path)
        {
            window.UpdateLayout();
            int width = (int)Math.Ceiling(window.ActualWidth);
            int height = (int)Math.Ceiling(window.ActualHeight);
            if (width <= 0 || height <= 0) throw new InvalidOperationException("Window has no size.");

            var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(path))
            {
                encoder.Save(fs);
            }
        }
    }
}
