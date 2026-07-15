# 📦 DANH SÁCH ĐẦY ĐỦ TOÀN BỘ DỰ ÁN — FPS SURVIVAL ZOMBIE
## Theo trình tự chơi từ đầu đến kết thúc game

---

## 1. MAIN MENU

### Scene: `Assets/Scenes/MainMenu.unity`

- [ ] Menu Camera Orbit — Camera xoay vòng ở menu
- [ ] MainMenuManager — Quản lý nút Play / Quit
- [ ] Best Score Display — Hiển thị điểm cao nhất
- [ ] PlayFab Login UI — Giao diện đăng nhập (Username/Password)

---

## 2. HỆ THỐNG NỀN TẢNG (KHỞI TẠO CÙNG GAME)

### Core Managers

- [ ] StoryManager — Điều phối cốt truyện 5 chương
- [ ] AIDirector — Độ khó động (Calm/Buildup/Attack/Recovery)
- [ ] WaveManager — Quản lý wave (base 10 + wave × 5)
- [ ] ScoreManager — Tính điểm
- [ ] CollectibleManager — Quản lý 39 journal
- [ ] SideQuestManager — Quản lý 8 side quest
- [ ] AchievementManager — 5 achievements
- [ ] PlayerStatsTracker — Thống kê chỉ số người chơi
- [ ] PauseManager — Menu tạm dừng
- [ ] PlayFabManager — Kết nối PlayFab cloud
- [ ] DayNightCycle — Chu kỳ ngày/đêm
- [ ] SkillTreeManager — Cây kỹ năng 3 nhánh, 15 node
- [ ] PlayerUpgradeManager — Áp dụng nâng cấp vào player

---

## 3. SKILL TREE (MỞ TỪ ĐẦU GAME)

### 3 nhánh — 15 node — mở khóa dần bằng điểm kỹ năng

#### Nhánh Movement (5 node)
- [ ] Node 1: Walk Speed (2 SP) — +Stamina
- [ ] Node 2: Run Speed (3 SP) — +Stamina
- [ ] Node 3: Air Control + **Dash** (5 SP) — +Stamina
- [ ] Node 4: **Wall Run / Wall Bounce** (8 SP) — +Stamina
- [ ] Node 5: **Double Jump + Grappling Hook** (12 SP) — +Stamina

#### Nhánh Aim (5 node)
- [ ] Node 1: Recoil Reduction (2 SP) — +Damage
- [ ] Node 2: Crit Chance 10% (3 SP) — +Damage
- [ ] Node 3: Crit Chance 20% (5 SP) — +Damage
- [ ] Node 4: Crit Damage x1.5 (8 SP) — +Damage
- [ ] Node 5: **One-shot Crook + Bonus vs Specials** (12 SP) — +Damage

#### Nhánh Intelligence (5 node)
- [ ] Node 1: XP Pickup Radius (2 SP) — +HP
- [ ] Node 2: XP x1.1 (3 SP) — +HP
- [ ] Node 3: Radius +10 (5 SP) — +HP
- [ ] Node 4: XP x1.15 (8 SP) — +HP
- [ ] Node 5: Radius +15 + **Highlight journal** (12 SP) — +HP

---

## 4. PLAYER & ĐIỀU KHIỂN

### Di chuyển cơ bản (có sẵn từ đầu)
- [ ] Walk / Run — Đi bộ / Chạy
- [ ] Crouch / Slide — Ngồi / Trượt
- [ ] Jump — Nhảy
- [ ] Climb Ladder — Leo thang
- [ ] Stamina — Hệ thống thể lực

### Kỹ năng mở khóa qua Skill Tree
- [ ] **Dash** — Node 3 Movement
- [ ] **Wall Run / Wall Bounce** — Node 4 Movement
- [ ] **Double Jump** — Node 5 Movement
- [ ] **Grappling Hook** — Node 5 Movement

### Camera
- [ ] Camera FOV Manager — Góc nhìn thay đổi
- [ ] Camera Shake — Rung camera khi bị đòn
- [ ] Speed Lines — Vạch tốc độ khi chạy nhanh
- [ ] Camera Animations — Hiệu ứng camera

### Khác
- [ ] Player Flashlight — Đèn pin (phím F)

---

## 5. VŨ KHÍ (THU THẬP DẦN QUA 5 CHƯƠNG)

