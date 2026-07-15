# 📦 DANH SÁCH ĐẦY ĐỦ TOÀN BỘ DỰ ÁN — FPS SURVIVAL ZOMBIE
## Theo trình tự chơi từ đầu đến kết thúc game

---

## 1. MAIN MENU

### Scene: `Assets/Scenes/MainMenu.unity`

- [ ] Menu Camera Orbit — Camera xoay vòng ở menu
- [ ] MainMenuManager — Quản lý nút Play / Quit
- [ ] Best Score Display — Hiển thị điểm cao nhất
- [ ] PlayFab Login UI — Giao diện đăng nhập (Username/Password)
- [ ] Menu Camera Orbit — Animation camera nền

---

## 2. HỆ THỐNG NỀN TẢNG (KHỞI TẠO CÙNG GAME)

### Scripts Core (khởi tạo khi vào game)

- [ ] StoryManager — Điều phối toàn bộ cốt truyện 5 chương
- [ ] AIDirector — Hệ thống độ khó động (Calm/Buildup/Attack/Recovery)
- [ ] WaveManager — Quản lý wave (base 10 + wave × 5)
- [ ] ScoreManager — Tính điểm
- [ ] CollectibleManager — Quản lý 39 journal
- [ ] SideQuestManager — Quản lý 8 side quest
- [ ] AchievementManager — 5 achievements
- [ ] PlayerStatsTracker — Thống kê toàn bộ chỉ số người chơi
- [ ] PauseManager — Menu tạm dừng
- [ ] PlayFabManager — Kết nối PlayFab cloud
- [ ] DayNightCycle — Chu kỳ ngày/đêm
- [ ] SkillTreeManager — Cây kỹ năng 3 nhánh
- [ ] PlayerUpgradeManager — Áp dụng nâng cấp vào player

---

## 3. PLAYER & ĐIỀU KHIỂN

### Hệ thống di chuyển (Cowsins Engine)

- [ ] Basic Movement — Đi bộ / chạy
- [ ] Camera Look — Xoay camera chuột
- [ ] Jump — Nhảy
- [ ] Crouch & Slide — Ngồi / Trượt
- [ ] Dash — Lướt nhanh
- [ ] Wall Run — Chạy tường
- [ ] Wall Bounce — Bật tường
- [ ] Double Jump — Nhảy đôi (skill tree)
- [ ] Grappling Hook — Móc câu
- [ ] Climb Ladder — Leo thang
- [ ] Stamina — Hệ thống thể lực
- [ ] Footsteps — Âm thanh bước chân
- [ ] Ground Detection — Phát hiện mặt đất
- [ ] Speed Lines — Hiệu ứng vạch tốc độ
- [ ] Camera FOV Manager — Quản lý góc nhìn
- [ ] Camera Animations — Hiệu ứng camera
- [ ] Camera Shake — Rung camera
- [ ] Player Flashlight — Đèn pin (phím F)

### Hệ thống vũ khí (Cowsins Engine)

- [ ] WeaponController — Điều khiển vũ khí
- [ ] Weapon ScriptableObject — Định nghĩa từng loại vũ khí

#### Danh sách vũ khí

- [ ] Pistol — Súng lục
- [ ] Rifle — Súng trường
- [ ] SMG — Súng tiểu liên
- [ ] Shotgun — Súng ngắn
- [ ] Rocket Launcher — Súng rocket
- [ ] Revolver — Súng ổ quay
- [ ] Katana — Kiếm cận chiến
- [ ] Burst Rifle — Súng bắn 3 phát
- [ ] Turret — Tháp súng

#### Cơ chế vũ khí

- [ ] Shoot Styles: Hitscan / Projectile / Melee / Custom
- [ ] Reload — Nạp đạn (magazine-based)
- [ ] Recoil — Độ giật
- [ ] Spread — Độ xoáy đạn
- [ ] Aim Down Sights (ADS) — Ngắm bắn
- [ ] Weapon Weight — Trọng lượng vũ khí
- [ ] Weapon Inventory — Túi đồ vũ khí
- [ ] Weapon Effects — Hiệu ứng vũ khí
- [ ] Muzzle Flash VFX — Hiệu ứng lửa nòng
- [ ] Bullet Holes — Lỗ đạn (Wood/Metal/Mud/Grass)
- [ ] Hitmarker — Dấu hit

