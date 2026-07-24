# Ringkasan Analisis Proyek (GT-DREAMRIFT)

Berdasarkan analisis struktur direktori dan file script yang ada pada folder `Assets/Script`, berikut adalah ringkasan dari fitur-fitur yang sudah diimplementasikan dan yang masih kurang atau belum ada dalam proyek ini.

## ✅ Fitur yang Sudah Diimplementasikan

1. **Sistem Pergerakan & Input Pemain (Player Movement & Input)**
   - Terintegrasi dengan *New Input System* bawaan Unity (terdapat `StarterAssetsInputs.cs` dan konfigurasi `.inputactions`).

2. **Sistem Pertarungan (Combat System)**
   - **Mekanik Pemain**: Pemain dapat melakukan serangan dan memiliki jendela kombo serangan (`PlayerFight.cs`, `PlayerAttack.cs`).
   - **Kecerdasan Buatan (AI) Musuh**: Terdapat AI khusus (Organic Swarm AI) yang menggunakan sistem *ScriptableObject* untuk membedakan pola serangan, seperti serangan jarak dekat (*Melee*), proyektil jarak jauh (*Ranged*), dan serangan serudukan (*Slime Dash*).

3. **Sistem Kesehatan (Health System)**
   - Pengelolaan nyawa/HP (Health Point) secara terpisah untuk Pemain (`playerHealth.cs`) dan Musuh (`EnemyHealth.cs`).
   - Terdapat *Interface* `IDamageable` standar untuk objek-objek yang bisa menerima kerusakan.
   - Antarmuka (UI) darah musuh yang sudah dioptimasi menggunakan sistem *Object Pooling* (`EnemyHealthBarPool.cs`, `EnemyHealthBarUI.cs`).

4. **Sistem Interaksi (Interaction System)**
   - Deteksi interaksi pemain terhadap objek sekitar (`PlayerInteraction.cs`).
   - Terdapat objek interaktif yang sudah berfungsi seperti pintu (`Door.cs`) menggunakan *Interface* `IInteractable`.

5. **Sistem Inventaris (Inventory System)**
   - Kemampuan mengambil item (`ItemPickup.cs`), penyimpanan berbasis data item (`ItemData.cs`), serta pengelolaan isi inventaris (`InventoryManager.cs`).
   - Sudah terhubung dengan Antarmuka Pengguna/UI inventaris (`InventoryUI.cs`, `InventoryUISlot.cs`).

6. **Sistem Sinematik & Dialog (Cutscene & Dialogue System)**
   - Terdapat manajer cutscene in-game (`InGameCutsceneManager.cs`, `CutsceneManager.cs`) dan data cutscene berbasis *ScriptableObject* (`CutsceneData.cs`).
   - Pemicu cutscene berbasis trigger kolisi (`CutsceneTrigger.cs`).
   - Terdapat interaksi UI untuk melanjutkan dialog (`UIConversationButton.cs`).

7. **Sistem Inti & Navigasi Antarmuka (Core & UI Navigation)**
   - Pemindahan antar level/scene sudah dikelola khusus (`LoadingManager.cs`, `SceneField.cs`).
   - Kontrol navigasi Menu Utama (`MainMenuController.cs`).

---

## ❌ Fitur yang Kurang / Belum Diimplementasikan

Berdasarkan analisis, beberapa fitur esensial yang biasanya ada pada game bergenre RPG/Action-Adventure masih belum ditemukan implementasinya:

1. **Sistem Penyimpanan (Save & Load System)**
   - Belum ada sistem yang menyimpan data progres permainan pemain (seperti posisi terakhir, HP terakhir, atau isi inventaris) agar bisa dilanjutkan kembali (tidak ada implementasi JSON, BinaryFormatter, maupun PlayerPrefs yang terlihat).

2. **Pengelola Audio Terpusat (Global Audio Manager)**
   - Saat ini audio (BGM) hanya dikendalikan sementara oleh skrip spesifik seperti `InGameCutsceneManager.cs`. Belum ada satu *Audio Manager* terpusat yang mengatur BGM dan berbagai SFX (Sound Effects) di seluruh permainan.

3. **Sistem Misi / Objektif (Quest System)**
   - Belum ada skrip untuk memberikan tugas kepada pemain atau melacak objektif apa yang sedang dijalani dan status penyelesaiannya (Quest Log).

4. **Sistem Pengelola State Permainan (Game Manager)**
   - Belum ada sistem yang mengatur *Game State* secara utuh, misalnya transisi masuk ke state **Game Over** jika pemain mati, menu **Pause Game**, atau state Kemenangan.

5. **Sistem Progresi Karakter (Character Progression)**
   - Belum ada mekanisme perolehan poin *Experience* (EXP), sistem naik level (*Level-up*), ataupun peningkatan *stats* karakter seiring berjalannya permainan.

6. **Menu Pengaturan/Opsi (Settings/Options Menu)**
   - Skrip UI Menu Utama belum memiliki logika untuk mengatur resolusi layar, memetakan ulang kontrol (*keybinding*), ataupun mengatur volume suara secara dinamis.