### Danh sách 8 vũ khí

| # | Vũ khí | Chương tìm thấy | Đặc điểm |
|---|---|---|---|
| 1 | **Pistol** | Chương 1 | Súng lục cơ bản |
| 2 | **Rifle** | Chương 2 | Súng trường, ổn định |
| 3 | **SMG** | Chương 3 | Tốc độ bắn cao |
| 4 | **Shotgun** | Chương 3 | Sát thương gần |
| 5 | **Rocket Launcher** | Chương 4 | Nổ diện rộng |
| 6 | **Revolver** | Chương 4 | Sát thương cao, đạn ít |
| 7 | **Katana** | Chương 4 | Cận chiến |
| 8 | **Burst Rifle** | Chương 4 | Bắn 3 phát |

### Cơ chế vũ khí (chung)
- [ ] WeaponController — Điều khiển vũ khí
- [ ] Weapon ScriptableObject — Định nghĩa từng loại
- [ ] ADS — Ngắm bắn
- [ ] Reload — Nạp đạn (magazine-based)
- [ ] Recoil — Độ giật
- [ ] Spread — Độ xoáy đạn
- [ ] Muzzle Flash — Lửa nòng (Rifle/Rocket)
- [ ] Bullet Holes — Lỗ đạn (Wood/Metal/Mud/Grass)
- [ ] Hitmarker — Dấu trúng đích

---

## 6. ENEMY & AI

### Crook — Zombie thường (từ Chương 1)
- [ ] 28+ biến thể ngoại hình (Biker, Clown, Cop, Bride, Cheerleader...)
- [ ] Chiều cao ngẫu nhiên 1.5-2m
- [ ] NavMesh pathfinding
- [ ] Phát hiện qua tầm nhìn + khoảng cách (LOS)
- [ ] Lunge/feint — hành vi giả vờ tấn công
- [ ] Wander — đi lang thang khi mất dấu
- [ ] Headshot / Critical hit riêng
- [ ] Stuck detection — tự thoát khi kẹt

### Boomer (từ Chương 3, wave 3+)
- [ ] 100 HP
- [ ] Kêu rít cảnh báo → lao → phát nổ
- [ ] Sát thương vùng
- [ ] Để lại vũng acid sát thương liên tục
- [ ] Có thể bắn chết từ xa

### Tank (từ Chương 4, wave 5+)
- [ ] 500+ HP (scale theo wave)
- [ ] 3 đòn: Punch, Swipe, Jump Attack
- [ ] Gầm toàn map cảnh báo
- [ ] Boss Health Bar trên HUD

### Witch (Chương 5)
- [ ] 60 HP
- [ ] Ngồi khóc tại chỗ
- [ ] Kích động → lao 6.5 m/s
- [ ] Mất dấu → quay lại khóc

### Big Guy (Chương 5)
- [ ] 80 HP
- [ ] Đứng choáng tại chỗ
- [ ] Kích động → gầm → rượt chậm
- [ ] Đòn đấm sát thương cao

### Enemies phụ
- [ ] Ceiling Zombie — Rơi từ trần nhà
- [ ] Snatcher — Enemy đặc biệt
- [ ] Hooker — Enemy đặc biệt

### Hệ thống AI chung
- [ ] EnemyLocomotion — NavMesh + LOS + stuck recovery
- [ ] EnemyHealthBar — Thanh máu thế giới
- [ ] AIDirector — 4 trạng thái, camper punish
- [ ] Spawm.cs — Spawner: Object Pooling (60 con), NavMesh validated

---

## 7. LOOT SYSTEM (HOẠT ĐỘNG TỪ ĐẦU GAME)

- [ ] LootDropHelper — Roll loot từ bảng
- [ ] LootPop — Hiệu ứng nảy
- [ ] LootTrail — Vệt sáng
- [ ] Coin — Xu
- [ ] Experience — EXP
- [ ] Healthpack — Máu
- [ ] Ammo pickups — Đạn
- [ ] PowerUp — Nâng cấp tạm thời

---

## 8. HUD & GIAO DIỆN NGƯỜI CHƠI

### Scene: `Assets/Scenes/Play Scene (Story).unity`

