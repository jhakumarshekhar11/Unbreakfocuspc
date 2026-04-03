namespace Unbreakfocuspc.Models
{
    public class SubjectCard
    {
        // These are "Properties" in C#. Think of them as variables with built-in getters/setters.
        public string SubjectName { get; set; }
        public int TotalMinutes { get; set; }
        public string TimeRemainingString { get; set; } 

        // A Constructor, exactly like Java
        public SubjectCard(string name, int minutes)
        {
            SubjectName = name;
            TotalMinutes = minutes;
            TimeRemainingString = $"{minutes}:00"; // Formats it like "60:00"
        }
    }
}