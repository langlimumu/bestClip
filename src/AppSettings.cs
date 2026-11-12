using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Win32;

namespace BestClip
{
    [DataContract]
    public class AppSettings
    {
        [DataMember(Order = 1)] public uint HotkeyModifiers { get; set; }
        [DataMember(Order = 2)] public uint HotkeyKey { get; set; }
        [DataMember(Order = 3)] public bool AutoPaste { get; set; }
        [DataMember(Order = 4)] public int MaxItems { get; set; }
        [DataMember(Order = 5)] public bool FirstRunDone { get; set; }
        [DataMember(Order = 6)] public bool IgnorePasswordManagers { get; set; }
        [DataMember(Order = 7)] public string Theme { get; set; }

        public AppSettings()
        {
            HotkeyModifiers = Native.MOD_CONTROL | Native.MOD_SHIFT;
            HotkeyKey = Native.VK_V;
            AutoPaste = true;
            MaxItems = 300;
            IgnorePasswordManagers = true;
            Theme = "system";
        }

        public string HotkeyText
        {
            get { return HotkeyUtil.ToText(HotkeyModifiers, HotkeyKey); }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(AppPaths.SettingsFile))
                {
                    using (var fs = File.OpenRead(AppPaths.SettingsFile))
                    {
                        var ser = new DataContractJsonSerializer(typeof(AppSettings));
                        var s = (AppSettings)ser.ReadObject(fs);
                        if (s != null)
                        {
                            if (s.HotkeyKey == 0)
                            {
                                s.HotkeyModifiers = Native.MOD_CONTROL | Native.MOD_SHIFT;
                                s.HotkeyKey = Native.VK_V;
                            }
                            if (s.MaxItems < 20) s.MaxItems = 300;
                            if (string.IsNullOrEmpty(s.Theme)) s.Theme = "system";
                            return s;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                AppPaths.EnsureCreated();
                var ser = new DataContractJsonSerializer(typeof(AppSettings));
                using (var ms = new MemoryStream())
                {
                    ser.WriteObject(ms, this);
                    AtomicWrite(AppPaths.SettingsFile, ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
        }

        internal static void AtomicWrite(string path, byte[] bytes)
        {
            string tmp = path + ".tmp";
            File.WriteAllBytes(tmp, bytes);
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tmp, path, null);
                }
                catch
                {
                    File.Copy(tmp, path, true);
                    File.Delete(tmp);
                }
            }
            else
            {
                File.Move(tmp, path);
            }
        }
    }

    public static class HotkeyUtil
    {
        public static string ToText(uint modifiers, uint key)
        {
            var sb = new StringBuilder();
            if ((modifiers & Native.MOD_CONTROL) != 0) sb.Append("Ctrl + ");
            if ((modifiers & Native.MOD_ALT) != 0) sb.Append("Alt + ");
            if ((modifiers & Native.MOD_SHIFT) != 0) sb.Append("Shift + ");
            if ((modifiers & Native.MOD_WIN) != 0) sb.Append("Win + ");
            sb.Append(KeyName(key));
            return sb.ToString();
        }

        public static string KeyName(uint vk)
        {
            if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
            if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
            if (vk >= 0x70 && vk <= 0x87) return "F" + (vk - 0x6F);
            switch (vk)
            {
                case 0x08: return "Backspace";
                case 0x09: return "Tab";
                case 0x0D: return "Enter";
                case 0x1B: return "Esc";
                case 0x20: return "Space";
                case 0x21: return "PageUp";
                case 0x22: return "PageDown";
                case 0x23: return "End";
                case 0x24: return "Home";
                case 0x25: return "←";
                case 0x26: return "↑";
                case 0x27: return "→";
                case 0x28: return "↓";
                case 0x2D: return "Insert";
                case 0x2E: return "Delete";
                case 0xBA: return ";";
                case 0xBB: return "=";
                case 0xBC: return ",";
                case 0xBD: return "-";
                case 0xBE: return ".";
                case 0xBF: return "/";
                case 0xC0: return "`";
                case 0xDB: return "[";
                case 0xDC: return "\\";
                case 0xDD: return "]";
                case 0xDE: return "'";
            }
            return "0x" + vk.ToString("X2");
        }
    }

    public static class AutoStart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "BestClip";

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (key == null) return false;
                    return key.GetValue(ValueName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool Set(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (key == null) return false;
                    if (enabled)
                    {
                        string exe = Assembly.GetExecutingAssembly().Location;
                        key.SetValue(ValueName, "\"" + exe + "\" --autostart");
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
                return false;
            }
        }

        /// <summary>Keeps the Run entry pointing at the current exe after the portable folder is moved.</summary>
        public static void RefreshIfEnabled()
        {
            try
            {
                if (!IsEnabled()) return;

                string exe = Assembly.GetExecutingAssembly().Location;
                string expected = "\"" + exe + "\" --autostart";
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return;
                    string current = key.GetValue(ValueName) as string;
                    if (!string.Equals(current, expected, StringComparison.OrdinalIgnoreCase))
                        key.SetValue(ValueName, expected);
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log(ex);
            }
        }
    }
}
