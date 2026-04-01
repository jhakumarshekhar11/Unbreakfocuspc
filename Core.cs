using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using Google.Cloud.Firestore;
using System.Threading.Tasks;

namespace UnbreakfocusPC {
    public class Subject {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public int GoalMins { get; set; }
        public double TimeDone { get; set; }
        public string Color { get; set; } = "#38BDF8";
    }

    [FirestoreData]
    public class UserData {
        [FirestoreProperty] public string UniqueId { get; set; }
        [FirestoreProperty] public string Pin { get; set; }
        [FirestoreProperty] public string UserName { get; set; }
        [FirestoreProperty] public double XP { get; set; }
        [FirestoreProperty] public int Streak { get; set; }
        public List<Subject> Subjects { get; set; } = new();
        public List<string> Inventory { get; set; } = new();

        public int GetLevel() => XP < 5 ? 0 : (int)Math.Floor(Math.Log(XP, 5));
    }

    public static class Watchdog {
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static string GetActiveProcessName() {
            IntPtr hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out uint pid);
            return System.Diagnostics.Process.GetProcessById((int)pid).ProcessName.ToLower();
        }
    }

    public static class Persistence {
        private static string Path => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ubf_data.json");
        public static void Save(UserData data) => File.WriteAllText(Path, JsonSerializer.Serialize(data));
        public static UserData Load() => File.Exists(Path) ? JsonSerializer.Deserialize<UserData>(File.ReadAllText(Path)) : null;
    }
}