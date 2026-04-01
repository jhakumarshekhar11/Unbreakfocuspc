using System;
using System.Windows;

namespace UnbreakfocusPC 
{
    // Explicitly declaring System.Windows.Application resolves the WinForms collision
    public partial class App : System.Windows.Application 
    {
        // No Firestore initialization needed here anymore. 
        // The app relies entirely on the local Persistence.cs layer now.
    }
}