#### Attachments (Phụ kiện)

- [ ] Barrel — Nòng súng
- [ ] Grip — Tay cầm
- [ ] Scope — Ống ngắm
- [ ] Stock — Báng súng
- [ ] Laser — Laser
- [ ] Flashlight — Đèn gắn súng
- [ ] Magazine — Băng đạn

---

## 4. HUD & GIAO DIỆN NGƯỜI CHƠI

### Scene: `Assets/Scenes/Play Scene (Story).unity`

### Widgets hiển thị

- [ ] Health Widget — Thanh máu
- [ ] Low Health Vignette — Viền đỏ khi máu thấp
- [ ] Health Flash — Hiệu ứng nhấp nháy khi bị đòn
- [ ] Stamina Widget — Thanh thể lực
- [ ] Ammo Widget — Đếm đạn
- [ ] Dash Cooldown Widget — Hồi chiêu dash
- [ ] Reload Indicator — Chỉ báo đang nạp đạn
- [ ] Crosshair Widget — Tâm ngắm (độ xoáy động)
- [ ] Compass Widget — La bàn
- [ ] Compass Marker — Điểm đánh dấu trên la bàn
- [ ] Damage Direction HUD — Chỉ thị hướng bị tấn công
- [ ] Combat Feedback HUD — Số sát thương + kill feed
- [ ] FPS Widget — Chỉ số FPS
- [ ] Threat Widget — Mức đe dọa (AIDirector)
- [ ] Progression HUD — Thanh kinh nghiệm
- [ ] Quest Tracker Widget — Theo dõi nhiệm vụ
- [ ] Quest Beacon — Điểm đánh dấu mục tiêu
- [ ] Interact Prompt Widget — "Nhấn E để tương tác"
- [ ] Wave Announcer — Thông báo wave
- [ ] Boss Health Bar — Thanh máu boss
- [ ] Weapon Inventory Widget — Túi vũ khí
- [ ] Weapon Indicator Widget — Vũ khí hiện tại
- [ ] Upgrades / Skill Tree Widget — Cây kỹ năng
- [ ] Journal UI — Popup nhật ký
- [ ] Player Profile Widget — Hồ sơ người chơi
- [ ] Leaderboard Widget — Bảng xếp hạng
- [ ] Achievement Widget — Thông báo achievement
- [ ] Stats Panel UI — Bảng thống kê
- [ ] Simple Notification — Thông báo toast
- [ ] Icon Displayer Widget — Hiển thị icon
- [ ] Cowsins HUD Adapter — Cầu nối Cowsins → HUD tùy chỉnh

---

## 5. CHƯƠNG 1 — BÌNH MINH (DAWN)

### Thời gian: 6h — DayNightCycle: Dawn keyframe

### Khu vực: Trại/Căn cứ ban đầu

### Thành phần

- [ ] ChapterBoundary — Ranh giới chương, kích hoạt spawner
- [ ] SaveRoom (Checkpoint 1) — Checkpoint đầu + hồi máu
- [ ] Spawner (Spawm.cs) — Spawn zombie trong chương
- [ ] QuestTrigger — Các trigger volume kích hoạt quest
- [ ] KillCountObjective — Mục tiêu tiêu diệt
- [ ] CollectibleQuestObjective — Mục tiêu thu thập
- [ ] QuestInteractable — Vật phẩm tương tác hoàn thành quest
- [ ] CutscenePlayer — Cutscene chuyển chương

### Side Quest khả dụng sau main quest

- [ ] Side Quest: Church
- [ ] Side Quest: Auto Repair

### Journal thu thập được

- [ ] Soldier Journal #1-3 (3 cái)
- [ ] Neighbor Journal #1-21 (một phần)

### Kết thúc chương

- [ ] SaveRoom trigger → Cutscene "CHƯƠNG 2 — BỆNH VIỆN"
- [ ] DayNightCycle chuyển snap → Noon (12h)

---

## 6. CHƯƠNG 2 — BUỔI TRƯA (NOON)

