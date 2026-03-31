using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace Unbreakfocuspc
{
    public partial class App : Application
    {
        private Window m_window;

        public App()
        {
            // 🟢 FIX: Attach the black box BEFORE the UI initializes
            this.UnhandledException += App_UnhandledException;
            this.InitializeComponent();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true; 
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string logPath = Path.Combine(desktop, "Unbreakfocus_CrashLog.txt");
            File.WriteAllText(logPath, "FATAL CRASH: \n" + e.Exception.ToString());
            Environment.Exit(1);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}