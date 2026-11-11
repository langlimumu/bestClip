using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace BestClip
{
    public class HistoryStore
    {
        private readonly List<ClipItem> _items = new List<ClipItem>();

        public event EventHandler Changed;

        public IList<ClipItem> Items { get { return _items; } }
        public int MaxItems { get; set; }

        public HistoryStore(int maxItems)
        {
            MaxItems = maxItems <= 0 ? 300 : maxItems;
        }

        public void Load()
        {
            _items.Clear();
            try
            {
                if (File.Exists(AppPaths.HistoryFile))
                {
                    using (var fs = File.OpenRead(AppPaths.HistoryFile))
                    {
                        var ser = new DataContractJsonSerializer(typeof(List<ClipItem>));
                        var list = ser.ReadObject(fs) as List<ClipItem>;
                        if (list != null)
                        {
                            foreach (ClipItem item in list)
                            {
                                if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                                if (item.Kind == ClipKind.Image)
                                {
                                    string path = AppPaths.ImagePath(item.ImageFile);
                                    if (!File.Exists(path)) continue;
                                    item.LoadThumbnail();
                                }
                                if (item.CreatedUtc == DateTime.MinValue) item.CreatedUtc = DateTime.UtcNow;
                                if (item.LastUsedUtc == DateTime.MinValue) item.LastUsedUtc = item.CreatedUtc;
                                _items.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
                TryBackupBrokenHistory();
            }

            Reorder();
            Trim();
        }

        private static void TryBackupBrokenHistory()
        {
            try
            {
                if (File.Exists(AppPaths.HistoryFile))
                {
                    string backup = Path.Combine(
                        AppPaths.DataDir,
                        "history.broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                    File.Copy(AppPaths.HistoryFile, backup, true);
                }
            }
            catch
            {
            }
        }

        public ClipItem Add(ClipItem item)
        {
            if (item == null) return null;

            ClipItem existing = null;
            if (!string.IsNullOrEmpty(item.Hash))
            {
                existing = _items.FirstOrDefault(i => i.Hash == item.Hash && i.Kind == item.Kind);
            }

            if (existing != null)
            {
                existing.CreatedUtc = item.CreatedUtc;
                existing.LastUsedUtc = item.LastUsedUtc;
                existing.AppName = item.AppName;
                existing.AppTitle = item.AppTitle;
                if (existing.Kind == ClipKind.Image && existing.ImageFile != item.ImageFile)
                {
                    DeleteImageFile(item.ImageFile);
                }
                item = existing;
            }
            else
            {
                _items.Add(item);
            }

            Reorder();
            Trim();
            RaiseChanged();
            return item;
        }

        public void MarkUsed(ClipItem item)
        {
            if (item == null) return;
            item.LastUsedUtc = DateTime.UtcNow;
            Reorder();
            RaiseChanged();
        }

        public void TogglePin(ClipItem item)
        {
            if (item == null) return;
            item.Pinned = !item.Pinned;
            Reorder();
            RaiseChanged();
        }

        public void Remove(ClipItem item)
        {
            if (item == null) return;
            _items.Remove(item);
            if (item.Kind == ClipKind.Image) DeleteImageFile(item.ImageFile);
            RaiseChanged();
        }

        public void Clear()
        {
            _items.Clear();
            try
            {
                if (Directory.Exists(AppPaths.ImagesDir))
                {
                    foreach (string f in Directory.GetFiles(AppPaths.ImagesDir))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
            RaiseChanged();
        }

        public void SetMaxItems(int maxItems)
        {
            MaxItems = maxItems <= 0 ? 300 : maxItems;
            Trim();
            RaiseChanged();
        }

        public int PinnedCount
        {
            get { return _items.Count(i => i.Pinned); }
        }

        private void Reorder()
        {
            var ordered = _items
                .OrderByDescending(i => i.Pinned)
                .ThenByDescending(i => i.LastUsedUtc)
                .ToList();
            _items.Clear();
            _items.AddRange(ordered);
        }

        private void Trim()
        {
            int unpinned = _items.Count(i => !i.Pinned);
            if (unpinned <= MaxItems) return;

            int toRemove = unpinned - MaxItems;
            for (int i = _items.Count - 1; i >= 0 && toRemove > 0; i--)
            {
                if (_items[i].Pinned) continue;
                ClipItem dead = _items[i];
                _items.RemoveAt(i);
                if (dead.Kind == ClipKind.Image) DeleteImageFile(dead.ImageFile);
                toRemove--;
            }
        }

        private void DeleteImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            try
            {
                string path = AppPaths.ImagePath(fileName);
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }

        public void SaveNow()
        {
            try
            {
                AppPaths.EnsureCreated();
                var ser = new DataContractJsonSerializer(typeof(List<ClipItem>));
                using (var ms = new MemoryStream())
                {
                    ser.WriteObject(ms, _items.ToList());
                    AppSettings.AtomicWrite(AppPaths.HistoryFile, ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
        }

        public void RaiseChanged()
        {
            var handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
