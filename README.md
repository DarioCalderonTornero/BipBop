# 🎮 Bipbop, Mobile Party Game

> A fast-paced mobile party game featuring 7 independent minigames with unique mechanics, progressive difficulty, and a full online progression system. Published on Google Play.

![Platform](https://img.shields.io/badge/Platform-Android-green?logo=android)
![Engine](https://img.shields.io/badge/Engine-Unity%206-black?logo=unity)
![Language](https://img.shields.io/badge/Language-C%23-purple)
![Backend](https://img.shields.io/badge/Backend-PlayFab-blue)
![Status](https://img.shields.io/badge/Status-Published-brightgreen)

> **About this repository.** This is a public copy of the project, published with the team's permission so the code can be reviewed. The original development took place in a private repository shared between two programmers; the full commit history is preserved here.
>
> The paid Asset Store package *AllIn1 Sprite Shader* has been removed and is **not included**, so some materials will show missing shader references if the project is opened in Unity. The published game on Google Play is unaffected.

---

## 📲 Download

[![Google Play](https://img.shields.io/badge/Google_Play-Available-green?logo=google-play)](https://play.google.com/store/apps/details?id=com.twotapsstudio.bipbop)

🌐 [Project page](https://dariogamedev.com/bipbop.html) · 🎬 [Trailer](https://www.youtube.com/watch?v=-W2FQWRpG8g)

---

## 🕹️ Overview

**Bipbop** is a mobile party game built in Unity where players face 7 fully independent minigames, each with its own mechanics and visual identity. Every minigame is designed around increasing time pressure: the longer you survive, the faster it gets.

The game features a complete meta-progression system with daily missions, in-game currency, unlockables, online leaderboards powered by PlayFab, and full multi-language support.

Developed by a team of two programmers, published on Google Play and later migrated from Unity 2022 to Unity 6.

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

### 💰 Monetization & Release
- **AdMob** and **IronSource / LevelPlay** mediation integrated, including rewarded ads tied to the in-game economy
- Google Play release pipeline: store listing, signed builds, versioning and updates
- In-app update prompts handled through the Google Play Core libraries
- Addressables used to keep build size under control

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

- **Mobile input detection.** Gyroscope, accelerometer, touch gestures, pinch-to-zoom, and multi-tap inputs handled natively
- **Dynamic difficulty scaling.** Timer acceleration system that increases pressure progressively per minigame
- **PlayFab integration.** Player authentication, leaderboard management, and profile data stored and retrieved in real time
- **Ad mediation.** AdMob and IronSource/LevelPlay wired into the reward and economy loops
- **ScriptableObject architecture.** Minigame configs, shop items, mission definitions, and cosmetic data driven by SO assets
- **Persistent data management.** Player currency, unlocks, settings, and daily mission state saved and loaded reliably across sessions
- **Modular UI system.** Independent UI flows for main menu, minigame selection, game over, pause, settings, profile, shop, and leaderboards
- **Engine migration.** Project upgraded from Unity 2022 to Unity 6 without breaking the live release

---

## 🛠️ Tech Stack

| Category | Technology |
|----------|------------|
| Engine | Unity 6 (6000.0), migrated from Unity 2022 |
| Language | C# |
| Backend | Microsoft PlayFab |
| Ads | Google AdMob + IronSource / LevelPlay mediation |
| Store | Google Play Plugins (in-app updates, delivery) |
| Input | Unity Input System + Native Mobile APIs |
| Data | ScriptableObjects + PlayerPrefs + Addressables |
| UI | Unity UI + TextMeshPro |
| Rendering | Universal Render Pipeline (URP) |
| Platform | Android (Google Play) |

---

## 👥 Team & My Contribution

Developed by a team of **2 programmers**: [Darío Calderón Tornero](https://github.com/DarioCalderonTornero) and [Alexr5k7](https://github.com/Alexr5k7).

**What I built:**
- 3 of the 7 minigames end to end: **Color Rush**, **Meteor Crash** and **Sort It**
- Ad integration: AdMob and IronSource/LevelPlay, including rewarded ads
- PlayFab integration for authentication, player profiles and online leaderboards
- Co-developed the shop, the in-game economy, the progression system and the settings menu
- Google Play release and maintenance, and the Unity 2022 → Unity 6 migration

---

## 📸 Media

🎬 **[Watch the trailer](https://www.youtube.com/watch?v=-W2FQWRpG8g).** Full showcase of the 7 minigames and the progression systems.

---

## 👤 Author

**Darío Calderón Tornero**, Gameplay Programmer (Unity & Unreal Engine 5)
[Portfolio](https://dariogamedev.com) · [LinkedIn](https://www.linkedin.com/in/dariocalderontornero/) · [GitHub](https://github.com/DarioCalderonTornero)

---

## 📄 License

This project is not open source. All rights reserved.
