using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Unbreakfocuspc.Models;

namespace Unbreakfocuspc.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserData();
        }

        private void UpdateName_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.CurrentUser != null)
            {
                AppState.CurrentUser.Name = NameInput.Text;
                DataManager.SaveProfile(AppState.CurrentUser);
            }
        }

        private bool _isUpdatingUI = false; // Flag to prevent event loops

        private void LoadUserData()
        {
            var user = AppState.CurrentUser;
            if (user == null) return;

            // 1. SET THE UI-UPDATING FLAG TO TRUE
            _isUpdatingUI = true;

            NameInput.Text = user.Name ?? "";
            TargetDateInput.Date = user.GoalDate;
            StrictModeToggle.IsOn = user.IsStrictMode;
            
            if (!string.IsNullOrEmpty(user.TargetGoal))
            {
                var match = MissionSelector.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == user.TargetGoal);
                
                if (match != null)
                {
                    MissionSelector.SelectedItem = match;
                }
            }

            // 2. RESET THE FLAG AFTER SETTING EVERYTHING
            _isUpdatingUI = false;
        
            if (user.IsStrictMode && user.LastStrictModeDate.Date == DateTime.Today)
            {
                StrictModeToggle.IsEnabled = false; 
            }
            else
            {
                StrictModeToggle.IsEnabled = true;
            }
        }

        private async void MissionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 3. IGNORE THE EVENT IF IT'S FIRED BY LOADUSERDATA
            if (_isUpdatingUI) return;

            if (MissionSelector.SelectedItem is ComboBoxItem selected && AppState.CurrentUser != null)
            {
                string? newTarget = selected.Content?.ToString();
                if (string.IsNullOrEmpty(newTarget)) return;

                AppState.CurrentUser.TargetGoal = newTarget;
                DataManager.SaveProfile(AppState.CurrentUser);

                if (newTarget != "Custom")
                {
                    ContentDialog confirmDialog = new ContentDialog
                    {
                        Title = "GENERATE MISSION SUBJECTS?",
                        Content = $"Would you like to overwrite your current cards with the standard {newTarget} template?",
                        PrimaryButtonText = "Overwrite",
                        CloseButtonText = "Keep Current",
                        XamlRoot = this.XamlRoot
                    };

                    var result = await confirmDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        DataManager.ProvisionSubjects(newTarget);
                    }
                }
            }
        }

        private void TargetDateInput_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (AppState.CurrentUser != null && args.NewDate.HasValue)
            {
                AppState.CurrentUser.GoalDate = args.NewDate.Value.DateTime;
                DataManager.SaveProfile(AppState.CurrentUser);
            }
        }

        private void StrictModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUI) return;
        
            if (AppState.CurrentUser != null)
            {
                AppState.CurrentUser.IsStrictMode = StrictModeToggle.IsOn;
                
                if (StrictModeToggle.IsOn)
                {
                    // Record the date it was turned on
                    AppState.CurrentUser.LastStrictModeDate = DateTime.Today;
                    // Instantly disable the toggle so they can't regret it
                    StrictModeToggle.IsEnabled = false;
                }
                
                DataManager.SaveProfile(AppState.CurrentUser);
            }
        }
    }
}