### Widgets
- [ ] Health Widget — Thanh máu
- [ ] Low Health Vignette — Viền đỏ khi máu thấp
- [ ] Health Flash — Hiệu ứng nhấp nháy khi bị đòn
- [ ] Stamina Widget — Thanh thể lực
- [ ] Ammo Widget — Đếm đạn
- [ ] Dash Cooldown Widget — Hồi chiêu dash
- [ ] Reload Indicator — Chỉ báo đang nạp đạn
- [ ] Crosshair Widget — Tâm ngắm (độ xoáy động)
- [ ] Compass Widget — La bàn
- [ ] Compass Marker — Điểm đánh dấu
- [ ] Damage Direction HUD — Chỉ hướng bị tấn công
- [ ] Combat Feedback HUD — Số DMG + kill feed
- [ ] FPS Widget — Chỉ số FPS
- [ ] Threat Widget — Mức đe dọa
- [ ] Progression HUD — Thanh kinh nghiệm
- [ ] Quest Tracker Widget — Theo dõi nhiệm vụ
- [ ] Quest Beacon — Điểm đánh dấu mục tiêu
- [ ] Interact Prompt Widget — "Nhấn E"
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

## 9. CHƯƠNG 1 — BÌNH MINH (DAWN)

### Thời gian: 6h — DayNightCycle: Dawn keyframe

### Thành phần
- [ ] ChapterBoundary — Ranh giới chương
- [ ] SaveRoom (Checkpoint 1) — Checkpoint + hồi máu
- [ ] Spawner (Spawm.cs) — Spawn zombie
- [ ] QuestTrigger — Trigger volume
- [ ] KillCountObjective — Mục tiêu tiêu diệt
- [ ] CollectibleQuestObjective — Mục tiêu thu thập
- [ ] QuestInteractable — Vật phẩm tương tác
- [ ] CutscenePlayer — Cutscene chuyển chương
- [ ] Pistol — Vũ khí duy nhất

### Loot System hoạt động
- [ ] Zombie chết rơi Coin, EXP, Healthpack
- [ ] LootPop, LootTrail

### Kết thúc chương
- [ ] SaveRoom trigger → Cutscene "CHƯƠNG 2 — BỆNH VIỆN"
- [ ] DayNightCycle → Noon (12h)

---

## 10. CHƯƠNG 2 — BUỔI TRƯA (NOON)

### Thời gian: 12h — DayNightCycle: Noon keyframe

### Thành phần
- [ ] ChapterBoundary mới
- [ ] SaveRoom (Checkpoint 2)
- [ ] Spawner bệnh viện
- [ ] Quest mới (cốt truyện bệnh viện)
- [ ] **Nâng Skill Tree lần đầu** — đủ EXP để upgrade

### Vũ khí
- [ ] **Rifle** — vũ khí thứ 2

### Side Quest
- [ ] Church
- [ ] Auto Repair
- [ ] Motel
- [ ] Quarantine
- [ ] HighRise Base
- [ ] Mother's Story
- [ ] Lighthouse

### Journal
- [ ] Soldier Journal #1-3
- [ ] Neighbor Journal #1-21 (một phần)
- [ ] Military Record #1-8
- [ ] Doctor Journal #1-3

### Kết thúc chương
- [ ] SaveRoom → Cutscene "CHƯƠNG 3 — CÔNG TRƯỜNG"
- [ ] DayNightCycle → Dusk (18h)

---

## 11. CHƯƠNG 3 — HOÀNG HÔN (DUSK)

### Thời gian: 18h — DayNightCycle: Dusk keyframe

### Kỹ năng đã mở khóa (nếu nâng Movement)
- [ ] Air Control
- [ ] **Dash**

### Vũ khí mới
- [ ] **SMG**
- [ ] **Shotgun**

### Wave System
- [ ] WaveManager — Wave đầu 10 kill, +5 mỗi wave
- [ ] WaveQuestInteractable — Khóa vùng, sống sót N wave
- [ ] Boundary lock + teleport-back
- [ ] Wave Announcer

### SpecialEnemyDirector
- [ ] Boomer từ wave 3+

### Enemies phụ
- [ ] Ceiling Zombie
- [ ] Snatcher
- [ ] Hooker

### Journal
- [ ] Experiment Report #1-3
- [ ] Cure Record #1-4

