using System;
using System.Collections.ObjectModel;

namespace Unbreakfocuspc.Models
{
    public static class AppState
    {
        public static UserProfile CurrentUser { get; set; } = new UserProfile();
        public static ObservableCollection<SubjectCard> ActiveSubjects => CurrentUser.Subjects;

        // --- LIVE SESSION TRACKING (Survives Tab Switching!) ---
        public static SubjectCard? CurrentFocusSubject { get; set; }
        public static DateTime SessionStartTime { get; set; }
        public static int SecondsAtStart { get; set; }
        public static bool IsPaused { get; set; }
        public static DateTime PauseStartTime { get; set; }
        public static TimeSpan TotalPausedTime { get; set; }
    }
}