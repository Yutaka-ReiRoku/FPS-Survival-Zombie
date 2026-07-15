# 📋 FLOW TRÌNH BÀY & CHECKLIST THUYẾT TRÌNH — DỰ ÁN FPS SURVIVAL ZOMBIE

---

## 🎯 PHẦN 1: GIỚI THIỆU (1-2 phút)

**Màn hình: Main Menu**

**Script:**
> "Xin chào thầy cô và các bạn. Nhóm em xin trình bày đồ án: **FPS Survival Zombie** – game bắn súng góc nhìn thứ nhất kết hợp sinh tồn, bối cảnh ngày tận thế zombie.
>
> Game có cốt truyện 5 chương từ bình minh đến đêm khuya. Người chơi chiến đấu, thu thập vũ khí qua từng chương, nâng cấp kỹ năng qua Skill Tree, tìm nhật ký và khám phá sự thật đằng sau đại dịch.
>
> Dự án phát triển trên Unity, Cowsins FPS Engine nền tảng, PlayFab cho dữ liệu đám mây.
>
> Sau đây nhóm em xin demo luồng chơi từ đầu đến cuối."

---

## 🎮 PHẦN 2: MAIN MENU (1 phút)

**Màn hình: Main Menu → click Play**

| Thành phần | Miêu tả |
|---|---|
| Nút Play | Bắt đầu game |
| Nút Quit | Thoát |
| Best Score | Điểm cao nhất |
| PlayFab Login | Đăng ký/đăng nhập |
| Leaderboard | Bảng xếp hạng |
| Auto-save | Tự động 60s |

**Script:**
> "Main Menu với các nút Play, Quit, Best Score. PlayFab Login để đăng nhập, đồng bộ dữ liệu lên cloud. Leaderboard và auto-save 60 giây."

---

## 🌅 PHẦN 3: CHƯƠNG 1 — BÌNH MINH (6 phút)

### 3.1 — Môi trường & hệ thống

| Thành phần | Chi tiết |
|---|---|
| DayNight Cycle | Dawn (6h): mặt trời, bầu trời, sương mù, PostProcess |
| SaveRoom | Vùng an toàn, hồi máu, checkpoint |
| StoryManager | Kích hoạt quest đầu |
| QuestTracker HUD | Hiển thị mục tiêu |
| ChapterBoundary | Ranh giới chương |

**Script:**
> "Chương 1 — Bình Minh (6h). DayNight Cycle kiểm soát thời gian. SaveRoom là checkpoint và hồi máu. StoryManager + QuestTracker quản lý cốt truyện."

### 3.2 — Di chuyển cơ bản + Skill Tree giới thiệu

| Kỹ năng cơ bản | Walk, Run, Crouch, Slide, Jump, Climb Ladder |
|---|---|
| Stamina | Thanh thể lực dùng chung |

| Skill Tree | Chi tiết |
|---|---|
| Nhánh Movement | 5 node: Speed → Air Control → **Dash → Wall Run → Double Jump + Grapple** |
| Nhánh Aim | 5 node: Recoil → Crit 10% → 20% → x1.5 → **One-shot + Bonus Special** |
| Nhánh Intelligence | 5 node: XP Radius → x1.1 → Radius → x1.15 → **Highlight** |
| Chi phí | 2 → 3 → 5 → 8 → 12 SP mỗi nhánh |
| Bonus | +Stamina (Move), +Damage (Aim), +HP (Intel) |

**Script:**
> "Di chuyển cơ bản: walk, run, crouch, slide, jump, leo thang. Dùng chung Stamina.
>
> **Skill Tree có sẵn từ đầu game** — 3 nhánh, 15 node. Các kỹ năng như Dash, Wall Run, Double Jump, Grapple bị khóa, phải nâng cấp mới dùng được."

### 3.3 — Vũ khí: Pistol

| Vũ khí | Pistol (khẩu duy nhất) |
|---|---|
| Cơ chế | ADS, Reload, Recoil, Spread, Muzzle Flash, Bullet Holes, Hitmarker |

**Script:**
> "Đầu game chỉ có Pistol. Các vũ khí khác tìm thấy ở chương sau."

### 3.4 — Zombie Crook + Loot

| Loại zombie | HP | Đặc điểm |
|---|---|---|
| Crook | 100 | 28+ biến thể, cao 1.5-2m |

