using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization; // <-- Required

namespace Unbreakfocuspc.Models
{
    public class SubjectCard : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _subjectName = string.Empty;
        
        [JsonInclude] // FORCE SAVE
        public string SubjectName
        {
            get => _subjectName;
            set { _subjectName = value; OnPropertyChanged(); }
        }

        private int _totalSecondsLeft;
        
        [JsonInclude] // FORCE SAVE
        public int TotalSecondsLeft
        {
            get => _totalSecondsLeft;
            set
            {
                _totalSecondsLeft = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeRemainingString));
            }
        }

        // Ignore this because it's just calculated on the fly
        [JsonIgnore]
        public string TimeRemainingString
        {
            get
            {
                TimeSpan time = TimeSpan.FromSeconds(TotalSecondsLeft);
                if (time.TotalHours >= 1)
                    return time.ToString(@"hh\:mm\:ss");
                else
                    return time.ToString(@"mm\:ss");
            }
        }

        public SubjectCard() { }

        public SubjectCard(string name, int minutes)
        {
            SubjectName = name;
            TotalSecondsLeft = minutes * 60;
        }
    }
}