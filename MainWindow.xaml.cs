using System;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using Wpf.Ui.Controls;
using System.Windows.Media;
using System.ComponentModel;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;

namespace UnbreakfocusPC {
    public partial class MainWindow : FluentWindow {
        private UserData _user;
        private DispatcherTimer _timer;
        private DispatcherTimer _watchdogTimer;
        private int _totalSeconds;
        private int _secondsRemaining;
        private int _autosaveTickCounter = 0;
        private bool _isStrictMode = true;
        private bool _isRestMode = false;
        private bool _isExplicitExit = false;
        
        private BlockerWindow? _activeBlocker;
        private System.Windows.Forms.NotifyIcon _trayIcon;

        private string[] _blockedProcesses = { "discord", "steam", "riotclient" };
        private string[] _blockedTitles = { "youtube", "twitter", "reddit", "netflix", "instagram" };

        public MainWindow() {
            InitializeComponent();
            _user = Persistence.Load() ?? CreateNewUser();
            
            CheckMidnightReset();
            SetupTrayIcon();
            CheckStartupStatus();
            
            if (!string.IsNullOrEmpty(_user.UniqueId)) {
                CompleteAuthentication();
                RequirePinLock(); // Lock the app on launch if profile exists
            }
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _watchdogTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _watchdogTimer.Tick += Watchdog_Tick;
        }

        private UserData CreateNewUser() {
            var newUser = new UserData { UniqueId = "", UserName = "" };
            return newUser;
        }

        // --- AUTH, ONBOARDING & PIN LOCK ---
        private void CompleteAuthentication() {
            if (AuthView != null) AuthView.Visibility = Visibility.Collapsed;
            MainContainer.Visibility = Visibility.Visible;
            UpdateUI();
        }

        private void ClearPlaceholder(object? sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.TextBox tb && 
               (tb.Text == "ENTER 4-DIGIT PIN" || tb.Text == "PIN" || tb.Text == "Subject Name" || 
                tb.Text == "Mins" || tb.Text == "CUSTOM_ID" || tb.Text == "ENTER OPERATOR NAME")) {
                tb.Text = string.Empty;
            }
        }

        private void CreateProfile_Click(object? sender, RoutedEventArgs e) {
            string idSuffix = TxtNewId.Text.Trim();
            string operatorName = TxtNewName.Text.Trim();

            if (string.IsNullOrWhiteSpace(idSuffix) || idSuffix == "CUSTOM_ID") { ShowAuthError("INVALID ID."); return; }
            if (string.IsNullOrWhiteSpace(operatorName) || operatorName == "ENTER OPERATOR NAME") { ShowAuthError("INVALID NAME."); return; }
            if (TxtNewPin.Text.Length != 4 || !int.TryParse(TxtNewPin.Text, out _)) { ShowAuthError("INVALID PIN FORMAT."); return; }

            _user = new UserData { 
                UniqueId = "@UFDESK-" + idSuffix, 
                Pin = TxtNewPin.Text, 
                UserName = operatorName, 
                XP = 0, 
                Streak = 0 
            };
            
            _user.Subjects.Add(new Subject { Name = "Deep Work", GoalMins = 60 });
            Persistence.Save(_user);
            CompleteAuthentication();
        }

        private void RestoreProfile_Click(object? sender, RoutedEventArgs e) {
            ShowAuthError("CLOUD RESTORE IS DISABLED FOR DESKTOP.");
        }

        private void ShowAuthError(string message) {
            if (TxtAuthError != null) {
                TxtAuthError.Text = message;
                TxtAuthError.Visibility = Visibility.Visible;
            }
        }

        private void RequirePinLock() {
            if (!string.IsNullOrEmpty(_user.Pin)) {
                PinLockOverlay.Visibility = Visibility.Visible;
                TxtUnlockPin.Password = "";
                TxtLockError.Visibility = Visibility.Collapsed;
            }
        }

        private void UnlockApp_Click(object sender, RoutedEventArgs e) {
            if (TxtUnlockPin.Password == _user.Pin) {
                PinLockOverlay.Visibility = Visibility.Collapsed;
            } else {
                TxtLockError.Text = "INVALID PIN.";
                TxtLockError.Visibility = Visibility.Visible;
            }
        }

        protected override void OnStateChanged(EventArgs e) {
            base.OnStateChanged(e);
            // Relock the app when brought back from the system tray
            if (this.WindowState == WindowState.Normal && !string.IsNullOrEmpty(_user.Pin)) {
                RequirePinLock();
            }
        }

