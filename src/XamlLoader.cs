using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace BestClip
{
    public static class XamlLoader
    {
        public static Window LoadWindow(string fileName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string resourceName = "BestClip." + fileName;
            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException("Missing embedded XAML: " + resourceName);
                return (Window)XamlReader.Load(stream);
            }
        }

        public static void ApplyWindowIcon(Window window)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream stream = asm.GetManifestResourceStream("BestClip.app.ico"))
                {
                    if (stream == null) return;
                    window.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(
                        stream, System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                }
            }
            catch
            {
            }
        }
    }
}
