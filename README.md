# GT-DREAMRIFT ⚔️🌌

**GT-DREAMRIFT** adalah sebuah game 3D bergenre **Action-RPG / Adventure** yang dibangun menggunakan **Unity 6 (URP)**. Dalam game ini, pemain akan menjelajahi dunia fantasi, bertarung melawan berbagai tipe musuh dengan sistem pertarungan *real-time combo*, menyelesaikan *quest*, dan mengikuti jalan cerita yang imersif melalui sistem *cutscene* & dialog.

---

## 🎯 Target Platform & Engine

- **Engine Version**: Unity 6 (`6000.3.14f1`) - Universal Render Pipeline (URP)
- **Target Platforms**:
  - 🖥️ **PC / Windows** (Standalone Executable)
  - 📱 **Android** (Mobile APK / AAB) - Automated Build via Game CI

---

## 🎮 Mekanik & Sistem Game

Proyek ini dibangun secara modular menggunakan C# dengan sistem-sistem utama berikut:

### 1. Sistem Pertarungan (Action Combat & Combo)
- **Third-Person Action Combat**: Sistem pertarungan *real-time* berbasis kombo serangan beruntun.
- **AI Musuh Variatif**: Kecerdasan buatan musuh dengan tipe serangan berbeda (Melee, Ranged, & Slime behavior).
- **Health & Damage System**: Pengaturan status HP, damage calculation, dan efek kena serang (*hit reaction*).

### 2. Sistem Quest Bercabang & Quest Log UI
- **Sistem Quest Bercabang (Branching Quests & Prerequisite Chain)**:
  - Pelacakan alur quest bercabang dengan syarat pembuka (`Prerequisites`) dan quest kelanjutan (`NextQuestsOnComplete`).
  - Status Quest runtime: `Locked` (terkunci), `Available` (siap diambil), `Active` (sedang berjalan), dan `Complete` (selesai).
- **4 Tipe Quest Objective**:
  - 🗣️ **TalkToNPC**: Berbicara dengan NPC (terhubung ke NPC interaction `IInteractable`).
  - 📍 **ReachLocation**: Menjangkau area trigger / checkpoint lokasi.
  - ⚔️ **DefeatEnemy**: Mengalahkan sejumlah musuh (terhubung otomatis ke `EnemyHealth.Die()`).
  - 🎒 **CollectItem**: Mengumpulkan item di inventaris.
- **Multiple Item Rewards**: Pengiriman otomatis hadiah item (`QuestReward[]`) langsung ke [InventoryManager](Assets/Script/InventorySystem/InventoryManager.cs) saat quest diselesaikan.
- **Quest Log UI Skeleton**: Panel UI daftar quest (List View & Detail Panel) yang dapat di-toggle via shortcut **`J`** di PC atau tombol UI di Mobile.

### 3. Sistem Inventaris & World Item Pickup
- **Inventory System**: Penyimpanan item hasil pengumpulan (*loot*) dan hadiah quest di sepanjang eksplorasi.
- **Interactive Item Pickup & World Drops**:
  - Objek item di 3D world ([ItemPickup.cs](Assets/Script/InventorySystem/ItemPickup.cs)) yang terhubung ke `IInteractable` dan `PlayerInteraction`.
  - **Dua Mode Pemungutan**: Mode Button Interaction (`Ambil Item (E)`) dan Mode Walkover (otomatis terambil saat diinjak).
  - **3D Floating Motion**: Efek rotasi dan melayang halus ([ItemFloatingAnimation.cs](Assets/Script/InventorySystem/ItemFloatingAnimation.cs)) pada barang drop.
  - **Enemy Loot Drops**: Musuh mati ([EnemyHealth.cs](Assets/Script/Health%20System/EnemyHealth.cs)) otomatis menjatuhkan item drop di world.
  - **Audio & Visual Feedback**: Pemutaran suara SFX dan efek partikel VFX saat item dipungut.

### 4. Sistem Cutscene & Cerita
- **In-Game Cutscene Manager**: Pengatur alur rekaman *cinematic* dan transisi alur permainan.
- **Dialogue System & Editor**: Integrasi *Dialogue Editor* untuk manajemen percakapan karakter secara dinamis.

