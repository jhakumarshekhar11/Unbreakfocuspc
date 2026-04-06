using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using Unbreakfocuspc.Models;

namespace Unbreakfocuspc.Views
{
    public sealed partial class FocusTab : Page
    {
        public ObservableCollection<SubjectCard> ActiveSubjects => AppState.ActiveSubjects;

        public FocusTab()
        {
            this.InitializeComponent();
            
            // Trigger the auto-forward check when the page finishes drawing
            this.Loaded += FocusTab_Loaded;
        }

        private void FocusTab_Loaded(object sender, RoutedEventArgs e)
        {
            // AUTO-FORWARD: If a timer is already running, skip this page and go to the Timer!
            if (AppState.CurrentFocusSubject != null)
            {
                this.Frame.Navigate(typeof(Views.TimerPage));
            }
        }

        private async void AddSubject_Click(object sender, RoutedEventArgs e)
        {
            TextBox nameInput = new TextBox { Header = "Subject Name", PlaceholderText = "e.g. Mathematics", Width = 350 };
            NumberBox minsInput = new NumberBox { Header = "Total Focus Time (Minutes)", Value = 60, Minimum = 1, Maximum = 1440, Width = 350, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };

            StackPanel panel = new StackPanel { Spacing = 20, Margin = new Thickness(0, 15, 0, 0) };
            panel.Children.Add(nameInput);
            panel.Children.Add(minsInput);

            ContentDialog dialog = new ContentDialog
            {
                Title = "ADD NEW SUBJECT",
                Content = panel,
                PrimaryButtonText = "Start Tracking",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 26, 26)),
                RequestedTheme = ElementTheme.Dark,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string newName = string.IsNullOrWhiteSpace(nameInput.Text) ? "Custom Subject" : nameInput.Text;
                int newMinutes = double.IsNaN(minsInput.Value) ? 60 : (int)minsInput.Value;

                AppState.ActiveSubjects.Add(new SubjectCard(newName, newMinutes));
                DataManager.SaveProfile(AppState.CurrentUser);
            }
        }

        private void StartTimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SubjectCard subject)
            {
                // RECORD THE START IN THE GLOBAL BRAIN
                AppState.CurrentFocusSubject = subject;
                AppState.SecondsAtStart = subject.TotalSecondsLeft;
                AppState.SessionStartTime = DateTime.Now;
                AppState.IsPaused = false;
                AppState.TotalPausedTime = TimeSpan.Zero;

                this.Frame.Navigate(typeof(Views.TimerPage));
            }
        }

        private void DeleteSubject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SubjectCard subject)
            {
                AppState.ActiveSubjects.Remove(subject);
                DataManager.SaveProfile(AppState.CurrentUser);
            }
        }
    }
}