using System;
using System.Windows;
using System.ComponentModel;

namespace UnbreakfocusPC {
    public partial class BlockerWindow : Window {
        public bool IsStrictMode { get; set; }
        public event EventHandler? OverrideTriggered; // Trigger for MainWindow

        public BlockerWindow(bool isStrict) {
            InitializeComponent();
            IsStrictMode = isStrict;
            if (!isStrict) {
                TxtPenalty.Text = "RETURN TO WORK OR ACCEPT THE CONSEQUENCES.";
                BtnOverride.Visibility = Visibility.Visible; // Show escape hatch
            }
        }

        private void Override_Click(object sender, RoutedEventArgs e) {
            OverrideTriggered?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnClosing(CancelEventArgs e) {
            if (IsStrictMode) e.Cancel = true; 
            base.OnClosing(e);
        }
    }
}