| Hệ thống | Chi tiết |
|---|---|
| AI | NavMesh, LOS detection, lunge/feint, wander, headshot |
| AIDirector | 4 trạng thái (Calm → BuildUp → Attack → Recovery), camper punishment |
| Spawner | Object Pooling (60 con), NavMesh validation |
| Loot System | Coin, EXP, Healthpack, LootPop, LootTrail — từ đầu game |

**Script:**
> "Crook — 28+ biến thể. AI dùng NavMesh, phát hiện qua tầm nhìn. AIDirector quản lý độ khó. Spawner dùng Object Pooling.
>
> **Loot System hoạt động từ đầu**: zombie chết rơi Coin, EXP, Healthpack — có hiệu ứng LootPop và LootTrail."

### 3.5 — HUD

| Widget | Chức năng |
|---|---|
| Health + Vignette | Máu + viền đỏ khi yếu |
| Stamina + Ammo + Dash Cooldown | Thể lực, đạn, hồi dash |
| Crosshair | Giãn theo spread |
| Compass + Quest Beacon | La bàn, điểm mục tiêu |
| Damage Direction + Combat Feedback | Hướng đòn, số DMG, kill feed |
| FPS + Threat Level + Interact Prompt | Chỉ số, mức đe dọa, tương tác |
| Boss Health Bar + Wave Announcer | Tự động xuất hiện |

### 3.6 — Quest & Cutscene

| Thành phần | Mô tả |
|---|---|
| 12+ main quest | Đến điểm / tiêu diệt / thu thập / tương tác |
| CutscenePlayer | Chuyển chương: "Chương 2 — Bệnh Viện" |

---

## ☀️ PHẦN 4: CHƯƠNG 2 — BUỔI TRƯA (3 phút)

### 4.1 — Nâng cấp Skill Tree lần đầu

| Hành động | Mô tả |
|---|---|
| Mở Skill Tree | Click upgrade node đầu tiên |
| PlayerUpgradeManager | Tự động áp dụng chỉ số |

**Script:**
> "Chương 2 — Buổi Trưa (12h). Đã đủ EXP, nâng skill tree lần đầu: tăng speed, giảm recoil, hoặc tăng XP radius."

### 4.2 — Vũ khí mới: Rifle

| Vũ khí thêm | Rifle |
|---|---|
| Tổng hiện có | Pistol + Rifle = 2 |

### 4.3 — Side Quest

| 8 Side Quest | Church, Auto Repair, Motel, Quarantine, HighRise Base, Mother's Story, Lighthouse |

### 4.4 — Journal

| Nhóm | Số lượng |
|---|---|
| Soldier / Neighbor / Military Record / Experiment Report / Doctor Journal / Cure Record / Brother Journal | 39 tổng cộng |

**Script:**
> "8 Side Quest mở khóa. 39 journal thu thập xuyên suốt — mỗi cái có hình, nội dung, voice log."

---

## 🏗️ PHẦN 5: CHƯƠNG 3 — HOÀNG HÔN (3 phút)

### 5.1 — Wave System

| Thành phần | Chi tiết |
|---|---|
| WaveManager | Wave đầu 10 kill, +5 mỗi wave |
| WaveQuestInteractable | Khóa vùng, sống sót N wave, teleport về |
| Wave Announcer | Thông báo HUD |

### 5.2 — Kỹ năng đã mở khóa

| Kỹ năng | Dash + Air Control (nếu nâng Movement) |

### 5.3 — Vũ khí mới: SMG + Shotgun

| Vũ khí thêm | SMG, Shotgun |
|---|---|
| Tổng hiện có | Pistol + Rifle + SMG + Shotgun = 4 |

### 5.4 — Boomer + SpecialEnemyDirector

| Zombie | Đặc điểm |
|---|---|
| Boomer | 100 HP, kêu rít → phát nổ → vũng acid |
| SpecialEnemyDirector | Spawn từ wave 3+, NavMesh check, scale stat |

### 5.5 — Enemies phụ

| Ceiling Zombie | Rơi từ trần |
|---|---|
| Snatcher, Hooker | Enemy đặc biệt |

---

## 🌃 PHẦN 6: CHƯƠNG 4 — MÀN ĐÊM (3 phút)

### 6.1 — Kỹ năng cao cấp