        // --- DATA SAFETY & OS INTEGRATION ---
        private void CheckMidnightReset() {
            if (_user.LastDate.Date < DateTime.Now.Date) {
                _user.BreachesToday = 0;
                
                if (_user.Subjects.Count > 0) {
                    _user.History[_user.LastDate.ToString("yyyy-MM-dd")] = true; 
                }
                
                _user.LastDate = DateTime.Now;
                Persistence.Save(_user);
            }
        }

        private void SetupTrayIcon() {
            _trayIcon = new System.Windows.Forms.NotifyIcon {
                Icon = new System.Drawing.Icon("Assets/logo.ico"),
                Visible = true,
                Text = "Unbreakfocus PC"
            };
            _trayIcon.DoubleClick += (s, e) => {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Open Hub", null, (s, e) => { this.Show(); this.WindowState = WindowState.Normal; });
            menu.Items.Add("Exit Sequence", null, (s, e) => {
                if (_timer.IsEnabled && _isStrictMode) {
                    System.Windows.MessageBox.Show("STRICT MODE ACTIVE. EXIT DENIED.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _isExplicitExit = true;
                Application.Current.Shutdown();
            });
            _trayIcon.ContextMenuStrip = menu;
        }

        protected override void OnClosing(CancelEventArgs e) {
            if (_isExplicitExit) {
                _trayIcon.Dispose();
                base.OnClosing(e);
                return;
            }

            if (_timer.IsEnabled && _isStrictMode) {
                e.Cancel = true;
                System.Windows.MessageBox.Show("SHIELD ACTIVE. YOU CANNOT CLOSE THE APP.", "Strict Mode", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            e.Cancel = true;
            this.Hide();
            new ToastContentBuilder().AddText("Unbreakfocus is still active in the background.").Show();
        }

        private void CheckStartupStatus() {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;
            ChkStartup.IsChecked = key.GetValue("UnbreakfocusPC") != null;
        }

        private void StartupToggle_Changed(object sender, RoutedEventArgs e) {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;
            if (ChkStartup.IsChecked == true) {
                key.SetValue("UnbreakfocusPC", System.Reflection.Assembly.GetExecutingLibrary().Location);
            } else {
                key.DeleteValue("UnbreakfocusPC", false);
            }
        }

        // --- SETTINGS CRUD ---
        private void SaveSettings_Click(object sender, RoutedEventArgs e) {
            if (TxtEditPin.Password.Length > 0 && TxtEditPin.Password.Length != 4) {
                System.Windows.MessageBox.Show("PIN must be exactly 4 digits.", "Invalid Configuration");
                return;
            }

            _user.UserName = TxtEditName.Text.Trim();
            if (TxtEditPin.Password.Length == 4) _user.Pin = TxtEditPin.Password;
            
            Persistence.Save(_user);
            UpdateUI();
            System.Windows.MessageBox.Show("Configuration updated securely.", "Settings Saved");
        }

        // --- POMODORO ENGINE LOGIC ---
        private void Timer_Tick(object? sender, EventArgs e) {
            if (_secondsRemaining > 0) {
                _secondsRemaining--;
                
                if (!_isRestMode) {
                    _user.XP += 0.0033; // ~0.2 XP per min
                }

                TxtTimer.Text = TimeSpan.FromSeconds(_secondsRemaining).ToString(@"mm\:ss");
                TimerRing.Progress = ((double)_secondsRemaining / _totalSeconds) * 100;

                _autosaveTickCounter++;
                if (_autosaveTickCounter >= 30) {
                    Persistence.Save(_user);
                    _autosaveTickCounter = 0;
                }
            } else {
                if (!_isRestMode) {
                    _isRestMode = true;
                    _totalSeconds = 5 * 60; // 5 Minute Rest
                    _secondsRemaining = _totalSeconds;
                    TimerRing.Progress = 100;
                    TimerRing.Foreground = new SolidColorBrush(Colors.SkyBlue);
                    new ToastContentBuilder().AddText("REST CYCLE ACTIVE").AddText("Take a 5 minute break.").Show();
                } else {
                    EndSession(true);
                }
            }
        }

        private void StartFocus_Click(object? sender, RoutedEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is Subject sub) {
                _totalSeconds = sub.GoalMins * 60;
                _secondsRemaining = _totalSeconds;
                TimerRing.Progress = 100;
                TimerRing.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
                EngineView.Visibility = Visibility.Visible;
                _timer.Start();
                _watchdogTimer.Start();
            }
        }

        private void EndSession(bool success) {
            _timer.Stop();
            _watchdogTimer.Stop();
            _isRestMode = false;
            EngineView.Visibility = Visibility.Collapsed;
            
            if (_activeBlocker != null) {
                _activeBlocker.Close();
                _activeBlocker = null;
            }

            if (success) {
                _user.Streak++;
                System.Media.SystemSounds.Asterisk.Play(); // Auditory success alert
                new ToastContentBuilder().AddText("CYCLE COMPLETE").AddText("Focus session finished successfully.").Show();
            } else {
                System.Media.SystemSounds.Hand.Play(); // Auditory penalty alert
            }

            Persistence.Save(_user);
            UpdateUI();
        }

        // --- ADVANCED WATCHDOG ---
        private void Watchdog_Tick(object? sender, EventArgs e) {
            if (_isRestMode) return; 

            var (process, title) = Watchdog.GetActiveWindowInfo();
            bool isBreach = _blockedProcesses.Contains(process) || _blockedTitles.Any(t => title.Contains(t));

            if (isBreach) {
                if (_activeBlocker == null) {
                    _activeBlocker = new BlockerWindow(_isStrictMode);
                    
                    // Hook into the escape hatch event
                    _activeBlocker.OverrideTriggered += (s, ev) => ApplyPenalty();
                    
                    _activeBlocker.Show();
                }
                if (_isStrictMode) ApplyPenalty();
            } else {
                if (_activeBlocker != null) {
                    _activeBlocker.Close();
                    _activeBlocker = null;
                }
            }
        }

        private void ApplyPenalty() {
            _user.BreachesToday++;
            int penalty = 150 * _user.BreachesToday;
            _user.XP = Math.Max(0, _user.XP - penalty);
            _user.Streak = 0;
            EndSession(false);
            new ToastContentBuilder().AddText("STRICT MODE BREACH").AddText($"Penalty Applied: -{penalty} XP").Show();
        }

        // --- UI & CRUD LOGIC ---
        private void AddSub_Click(object? sender, RoutedEventArgs e) {
            TxtSubMins.BorderBrush = Brushes.Transparent;
            TxtSubName.BorderBrush = Brushes.Transparent;

            if (string.IsNullOrWhiteSpace(TxtSubName.Text)) { TxtSubName.BorderBrush = Brushes.Red; return; }
            if (!int.TryParse(TxtSubMins.Text, out int mins) || mins <= 0) { TxtSubMins.BorderBrush = Brushes.Red; return; }

            _user.Subjects.Add(new Subject { Name = TxtSubName.Text.Trim(), GoalMins = mins });
            Persistence.Save(_user);
            TxtSubName.Text = ""; TxtSubMins.Text = "";
            UpdateUI();
        }

        private void DeleteSub_Click(object? sender, RoutedEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is Subject sub) {
                _user.Subjects.Remove(sub);
                Persistence.Save(_user);
                UpdateUI();
            }
        }

        private void BuyFreeze_Click(object? sender, RoutedEventArgs e) {
            if (_user.XP >= 1500) { 
                _user.XP -= 1500; 
                _user.Inventory.Add("STREAK_FREEZE"); 
                Persistence.Save(_user); 
                UpdateUI(); 
            }
        }

        private void UpdateUI() {
            if (_user == null) return;
            TxtUser.Text = $"{_user.UserName.ToUpper()} [{_user.UniqueId}]";
            TxtRank.Text = $"Rank: Level {_user.GetLevel()}";
            XPBar.Value = _user.XP % 100;
            TxtStreak.Text = $"Current Streak: {_user.Streak} Days";
            
            SubjectList.ItemsSource = null;
            SubjectList.ItemsSource = _user.Subjects;

            DrawHeatmap();
        }

        private void DrawHeatmap() {
            if (HeatmapGrid == null) return;
            HeatmapGrid.Children.Clear();
            for (int i = 29; i >= 0; i--) {
                string dateKey = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
                var block = new System.Windows.Controls.Border {
                    Width = 15, Height = 15, Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(2),
                    Background = Brushes.DarkGray
                };

                if (_user.History.ContainsKey(dateKey)) {
                    block.Background = _user.History[dateKey] ? Brushes.MediumSeaGreen : Brushes.DarkRed;
                }
                HeatmapGrid.Children.Add(block);
            }
        }

        // --- NAVIGATION ---
        private void Nav_Hub(object? sender, RoutedEventArgs e) => ShowView(HubView);
        private void Nav_Focus(object? sender, RoutedEventArgs e) => ShowView(FocusView);
        private void Nav_Store(object? sender, RoutedEventArgs e) => ShowView(StoreView);
        
        private void Nav_Settings(object? sender, RoutedEventArgs e) {
            TxtEditName.Text = _user.UserName;
            TxtEditPin.Password = _user.Pin;
            ShowView(SettingsView);
        }

        private void ShowView(UIElement view) {
            HubView.Visibility = FocusView.Visibility = StoreView.Visibility = SettingsView.Visibility = Visibility.Collapsed;
            view.Visibility = Visibility.Visible;
        }
    }
}