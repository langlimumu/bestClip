using System;
using System.Threading;
using System.Windows;

namespace BestClip
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew;
            using (var mutex = new Mutex(true, @"Local\BestClip.SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    SignalRunningInstance();
                    return;
                }

                var app = new BestClipApp();
                app.Run();
            }
        }

        private static void SignalRunningInstance()
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                try
                {
                    using (EventWaitHandle ev = EventWaitHandle.OpenExisting(@"Local\BestClip.Show"))
                    {
                        ev.Set();
                        return;
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    Thread.Sleep(120);
                }
                catch
                {
                    return;
                }
            }
        }
    }
}
