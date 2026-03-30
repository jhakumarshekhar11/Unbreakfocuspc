using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Unbreakfocuspc
{
    public sealed partial class MainWindow : Window
    {
        private DispatcherTimer _sessionTimer;
        private FocusEngine _focusEngine;
        private int _sessionContinuousSeconds = 0;
        private Subject _activeSubject;

        public MainWindow()
        {
            this.InitializeComponent();
            DataManager.Instance.LoadUser();
            
            _focusEngine = new FocusEngine();
            _focusEngine.OnDistractionDetected += FocusEngine_OnDistractionDetected;

            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sessionTimer.Tick += SessionTimer_Tick;

            UpdateUI();
        }

        private void MainNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                HubView.Visibility = item.Tag.ToString() == "Hub" ? Visibility.Visible : Visibility.Collapsed;
                FocusView.Visibility = item.Tag.ToString() == "Focus" ? Visibility.Visible : Visibility.Collapsed;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            var user = DataManager.Instance.CurrentUser;
            UserNameTxt.Text = $"Hi, {user.UserName.ToUpper()}";
            XpTxt.Text = $"LEVEL {user.Level}  •  {user.Xp} XP IN WALLET";
            LevelProgress.Value = user.LevelProgress * 100;
            StreakTxt.Text = $"{user.Streak} DAYS";
            
            int m = user.DailyGlobalSeconds / 60;
            int s = user.DailyGlobalSeconds % 60;
            TodayFocusTxt.Text = $"{m:D2}m {s:D2}s";

            // Setup a default subject if empty
            if (user.Subjects.Count == 0)
            {
                user.Subjects.Add(new Subject { Name = "Deep Work", GoalMins = 60 });
            }
            _activeSubject = user.Subjects[0];
            UpdateTimerDisplay();
        }

        private void StartTimer_Click(object sender, RoutedEventArgs e)
        {
            _sessionTimer.Start();
            _focusEngine.StartShield();
            DataManager.Instance.CurrentUser.IsStrictMode = true; // Engage lock
        }

        private void AbortTimer_Click(object sender, RoutedEventArgs e)
        {
            _sessionTimer.Stop();
            _focusEngine.StopShield();
            
            var user = DataManager.Instance.CurrentUser;
            if (user.IsStrictMode)
            {
                // Strict Mode Penalty Applied
                int penalty = Math.Min(1000, (int)(user.Xp * 0.25));
                user.Xp -= penalty;
                user.Streak = 0;
            }

            _sessionContinuousSeconds = 0;
            DataManager.Instance.SaveUser();
            UpdateUI();
        }

        private void SessionTimer_Tick(object sender, object e)
        {
            var user = DataManager.Instance.CurrentUser;
            
            _sessionContinuousSeconds++;
            user.DailyGlobalSeconds++;
            _activeSubject.TimeDone++;

            // EXACT MATH TRANSLATION FROM DART
            double baseXpPerSecond = 0.2 / 60.0;
            int currentMinutes = _sessionContinuousSeconds / 60;
            
            double flowMultiplier = 1.0;
            if (currentMinutes >= 120) flowMultiplier = 1.5;
            else if (currentMinutes >= 60) flowMultiplier = 1.25;

            double streakMultiplier = 1.0 + (Math.Min(user.Streak, 25) * 0.02);
            bool isOvertime = _activeSubject.TimeDone >= (_activeSubject.GoalMins * 60);
            double overtimeMultiplier = isOvertime ? 1.5 : 1.0;

            double finalXpPerSecond = baseXpPerSecond * flowMultiplier * streakMultiplier * overtimeMultiplier;
            _activeSubject.XpBuffer += finalXpPerSecond;

            if (_activeSubject.XpBuffer >= 1.0)
            {
                int earned = (int)_activeSubject.XpBuffer;
                user.Xp += earned;
                user.LifetimeXp += earned;
                _activeSubject.XpBuffer -= earned;
            }

            if (_sessionContinuousSeconds % 30 == 0) DataManager.Instance.SaveUser();
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            int goalSecs = _activeSubject.GoalMins * 60;
            int remaining = Math.Max(0, goalSecs - _activeSubject.TimeDone);
            int overtime = Math.Max(0, _activeSubject.TimeDone - goalSecs);

            int displaySecs = remaining > 0 ? remaining : overtime;
            
            int h = displaySecs / 3600;
            int m = (displaySecs % 3600) / 60;
            int s = displaySecs % 60;

            string prefix = remaining == 0 ? "+" : "";
            TimerDisplay.Text = $"{prefix}{h:D2}:{m:D2}:{s:D2}";
            TimerDisplay.Foreground = remaining == 0 ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.MediumSpringGreen) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private void FocusEngine_OnDistractionDetected(object sender, string appName)
        {
            // Must dispatch back to UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                DistractedAppTxt.Text = $"RESTRICTED PROCESS: {appName}";
                OverlayBlocker.Visibility = Visibility.Visible;
            });
        }

        private void DismissBlocker_Click(object sender, RoutedEventArgs e)
        {
            OverlayBlocker.Visibility = Visibility.Collapsed;
        }
    }
}