### Thời gian: 12h — DayNightCycle: Noon keyframe

### Khu vực: Bệnh viện

### Thành phần

- [ ] ChapterBoundary mới
- [ ] SaveRoom (Checkpoint 2)
- [ ] Spawner bệnh viện
- [ ] Quest mới (cốt truyện bệnh viện)
- [ ] Side Quest mở khóa thêm

### Side Quest bổ sung

- [ ] Side Quest: Motel
- [ ] Side Quest: Quarantine
- [ ] Side Quest: HighRise Base

### Journal thu thập

- [ ] Military Record #1-8
- [ ] Doctor Journal #1-3

### Kết thúc chương

- [ ] SaveRoom trigger → Cutscene "CHƯƠNG 3 — CÔNG TRƯỜNG"
- [ ] DayNightCycle chuyển → Dusk (18h)

---

## 7. CHƯƠNG 3 — HOÀNG HÔN (DUSK)

### Thời gian: 18h — DayNightCycle: Dusk keyframe

### Khu vực: Công trường xây dựng

### Thành phần

- [ ] ChapterBoundary mới
- [ ] SaveRoom (Checkpoint 3)
- [ ] Spawner công trường
- [ ] WaveQuestInteractable — Quest generator (sống sót 3 wave + Boomer)
- [ ] Kill boundary lock trong wave
- [ ] Teleport-back khi ra ngoài trong wave
- [ ] SpecialEnemyDirector — Bắt đầu spawn Boomer từ wave 3+

### Quái đặc biệt xuất hiện lần đầu

- [ ] Boomer — Zombie phát nổ, để lại vũng acid

### Loot System

- [ ] LootDropHelper — Rơi loot khi chết
- [ ] LootPop — Hiệu ứng nảy
- [ ] LootTrail — Hiệu ứng vệt sáng
- [ ] Coin — Xu
- [ ] Experience — EXP
- [ ] Healthpack — Máu
- [ ] PowerUp — Nâng cấp tạm thời
- [ ] Ammo pickups — Đạn

### Kết thúc chương

- [ ] Hoàn thành wave quest → SaveRoom
- [ ] Cutscene "CHƯƠNG 4 — KHU DÂN CƯ"
- [ ] DayNightCycle chuyển → Night (22h)

---

## 8. CHƯƠNG 4 — MÀN ĐÊM (NIGHT)

### Thời gian: 22h — DayNightCycle: Night keyframe

### Khu vực: Khu dân cư

### Thành phần

- [ ] ChapterBoundary mới
- [ ] SaveRoom (Checkpoint 4)
- [ ] Spawner khu dân cư
- [ ] Quest mới (cốt truyện khu dân cư)

### Skill Tree khả dụng (khi đủ điểm)

- [ ] Movement Branch — 5 nodes
  - [ ] Node 1: Walk Speed (2 SP)
  - [ ] Node 2: Run Speed (3 SP)
  - [ ] Node 3: Air Control + Dash (5 SP)
  - [ ] Node 4: Wall Run / Wall Bounce (8 SP)
  - [ ] Node 5: Double Jump + Grapple (12 SP)
- [ ] Aim Branch — 5 nodes
  - [ ] Node 1: Recoil Reduction (2 SP)
  - [ ] Node 2: Crit Chance 10% (3 SP)
  - [ ] Node 3: Crit Chance 20% (5 SP)
  - [ ] Node 4: Crit Damage x1.5 (8 SP)
  - [ ] Node 5: One-shot Crook + Bonus vs Specials (12 SP)
- [ ] Intelligence Branch — 5 nodes
  - [ ] Node 1: XP Pickup Radius (2 SP)
  - [ ] Node 2: XP x1.1 (3 SP)
  - [ ] Node 3: Radius +10 (5 SP)
  - [ ] Node 4: XP x1.15 (8 SP)
  - [ ] Node 5: Radius +15 + Highlight (12 SP)

### Survival Bonuses

- [ ] Mỗi node Movement: +Stamina
- [ ] Mỗi node Aim: +Damage
- [ ] Mỗi node Intelligence: +HP

### SpecialEnemyDirector mở rộng

