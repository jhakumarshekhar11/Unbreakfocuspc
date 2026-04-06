using Microsoft.UI.Xaml.Controls;
using System;
using Unbreakfocuspc.Models;

namespace Unbreakfocuspc.Views
{
    public sealed partial class HomePage : Page
    {
        public UserProfile User => AppState.CurrentUser;

        public string DaysLeftString
        {
            get
            {
                int days = (User.GoalDate - DateTime.Now).Days;
                return days > 0 ? $"{days} DAYS LEFT" : "MISSION IMMINENT";
            }
        }

        public string TotalTimeString
        {
            get
            {
                int hours = User.TotalMinutesFocused / 60;
                int mins = User.TotalMinutesFocused % 60;
                return $"{hours}h {mins}m";
            }
        }

        // Safe string conversions for XAML
        public string CurrentLevelString => User.CurrentLevel.ToString();
        public string LifetimeXPString => User.LifetimeXP.ToString();

        public HomePage()
        {
            this.InitializeComponent();
        }
    }
}