using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unbreakfocuspc.Models;

namespace Unbreakfocuspc.Models
{
    public static class DataManager
    {
        private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unbreakfocuspc");
        private static readonly string ProfileFilePath = Path.Combine(FolderPath, "UserProfile.json");

        // The exact templates from your Flutter app
        private static readonly Dictionary<string, List<SubjectCard>> Templates = new()
        {
            { "Class 10th ICSE", new List<SubjectCard> {
                new("Mathematics", 120), new("History & Civics", 60), new("Geography", 60),
                new("English Literature", 60), new("English Language", 45), new("Hindi", 60)
            }},
            { "Class 10th CBSE", new List<SubjectCard> {
                new("Mathematics", 120), new("Science", 60), new("Social Studies", 60),
                new("English", 60), new("Hindi", 60)
            }},
            { "JEE Mains", new List<SubjectCard> {
                new("Mathematics", 480), new("Physics", 480), new("Chemistry", 480)
            }},
            { "NEET", new List<SubjectCard> {
                new("Biology", 480), new("Physics", 360), new("Chemistry", 360)
            }}
        };

        public static bool IsFirstLaunch() => !File.Exists(ProfileFilePath);

        public static void SaveProfile(UserProfile profile)
        {
            if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
            string jsonString = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProfileFilePath, jsonString);
        }

        public static UserProfile LoadProfile()
        {
            if (IsFirstLaunch()) return new UserProfile();
            string jsonString = File.ReadAllText(ProfileFilePath);
            return JsonSerializer.Deserialize<UserProfile>(jsonString) ?? new UserProfile();
        }

        // PORTED FROM FLUTTER: Automatically creates subject cards
        public static void ProvisionSubjects(string target)
        {
            if (Templates.ContainsKey(target))
            {
                AppState.ActiveSubjects.Clear();
                foreach (var subject in Templates[target])
                {
                    AppState.ActiveSubjects.Add(new SubjectCard(subject.SubjectName, subject.TotalSecondsLeft / 60));
                }
                
                // THE MISSING TRIGGER: Actually save the newly generated cards!
                SaveProfile(AppState.CurrentUser);
            }
        }
    }
}