- [ ] Tank bắt đầu spawn từ wave 5+
- [ ] Tank stat scale theo wave

### Side Quest bổ sung

- [ ] Side Quest: Mother's Story
- [ ] Side Quest: Lighthouse

### Journal thu thập

- [ ] Experiment Report #1-3
- [ ] Cure Record #1-4

### Đặc điểm môi trường

- [ ] PostProcess Volume: bầu không khí tối hơn
- [ ] Fog dày hơn
- [ ] Ambient tối
- [ ] Flashlight thiết yếu

### Kết thúc chương

- [ ] SaveRoom trigger → Cutscene "CHƯƠNG 5 — CHUNG CƯ"
- [ ] DayNightCycle chuyển → Deep Night (24h/2h sáng)

---

## 9. CHƯƠNG 5 — ĐÊM KHUYA (DEEP NIGHT)

### Thời gian: 2h sáng — DayNightCycle: Deep Night keyframe

### Khu vực: Chung cư (Apartment Building)

### Thành phần

- [ ] ChapterBoundary cuối cùng
- [ ] SaveRoom (Checkpoint 5) — cuối cùng
- [ ] Spawner chung cư

### Mini-boss xuất hiện lần đầu

- [ ] Witch — Ngồi khóc, kích động → lao nhanh 6.5m/s
- [ ] Big Guy — Choáng, kích động → rượt chậm, trâu bò

### Quest cuối cùng

- [ ] Quest 12: "Escape Town" — Nhiệm vụ thoát khỏi thị trấn
- [ ] WaveQuestInteractable — Trận cuối:
  - [ ] Sequential waves (3-5 wave)
  - [ ] Tank boss trong wave cuối
  - [ ] Boundary lock
  - [ ] Boss health tracking

### Các enemy khác

- [ ] Snatcher — Enemy đặc biệt (có controller)
- [ ] Hooker — Enemy đặc biệt (có controller)
- [ ] Ceiling Zombie — Zombie từ trần nhà rơi xuống

### Journal cuối cùng

- [ ] Brother Journal #1-5

### Outline System (kích hoạt)

- [ ] Outline.cs — Hiệu ứng viền
- [ ] OutlineMask.shader — Shader mặt nạ
- [ ] OutlineFill.shader — Shader tô viền

---

## 10. KẾT THÚC GAME (ENDING SEQUENCE)

### Điều phối

- [ ] EndingSequenceManager — Điều phối toàn bộ kết thúc

### Các bước tuần tự

1. [ ] Quest 12 hoàn thành → Journal popup cuối cùng xuất hiện
2. [ ] Chờ journal đóng
3. [ ] BombExplosionCutscene — Cảnh nổ bom:
   - [ ] Temporary Camera (camera tạm thời)
   - [ ] Nuke VFX (hiệu ứng nổ hạt nhân)
   - [ ] Explosion SFX (âm thanh nổ)
   - [ ] Fade to Black → Fade from Black
4. [ ] EpilogueSlide — Slide kết:
   - [ ] Text: "Dịch bệnh đã được kiểm soát..."
5. [ ] CreditsSequence — Credit cuộn:
   - [ ] Dev Team (thành viên nhóm)
   - [ ] School (trường)
   - [ ] Members
   - [ ] Resources (tài nguyên sử dụng)
   - [ ] Engine (Cowsins Engine)
   - [ ] Thanks
   - [ ] Logo trường
6. [ ] Load Main Menu

---

## 11. GAME OVER / DEATH

### Màn hình Game Over

- [ ] GameOverManager — Quản lý màn hình Game Over
- [ ] Stats Display:
  - [ ] Story mode: Chapter, quests completed, journals, score
  - [ ] Wave mode: Score, wave, kills, best score
- [ ] Tùy chọn:
  - [ ] Restart from Checkpoint (respawn tại SaveRoom gần nhất)
  - [ ] Main Menu
  - [ ] Quit

---

## 12. ACHIEVEMENTS (5)

