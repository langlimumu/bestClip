using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace BestClip
{
    public class TrayIcon : IDisposable
    {
        private readonly BestClipApp _app;
        private readonly NotifyIcon _notify;
        private readonly Icon _icon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _pauseItem;
        private readonly ToolStripMenuItem _autoStartItem;

        public TrayIcon(BestClipApp app)
        {
            _app = app;

            try
            {
                _icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
            }
            if (_icon == null) _icon = SystemIcons.Application;

            _menu = new ContextMenuStrip();
            _menu.ShowImageMargin = false;
            _menu.BackColor = Color.FromArgb(23, 26, 35);
            _menu.ForeColor = Color.FromArgb(230, 234, 242);
            _menu.Font = new Font("Microsoft YaHei UI", 9f);
            _menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            _menu.Padding = new Padding(4);

            var open = CreateItem("打开 BestClip", "");
            open.Font = new Font(_menu.Font, FontStyle.Bold);
            open.Click += delegate { _app.ShowMain(); };
            _menu.Items.Add(open);
            _menu.Items.Add(new ToolStripSeparator());

            _pauseItem = CreateItem("暂停记录", "");
            _pauseItem.Click += delegate { _app.TogglePause(); };
            _menu.Items.Add(_pauseItem);

            var clear = CreateItem("清空历史", "");
            clear.Click += delegate { _app.RequestClearHistoryFromTray(); };
            _menu.Items.Add(clear);

            _menu.Items.Add(new ToolStripSeparator());

            var settings = CreateItem("设置", "");
            settings.Click += delegate { _app.OpenSettings(); };
            _menu.Items.Add(settings);

            _autoStartItem = CreateItem("开机自动启动", "");
            _autoStartItem.Click += delegate { _app.ToggleAutoStartFromTray(); };
            _menu.Items.Add(_autoStartItem);

            _menu.Items.Add(new ToolStripSeparator());

            var exit = CreateItem("退出", "");
            exit.Click += delegate { _app.ExitApp(); };
            _menu.Items.Add(exit);

            _menu.Opening += Menu_Opening;

            _notify = new NotifyIcon();
            _notify.Icon = _icon;
            _notify.Text = "BestClip · 剪贴板历史";
            _notify.ContextMenuStrip = _menu;
            _notify.Visible = true;
            _notify.MouseClick += Notify_MouseClick;
            _notify.DoubleClick += delegate { _app.ShowMain(); };
        }

        private ToolStripMenuItem CreateItem(string text, string shortcut)
        {
            var item = new ToolStripMenuItem(text);
            item.ForeColor = Color.FromArgb(230, 234, 242);
            item.Padding = new Padding(7, 5, 7, 5);
            item.ShortcutKeyDisplayString = shortcut;
            return item;
        }

        private void Menu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _pauseItem.Checked = !_app.Clipboard.Enabled;
            _pauseItem.Text = _app.Clipboard.Enabled ? "暂停记录" : "继续记录";
            _autoStartItem.Checked = AutoStart.IsEnabled();
        }

        private void Notify_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _app.ShowMain();
        }

        public void ShowBalloon(string title, string text)
        {
            try
            {
                _notify.ShowBalloonTip(4200, title, text, ToolTipIcon.Info);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            try
            {
                _notify.Visible = false;
                _notify.Dispose();
            }
            catch
            {
            }
            try
            {
                _menu.Dispose();
            }
            catch
            {
            }
            try
            {
                if (_icon != null && _icon != SystemIcons.Application) _icon.Dispose();
            }
            catch
            {
            }
        }
    }

    internal class DarkColorTable : ProfessionalColorTable
    {
        private static readonly Color Back = Color.FromArgb(23, 26, 35);
        private static readonly Color Hover = Color.FromArgb(38, 42, 56);

        public override Color ToolStripDropDownBackground { get { return Back; } }
        public override Color ImageMarginGradientBegin { get { return Back; } }
        public override Color ImageMarginGradientMiddle { get { return Back; } }
        public override Color ImageMarginGradientEnd { get { return Back; } }
        public override Color MenuItemSelected { get { return Hover; } }
        public override Color MenuItemBorder { get { return Color.FromArgb(60, 124, 108, 255); } }
        public override Color MenuBorder { get { return Color.FromArgb(60, 255, 255, 255); } }
        public override Color SeparatorDark { get { return Color.FromArgb(30, 255, 255, 255); } }
        public override Color SeparatorLight { get { return Color.FromArgb(30, 255, 255, 255); } }
        public override Color MenuItemSelectedGradientBegin { get { return Hover; } }
        public override Color MenuItemSelectedGradientEnd { get { return Hover; } }
    }
}
