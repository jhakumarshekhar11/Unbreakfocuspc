using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unbreakfocuspc
{
    public sealed partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private FocusEngine _engine;
        private Subject _currentSubject;
        private int _sessionSeconds = 0;
        private Microsoft.UI.Windowing.AppWindow _appWindow;

        // 🟢 FIX 1: Initialize collections here so they are NEVER null during XAML parse
        public ObservableCollection<Subject> SubjectsData { get; set; } = new ObservableCollection<Subject>();
        public ObservableCollection<CalendarDay> CalendarDays { get; set; } = new ObservableCollection<CalendarDay>();

        private string _editingSubjectId = null;

        public MainWindow()
        {
            // 🟢 FIX 2: Ensure data is loaded BEFORE the UI starts binding
            DataManager.Instance.LoadUser();

            this.InitializeComponent();
            
            // Native Window Logic...
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            _appWindow.Resize(new Windows.Graphics.SizeInt32(1000, 800));

            // System backdrop via runtime guard (avoids XAML parse crash on unsupported platforms)
            if (Microsoft.UI.Xaml.Media.MicaBackdrop.IsSupported())
            {
                var mica = new Microsoft.UI.Xaml.Media.MicaBackdrop
                {
                    Kind = Microsoft.UI.Xaml.Media.MicaBackdropKind.Base
                };
                this.SystemBackdrop = mica;
            }
            else if (Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop.IsSupported())
            {
                var acrylic = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop
                {
                    TintColor = Microsoft.UI.Colors.Black,
                    TintOpacity = 0.86f,
                    LuminousOpacity = 0.6f
                };
                this.SystemBackdrop = acrylic;
            }

            // Setup Engine
            _engine = new FocusEngine();
            _engine.OnDistractionDetected += Engine_DistractionDetected;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            CheckFirstRun();
            UpdateUI();
            GenerateCalendar();
        }

        // ==========================================
        // UI & NAVIGATION LOGIC
        // ==========================================

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

        private void MainNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                HubView.Visibility = Visibility.Collapsed;
                FocusView.Visibility = Visibility.Collapsed;
                SettingsView.Visibility = Visibility.Visible;
                return;
            }

            var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
            HubView.Visibility = tag == "Hub" ? Visibility.Visible : Visibility.Collapsed;
            FocusView.Visibility = tag == "Focus" ? Visibility.Visible : Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;
        }

        private void UpdateUI()
        {
            var user = DataManager.Instance.CurrentUser;

            // Sync Subject List
            SubjectsData.Clear();
            foreach (var sub in user.Subjects) SubjectsData.Add(sub);

            // Hub Text
            UserNameTxt.Text = user.UserName.ToUpper();
            XpTxt.Text = $"LEVEL {user.Level} • {user.Xp} XP";
            LevelProgress.Value = user.LevelProgress * 100;
            StreakTxt.Text = $"{user.Streak} DAYS";

            int totalMins = user.DailyGlobalSeconds / 60;
            TodayFocusTxt.Text = $"{totalMins} MINS";
            StrictModeToggle.IsOn = user.IsStrictMode;

            // Target Countdown
            if (user.TargetDate.HasValue)
            {
                int daysLeft = (user.TargetDate.Value.Date - DateTime.Now.Date).Days;
                TargetCountdownTxt.Text = daysLeft >= 0 ? $"{daysLeft} DAYS TO TARGET" : "TARGET REACHED";
                TargetCountdownTxt.Foreground = daysLeft < 30 ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            }
        }

        // ==========================================
        // ONBOARDING
        // ==========================================

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
            user.TargetDate = TargetDateEntry.Date.DateTime;
            
            DataManager.Instance.SaveUser();
            OnboardingOverlay.Visibility = Visibility.Collapsed;
            UpdateUI();
        }

        // ==========================================
        // SUBJECT MANAGEMENT
        // ==========================================

        private void OpenSubjectEditor_Click(object sender, RoutedEventArgs e)
        {
            _editingSubjectId = null;
            SubjectEditorTitle.Text = "NEW SUBJECT";
            SubjectNameEntry.Text = "";
            SubjectGoalEntry.Text = "60";
            SubjectEditorOverlay.Visibility = Visibility.Visible;
        }

        private void EditSubject_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            var subject = DataManager.Instance.CurrentUser.Subjects.FirstOrDefault(s => s.Id == id);
            
            if (subject != null)
            {
                _editingSubjectId = id;
                SubjectEditorTitle.Text = "EDIT SUBJECT";
                SubjectNameEntry.Text = subject.Name;
                SubjectGoalEntry.Text = subject.GoalMins.ToString();
                SubjectEditorOverlay.Visibility = Visibility.Visible;
            }
        }

        private void CloseSubjectEditor_Click(object sender, RoutedEventArgs e) => SubjectEditorOverlay.Visibility = Visibility.Collapsed;

        private void SaveSubject_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SubjectNameEntry.Text)) return;
            
            var user = DataManager.Instance.CurrentUser;
            int goal = int.TryParse(SubjectGoalEntry.Text, out int g) ? g : 60;

            if (_editingSubjectId != null)
            {
                var existing = user.Subjects.First(s => s.Id == _editingSubjectId);
                existing.Name = SubjectNameEntry.Text;
                existing.GoalMins = goal;
            }
            else
            {
                user.Subjects.Add(new Subject { Name = SubjectNameEntry.Text, GoalMins = goal });
            }

            DataManager.Instance.SaveUser();
            SubjectEditorOverlay.Visibility = Visibility.Collapsed;
            UpdateUI();
        }

        // ==========================================
        // MISSION LOGBOOK (CALENDAR)
        // ==========================================

        private void GenerateCalendar()
        {
            CalendarDays.Clear();
            var user = DataManager.Instance.CurrentUser;
            DateTime now = DateTime.Now;
            int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            int dailyGoal = user.StreakThresholdSeconds / 60;

            for (int i = 1; i <= daysInMonth; i++)
            {
                DateTime dayDate = new DateTime(now.Year, now.Month, i);
                string key = dayDate.ToString("yyyy-MM-dd");
                
                string hexColor = "#1A1A1A"; // Default Gray
                
                if (dayDate.Date < now.Date)
                {
                    if (user.History.TryGetValue(key, out int minsDone))
                    {
                        if (minsDone >= dailyGoal) hexColor = "#FBBF24"; // Amber (Success)
                        else if (minsDone > 0) hexColor = "#38BDF8";     // Sky (Partial)
                        else hexColor = "#EF4444";                       // Red (Failed)
                    }
                }
                else if (dayDate.Date == now.Date)
                {
                    int currentMins = user.DailyGlobalSeconds / 60;
                    if (currentMins >= dailyGoal) hexColor = "#FBBF24";
                    else if (currentMins > 0) hexColor = "#38BDF8";
                }

                CalendarDays.Add(new CalendarDay 
                { 
                    Day = i, 
                    HexColor = hexColor, 
                    IsToday = (dayDate.Date == now.Date) 
                });
            }
        }

        // ==========================================
        // ANALYTICS MODAL
        // ==========================================

        private void ShowStreakAnalytics_Click(object sender, RoutedEventArgs e)
        {
            var user = DataManager.Instance.CurrentUser;
            double multiplier = 1.0 + (Math.Min(user.Streak, 25) * 0.02);
            int bonusPercent = (int)((multiplier - 1.0) * 100);

            AnalyticsDetailsTxt.Text = $"Current Rank: {user.Level}\n" +
                                       $"Lifetime XP: {user.LifetimeXp}\n\n" +
                                       $"STREAK BONUS ACTIVE\n" +
                                       $"Your streak is currently generating a +{bonusPercent}% bonus to all XP earned.";
            
            AnalyticsOverlay.Visibility = Visibility.Visible;
        }

        private void CloseAnalytics_Click(object sender, RoutedEventArgs e) => AnalyticsOverlay.Visibility = Visibility.Collapsed;

        // ==========================================
        // FOCUS ENGINE (TIMER)
        // ==========================================

        private void LaunchEngine_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            _currentSubject = DataManager.Instance.CurrentUser.Subjects.First(s => s.Id == id);
            
            EngineSubjectTxt.Text = _currentSubject.Name.ToUpper();
            UpdateTimerText();
            
            SubjectDashboard.Visibility = Visibility.Collapsed;
            ActiveEngineView.Visibility = Visibility.Visible;
        }

        private void UpdateTimerText()
        {
            if (_currentSubject == null) return;
            
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
            if (_timer.IsEnabled)
            {
                if (DataManager.Instance.CurrentUser.IsStrictMode) return; // Cannot pause in strict mode
                _timer.Stop();
                _engine.StopShield();
            }
            else
            {
                _timer.Start();
                _engine.StartShield();
                DataManager.Instance.CurrentUser.IsStrictMode = true;
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            var user = DataManager.Instance.CurrentUser;
            _sessionSeconds++;
            user.DailyGlobalSeconds++;
            _currentSubject.TimeDone++;

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
            UpdateTimerText();
        }

        private void AbortTimer_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _engine.StopShield();

            var user = DataManager.Instance.CurrentUser;
            if (user.IsStrictMode)
            {
                int penalty = Math.Min(1000, (int)(user.Xp * 0.25));
                user.Xp -= penalty;
                user.Streak = 0;
            }

            _sessionSeconds = 0;
            DataManager.Instance.SaveUser();
            
            // Switch back to Subject Dashboard
            ActiveEngineView.Visibility = Visibility.Collapsed;
            SubjectDashboard.Visibility = Visibility.Visible;
            
            UpdateUI();
            GenerateCalendar(); // Refresh calendar colors
        }

        // ==========================================
        // SETTINGS LOGIC
        // ==========================================

        private void StrictMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (StrictModeToggle != null && StrictModeToggle.IsOn)
            {
                DataManager.Instance.CurrentUser.IsStrictMode = true;
                DataManager.Instance.SaveUser();
            }
        }
        
        // ==========================================
        // BLOCKER LOGIC
        // ==========================================

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

        private void DismissBlocker_Click(object sender, RoutedEventArgs e) => OverlayBlocker.Visibility = Visibility.Collapsed;
    }
}