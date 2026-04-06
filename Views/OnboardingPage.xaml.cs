using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Unbreakfocuspc.Models;

namespace Unbreakfocuspc.Views
{
    public sealed partial class OnboardingPage : Page
    {
        public OnboardingPage()
        {
            this.InitializeComponent();
        }

        private void Initialize_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(NameInput.Text) || string.IsNullOrWhiteSpace(GoalInput.Text) || DateInput.Date == null)
            {
                ErrorText.Text = "All fields are required to initialize.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            // Create the profile
            UserProfile newProfile = new UserProfile
            {
                Name = NameInput.Text,
                TargetGoal = GoalInput.Text,
                GoalDate = DateInput.Date.Value.DateTime
            };

            // Save it to the JSON file
            DataManager.SaveProfile(newProfile);

            // Load it into the Global Brain
            AppState.CurrentUser = newProfile;

            // Travel to the main app!
            this.Frame.Navigate(typeof(ShellPage));
        }
    }
}