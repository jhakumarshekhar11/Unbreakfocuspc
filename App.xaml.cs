using System.Windows;
using System.IO;
using Google.Cloud.Firestore;

namespace UnbreakfocusPC
{
    public partial class App : Application
    {
        public static FirestoreDb? Database { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Path to your Firebase credentials - Must be in the root folder
            string authPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "google-services.json");
            
            if (File.Exists(authPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", authPath);
                // Replace 'your-project-id' with your actual Firebase Project ID
                // Database = FirestoreDb.Create("your-project-id"); 
            }
        }
    }
}