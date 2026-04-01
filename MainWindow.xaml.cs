using System;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using Wpf.Ui.Controls;
using System.Windows.Controls;

namespace UnbreakfocusPC {
    public partial class MainWindow : FluentWindow {
        private UserData _user;
        private DispatcherTimer _timer;
        private DispatcherTimer _watchdogTimer;
        private int _totalSeconds;
        private int _secondsRemaining;
        private bool _isStrictMode = true;
        private string[] _distractions = { "chrome", "discord", "steam", "brave", "msedge" };

        public MainWindow() {
            InitializeComponent();
            // Reverted to local Load() to match your current Core.cs
            _user = Persistence.Load() ?? new UserData();
            
            if (!string.IsNullOrEmpty(_user.UniqueId)) {
                CompleteAuthentication();
            }
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _watchdogTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _watchdogTimer.Tick += Watchdog_Tick;
        }

        /* --- AUTH LOGIC (LOCAL ONLY) --- */
        private void CompleteAuthentication() {
            AuthView.Visibility = Visibility.Collapsed;
            MainContainer.Visibility = Visibility.Visible;
            UpdateUI();
        }

        private void ClearPlaceholder(object? sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.TextBox tb && 
               (tb.Text == "ENTER 4-DIGIT PIN" || tb.Text == "PIN" || tb.Text == "Subject Name" || tb.Text == "Mins")) {
                tb.Text = string.Empty;
            }
        }

