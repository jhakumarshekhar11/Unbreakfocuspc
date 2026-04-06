using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Unbreakfocuspc.Models;

namespace Unbreakfocuspc.Views
{
    public sealed partial class TimerPage : Page
    {
        private DispatcherTimer? _uiUpdater;
        
        public bool IsAbortEnabled => AppState.CurrentUser != null && !AppState.CurrentUser.IsStrictMode;
        public Visibility IsPauseVisible => (AppState.CurrentUser != null && AppState.CurrentUser.IsStrictMode) ? Visibility.Collapsed : Visibility.Visible;

        public TimerPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Pull the active subject directly from the Global Brain
            if (AppState.CurrentFocusSubject != null)
            {
                SubjectTitleText.Text = AppState.CurrentFocusSubject.SubjectName.ToUpper();
                if (AppState.CurrentUser != null && AppState.CurrentUser.IsStrictMode) 
                {
                    SubjectTitleText.Text += " [STRICT]";
                }

                // Make sure the Pause button shows the correct word if we return while paused
                PauseButton.Content = AppState.IsPaused ? "RESUME" : "PAUSE";
                
                // Force an immediate UI update
                TimeDisplay.Text = AppState.CurrentFocusSubject.TimeRemainingString;

                StartUIUpdater();
            }
        }

        private void StartUIUpdater()
        {
            _uiUpdater = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _uiUpdater.Tick += (s, e) => 
            {
                if (AppState.CurrentFocusSubject == null || AppState.IsPaused) return;

                // PRECISION MATH using Global Variables
                var elapsed = DateTime.Now - AppState.SessionStartTime - AppState.TotalPausedTime;
                int remaining = AppState.SecondsAtStart - (int)elapsed.TotalSeconds;

                if (remaining <= 0)
                {
                    AppState.CurrentFocusSubject.TotalSecondsLeft = 0;
                    _uiUpdater.Stop();
                    SubjectTitleText.Text = "SESSION COMPLETE";
                    TimeDisplay.Text = "00:00";
                    TimeDisplay.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                    
                    // CLEAR THE GLOBAL SESSION AND SAVE
                    AppState.CurrentFocusSubject = null;
                    DataManager.SaveProfile(AppState.CurrentUser); 
                }
                else
                {
                    AppState.CurrentFocusSubject.TotalSecondsLeft = remaining;
                    TimeDisplay.Text = AppState.CurrentFocusSubject.TimeRemainingString;
                }
            };
            _uiUpdater.Start();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsPaused)
            {
                AppState.IsPaused = false;
                AppState.TotalPausedTime += (DateTime.Now - AppState.PauseStartTime);
                PauseButton.Content = "PAUSE";
            }
            else
            {
                AppState.IsPaused = true;
                AppState.PauseStartTime = DateTime.Now;
                PauseButton.Content = "RESUME";
            }
        }

        private async void AbortSession_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.CurrentFocusSubject == null) return;

            var dialog = new ContentDialog
            {
                Title = "Abort Session",
                Content = "Are you sure you want to abort? Time will reset.",
                PrimaryButtonText = "Abort",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _uiUpdater?.Stop();
                AppState.CurrentFocusSubject.TotalSecondsLeft = AppState.SecondsAtStart; // Reset time
                
                // CLEAR THE GLOBAL SESSION
                AppState.CurrentFocusSubject = null;
                DataManager.SaveProfile(AppState.CurrentUser);
                
                if (this.Frame.CanGoBack) this.Frame.GoBack();
            }
        }

        // STOP MEMORY LEAKS: Stop the timer when the page is destroyed
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _uiUpdater?.Stop();
        }
    }
}