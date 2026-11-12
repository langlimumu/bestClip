using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace BestClip
{
    /// <summary>Owns the main window loaded from loose XAML and all of its interaction logic.</summary>
    public class MainWindowController
    {
        private readonly BestClipApp _app;
        private readonly Window _window;
        private readonly TextBox _search;
        private readonly ListBox _list;
        private readonly TextBlock _searchPlaceholder;
        private readonly TextBlock _emptyTitle;
        private readonly TextBlock _emptyHint;
        private readonly TextBlock _count;
        private readonly TextBlock _toastText;
        private readonly TextBlock _hotkeyBadge;
        private readonly Button _clearSearch;
        private readonly Button _clearHistory;
        private readonly Border _toast;
        private readonly Border _pausedBadge;
        private readonly StackPanel _emptyState;
        private readonly Border _card;
        private readonly ScaleTransform _cardScale;
        private readonly TranslateTransform _cardTranslate;
        private readonly Border _chipAll;
        private readonly Border _chipText;
        private readonly Border _chipImage;
        private readonly Border _chipFiles;
        private readonly Border _chipPinned;
        private readonly Popup _previewPopup;
        private readonly Border _previewCard;
        private readonly TextBlock _previewKind;
        private readonly TextBlock _previewMeta;
        private readonly TextBlock _previewText;
        private readonly ScrollViewer _previewScroll;
        private readonly Image _previewImage;
        private readonly ScrollViewer _previewFilesScroll;
        private readonly StackPanel _previewFiles;
        private readonly DispatcherTimer _previewTimer;
        private ClipItem _previewItem;
        private ListBoxItem _previewContainer;
        private readonly DispatcherTimer _toastTimer;
        private readonly DispatcherTimer _clearTimer;
        private readonly ContextMenu _itemMenu;
        private readonly Style _menuItemStyle;
        private MenuItem _menuOpenLink;
        private MenuItem _menuSaveImage;

        private List<ClipItem> _visibleItems = new List<ClipItem>();
        private string _filter = "all";
        private bool _clearPending;

        public MainWindowController(BestClipApp app)
        {
            _app = app;
            _window = XamlLoader.LoadWindow("MainWindow.xaml");
            XamlLoader.ApplyWindowIcon(_window);

            _search = (TextBox)_window.FindName("SearchBox");
            _list = (ListBox)_window.FindName("ClipList");
            _searchPlaceholder = (TextBlock)_window.FindName("SearchPlaceholder");
            _emptyTitle = (TextBlock)_window.FindName("EmptyTitle");
            _emptyHint = (TextBlock)_window.FindName("EmptyHint");
            _count = (TextBlock)_window.FindName("CountText");
            _toastText = (TextBlock)_window.FindName("ToastText");
            _hotkeyBadge = (TextBlock)_window.FindName("HotkeyBadge");
            _clearSearch = (Button)_window.FindName("ClearSearchButton");
            _clearHistory = (Button)_window.FindName("ClearHistoryButton");
            _toast = (Border)_window.FindName("Toast");
            _pausedBadge = (Border)_window.FindName("PausedBadge");
            _emptyState = (StackPanel)_window.FindName("EmptyState");
            _card = (Border)_window.FindName("Card");
            _cardScale = (ScaleTransform)_window.FindName("CardScale");
            _cardTranslate = (TranslateTransform)_window.FindName("CardTranslate");
            _chipAll = (Border)_window.FindName("ChipAll");
            _chipText = (Border)_window.FindName("ChipText");
            _chipImage = (Border)_window.FindName("ChipImage");
            _chipFiles = (Border)_window.FindName("ChipFiles");
            _chipPinned = (Border)_window.FindName("ChipPinned");
            _previewPopup = (Popup)_window.FindName("PreviewPopup");
            _previewCard = (Border)_window.FindName("PreviewCard");
            _previewKind = (TextBlock)_window.FindName("PreviewKind");
            _previewMeta = (TextBlock)_window.FindName("PreviewMeta");
            _previewText = (TextBlock)_window.FindName("PreviewText");
            _previewScroll = (ScrollViewer)_window.FindName("PreviewScroll");
            _previewImage = (Image)_window.FindName("PreviewImage");
            _previewFilesScroll = (ScrollViewer)_window.FindName("PreviewFilesScroll");
            _previewFiles = (StackPanel)_window.FindName("PreviewFiles");

            _previewTimer = new DispatcherTimer();
            _previewTimer.Interval = TimeSpan.FromMilliseconds(450);
            _previewTimer.Tick += PreviewTimer_Tick;

            _toastTimer = new DispatcherTimer();
            _toastTimer.Interval = TimeSpan.FromMilliseconds(1700);
            _toastTimer.Tick += ToastTimer_Tick;

            _clearTimer = new DispatcherTimer();
            _clearTimer.Interval = TimeSpan.FromSeconds(3.5);
            _clearTimer.Tick += ClearTimer_Tick;

            _menuItemStyle = (Style)_window.FindResource("DarkMenuItem");
            _itemMenu = BuildItemMenu();

            WireEvents();
            ApplyFilter();
            UpdateChrome();
        }

        public Window Window { get { return _window; } }
        public bool IsVisible { get { return _window.IsVisible; } }

        private void WireEvents()
        {
            var header = (Grid)_window.FindName("HeaderArea");
            header.MouseLeftButtonDown += Header_MouseLeftButtonDown;

            ((Button)_window.FindName("SettingsButton")).Click += delegate { _app.OpenSettings(); };
            ((Button)_window.FindName("CloseButton")).Click += delegate { _app.HideMainWithHint(); };

            _search.TextChanged += Search_TextChanged;
            _clearSearch.Click += delegate
            {
                _search.Text = "";
                _search.Focus();
            };
            _clearHistory.Click += ClearHistory_Click;

            _chipAll.MouseLeftButtonUp += delegate { SetFilter("all"); };
            _chipText.MouseLeftButtonUp += delegate { SetFilter("text"); };
            _chipImage.MouseLeftButtonUp += delegate { SetFilter("image"); };
            _chipFiles.MouseLeftButtonUp += delegate { SetFilter("files"); };
            _chipPinned.MouseLeftButtonUp += delegate { SetFilter("pinned"); };

            _list.MouseLeftButtonUp += List_MouseLeftButtonUp;
            _list.MouseRightButtonUp += List_MouseRightButtonUp;
            _list.PreviewMouseMove += List_MouseMove;
            _list.MouseLeave += List_MouseLeave;
            _list.SelectionChanged += delegate { HidePreview(); };
            _list.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(List_ScrollChanged));
            _window.PreviewKeyDown += Window_PreviewKeyDown;
            _window.Deactivated += Window_Deactivated;
            _window.Closing += Window_Closing;
            _window.PreviewMouseWheel += Window_PreviewMouseWheel;

            _app.Store.Changed += Store_Changed;
        }

        private void Store_Changed(object sender, EventArgs e)
        {
            if (_window.IsVisible) ApplyFilter();
            UpdateChrome();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;
            try { _window.DragMove(); } catch { }
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta == 0) return;
            if (Keyboard.FocusedElement == _search || Keyboard.FocusedElement == _list)
            {
                ScrollList(-e.Delta);
                e.Handled = true;
            }
        }

        private void ScrollList(int delta)
        {
            if (_list.Items.Count == 0) return;
            int index = _list.SelectedIndex;
            if (index < 0) index = 0;
            if (delta < 0)
                index = Math.Max(0, index - 1);
            else
                index = Math.Min(_list.Items.Count - 1, index + 1);
            _list.SelectedIndex = index;
            ScrollToSelection();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
            _searchPlaceholder.Visibility = string.IsNullOrEmpty(_search.Text)
                ? Visibility.Visible : Visibility.Collapsed;
            _clearSearch.Visibility = string.IsNullOrEmpty(_search.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SetFilter(string filter)
        {
            if (_filter == filter) return;
            _filter = filter;
            ApplyFilter();
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            if (_app.Store.Items.Count == 0)
            {
                ShowToast("历史记录已经是空的");
                return;
            }

            if (!_clearPending)
            {
                _clearPending = true;
                _clearHistory.Content = "确认清空？";
                _clearHistory.Foreground = ResourceBrush("DangerTextBrush", Color.FromRgb(0xFF, 0x6B, 0x81));
                _clearTimer.Start();
                ShowToast("再次点击“确认清空？”即可删除全部记录");
                return;
            }

            ResetClearPending();
            _app.Store.Clear();
            _app.SaveSoon();
            ShowToast("历史记录已清空");
            ApplyFilter();
        }

        public void PromptClearHistory()
        {
            if (_app.Store.Items.Count == 0)
            {
                ShowToast("历史记录已经是空的");
                return;
            }
            _clearPending = true;
            _clearHistory.Content = "确认清空？";
            _clearHistory.Foreground = ResourceBrush("DangerTextBrush", Color.FromRgb(0xFF, 0x6B, 0x81));
            _clearTimer.Start();
            ShowToast("再次点击“确认清空？”即可删除全部记录");
        }

        private void ClearTimer_Tick(object sender, EventArgs e)
        {
            ResetClearPending();
        }

        private void ResetClearPending()
        {
            _clearPending = false;
            _clearTimer.Stop();
            _clearHistory.Content = "清空历史";
            _clearHistory.ClearValue(Control.ForegroundProperty);
        }

        private void List_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            var container = _list.ContainerFromElement(e.OriginalSource as DependencyObject) as ListBoxItem;
            if (container == null) return;
            var item = container.DataContext as ClipItem;
            if (item == null) return;
            _list.SelectedItem = item;
            PasteSelected();
        }

        private static void Diag(string message)
        {
            try { System.IO.File.AppendAllText(AppPaths.LogFile, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + "\r\n"); } catch { }
        }

        /// <summary>Test hook: open the preview for the first visible item.</summary>
        public void ShowPreviewForTest()
        {
            if (_visibleItems.Count == 0) return;
            _window.UpdateLayout();
            ClipItem item = _visibleItems[0];
            var container = _list.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            if (container == null)
            {
                _list.ScrollIntoView(item);
                _list.UpdateLayout();
                container = _list.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            }
            if (container == null) return;
            _previewContainer = container;
            _previewItem = item;
            ShowPreview(item);
        }

        private void List_MouseMove(object sender, MouseEventArgs e)
        {
            var container = _list.ContainerFromElement(e.OriginalSource as DependencyObject) as ListBoxItem;
            Diag("move container=" + (container == null ? "null" : "item"));
            if (container == null || container.DataContext == null)
            {
                _previewTimer.Stop();
                _previewItem = null;
                _previewContainer = null;
                HidePreview();
                return;
            }
            if (container == _previewContainer) return;

            HidePreview();
            _previewContainer = container;
            _previewItem = container.DataContext as ClipItem;
            _previewTimer.Stop();
            _previewTimer.Start();
        }

        private void List_MouseLeave(object sender, MouseEventArgs e)
        {
            _previewTimer.Stop();
            _previewItem = null;
            _previewContainer = null;
            HidePreview();
        }

        private void List_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HidePreview();
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            _previewTimer.Stop();
            Diag("tick item=" + (_previewItem == null ? "null" : "set"));
            if (!_window.IsVisible || _previewItem == null || _previewContainer == null) return;
            ShowPreview(_previewItem);
        }

        private void ShowPreview(ClipItem item)
        {
            Diag("show " + item.Kind);
            _previewKind.Text = item.KindLabel + "预览";
            _previewMeta.Text = item.MetaText + " · " + item.SourceLabel;
            _previewText.Text = "";
            _previewImage.Source = null;
            _previewFiles.Children.Clear();
            _previewScroll.Visibility = Visibility.Collapsed;
            _previewImage.Visibility = Visibility.Collapsed;
            _previewFilesScroll.Visibility = Visibility.Collapsed;

            if (item.Kind == ClipKind.Image)
            {
                try
                {
                    string path = AppPaths.ImagePath(item.ImageFile);
                    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                    {
                        _previewText.Text = "图片文件已被清理。";
                        _previewScroll.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.DecodePixelWidth = 1000;
                        bmp.UriSource = new Uri(path, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        _previewImage.Source = bmp;
                        _previewImage.Visibility = Visibility.Visible;
                    }
                }
                catch
                {
                    _previewText.Text = "无法读取图片预览。";
                    _previewScroll.Visibility = Visibility.Visible;
                }
            }
            else if (item.Kind == ClipKind.Files && item.Files != null && item.Files.Length > 0)
            {
                Brush textBrush = ResourceBrush("TextBodyBrush", Color.FromRgb(0xED, 0xF1, 0xF7));
                int shown = 0;
                foreach (string file in item.Files)
                {
                    if (shown >= 30) break;
                    var line = new TextBlock();
                    line.Text = "•  " + file;
                    line.FontSize = 11.5;
                    line.TextWrapping = TextWrapping.Wrap;
                    line.Margin = new Thickness(0, 0, 0, 5);
                    line.Foreground = textBrush;
                    line.ToolTip = file;
                    _previewFiles.Children.Add(line);
                    shown++;
                }
                if (item.Files.Length > shown)
                {
                    var more = new TextBlock();
                    more.Text = "等 " + item.Files.Length + " 个文件";
                    more.FontSize = 11;
                    more.Margin = new Thickness(0, 3, 0, 0);
                    more.Foreground = ResourceBrush("TextMutedBrush", Color.FromRgb(0x7D, 0x87, 0x9C));
                    _previewFiles.Children.Add(more);
                }
                _previewFilesScroll.Visibility = Visibility.Visible;
            }
            else
            {
                string text = item.Text ?? "";
                bool truncated = text.Length > 20000;
                if (truncated) text = text.Substring(0, 20000) + "\n\n…（预览已截断）";
                _previewText.Text = string.IsNullOrEmpty(text) ? "(空文本)" : text;
                _previewScroll.Visibility = Visibility.Visible;
            }

            PositionPreview();
            _previewPopup.IsOpen = true;
        }

        private void PositionPreview()
        {
            try
            {
                Point item = _previewContainer.TranslatePoint(new Point(0, 0), _window);
                double scale = 1.0;
                PresentationSource source = PresentationSource.FromVisual(_window);
                if (source != null && source.CompositionTarget != null)
                    scale = source.CompositionTarget.TransformToDevice.M11;
                if (scale <= 0) scale = 1.0;

                const double popWidth = 520;
                const double popHeight = 440;
                double x = _window.ActualWidth + 10;
                double y = item.Y - 26;

                Point windowScreen = _window.PointToScreen(new Point(0, 0));
                IntPtr handle = new WindowInteropHelper(_window).Handle;
                System.Drawing.Rectangle area = WinForms.Screen.FromHandle(handle).WorkingArea;

                double windowRight = windowScreen.X + (_window.ActualWidth * scale);
                bool fitsRight = windowRight + ((popWidth + 12) * scale) <= area.Right;
                if (!fitsRight)
                {
                    bool fitsLeft = (windowScreen.X - ((popWidth + 12) * scale)) >= area.Left + (8 * scale);
                    x = fitsLeft ? -popWidth - 12 : 8;
                }
                if (y + popHeight > _window.ActualHeight) y = _window.ActualHeight - popHeight - 8;
                if (y < 8) y = 8;

                _previewPopup.PlacementTarget = _window;
                _previewPopup.Placement = PlacementMode.Relative;
                _previewPopup.HorizontalOffset = x;
                _previewPopup.VerticalOffset = y;
            }
            catch
            {
                _previewPopup.PlacementTarget = _previewContainer;
                _previewPopup.Placement = PlacementMode.Right;
                _previewPopup.HorizontalOffset = 10;
                _previewPopup.VerticalOffset = -26;
            }
        }

        private void HidePreview()
        {
            _previewPopup.IsOpen = false;
            _previewImage.Source = null;
            _previewFiles.Children.Clear();
        }

        private void List_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var container = _list.ContainerFromElement(e.OriginalSource as DependencyObject) as ListBoxItem;
            if (container == null) return;
            var item = container.DataContext as ClipItem;
            if (item == null) return;
            _list.SelectedItem = item;
            _menuOpenLink.Visibility = IsUrl(item.Text) ? Visibility.Visible : Visibility.Collapsed;
            _menuSaveImage.Visibility = item.Kind == ClipKind.Image ? Visibility.Visible : Visibility.Collapsed;
            _itemMenu.PlacementTarget = _list;
            _itemMenu.Placement = PlacementMode.MousePoint;
            _itemMenu.IsOpen = true;
            e.Handled = true;
        }

        private static bool IsUrl(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string value = text.Trim();
            if (value.Length > 2048 || value.IndexOfAny(new[] { ' ', '\r', '\n' }) >= 0) return false;
            Uri uri;
            return (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                   && Uri.TryCreate(value, UriKind.Absolute, out uri);
        }

        private void OpenLinkSelected()
        {
            var item = _list.SelectedItem as ClipItem;
            if (item == null || !IsUrl(item.Text)) return;
            try
            {
                string url = item.Text.Trim();
                HidePreview();
                _app.HideMain();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
                ShowToast("无法打开链接");
            }
        }

        private void SaveImageSelected()
        {
            var item = _list.SelectedItem as ClipItem;
            if (item == null || item.Kind != ClipKind.Image || string.IsNullOrEmpty(item.ImageFile)) return;
            string source = AppPaths.ImagePath(item.ImageFile);
            if (!System.IO.File.Exists(source))
            {
                ShowToast("图片文件已被清理");
                return;
            }
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = "保存图片";
            dialog.Filter = "PNG 图片 (*.png)|*.png";
            dialog.FileName = "BestClip-" + item.CreatedUtc.ToLocalTime().ToString("yyyyMMdd-HHmmss") + ".png";
            bool? result = dialog.ShowDialog(_window);
            if (result != true) return;
            try
            {
                System.IO.File.Copy(source, dialog.FileName, true);
                ShowToast("图片已保存");
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
                ShowToast("保存图片失败");
            }
        }

        private ContextMenu BuildItemMenu()
        {
            var menu = new ContextMenu();
            menu.Style = (Style)_window.FindResource("DarkContextMenu");

            var paste = new MenuItem { Header = "粘贴", InputGestureText = "Enter", Style = _menuItemStyle };
            paste.Click += delegate { PasteSelected(); };
            menu.Items.Add(paste);

            _menuOpenLink = new MenuItem { Header = "在浏览器中打开", Style = _menuItemStyle, Visibility = Visibility.Collapsed };
            _menuOpenLink.Click += delegate { OpenLinkSelected(); };
            menu.Items.Add(_menuOpenLink);

            _menuSaveImage = new MenuItem { Header = "图片另存为…", Style = _menuItemStyle, Visibility = Visibility.Collapsed };
            _menuSaveImage.Click += delegate { SaveImageSelected(); };
            menu.Items.Add(_menuSaveImage);

            var pin = new MenuItem { Header = "固定 / 取消固定", InputGestureText = "Ctrl+P", Style = _menuItemStyle };
            pin.Click += delegate { TogglePinSelected(); };
            menu.Items.Add(pin);

            menu.Items.Add(new Separator { Style = (Style)_window.FindResource("DarkSeparator") });

            var delete = new MenuItem { Header = "删除", InputGestureText = "Del", Style = _menuItemStyle };
            delete.Click += delegate { DeleteSelected(); };
            menu.Items.Add(delete);

            menu.Opened += delegate { _app.SuppressHide = true; };
            menu.Closed += delegate { _app.SuppressHide = false; };
            return menu;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_itemMenu.IsOpen)
                    _itemMenu.IsOpen = false;
                else if (!string.IsNullOrEmpty(_search.Text))
                {
                    _search.Text = "";
                    _search.Focus();
                }
                else
                    _app.HideMain();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Home)
            {
                if (_list.Items.Count > 0) { _list.SelectedIndex = 0; ScrollToSelection(); }
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                if (_list.Items.Count > 0) { _list.SelectedIndex = _list.Items.Count - 1; ScrollToSelection(); }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                MoveSelection(-5);
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                MoveSelection(5);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                int index = (int)e.Key - (int)Key.D1;
                if (index >= 0 && index < _visibleItems.Count)
                {
                    _list.SelectedIndex = index;
                    PasteSelected();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                PasteSelected();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelected();
                e.Handled = true;
            }
            else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
            {
                TogglePinSelected();
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _search.Focus();
                _search.SelectAll();
                e.Handled = true;
            }
        }

        private void MoveSelection(int delta)
        {
            if (_list.Items.Count == 0) return;
            int index = _list.SelectedIndex;
            if (index < 0) index = 0;
            index = Math.Max(0, Math.Min(_list.Items.Count - 1, index + delta));
            _list.SelectedIndex = index;
            ScrollToSelection();
        }

        private void ScrollToSelection()
        {
            if (_list.SelectedItem != null)
                _list.ScrollIntoView(_list.SelectedItem);
        }

        private void PasteSelected()
        {
            var item = _list.SelectedItem as ClipItem;
            if (item == null) return;
            _app.PasteItem(item);
        }

        private void TogglePinSelected()
        {
            var item = _list.SelectedItem as ClipItem;
            if (item == null) return;
            _app.Store.TogglePin(item);
            _app.SaveSoon();
            ApplyFilter();
            ShowToast(item.Pinned ? "已固定到顶部" : "已取消固定");
        }

        private void DeleteSelected()
        {
            var item = _list.SelectedItem as ClipItem;
            if (item == null) return;
            int index = _list.SelectedIndex;
            _app.Store.Remove(item);
            _app.SaveSoon();
            ApplyFilter();
            if (_list.Items.Count > 0)
            {
                _list.SelectedIndex = Math.Min(index, _list.Items.Count - 1);
                ScrollToSelection();
            }
            ShowToast("已删除");
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (_app.SuppressHide || _app.SettingsOpen || _app.ShuttingDown) return;
            if (_itemMenu.IsOpen) return;
            _app.HideMain();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_app.ShuttingDown) return;
            e.Cancel = true;
            _app.HideMain();
        }

        private void ApplyFilter()
        {
            string query = (_search.Text ?? "").Trim().ToLowerInvariant();
            IEnumerable<ClipItem> source = _app.Store.Items;

            if (_filter == "pinned")
                source = source.Where(i => i.Pinned);
            else if (_filter == "text")
                source = source.Where(i => i.Kind == ClipKind.Text);
            else if (_filter == "image")
                source = source.Where(i => i.Kind == ClipKind.Image);
            else if (_filter == "files")
                source = source.Where(i => i.Kind == ClipKind.Files);

            if (query.Length > 0)
                source = source.Where(i => i.SearchText.Contains(query));

            ClipItem selected = _list.SelectedItem as ClipItem;
            _visibleItems = source.ToList();
            for (int i = 0; i < _visibleItems.Count; i++)
                _visibleItems[i].Index = i < 9 ? i + 1 : 0;

            _list.ItemsSource = null;
            _list.ItemsSource = _visibleItems;

            if (_visibleItems.Count == 0)
            {
                _emptyState.Visibility = Visibility.Visible;
                if (_app.Store.Items.Count == 0)
                {
                    _emptyTitle.Text = "还没有剪贴板记录";
                    _emptyHint.Text = "复制文字、图片或文件后，会自动出现在这里";
                }
                else
                {
                    _emptyTitle.Text = "没有找到匹配的记录";
                    _emptyHint.Text = "试试其他关键词或筛选条件";
                }
            }
            else
            {
                _emptyState.Visibility = Visibility.Collapsed;
                int index = selected != null ? _visibleItems.IndexOf(selected) : -1;
                if (index < 0) index = 0;
                _list.SelectedIndex = index;
            }

            int total = _app.Store.Items.Count;
            _count.Text = (query.Length > 0 || _filter != "all")
                ? string.Format("{0} / {1} 条", _visibleItems.Count, total)
                : string.Format("共 {0} 条", total);

            UpdateChips();
        }

        private void UpdateChips()
        {
            StyleChip(_chipAll, _filter == "all");
            StyleChip(_chipText, _filter == "text");
            StyleChip(_chipImage, _filter == "image");
            StyleChip(_chipFiles, _filter == "files");
            StyleChip(_chipPinned, _filter == "pinned");
        }

        private static void StyleChip(Border chip, bool selected)
        {
            Brush selectedBg = ResourceBrush("SegmentCheckedBgBrush", Color.FromArgb(0x22, 0x7C, 0x6C, 0xFF));
            Brush selectedBorder = ResourceBrush("SegmentCheckedBorderBrush", Color.FromArgb(0x55, 0x7C, 0x6C, 0xFF));
            Brush selectedText = ResourceBrush("SegmentCheckedTextBrush", Color.FromRgb(0xD6, 0xD0, 0xFF));
            Brush normalText = ResourceBrush("ChipTextBrush", Color.FromRgb(0x81, 0x8C, 0xA1));

            chip.Background = selected ? selectedBg : Brushes.Transparent;
            chip.BorderBrush = selected ? selectedBorder : Brushes.Transparent;

            foreach (TextBlock tb in FindVisualChildren<TextBlock>(chip))
            {
                string family = tb.FontFamily == null ? "" : tb.FontFamily.Source;
                if (family.IndexOf("Segoe Fluent", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                tb.Foreground = selected ? selectedText : normalText;
            }
        }

        private static Brush ResourceBrush(string key, Color fallback)
        {
            Brush brush = Application.Current == null ? null : Application.Current.TryFindResource(key) as Brush;
            return brush ?? new SolidColorBrush(fallback);
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T) yield return (T)child;
                foreach (T nested in FindVisualChildren<T>(child)) yield return nested;
            }
        }

        public void UpdateChrome()
        {
            bool hotkeyOk = string.IsNullOrEmpty(_app.HotkeyError);
            _hotkeyBadge.Text = hotkeyOk ? _app.Settings.HotkeyText : "快捷键未生效";
            _hotkeyBadge.ToolTip = hotkeyOk
                ? null
                : "全局快捷键注册失败：" + _app.HotkeyError + "，请在设置中更换";
            _pausedBadge.Visibility = _app.Clipboard.Enabled ? Visibility.Collapsed : Visibility.Visible;
        }

        public void ShowForCapture()
        {
            _search.Text = "";
            _filter = "all";
            ApplyFilter();
            UpdateChrome();
            PositionNearCursor();

            _toast.Visibility = Visibility.Collapsed;
            _card.Opacity = 1;
            _cardScale.ScaleX = 1;
            _cardScale.ScaleY = 1;
            _cardTranslate.Y = 0;

            _window.Show();
            _window.Activate();
            _window.Topmost = true;

            if (_visibleItems.Count > 0) _list.SelectedIndex = 0;
            ScrollToSelection();

            _search.Focus();
            Keyboard.Focus(_search);

            var duration = TimeSpan.FromMilliseconds(150);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var opacity = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
            var scaleX = new DoubleAnimation(0.965, 1, duration) { EasingFunction = ease };
            var scaleY = new DoubleAnimation(0.965, 1, duration) { EasingFunction = ease };
            var slide = new DoubleAnimation(10, 0, duration) { EasingFunction = ease };
            _card.BeginAnimation(UIElement.OpacityProperty, opacity);
            _cardScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            _cardScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            _cardTranslate.BeginAnimation(TranslateTransform.YProperty, slide);
        }

        public void Hide()
        {
            _previewTimer.Stop();
            _previewItem = null;
            _previewContainer = null;
            HidePreview();
            _toast.Visibility = Visibility.Collapsed;
            _toast.BeginAnimation(UIElement.OpacityProperty, null);
            _toast.Opacity = 0;
            _window.Hide();
        }

        private void PositionNearCursor()
        {
            try
            {
                IntPtr handle = new WindowInteropHelper(_window).EnsureHandle();
                uint dpi = Native.GetDpiForWindow(handle);
                double scale = dpi > 0 ? dpi / 96.0 : 1.0;

                System.Drawing.Point cursor = WinForms.Cursor.Position;
                WinForms.Screen screen = WinForms.Screen.FromPoint(cursor);
                var wa = screen.WorkingArea;

                // Keep the whole window on screen on small or high-DPI displays.
                double maxDipHeight = (wa.Height - 16) / scale;
                _window.Height = Math.Min(680, Math.Max(480, maxDipHeight));

                int width = (int)Math.Ceiling(_window.Width * scale);
                int height = (int)Math.Ceiling(_window.Height * scale);

                int x = cursor.X + 14;
                int y = cursor.Y + 14;
                if (x + width > wa.Right - 8) x = cursor.X - width - 14;
                if (y + height > wa.Bottom - 8) y = cursor.Y - height - 14;

                x = Math.Max(wa.Left + 8, Math.Min(x, wa.Right - width - 8));
                y = Math.Max(wa.Top + 8, Math.Min(y, wa.Bottom - height - 8));

                Native.SetWindowPos(handle, Native.HWND_TOPMOST, x, y, 0, 0,
                    Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
        }

        public void ShowToast(string message)
        {
            _toastTimer.Stop();
            _toastText.Text = message;
            _toast.Visibility = Visibility.Visible;
            _toast.BeginAnimation(UIElement.OpacityProperty, null);
            _toast.Opacity = 1;
            _toastTimer.Start();
        }

        private void ToastTimer_Tick(object sender, EventArgs e)
        {
            _toastTimer.Stop();
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
            fade.Completed += delegate
            {
                _toast.Visibility = Visibility.Collapsed;
                _toast.BeginAnimation(UIElement.OpacityProperty, null);
                _toast.Opacity = 0;
            };
            _toast.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        public void FocusSearch()
        {
            _search.Focus();
            Keyboard.Focus(_search);
        }

        public void Refresh()
        {
            ApplyFilter();
            UpdateChrome();
        }
    }
}
