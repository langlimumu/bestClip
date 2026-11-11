using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace BestClip
{
    /// <summary>A message-only window that receives global hotkeys and clipboard notifications.</summary>
    public class HotkeyWindow : IDisposable
    {
        private const int HotkeyId = 0x4243;
        private readonly HwndSource _source;
        private bool _clipboardListener;
        private bool _hotkeyRegistered;
        private uint _modifiers;
        private uint _key;

        public event EventHandler HotkeyPressed;
        public event EventHandler ClipboardChanged;

        public HotkeyWindow()
        {
            var parameters = new HwndSourceParameters("BestClip.MessageWindow");
            parameters.Width = 0;
            parameters.Height = 0;
            parameters.PositionX = 0;
            parameters.PositionY = 0;
            parameters.WindowStyle = 0;
            parameters.ExtendedWindowStyle = 0;
            parameters.ParentWindow = new IntPtr(-3); // HWND_MESSAGE

            _source = new HwndSource(parameters);
            _source.AddHook(WndProc);
            _clipboardListener = Native.AddClipboardFormatListener(_source.Handle);
        }

        public bool ClipboardListenerAvailable { get { return _clipboardListener; } }
        public IntPtr Handle { get { return _source.Handle; } }

        public bool RegisterHotkey(uint modifiers, uint key, out string error)
        {
            error = null;
            if (_source == null || _source.Handle == IntPtr.Zero)
            {
                error = "消息窗口未就绪";
                return false;
            }

            if (Native.RegisterHotKey(_source.Handle, HotkeyId,
                    modifiers | Native.MOD_NOREPEAT, key))
            {
                _modifiers = modifiers;
                _key = key;
                _hotkeyRegistered = true;
                return true;
            }

            int code = Marshal.GetLastWin32Error();
            if (code == 1409)
                error = "该快捷键已被其他程序占用";
            else
                error = "无法注册快捷键（错误 " + code + "）";
            return false;
        }

        public void UnregisterHotkey()
        {
            if (_hotkeyRegistered && _source != null)
            {
                Native.UnregisterHotKey(_source.Handle, HotkeyId);
                _hotkeyRegistered = false;
            }
        }

        public uint Modifiers { get { return _modifiers; } }
        public uint Key { get { return _key; } }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_CLIPBOARDUPDATE)
            {
                var h = ClipboardChanged;
                if (h != null) h(this, EventArgs.Empty);
                handled = true;
            }
            else if (msg == Native.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
            {
                var h = HotkeyPressed;
                if (h != null) h(this, EventArgs.Empty);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            try
            {
                if (_clipboardListener && _source != null)
                    Native.RemoveClipboardFormatListener(_source.Handle);
            }
            catch
            {
            }
            try
            {
                UnregisterHotkey();
            }
            catch
            {
            }
            try
            {
                if (_source != null)
                {
                    _source.RemoveHook(WndProc);
                    _source.Dispose();
                }
            }
            catch
            {
            }
        }
    }
}
