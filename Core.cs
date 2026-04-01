using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Text;

namespace UnbreakfocusPC {
    public class Subject {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int GoalMins { get; set; }
        public double TimeDone { get; set; }
    }

    public class UserData {
        public string UniqueId { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public double XP { get; set; }
        public int Streak { get; set; }
        public DateTime LastDate { get; set; } = DateTime.Now;
        public int BreachesToday { get; set; } = 0;
        
        // Heatmap tracking: "yyyy-MM-dd" -> true (Met Goal) / false (Failed)
        public Dictionary<string, bool> History { get; set; } = new(); 

        public List<Subject> Subjects { get; set; } = new();
        public List<string> Inventory { get; set; } = new();

        public int GetLevel() => XP < 5 ? 0 : (int)Math.Floor(Math.Log(XP, 5));
    }

    public static class Watchdog {
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public static (string ProcessName, string WindowTitle) GetActiveWindowInfo() {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return (string.Empty, string.Empty);
            
            GetWindowThreadProcessId(hwnd, out uint pid);
            string processName = string.Empty;
            string windowTitle = string.Empty;
            
            try {
                processName = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName.ToLower();
            } catch { }

            StringBuilder sb = new StringBuilder(256);
            if (GetWindowText(hwnd, sb, sb.Capacity) > 0) windowTitle = sb.ToString().ToLower();

            return (processName, windowTitle);
        }
    }

    public static class Persistence {
        private static string Path => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ubf_desk_data.json");
        public static void Save(UserData data) => File.WriteAllText(Path, JsonSerializer.Serialize(data));
        public static UserData? Load() => File.Exists(Path) ? JsonSerializer.Deserialize<UserData>(File.ReadAllText(Path)) : null;
    }
}