| # | ID | Tên | Điều kiện |
|---|---|---|---|
| [ ] | 1 | Speedrunner | Hoàn thành 5 chương trong <11 phút |
| [ ] | 2 | Hell Slayer | 130 Crook kills |
| [ ] | 3 | At Ease, Cooper | 20 kills khi wall run |
| [ ] | 4 | Tank Slayer | Kill Tank đầu tiên |
| [ ] | 5 | Close Call | Trong bán kính 5m khi Boomer nổ |

---

## 13. PLAYFAB CLOUD INTEGRATION

- [ ] PlayFabManager — Quản lý kết nối
- [ ] Register / Login — Đăng ký / Đăng nhập
- [ ] Cloud Save:
  - [ ] Best Score
  - [ ] Best Wave
  - [ ] Achievement unlock state
- [ ] Auto-save — 60 giây + khi quit/pause
- [ ] Leaderboard — BestScore statistic
- [ ] Cloud Data Merge — Hợp nhất dữ liệu đa thiết bị

---

## 14. HIỆU ỨNG & VFX

- [ ] Muzzle Flash — Lửa nòng súng (Rifle/Rocket/Turret)
- [ ] Bullet Holes — Lỗ đạn (Wood/Metal/Mud/Grass/Slash)
- [ ] Speed Lines — Vạch tốc độ khi chạy nhanh
- [ ] Loot Trail — Vệt sáng loot
- [ ] Loot Pop — Hiệu ứng nảy vật phẩm
- [ ] Acid Pool — Vũng acid Boomer
- [ ] Boomer Explosion — Vụ nổ Boomer
- [ ] Tank Jump Attack — Hiệu ứng nhảy của Tank
- [ ] Camera Shake — Rung camera
- [ ] Low Health Vignette — Viền đỏ máu thấp
- [ ] Damage Direction — Chỉ thị hướng đòn
- [ ] Hitmarker — Dấu trúng đạn
- [ ] Nuke VFX — Hiệu ứng nổ hạt nhân (ending)
- [ ] Outline — Viền vật phẩm tương tác
- [ ] Collectible Highlight — Highlight nhật ký

---

## 15. ÂM THANH (AUDIO)

- [ ] Footsteps — Bước chân (theo bề mặt)
- [ ] Weapon Sounds — Âm vũ khí (bắn, nạp, trang bị)
- [ ] Zombie Sounds — Âm zombie (gầm, tấn công, hit, chết)
- [ ] Boss Sounds — Âm boss (gầm, rít, tấn công, nhảy, chết)
- [ ] Boomer Scream — Tiếng kêu Boomer sắp nổ
- [ ] Tank Spawn Roar — Gầm Tank toàn map (2D)
- [ ] UI Sounds — Âm giao diện (nút bấm)
- [ ] Journal Voice Logs — Giọng đọc nhật ký
- [ ] Flashlight Toggle — Âm bật/tắt đèn
- [ ] Explosion SFX — Âm nổ
- [ ] Audio Mixer — Mixer groups cho volume

---

## 16. THÀNH PHẦN KỸ THUẬT NỀN

### Scenes (Game)

| Scene | Đường dẫn |
|---|---|
| Main Menu | `Assets/Scenes/MainMenu.unity` |
| Story Mode | `Assets/Scenes/Play Scene (Story).unity` |
| Sample (test) | `Assets/Scenes/SampleScene.unity` |

### Script tổng cộng: 80+

#### Enemy Scripts

| Script | File |
|---|---|
| ZombieAI | `Assets/Script/Test/ZombieAI.cs` |
| BoomerAI | `Assets/Script/Test/BoomerAI.cs` |
| BigGuyAI | `Assets/Script/Test/BigGuyAI.cs` |
| TankAI | `Assets/Script/Test/TankAI.cs` |
| WitchAI | `Assets/Script/Test/WitchAI.cs` |
| CeilingZombieController | `Assets/Script/Test/CeilingZombieController.cs` |
| EnemyLocomotion | `Assets/Script/Test/EnemyLocomotion.cs` |
| EnemyHealthBar | `Assets/Script/Test/EnemyHealthBar.cs` |
| SetLayerWeightBehaviour | `Assets/Script/Test/SetLayerWeightBehaviour.cs` |

#### Manager Scripts

