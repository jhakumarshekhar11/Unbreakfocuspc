# 🎯 Unbreakfocuspc

**Unbreakfocuspc** is a gamified, privacy-first native Windows desktop application designed to eliminate digital distractions and build deep focus habits. Originally built in Flutter, this project has been fully re-engineered from the ground up using **WinUI 3** and **.NET 8** to provide a seamless, native Windows 11 experience.

---

## ✨ Key Features

* **🛡️ The Focus Shield (Win32 Interop):** Uses native Windows APIs to monitor active processes. If you open a restricted app (like Discord or a web browser) during a session, the Shield instantly locks it down with a full-screen overlay.
* **🎮 Gamified Productivity:** Earn XP, level up your rank, and maintain daily streaks by completing focused deep-work sessions.
* **🔒 100% Offline & Privacy-First:** No cloud, no tracking, no Firebase. Your profile, XP, streak history, and inventory are saved exclusively on your local machine using native JSON serialization.
* **🪟 Windows 11 Aesthetics:** Built with WinUI 3, the UI leverages the `MicaBackdrop` material for a modern, glassmorphic look that feels right at home on Windows.

---

## 🏗️ Minimalist Architecture

Unbreakfocuspc is designed to be lightweight and easy to understand, rejecting bloated enterprise patterns in favor of a consolidated, highly readable file structure:

* **`Models.cs`**: The brain of the app. Handles the mathematical logic for XP, Levels, and Streaks, alongside the `DataManager` for local JSON read/writes.
* **`FocusEngine.cs`**: The native Win32 watchdog. Uses P/Invoke (`GetForegroundWindow`, `GetWindowThreadProcessId`) to silently monitor for distracting desktop processes.
* **`MainWindow.xaml` & `.cs`**: The unified frontend. Contains the glassmorphic UI, navigation, and the core session timer logic.

---

## 🚀 Getting Started

### Prerequisites
* Windows 10 (version 1809 or later) or Windows 11.

### Installation & Build
1. Clone the repository:
   ```bash
   git clone [https://github.com/yourusername/Unbreakfocuspc.git](https://github.com/yourusername/Unbreakfocuspc.git)
