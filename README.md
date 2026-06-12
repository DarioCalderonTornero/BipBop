<img width="452" height="914" alt="Captura de pantalla 2026-03-26 190420" src="https://github.com/user-attachments/assets/c49201d3-51e7-43bd-a5cc-500feb63dd81" />
# 🎮 Bipbop — Mobile Party Game

> A fast-paced mobile party game featuring 7 independent minigames with unique mechanics, progressive difficulty, and a full online progression system. Published on Google Play.

![Platform](https://img.shields.io/badge/Platform-Android-green?logo=android)
![Engine](https://img.shields.io/badge/Engine-Unity-black?logo=unity)
![Language](https://img.shields.io/badge/Language-C%23-purple)
![Backend](https://img.shields.io/badge/Backend-PlayFab-blue)
![Status](https://img.shields.io/badge/Status-Published-brightgreen)

---

## 📲 Download

[![Google Play](https://img.shields.io/badge/Google_Play-Available-green?logo=google-play)](YOUR_GOOGLE_PLAY_LINK)

---

## 🕹️ Overview

**Bipbop** is a mobile party game built in Unity where players face 7 fully independent minigames, each with its own mechanics and visual identity. Every minigame is designed around increasing time pressure — the longer you survive, the faster it gets.

The game features a complete meta-progression system with daily missions, in-game currency, unlockables, online leaderboards powered by PlayFab, and full multi-language support.

Developed by a team of two programmers.

---

## 🎯 Minigames

| # | Name | Core Mechanic |
|---|------|---------------|
| 1 | **Do It** | Follow on-screen orders using real mobile inputs (rotate, tap, zoom, shake) |
| 2 | **Color Rush** | Identify the correct color under misleading text/background combinations |
| 3 | **Shape Hunt** | Tap the correct geometric shape among increasing distractors |
| 4 | **Meteor Crash** | Control a spaceship and redirect meteors to collide with each other |
| 5 | **Grid Escape** | Survive a 4x4 grid while dodging arrows and collecting gems dynamically |
| 6 | **Spot It** | Find the odd one out in a 5x5 grid with subtle visual differences |
| 7 | **Sort It** | Classify items into 4 fields with dynamic modifiers (inverted input, swapped fields, color changes) |

---

## ⚙️ Key Systems

### 🧩 Minigame Architecture
- Each minigame is fully self-contained with its own logic, input handling, and difficulty curve
- Shared timer system with progressive speed increase across all minigames
- ScriptableObject-driven configuration for easy tuning without code changes

### 👤 Player Profile & Progression
- Personal profile system with custom avatar and background selection
- **36 unlockable avatars** and **36 unlockable backgrounds**
- Unlock methods: daily missions, daily shop, and luck chests
- In-game currency earned through gameplay and mission completion

### 📅 Daily Mission System
- Refreshing daily missions tied to specific minigames and objectives
- Rewards in-game currency upon completion
- Persistent tracking across sessions

### 🏆 Online Leaderboards (PlayFab)
- Global top 10 leaderboard per minigame
- Player's own score always visible regardless of ranking
- Profile browsing: view other players' avatars and minigame scores
- Real-time data synchronization via PlayFab backend

### 🌐 Localization
- Full multi-language support: **English** and **Spanish**
- Language switchable at runtime from the settings menu

### ⚙️ Settings
- Independent audio controls for music and sound effects
- FPS cap selection for performance optimization on lower-end devices
- All settings persisted across sessions

### 🎁 Luck Chests & Daily Shop
- Daily rotating shop with cosmetic items
- Luck chest system with randomized unlockables
- Economy balanced around daily engagement loops

---

## 🧠 Technical Highlights

- **Mobile input detection** — Gyroscope, accelerometer, touch gestures, pinch-to-zoom, and multi-tap inputs handled natively
- **Dynamic difficulty scaling** — Timer acceleration system that increases pressure progressively per minigame
- **PlayFab integration** — Player authentication, leaderboard management, and profile data stored and retrieved in real time
- **ScriptableObject architecture** — Minigame configs, shop items, mission definitions, and cosmetic data driven by SO assets
- **Persistent data management** — Player currency, unlocks, settings, and daily mission state saved and loaded reliably across sessions
- **Modular UI system** — Independent UI flows for main menu, minigame selection, game over, pause, settings, profile, shop, and leaderboards

---

## 🛠️ Tech Stack

| Category | Technology |
|----------|------------|
| Engine | Unity |
| Language | C# |
| Backend | Microsoft PlayFab |
| Input | Unity Input System + Native Mobile APIs |
| Data | ScriptableObjects + PlayerPrefs |
| UI | Unity UI + TextMeshPro |
| Platform | Android (Google Play) |

---

## 👥 Team

Developed by a team of **2 programmers**.

| Role | Name |
|------|------|
| Game Programmer | Darío Calderón Tornero |
| Game Programmer | [Teammate name] |

---

## 📸 Screenshots

<!-- Add screenshots or GIFs here -->
<img width="452" height="914" alt="Captura de pantalla 2026-03-26 190420" src="https://github.com/user-attachments/assets/8e94bc2f-c3a2-4f80-b11e-5c75aab1ea41" />


---

## 📄 License

This project is not open source. All rights reserved.
