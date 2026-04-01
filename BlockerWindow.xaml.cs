using System.Windows;
using System.ComponentModel;

namespace UnbreakfocusPC {
    public partial class BlockerWindow : Window {
        public bool IsStrictMode { get; set; }

        public BlockerWindow(bool isStrict) {
            InitializeComponent();
            IsStrictMode = isStrict;
            if (!isStrict) TxtPenalty.Text = "RETURN TO WORK.";
        }

        // Prevent the user from bypassing the blocker using Alt+F4
        protected override void OnClosing(CancelEventArgs e) {
            if (IsStrictMode) e.Cancel = true; 
            base.OnClosing(e);
        }
    }
}