        private void ClearPlaceholder(object? sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.TextBox tb && 
               (tb.Text == "ENTER 4-DIGIT PIN" || tb.Text == "PIN" || tb.Text == "Subject Name" || 
                tb.Text == "Mins" || tb.Text == "CUSTOM_ID" || tb.Text == "ENTER OPERATOR NAME" || 
                tb.Text == "CUSTOM GOAL NAME")) {
                tb.Text = string.Empty;
            }
        }

        private void CmbGoal_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (CustomGoalPanel == null) return;
            // Index 6 is "Custom Goal..."
            CustomGoalPanel.Visibility = CmbGoal.SelectedIndex == 6 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CreateProfile_Click(object? sender, RoutedEventArgs e) {
            string idSuffix = TxtNewId.Text.Trim();
            string operatorName = TxtNewName.Text.Trim();

            // Strict Validation
            if (string.IsNullOrWhiteSpace(idSuffix) || idSuffix == "CUSTOM_ID") { ShowAuthError("INVALID ID."); return; }
            if (string.IsNullOrWhiteSpace(operatorName) || operatorName == "ENTER OPERATOR NAME") { ShowAuthError("INVALID NAME."); return; }
            if (TxtNewPin.Text.Length != 4 || !int.TryParse(TxtNewPin.Text, out _)) { ShowAuthError("INVALID PIN FORMAT."); return; }
            if (CmbGoal.SelectedIndex == -1) { ShowAuthError("SELECT A TARGET GOAL."); return; }

            string targetGoal = "";
            DateTime targetDate = DateTime.MinValue;

            // Handle Matrix vs Custom
            if (CmbGoal.SelectedIndex == 6) {
                if (string.IsNullOrWhiteSpace(TxtCustomGoal.Text) || TxtCustomGoal.Text == "CUSTOM GOAL NAME") { ShowAuthError("ENTER CUSTOM GOAL."); return; }
                if (!DpCustomDate.SelectedDate.HasValue) { ShowAuthError("SELECT CUSTOM DATE."); return; }
                
                targetGoal = TxtCustomGoal.Text.Trim();
                targetDate = DpCustomDate.SelectedDate.Value;
            } else {
                var selected = (System.Windows.Controls.ComboBoxItem)CmbGoal.SelectedItem;
                targetGoal = selected.Content.ToString()!.Split(" (")[0]; // Extracts name before the parenthesis
                
                // Hardcoded matrix mapping based on your requirements
                targetDate = targetGoal switch {
                    "ICSE" => new DateTime(2027, 2, 17),
                    "CBSE 10th" => new DateTime(2027, 2, 15),
                    "ISC" => new DateTime(2027, 2, 10),
                    "CBSE 12th" => new DateTime(2027, 2, 15),
                    "JEE Mains" => new DateTime(2027, 1, 28),
                    "JEE Advance" => new DateTime(2026, 5, 17),
                    _ => DateTime.Now
                };
            }

            _user = new UserData { 
                UniqueId = "@UF-" + idSuffix, 
                Pin = TxtNewPin.Text, 
                UserName = operatorName, 
                TargetGoal = targetGoal,
                TargetDate = targetDate,
                XP = 0, 
                Streak = 0 
            };
            
            _user.Subjects.Add(new Subject { Name = " Deep Work", GoalMins = 25 });
            Persistence.Save(_user);
            CompleteAuthentication();
        }

        private void ShowAuthError(string message) { TxtAuthError.Text = message; TxtAuthError.Visibility = Visibility.Visible; }

        /* --- FOCUS CRUD LOGIC --- */
        private void AddSub_Click(object? sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(TxtSubName.Text) || !int.TryParse(TxtSubMins.Text, out int mins) || mins <= 0) {
                System.Windows.MessageBox.Show("Invalid Subject Data."); return;
            }
            _user.Subjects.Add(new Subject { Name = " " + TxtSubName.Text.Trim(), GoalMins = mins });
            Persistence.Save(_user);
            TxtSubName.Text = "Subject Name"; TxtSubMins.Text = "Mins";
            UpdateUI();
        }

        private void DeleteSub_Click(object? sender, RoutedEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is Subject sub) {
                _user.Subjects.Remove(sub);
                Persistence.Save(_user);
                UpdateUI();
            }
        }

        private void MoveSubUp_Click(object? sender, RoutedEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is Subject sub) {
                int index = _user.Subjects.IndexOf(sub);
                if (index > 0) {
                    _user.Subjects.RemoveAt(index);
                    _user.Subjects.Insert(index - 1, sub);
                    Persistence.Save(_user);
                    UpdateUI();
                }
            }
        }

        private void MoveSubDown_Click(object? sender, RoutedEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is Subject sub) {
                int index = _user.Subjects.IndexOf(sub);
                if (index >= 0 && index < _user.Subjects.Count - 1) {
                    _user.Subjects.RemoveAt(index);
                    _user.Subjects.Insert(index + 1, sub);
                    Persistence.Save(_user);
                    UpdateUI();
                }
            }
        }

        /* --- TIMER ENGINE LOGIC --- */
        private void Timer_Tick(object? sender, EventArgs e) {
            if (_secondsRemaining > 0) {
                _secondsRemaining--;
                _user.XP += 0.0033;
                TxtTimer.Text = TimeSpan.FromSeconds(_secondsRemaining).ToString(@"mm\:ss");
                
                double percentage = ((double)_secondsRemaining / _totalSeconds) * 100;
                TimerRing.Progress = percentage;
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
            System.Windows.MessageBox.Show("PENALTY APPLIED: -150 XP & STREAK RESET.");
        }

        private void EndSession(bool success) {
            _timer.Stop();
            _watchdogTimer.Stop();
            EngineView.Visibility = Visibility.Collapsed;
            if (success) _user.Streak++;
            Persistence.Save(_user);
            UpdateUI();
        }

        private void StartFocus_Click(object? sender, RoutedEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is Subject sub) {
                _totalSeconds = sub.GoalMins * 60;
                _secondsRemaining = _totalSeconds;
                TimerRing.Progress = 100;
                EngineView.Visibility = Visibility.Visible;
                _timer.Start();
                _watchdogTimer.Start();
            }
        }

        /* --- NAVIGATION & UI UPDATES --- */
        private void UpdateUI() {
            if (_user == null) return;
            TxtUser.Text = _user.UniqueId;
            TxtRank.Text = $"Rank: {(_user.XP < 125 ? "Novice" : "Focus Elite")} | Level {_user.GetLevel()}";
            XPBar.Value = _user.XP % 100;
            TxtStreak.Text = $"Current Streak: {_user.Streak} Days";
            SubjectList.ItemsSource = null; 
            SubjectList.ItemsSource = _user.Subjects;
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