| Script | File |
|---|---|
| StoryManager | `Assets/Script/Test/Managers/StoryManager.cs` |
| WaveManager | `Assets/Script/Test/Managers/WaveManager.cs` |
| AIDirector | `Assets/Script/Test/Managers/AIDirector.cs` |
| ScoreManager | `Assets/Script/Test/Managers/ScoreManager.cs` |
| CollectibleManager | `Assets/Script/Test/Managers/CollectibleManager.cs` |
| SideQuestManager | `Assets/Script/Test/Managers/SideQuestManager.cs` |
| DayNightCycle | `Assets/Script/Test/Managers/DayNightCycle.cs` |
| AchievementManager | `Assets/Script/Test/Managers/AchievementManager.cs` |
| PlayerStatsTracker | `Assets/Script/Test/Managers/PlayerStatsTracker.cs` |
| GameOverManager | `Assets/Script/Test/Managers/GameOverManager.cs` |
| PauseManager | `Assets/Script/Test/Managers/PauseManager.cs` |
| PlayFabManager | `Assets/Script/Test/Managers/PlayFabManager.cs` |
| PlayerFlashlight | `Assets/Script/Test/Managers/PlayerFlashlight.cs` |
| ChapterBoundary | `Assets/Script/Test/Managers/ChapterBoundary.cs` |
| SaveRoom | `Assets/Script/Test/Managers/SaveRoom.cs` |
| SpawnOnPlayerEnter | `Assets/Script/Test/Managers/SpawnOnPlayerEnter.cs` |
| SpawnOnQuestEvent | `Assets/Script/Test/Managers/SpawnOnQuestEvent.cs` |
| MovementSkillSystem | `Assets/Script/Test/Managers/MovementSkillSystem.cs` |
| AimSkillSystem | `Assets/Script/Test/Managers/AimSkillSystem.cs` |
| IntelligenceSkillSystem | `Assets/Script/Test/Managers/IntelligenceSkillSystem.cs` |
| SkillTreeManager | `Assets/Script/Test/Managers/SkillTreeManager.cs` |
| PlayerUpgradeManager | `Assets/Script/Test/Managers/PlayerUpgradeManager.cs` |
| CutscenePlayer | `Assets/Script/Test/Managers/CutscenePlayer.cs` |
| BombExplosionCutscene | `Assets/Script/Test/Managers/BombExplosionCutscene.cs` |
| EpilogueSlide | `Assets/Script/Test/Managers/EpilogueSlide.cs` |
| CreditsSequence | `Assets/Script/Test/Managers/CreditsSequence.cs` |
| EndingSequenceManager | `Assets/Script/Test/Managers/EndingSequenceManager.cs` |
| WaveQuestInteractable | `Assets/Script/Test/Managers/WaveQuestInteractable.cs` |
| QuestInteractable | `Assets/Script/Test/Managers/QuestInteractable.cs` |

#### ScriptableObjects (Quests, SideQuests, Journals, Achievements)

| Loại | Số lượng |
|---|---|
| QuestData | 12+ |
| SideQuestData | 8 |
| JournalData | 39 |
| AchievementData | 5 |

### Cowsins Engine Scripts (kế thừa)

| Nhóm | Chức năng |
|---|---|
| WeaponController | Điều khiển vũ khí |
| Weapon_SO | ScriptableObject vũ khí |
| WeaponAnimator | Animation vũ khí |
| Bullet | Đạn |
| ShootStyles | Hitscan/Projectile/Melee/Custom |
| Attachments | Barrel/Grip/Scope/Stock/Laser/Flashlight/Magazine |
| BasicMovement | Di chuyển cơ bản |
| CameraLook | Xoay camera |
| Jump | Nhảy |
| Dash | Lướt |
| CrouchSlide | Ngồi/trượt |
| WallRun | Chạy tường |
| WallBounce | Bật tường |
| GrapplingHook | Móc câu |
| ClimbLadder | Leo thang |
| Stamina | Thể lực |
| CameraFOVManager | Góc nhìn camera |
| UIController | Giao diện Cowsins |
| EnemyHealth | Máu kẻ địch |
| IDamageable | Interface sát thương |
| Interactable | Vật tương tác |
| DoorInteractable | Cửa |
| ExplosiveBarrel | Thùng nổ |
| Healthpack | Máu |
| Coin | Xu |
| Experience | EXP |
| PowerUp | Nâng cấp |

