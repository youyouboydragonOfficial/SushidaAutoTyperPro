using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace SushidaAutoTyper
{
    public partial class App : System.Windows.Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                SetProcessDPIAware();
            }
            catch { }

            base.OnStartup(e);
        }
    }
}
