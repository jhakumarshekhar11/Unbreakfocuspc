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

        // Add the PC apps you want to block here
        private readonly HashSet<string> _blocklist = new(StringComparer.OrdinalIgnoreCase) 
        { 
            "chrome", "msedge", "brave", "firefox", "discord", "spotify", "whatsapp" 
        };

        public void StartShield()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => WatchdogLoop(_cts.Token));
        }

        public void StopShield() => _cts?.Cancel();

        private async Task WatchdogLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd != IntPtr.Zero)
                {
                    GetWindowThreadProcessId(hWnd, out uint processId);
                    try
                    {
                        Process proc = Process.GetProcessById((int)processId);
                        string name = proc.ProcessName;

                        if (_blocklist.Contains(name))
                        {
                            OnDistractionDetected?.Invoke(this, name.ToUpper());
                        }
                    }
                    catch { /* System process access denied */ }
                }
                await Task.Delay(2000, token); // Scan every 2 seconds
            }
        }
    }
}