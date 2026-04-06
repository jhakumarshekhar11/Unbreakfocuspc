using Microsoft.UI.Xaml.Controls;
using Unbreakfocuspc.Models; // Need access to the Brain

namespace Unbreakfocuspc.Views
{
    public sealed partial class ShellPage : Page
    {
        public ShellPage()
        {
            this.InitializeComponent();
            ContentFrame.Navigate(typeof(HomePage));
            Sidebar.SelectedItem = Sidebar.MenuItems[0]; 
        }

        private void Sidebar_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItemContainer as NavigationViewItem;
            if (item != null)
            {
                string clickedTab = item.Tag?.ToString() ?? "";

                switch (clickedTab)
                {
                    case "Home":
                        ContentFrame.Navigate(typeof(HomePage));
                        break;
                    
                    case "Focus":
                        // SMART ROUTING: If a timer is running, trap them in the Timer Page!
                        if (AppState.CurrentFocusSubject != null)
                        {
                            ContentFrame.Navigate(typeof(TimerPage), AppState.CurrentFocusSubject);
                        }
                        else
                        {
                            ContentFrame.Navigate(typeof(FocusTab));
                        }
                        break;

                    case "Settings":
                        // Use the full name if the compiler is being difficult
                        ContentFrame.Navigate(typeof(Unbreakfocuspc.Views.SettingsPage));
                        break;
                }
            }
        }
    }
}