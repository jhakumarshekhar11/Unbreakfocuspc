using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Unbreakfocuspc.Models;

namespace Unbreakfocuspc
{
    public partial class App : Application
    {
        // The '?' fixes warning CS8618 (nullability)
        private Window? m_window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // We create a standard Window instead of 'MainWindow' to ensure it launches
            m_window = new Window();
            m_window.Title = "Unbreakfocus PC";

            Frame rootFrame = new Frame();

            // SMART LAUNCH ROUTING
            if (DataManager.IsFirstLaunch())
            {
                rootFrame.Navigate(typeof(Views.OnboardingPage));
            }
            else
            {
                AppState.CurrentUser = DataManager.LoadProfile();
                rootFrame.Navigate(typeof(Views.ShellPage));
            }

            m_window.Content = rootFrame;
            m_window.Activate();
        }
    }
}