### Kết thúc chương
- [ ] SaveRoom → Cutscene "CHƯƠNG 4 — KHU DÂN CƯ"
- [ ] DayNightCycle → Night (22h)

---

## 12. CHƯƠNG 4 — MÀN ĐÊM (NIGHT)

### Thời gian: 22h — DayNightCycle: Night keyframe

### Kỹ năng cao cấp (nếu nâng đủ)
- [ ] **Wall Run + Wall Bounce**
- [ ] **Double Jump + Grappling Hook**
- [ ] Aim cuối: One-shot Crook + Bonus Specials
- [ ] Intel cuối: Highlight journal (Outline shader)
- [ ] Outline — viền phát sáng vật phẩm

### Vũ khí mới
- [ ] **Rocket Launcher**
- [ ] **Revolver**
- [ ] **Katana**
- [ ] **Burst Rifle**

### Tank Boss
- [ ] 500+ HP (scale wave)
- [ ] Punch / Swipe / Jump Attack
- [ ] Gầm toàn map
- [ ] Boss Health Bar

### Journal
- [ ] Brother Journal #1-5

### Kết thúc chương
- [ ] SaveRoom → Cutscene "CHƯƠNG 5 — CHUNG CƯ"
- [ ] DayNightCycle → Deep Night (2h)

---

## 13. CHƯƠNG 5 — ĐÊM KHUYA (DEEP NIGHT)

### Thời gian: 2h sáng — DayNightCycle: Deep Night keyframe

### Full arsenal
- [ ] Đầy đủ 8 vũ khí: Pistol, Rifle, SMG, Shotgun, Rocket, Revolver, Katana, Burst Rifle
- [ ] Flashlight thiết yếu

### Mini-boss
- [ ] Witch — 60 HP, khóc → lao 6.5 m/s
- [ ] Big Guy — 80 HP, choáng → gầm → rượt

### Quest cuối
- [ ] Quest 12: "Escape Town"
- [ ] WaveQuestInteractable cuối:
  - [ ] Sequential waves
  - [ ] Tank boss trong wave cuối
  - [ ] Boundary lock
  - [ ] Kích hoạt bom

---

## 14. KẾT THÚC GAME (ENDING SEQUENCE)

### Điều phối
- [ ] EndingSequenceManager

### Các bước
1. [ ] Quest 12 hoàn thành → journal popup cuối
2. [ ] BombExplosionCutscene
   - [ ] Temporary Camera
   - [ ] Nuke VFX
   - [ ] Explosion SFX
   - [ ] Fade to Black → Fade from Black
3. [ ] EpilogueSlide: "Dịch bệnh đã được kiểm soát..."
4. [ ] CreditsSequence
   - [ ] Dev Team
   - [ ] School
   - [ ] Resources
   - [ ] Engine (Cowsins)
   - [ ] Thanks
5. [ ] Load Main Menu

---

## 15. GAME OVER / DEATH

- [ ] GameOverManager
- [ ] Stats: Story (chapter, quest, journal, score) / Wave (score, wave, kills, best score)
- [ ] Restart from Checkpoint
- [ ] Main Menu
- [ ] Quit

---

## 16. ACHIEVEMENTS (5)

| # | ID | Tên | Điều kiện |
|---|---|---|---|
| [ ] | 1 | Speedrunner | 5 chương <11 phút |
| [ ] | 2 | Hell Slayer | 130 Crook kills |
| [ ] | 3 | At Ease, Cooper | 20 kills khi wall run |
| [ ] | 4 | Tank Slayer | Hạ Tank đầu tiên |
| [ ] | 5 | Close Call | Trong 5m Boomer nổ |

---

## 17. PLAYFAB CLOUD INTEGRATION

- [ ] PlayFabManager
- [ ] Register / Login
- [ ] Cloud Save: Best Score, Best Wave, Achievement state
- [ ] Auto-save — 60 giây + khi quit/pause
- [ ] Leaderboard — BestScore statistic
- [ ] Cloud Data Merge

---

## 18. HIỆU ỨNG & VFX

