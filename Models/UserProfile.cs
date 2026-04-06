using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization; // <-- Required for JsonInclude

namespace Unbreakfocuspc.Models
{
    public class UserProfile
    {
        public string Name { get; set; } = string.Empty;
        public string TargetGoal { get; set; } = string.Empty;
        public DateTime GoalDate { get; set; } = DateTime.Now;

        public bool IsStrictMode { get; set; } = false;
        public DateTime LastStrictModeDate { get; set; } = DateTime.MinValue;

        public int TotalMinutesFocused { get; set; } = 0;
        public int LifetimeXP { get; set; } = 0;
        public int CurrentLevel { get; set; } = 1;

        public Dictionary<string, int> SubjectTimeTracker { get; set; } = new Dictionary<string, int>();

        // [JsonInclude] FORCES the serializer to save this list into the file!
        [JsonInclude]
        public ObservableCollection<SubjectCard> Subjects { get; set; } = new ObservableCollection<SubjectCard>();
    }
}