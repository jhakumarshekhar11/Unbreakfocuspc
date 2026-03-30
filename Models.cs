using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unbreakfocus.Desktop
{
    public class Subject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("goal_mins")]
        public int GoalMins { get; set; } = 60;
        [JsonPropertyName("time_done")]
        public int TimeDone { get; set; } = 0;
        [JsonPropertyName("color_value")]
        public uint ColorValue { get; set; } = 0xFF00F0FF;
        [JsonPropertyName("xp_buffer")]
        public double XpBuffer { get; set; } = 0.0;
    }

    public class UserData
    {
        public string UniqueId { get; set; } = "@UF-USER";
        public string UserName { get; set; } = "Aspirant";
        public string Target { get; set; } = "Custom";
        public int Xp { get; set; } = 0;
        public int LifetimeXp { get; set; } = 0;
        public int Streak { get; set; } = 0;
        public DateTime LastDate { get; set; } = DateTime.Now;
        public List<Subject> Subjects { get; set; } = new();
        public int DailyGlobalSeconds { get; set; } = 0;
        public bool IsStrictMode { get; set; } = false;

        [JsonIgnore]
        public int Level
        {
            get
            {
                if (LifetimeXp < 5) return 0;
                int currentLevel = 0;
                int threshold = 5;
                while (LifetimeXp >= threshold)
                {
                    currentLevel++;
                    threshold *= 5;
                }
                return currentLevel;
            }
        }

        [JsonIgnore]
        public double LevelProgress
        {
            get
            {
                if (LifetimeXp < 5) return LifetimeXp / 5.0;
                int lowerBound = 5;
                while (LifetimeXp >= lowerBound * 5) lowerBound *= 5;
                int upperBound = lowerBound * 5;
                int xpIntoLevel = LifetimeXp - lowerBound;
                int totalXpForLevel = upperBound - lowerBound;
                return (double)xpIntoLevel / totalXpForLevel;
            }
        }

        [JsonIgnore]
        public int XpToNextLevel
        {
            get
            {
                if (LifetimeXp < 5) return 5 - LifetimeXp;
                int threshold = 5;
                while (LifetimeXp >= threshold) threshold *= 5;
                return threshold - LifetimeXp;
            }
        }

        [JsonIgnore]
        public int StreakThresholdSeconds
        {
            get
            {
                int totalMins = Subjects.Sum(s => s.GoalMins);
                if (totalMins == 0) totalMins = 60;
                return (totalMins * 60) / 2;
            }
        }
    }

    public class DataManager
    {
        public static DataManager Instance { get; } = new DataManager();
        public UserData CurrentUser { get; private set; }

        private readonly string _filePath;

        private DataManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "Unbreakfocus");
            Directory.CreateDirectory(appFolder);
            _filePath = Path.Combine(appFolder, "app_data.json");
        }

        public void LoadUser()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    CurrentUser = JsonSerializer.Deserialize<UserData>(json) ?? CreateBlankUser();
                }
                catch
                {
                    CurrentUser = CreateBlankUser();
                }
            }
            else
            {
                CurrentUser = CreateBlankUser();
            }
            CheckMidnight();
        }

        public void SaveUser()
        {
            if (CurrentUser == null) return;
            CheckMidnight();
            string json = JsonSerializer.Serialize(CurrentUser, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        private void CheckMidnight()
        {
            DateTime now = DateTime.Now;
            if (now.Date > CurrentUser.LastDate.Date)
            {
                int daysDiff = (now.Date - CurrentUser.LastDate.Date).Days;
                int streakGoalMins = CurrentUser.StreakThresholdSeconds / 60;
                int totalMinsYesterday = CurrentUser.DailyGlobalSeconds / 60;
                
                bool metLastDateGoal = totalMinsYesterday >= streakGoalMins;
                int failedDays = (!metLastDateGoal ? 1 : 0) + (daysDiff - 1);

                if (failedDays == 0) CurrentUser.Streak++;
                else CurrentUser.Streak = 0;

                CurrentUser.DailyGlobalSeconds = 0;
                CurrentUser.IsStrictMode = false;
                foreach (var s in CurrentUser.Subjects) s.TimeDone = 0;
                
                CurrentUser.LastDate = now;
            }
        }

        private UserData CreateBlankUser() => new UserData { LastDate = DateTime.Now, Subjects = new List<Subject>() };
    }
}