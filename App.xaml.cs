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
            this.InitializeComponent();
            
            // 🟢 THE FIX: Catch any fatal UI crashes and save them to a log file
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true; // Prevent immediate hard-close
            
            // Write the error directly to your Desktop so you can see it
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string logPath = Path.Combine(desktop, "Unbreakfocus_CrashLog.txt");
            
            File.WriteAllText(logPath, "FATAL CRASH: \n" + e.Exception.ToString());
            
            // Exit safely after logging
            Environment.Exit(1);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}