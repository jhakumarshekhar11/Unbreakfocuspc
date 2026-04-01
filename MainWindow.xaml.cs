using System;
using System.Windows;
using System.Windows.Threading;
using System.Linq;

namespace UnbreakfocusPC {
    public partial class MainWindow : Window {
        private UserData _user;
        private DispatcherTimer _timer;
        private DispatcherTimer _watchdogTimer;
        private int _secondsRemaining;
        private bool _isStrictMode = true;
        private string[] _distractions = { "chrome", "discord", "steam", "brave", "msedge" };

        public MainWindow() {
            InitializeComponent();
            _user = Persistence.Load() ?? CreateNewUser();
            UpdateUI();
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            
            _watchdogTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _watchdogTimer.Tick += Watchdog_Tick;
        }

        private UserData CreateNewUser() {
            var newUser = new UserData { UniqueId = "@UF-" + new Random().Next(1000, 9999), UserName = "Operator", XP = 0, Streak = 0 };
            newUser.Subjects.Add(new Subject { Name = "Deep Work", GoalMins = 25 });
            Persistence.Save(newUser);
            return newUser;
        }

        private void Timer_Tick(object? sender, EventArgs e) {
            if (_secondsRemaining > 0) {
                _secondsRemaining--;
                _user.XP += 0.0033; // ~0.2 XP per minute
                TxtTimer.Text = TimeSpan.FromSeconds(_secondsRemaining).ToString(@"mm\:ss");
            } else {
                EndSession(true);
            }
        }

        private void Watchdog_Tick(object? sender, EventArgs e) {
            string active = Watchdog.GetActiveProcessName();
            if (_distractions.Contains(active)) {
                BlockerOverlay.Visibility = Visibility.Visible;
                if (_isStrictMode) ApplyPenalty();
            } else {
                BlockerOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyPenalty() {
            _user.XP = Math.Max(0, _user.XP - 150);
            _user.Streak = 0;
            EndSession(false);
            MessageBox.Show("PENALTY APPLIED: -150 XP & STREAK RESET.");
        }

        private void EndSession(bool success) {
            _timer.Stop();
            _watchdogTimer.Stop();
            EngineView.Visibility = Visibility.Collapsed;
            if (success) _user.Streak++;
            Persistence.Save(_user);
            UpdateUI();
        }

        private void UpdateUI() {
            TxtUser.Text = _user.UniqueId;
            TxtRank.Text = $"Rank: {(_user.XP < 125 ? "Novice" : "Focus Elite")} | Level {_user.GetLevel()}";
            XPBar.Value = _user.XP % 100; // Simplified scaling for UI
            TxtStreak.Text = $"Current Streak: {_user.Streak} Days";
            SubjectList.ItemsSource = _user.Subjects.ToList();
        }

        private void StartFocus_Click(object? sender, RoutedEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is Subject sub) {
                _secondsRemaining = sub.GoalMins * 60;
                EngineView.Visibility = Visibility.Visible;
                _timer.Start();
                _watchdogTimer.Start();
            }
        }

        private void Nav_Hub(object? sender, RoutedEventArgs e) => ShowView(HubView);
        private void Nav_Focus(object? sender, RoutedEventArgs e) => ShowView(FocusView);
        private void Nav_Store(object? sender, RoutedEventArgs e) => ShowView(StoreView);

        private void ShowView(UIElement view) {
            HubView.Visibility = FocusView.Visibility = StoreView.Visibility = Visibility.Collapsed;
            view.Visibility = Visibility.Visible;
        }

        private void BuyFreeze_Click(object? sender, RoutedEventArgs e) {
            if (_user.XP >= 1500) {
                _user.XP -= 1500;
                _user.Inventory.Add("STREAK_FREEZE");
                Persistence.Save(_user);
                UpdateUI();
            }
        }
    }
}