### Editor Scripts

| Script | File |
|---|---|
| StoryChapter1Builder | `Assets/Editor/StoryChapter1Builder.cs` |
| StoryChapter2Builder | `Assets/Editor/StoryChapter2Builder.cs` |
| StoryChapter3Builder | `Assets/Editor/StoryChapter3Builder.cs` |
| StoryChapter4Builder | `Assets/Editor/StoryChapter4Builder.cs` |
| StoryChapter5Builder | `Assets/Editor/StoryChapter5Builder.cs` |
| StoryMapDecorator | `Assets/Editor/StoryMapDecorator.cs` |
| StoryChapter4Decorator | `Assets/Editor/StoryChapter4Decorator.cs` |
| StoryChapter5Decorator | `Assets/Editor/StoryChapter5Decorator.cs` |
| StoryBarrelCratePlacer | `Assets/Editor/StoryBarrelCratePlacer.cs` |
| SideQuestAssetBuilder | `Assets/Editor/SideQuestAssetBuilder.cs` |
| OcclusionSetup | `Assets/Editor/OcclusionSetup.cs` |
| OcclusionBaker | `Assets/Editor/OcclusionBaker.cs` |
| CleanInstancedMaterials | `Assets/Editor/CleanInstancedMaterials.cs` |
| ProceduralUISpriteGenerator | `Assets/Editor/ProceduralUISpriteGenerator.cs` |

### Animations

| Controller | File |
|---|---|
| Zombie.controller | `Assets/Animation/Zombie/` |
| Zombie 2.controller | `Assets/Animation/Zombie/` |
| Tank.controller | Boss Tank |
| Boomer.controller | Boss Boomer |
| BigGuy.controller | Boss BigGuy |
| Witch.controller | Boss Witch |
| Hooker.controller | Boss Hooker |
| Snatcher.controller | Boss Snatcher |
| Rifle / SMG / Shotgun / Rocket / Pistol / Revolver / Katana / BurstRifle / Turret | Weapon controllers |

### Models (FBX)

| Nhóm | Vị trí |
|---|---|
| Zombie Mixamo | `Assets/Animation/Zombie/` |
| Tank Boss | `Assets/Animation/Zombie/Boss/Tank/` |
| Boomer | `Assets/Animation/Zombie/Boss/Boomer/` |
| Synty Rigs | `Assets/Animation/Synty model for Mixamo/` |
| Environment Models | `Assets/Map/PolygonApocalypse/Models/` |

### Prefabs

| Loại | Số lượng |
|---|---|
| Crook Zombie variants | 28+ |
| Bosses | 6 (Tank, Boomer, Witch, BigGuy, Snatcher, Hooker) |
| Props | Nhiều (từ Polygon Apocalypse) |
| Journal | 1 (spawn nhiều bản) |
| AcidPool | 2 (normal + bigger) |
| MuzzleFlashes | 3+ |

### Shaders

| Shader | File |
|---|---|
| OutlineMask | `Assets/Resources/Shaders/OutlineMask.shader` |
| OutlineFill | `Assets/Resources/Shaders/OutlineFill.shader` |
| SkyGradient | `Assets/Map/.../SkyGradient.shader` |
| POLYGON_Triplanar | Environment shader |
| POLYGON_Zombies | Zombie shader |
| POLYGON_ZombieBoss | Boss shader |
| WorldSpace / UIBlur | Cowsins shaders |
| TMP Shaders | 19 variants |

### Tổng kết số liệu dự án

| Hạng mục | Số lượng |
|---|---|
| **Scene game** | 3 |
| **Script C#** | 80+ |
| **Chương** | 5 |
| **Main Quest** | 12+ |
| **Side Quest** | 8 |
| **Journal** | 39 |
| **Loại zombie** | 7 |
| **Loại vũ khí** | 9+ |
| **Achievement** | 5 |
| **Node skill tree** | 15 |
| **Prefab zombie** | 28+ |
| **Editor script** | 14 |
| **PlayFab service** | 5 tính năng |
| **Animation controller** | 15+ |
