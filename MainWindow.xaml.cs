using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Unbreakfocuspc
{
    public sealed partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private FocusEngine _engine;
        private Subject _currentSubject;
        private int _sessionSeconds = 0;
        private Microsoft.UI.Windowing.AppWindow _appWindow;

        public MainWindow()
        {
            this.InitializeComponent();
            
            // Setup Native Window (Size & Icon)
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.GetWindowIdFromWindowHandle(hWnd);
            _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            
            _appWindow.Resize(new Windows.Graphics.SizeInt32(1000, 800));
            _appWindow.SetIcon("Assets/logo.ico");

            // Initialize App Data & Watchdog Engine
            DataManager.Instance.LoadUser();
            _engine = new FocusEngine();
            _engine.OnDistractionDetected += Engine_DistractionDetected;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            CheckFirstRun();
            UpdateUI();
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (_appWindow.Presenter.Kind == Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen)
            {
                _appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
                FullScreenBtn.Content = new SymbolIcon(Symbol.FullScreen) { Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) };
            }
            else
            {
                _appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                FullScreenBtn.Content = new SymbolIcon(Symbol.BackToWindow) { Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) };
            }
        }

        private void CheckFirstRun()
        {
            var user = DataManager.Instance.CurrentUser;
            if (string.IsNullOrEmpty(user.UserName) || user.UserName == "Aspirant")
            {
                OnboardingOverlay.Visibility = Visibility.Visible;
            }
        }

        private void InitializeProfile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewUserNameEntry.Text)) return;
            
            var user = DataManager.Instance.CurrentUser;
            user.UserName = NewUserNameEntry.Text;
            user.Target = (TargetPicker.SelectedItem as ComboBoxItem)?.Content.ToString();
            
            DataManager.Instance.SaveUser();
            OnboardingOverlay.Visibility = Visibility.Collapsed;
            UpdateUI();
        }

        private void MainNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                HubView.Visibility = Visibility.Collapsed;
                FocusView.Visibility = Visibility.Collapsed;
                SettingsView.Visibility = Visibility.Visible;
                return;
            }

            var item = args.SelectedItem as NavigationViewItem;
            string tag = item?.Tag?.ToString();

            HubView.Visibility = tag == "Hub" ? Visibility.Visible : Visibility.Collapsed;
            FocusView.Visibility = tag == "Focus" ? Visibility.Visible : Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;
        }

        private void UpdateUI()
        {
            var user = DataManager.Instance.CurrentUser;
            if (user.Subjects.Count == 0) user.Subjects.Add(new Subject { Name = "Deep Work" });
            _currentSubject = user.Subjects[0];

            UserNameTxt.Text = user.UserName.ToUpper();
            XpTxt.Text = $"LEVEL {user.Level} • {user.Xp} XP";
            LevelProgress.Value = user.LevelProgress * 100;
            StreakTxt.Text = $"{user.Streak} DAYS";

            int totalMins = user.DailyGlobalSeconds / 60;
            TodayFocusTxt.Text = $"{totalMins} MINS";
            StrictModeToggle.IsOn = user.IsStrictMode;

            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            int goalSecs = _currentSubject.GoalMins * 60;
            int remaining = Math.Max(0, goalSecs - _currentSubject.TimeDone);
            int overtime = Math.Max(0, _currentSubject.TimeDone - goalSecs);

            int display = remaining > 0 ? remaining : overtime;
            string prefix = remaining == 0 ? "+" : "";
            
            TimeSpan ts = TimeSpan.FromSeconds(display);
            TimerDisplay.Text = $"{prefix}{ts.Minutes:D2}:{ts.Seconds:D2}";
            TimerDisplay.Foreground = remaining == 0 ? 
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.SpringGreen) : 
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private void StartTimer_Click(object sender, RoutedEventArgs e)
        {
            _timer.Start();
            _engine.StartShield();
            DataManager.Instance.CurrentUser.IsStrictMode = true;
        }

        private void Timer_Tick(object sender, object e)
        {
            var user = DataManager.Instance.CurrentUser;
            _sessionSeconds++;
            user.DailyGlobalSeconds++;
            _currentSubject.TimeDone++;

            // Flow & Streak XP Mechanics
            double baseXp = 0.2 / 60.0;
            double streakBonus = 1.0 + (Math.Min(user.Streak, 25) * 0.02);
            _currentSubject.XpBuffer += (baseXp * streakBonus);

            if (_currentSubject.XpBuffer >= 1.0)
            {
                int earned = (int)_currentSubject.XpBuffer;
                user.Xp += earned;
                user.LifetimeXp += earned;
                _currentSubject.XpBuffer -= earned;
            }

            if (_sessionSeconds % 30 == 0) DataManager.Instance.SaveUser();
            UpdateUI();
        }

        private void AbortTimer_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _engine.StopShield();

            var user = DataManager.Instance.CurrentUser;
            
            // Evaluate Strict Mode Punishment
            if (user.IsStrictMode)
            {
                int penalty = Math.Min(1000, (int)(user.Xp * 0.25));
                user.Xp -= penalty;
                user.Streak = 0;
            }

            _sessionSeconds = 0;
            DataManager.Instance.SaveUser();
            UpdateUI();
        }

        private void StrictMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (StrictModeToggle.IsOn)
            {
                DataManager.Instance.CurrentUser.IsStrictMode = true;
                DataManager.Instance.SaveUser();
            }
        }

        private async void ShowCredits_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog creditsDialog = new ContentDialog
            {
                Title = "CONSTRUCTORS",
                Content = "DEVELOPER: SHEKHAR KUMAR JHA\n\nBuilt for Academic Dominance.\nVersion 2.5.0 (Windows Native)",
                CloseButtonText = "RESUME MISSION",
                XamlRoot = this.Content.XamlRoot
            };
            await creditsDialog.ShowAsync();
        }

        private void Engine_DistractionDetected(object sender, string app)
        {
            DispatcherQueue.TryEnqueue(() => {
                DistractedAppTxt.Text = $"DETECTION: {app}";
                OverlayBlocker.Visibility = Visibility.Visible;
            });
        }

        private void DismissBlocker_Click(object sender, RoutedEventArgs e)
        {
            OverlayBlocker.Visibility = Visibility.Collapsed;
        }
    }
}