| Kỹ năng | Wall Run + Bounce → Double Jump + Grapple |
|---|---|
| Aim cuối | One-shot Crook + Bonus Specials |
| Intel cuối | Highlight journal (Outline) |

### 6.2 — Vũ khí mới: Rocket, Revolver, Katana, Burst Rifle

| Vũ khí thêm | Rocket Launcher, Revolver, Katana, Burst Rifle |
|---|---|
| Tổng hiện có | 8 (full arsenal) |

### 6.3 — Tank Boss

| Boss | Chi tiết |
|---|---|
| Tank | 500+ HP (scale wave). Punch / Swipe / Jump Attack. Gầm toàn map. Boss Health Bar |

---

## 🌆 PHẦN 7: CHƯƠNG 5 — ĐÊM KHUYA + KẾT THÚC (3 phút)

### 7.1 — Full 8 vũ khí

| Hiện trạng | Đầy đủ 8 vũ khí: Pistol, Rifle, SMG, Shotgun, Rocket, Revolver, Katana, Burst Rifle |

### 7.2 — Witch & Big Guy

| Mini-boss | HP | Đặc điểm |
|---|---|---|
| Witch | 60 | Ngồi khóc → lao 6.5 m/s |
| Big Guy | 80 | Choáng → gầm → rượt chậm, đấm mạnh |

### 7.3 — Quest cuối & Ending

| Bước | Mô tả |
|---|---|
| Quest 12 — Escape Town | Wave Quest cuối: khóa vùng → nhiều wave → Tank boss → kích hoạt bom |
| BombExplosionCutscene | Camera tạm, VFX nuke, SFX, Fade to Black |
| EpilogueSlide | "Dịch bệnh đã được kiểm soát..." |
| CreditsSequence | Credit cuộn → Main Menu |

---

## 🏆 PHẦN 8: GAME OVER + ACHIEVEMENT + PLAYFAB (1 phút)

### 8.1 — Game Over

| Thông tin | Chi tiết |
|---|---|
| Story mode | Chapter, quest, journal, score |
| Wave mode | Score, wave, kills, best score |
| 3 tùy chọn | Restart (checkpoint), Main Menu, Quit |

### 8.2 — Achievement (5)

| # | Tên | Điều kiện |
|---|---|---|
| 1 | Speedrunner | 5 chương <11 phút |
| 2 | Hell Slayer | 130 Crook kills |
| 3 | At Ease, Cooper | 20 kills wall run |
| 4 | Tank Slayer | Hạ Tank đầu |
| 5 | Close Call | Trong 5m Boomer nổ |

### 8.3 — PlayFab

| Tính năng | Mô tả |
|---|---|
| Register/Login | Username + Password |
| Cloud Save | Best score, wave, achievement |
| Auto-save | 60 giây/lần |
| Leaderboard | Xếp hạng toàn cầu |

---

## 🎬 PHẦN 9: KẾT LUẬN (1 phút)

| Hạng mục | Số lượng |
|---|---|
| Engine | Unity — C# |
| Chương | 5 |
| Main quest | 12+ |
| Side quest | 8 |
| Journal | 39 (7 nhóm) |
| Loại zombie | 7 |
| Vũ khí | 8 (thu thập dần) |
| Skill tree | 3 nhánh / 15 node |
| Achievement | 5 |
| Cloud | PlayFab |

**Script:**
> "Tóm lại: game FPS Survival hoàn chỉnh với cốt truyện 5 chương, 8 loại vũ khí thu thập dần, 7 loại zombie, skill tree 3 nhánh, 39 journal, kết thúc cutscene, tích hợp PlayFab.
>
> Xin cảm ơn thầy cô và các bạn!"

---

## ⏱ TỔNG THỜI GIAN DỰ KIẾN

| Phần | Thời gian |
|---|---|
| 1. Giới thiệu | 1.5 phút |
| 2. Main Menu | 1 phút |
| 3. Chương 1 (Bình Minh) | 6 phút |
| 4. Chương 2 (Buổi Trưa) | 3 phút |
| 5. Chương 3 (Hoàng Hôn) | 3 phút |
| 6. Chương 4 (Màn Đêm) | 3 phút |
| 7. Chương 5 (Đêm Khuya + Kết thúc) | 3 phút |
| 8. Game Over + Achievement + PlayFab | 1 phút |
| 9. Kết luận | 1 phút |
| **Tổng cộng** | **~20 phút** |