- [ ] Muzzle Flash — Lửa nòng súng
- [ ] Bullet Holes — Lỗ đạn (Wood/Metal/Mud/Grass)
- [ ] Speed Lines — Vạch tốc độ
- [ ] Loot Trail — Vệt sáng loot
- [ ] Loot Pop — Hiệu ứng nảy
- [ ] Acid Pool — Vũng acid Boomer
- [ ] Boomer Explosion — Vụ nổ Boomer
- [ ] Tank Jump Attack — Hiệu ứng nhảy
- [ ] Camera Shake — Rung camera
- [ ] Low Health Vignette — Viền đỏ
- [ ] Health Flash — Nhấp nháy khi bị đòn
- [ ] Damage Direction — Chỉ thị hướng đòn
- [ ] Hitmarker — Dấu trúng đạn
- [ ] Nuke VFX — Hiệu ứng nổ hạt nhân (ending)
- [ ] Outline — Viền highlight journal

---

## 19. ÂM THANH (AUDIO)

- [ ] Footsteps — Bước chân theo bề mặt
- [ ] Weapon Sounds — Bắn, nạp, trang bị
- [ ] Zombie Sounds — Gầm, tấn công, hit, chết
- [ ] Boss Sounds — Tank, Boomer, Witch, Big Guy
- [ ] Boomer Scream — Tiếng kêu sắp nổ
- [ ] Tank Spawn Roar — Gầm toàn map (2D)
- [ ] UI Sounds — Nút bấm
- [ ] Journal Voice Logs — Giọng đọc nhật ký
- [ ] Flashlight Toggle — Bật/tắt đèn
- [ ] Explosion SFX — Âm nổ
- [ ] Audio Mixer — Groups cho volume

---

## 20. THÀNH PHẦN KỸ THUẬT

### Scenes
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

#### ScriptableObjects
| Loại | Số lượng |
|---|---|
| QuestData | 12+ |
| SideQuestData | 8 |
| JournalData | 39 |
| AchievementData | 5 |

#### Cowsins Engine Scripts (kế thừa)
| Nhóm | Chức năng |
|---|---|
| WeaponController | Điều khiển vũ khí |
| Weapon_SO | ScriptableObject vũ khí |
| WeaponAnimator | Animation vũ khí |
| Bullet | Đạn |
| ShootStyles | Hitscan/Projectile/Melee/Custom |
| BasicMovement | Di chuyển cơ bản |
| CameraLook | Xoay camera |
| Jump | Nhảy |
| CrouchSlide | Ngồi/trượt |
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
| StoryChapter1-5Builder | `Assets/Editor/StoryChapter{1-5}Builder.cs` |
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
| Rifle / SMG / Shotgun / Rocket / Pistol / Revolver / Katana / BurstRifle | Weapon controllers |

### Prefabs
| Loại | Số lượng |
|---|---|
| Crook Zombie variants | 28+ |
| Bosses | 6 (Tank, Boomer, Witch, BigGuy, Snatcher, Hooker) |
| Journal | 1 (spawn nhiều bản) |
| AcidPool | 2 (normal + bigger) |
| MuzzleFlashes | 3+ |

### Shaders
| Shader | File |
|---|---|
| OutlineMask | `Assets/Resources/Shaders/OutlineMask.shader` |
| OutlineFill | `Assets/Resources/Shaders/OutlineFill.shader` |
| SkyGradient | Environment shader |
| POLYGON_Triplanar | Environment shader |
| POLYGON_Zombies | Zombie shader |
| POLYGON_ZombieBoss | Boss shader |
| WorldSpace / UIBlur | Cowsins shaders |
| TMP Shaders | 19 variants |

---

## TỔNG KẾT SỐ LIỆU DỰ ÁN

| Hạng mục | Số lượng |
|---|---|
| **Scene** | 3 |
| **Script C#** | 80+ |
| **Chương** | 5 |
| **Main Quest** | 12+ |
| **Side Quest** | 8 |
| **Journal** | 39 (7 nhóm) |
| **Loại zombie** | 7 (Crook, Boomer, Tank, Witch, BigGuy, Snatcher, Hooker) |
| **Loại vũ khí** | 8 (thu thập dần) |
| **Skill tree node** | 15 (3 nhánh) |
| **Kỹ năng mở khóa** | Dash, Wall Run, Double Jump, Grapple, One-shot, Highlight |
| **Achievement** | 5 |
| **Editor script** | 14 |
| **PlayFab service** | 5 tính năng |
| **Animation controller** | 15+ |
| **Prefab zombie** | 28+ |