### 5. Custom Editor Tools (Developer Workflows)
- **2D Visual Quest Node Graph Editor (`Tools > Quest System > Visual Quest Creator`)**: Editor Window 2D interaktif untuk me-render alur percabangan quest sebagai kartu node 2D yang dapat di-drag, dilengkapi **Garis Lengkung Bezier Curves (`Handles.DrawBezier`)**, **Node Linking Tool**, **Auto Layout**, serta **Live Play Mode Debugger**.
- **Interactive Terrain & GameObject Cropper (`Tools > Terrain > Interactive Cropper`)**: Tool internal untuk memotong (*crop*) atau membagi (*split grid*) Unity Terrain beserta objek dekorasi/props secara visual di Scene View.

---

## 🕹️ Kontrol Permainan

Game ini mendukung **Unity New Input System** (Keyboard + Mouse untuk PC & Touch UI / Canvas Inputs untuk Android).

| Aksi | Keyboard & Mouse (PC) | Mobile (Android) |
| :--- | :--- | :--- |
| **Pergerakan Karakter** | `W, A, S, D` | On-Screen Virtual Joystick |
| **Kamera** | Mouse Movement (Tahan Klik Kanan) | On-Screen Touch Drag |
| **Serangan Combo** | Klik Kiri Mouse (`LMB`) | Virtual Attack Button |
| **Interaksi (Pintu/NPC/Loot)** | `E` / `F` | Virtual Interact Button |
| **Buka/Tutup Quest Log** | `J` | Virtual Quest Button / HUD |
| **Lanjut Dialog Cutscene**| Klik Kiri Mouse / `Spasi` | Tap Screen |

---

## 🤖 CI/CD Pipeline & Game CI (GitHub Actions)

Proyek ini dilengkapi dengan otomatisasi **CI/CD** menggunakan **GitHub Actions** dan **Game CI** untuk memunculkan Android Build APK secara otomatis pada setiap push ke branch `main` atau `develop`.

File alur kerja berada pada: [`.github/workflows/android-build.yml`](file:///.github/workflows/android-build.yml)

### Fitur Automated Pipeline:
- **Game CI Unity Builder (`game-ci/unity-builder@v4`)**: Kompilasi otomatis untuk target platform **Android** (Unity `6000.3.14f1`).
- **Swap Space Memory Management**: Menambahkan Virtual RAM 10GB pada Runner Ubuntu agar tidak terjadi out-of-memory saat kompilasi shader & asset.
- **Android Signing Keystore**: Dukungan penandatanganan release APK via Base64 secret.
- **Automated Artifact Upload**: File APK terenkapsulasi tersimpan langsung sebagai Artifact di GitHub Actions.
- **Discord Webhook Notifications**: Notifikasi otomatis (Success/Failure) dikirim langsung ke channel Discord tim dev.

---

## ⚙️ Petunjuk Setup & Menjalankan Proyek (Developer Guide)

1. **Prasyarat**:
   - Install **Unity Hub**.
   - Install **Unity 6 (`6000.3.14f1`)** dengan modul **Android Build Support** (termasuk NDK, SDK, dan OpenJDK).
2. **Cloning Repositori**:
   ```bash
   git clone https://github.com/Dzakhwan/GT-DREAMRIFT.git
   ```
3. **Membuka Proyek**:
   - Buka Unity Hub $\rightarrow$ Klik **Add project from disk** $\rightarrow$ Pilih folder `GT-DREAMRIFT`.
   - Pastikan menggunakan versi Unity 6 yang sesuai.
4. **Membuka Tools Visual (Editor Tools)**:
   - **2D Visual Quest Creator & Node Graph**: Klik menu **`Tools > Quest System > Visual Quest Creator`**.
   - **Interactive Terrain Cropper**: Klik menu **`Tools > Terrain > Interactive Cropper`**.
5. **Membuka Scene Utama**:
   - Navigasi ke `Assets/Scenes/`.
   - Buka scene `MainMenu.unity` atau `Lvl1.unity`.
   - Tekan tombol **Play** (`Ctrl + P`) di Unity Editor.
