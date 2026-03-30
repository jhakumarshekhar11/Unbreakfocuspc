using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Unbreakfocus.Desktop
{
    public class FocusEngine
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public event EventHandler<string> OnDistractionDetected;

        private CancellationTokenSource _cts;
        private readonly HashSet<string> _blocklist = new(StringComparer.OrdinalIgnoreCase)
        {
            "discord", "msedge", "chrome", "firefox", "brave", "whatsapp", "telegram", "spotify"
        };

        public void StartShield()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => WatchdogLoop(_cts.Token), _cts.Token);
        }

        public void StopShield()
        {
            _cts?.Cancel();
        }

        private async Task WatchdogLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IntPtr hWnd = GetForegroundWindow();
                    if (hWnd != IntPtr.Zero)
                    {
                        GetWindowThreadProcessId(hWnd, out uint processId);
                        Process proc = Process.GetProcessById((int)processId);
                        
                        string processName = proc.ProcessName.ToLower();

                        if (_blocklist.Contains(processName))
                        {
                            // Trigger overlay on UI thread via event
                            OnDistractionDetected?.Invoke(this, processName.ToUpper());
                        }
                    }
                }
                catch (Exception) { /* Ignore access denied exceptions for system processes */ }

                await Task.Delay(2000, token); // Check every 2 seconds
            }
        }
    }
}