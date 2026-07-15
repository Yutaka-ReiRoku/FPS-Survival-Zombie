# 🎬 KỊCH BẢN THUYẾT TRÌNH DỰ ÁN — FPS SURVIVAL ZOMBIE
## Trình bày theo cốt truyện — 5 chương — 20 phút

> **Cách dùng:**
> - `[A]/[B]` — người nói. Có thể chia cho người chơi game riêng
> - `[DEMO]` — thao tác trên game
> - `[CHỈ]` — chỉ tay vào màn hình
> - ⏱ Đồng hồ chạy — bám sát để không quá giờ

---

## ⏱ TỔNG QUAN THỜI GIAN

| Mốc | Phần | TG | Người nói |
|---|---|---|---|
| 0:00 | **Mở đầu** | 1' | A |
| 1:00 | **Main Menu → vào game** | 1' | A |
| 2:00 | **Chương 1 — Bình Minh** | 6' | A+B |
| 8:00 | **Chương 2 — Buổi Trưa** | 3.5' | A+B |
| 11:30 | **Chương 3 — Hoàng Hôn** | 3.5' | A+B |
| 15:00 | **Chương 4 — Màn Đêm** | 2.5' | A+B |
| 17:30 | **Chương 5 — Đêm Khuya + Kết thúc** | 2.5' | A+B |
| 20:00 | **Kết luận** | 1' | B |

---

## 🎬 MỞ ĐẦU (1 phút) ⏱ 0:00

**Màn hình: Main Menu**

---

**[A]:** "Kính thưa thầy cô và các bạn. Nhóm em xin trình bày đồ án **FPS Survival Zombie** — game bắn súng góc nhìn thứ nhất, lấy bối cảnh ngày tận thế zombie.

Trò chơi có **cốt truyện 5 chương** từ bình minh đến đêm khuya. Người chơi sẽ chiến đấu, **thu thập vũ khí qua từng chương**, **nâng cấp kỹ năng**, tìm nhật ký và khám phá sự thật đằng sau đại dịch.

Dự án phát triển trên **Unity**, sử dụng **Cowsins FPS Engine** và **PlayFab** cho dữ liệu đám mây.

Sau đây nhóm em xin demo toàn bộ luồng chơi."

---

## 🎮 MAIN MENU (1 phút) ⏱ 1:00

**[A]:** `[CHỈ]`

**PlayFab Login** — đăng ký, đăng nhập, đồng bộ dữ liệu cloud. **Leaderboard** — bảng xếp hạng. **Auto-save** 60 giây."

"**Main Menu**: nút **Play**, **Quit**, **Best Score**.

[CLICK Play — vào game]

---

## 🌅 CHƯƠNG 1 — BÌNH MINH (6 phút) ⏱ 2:00
### Dawn (6h) — Làm quen, vũ khí đầu tay, skill tree

---

### 1.1 — Môi trường & hệ thống (1 phút) ⏱ 2:00

**[B]:** "**Chương 1 — Bình Minh (6h)**."

`[CHỈ]`

"**DayNight Cycle** — chu kỳ ngày đêm. Điều khiển mặt trời, bầu trời, sương mù, ánh sáng.

**SaveRoom** — vùng an toàn hồi máu + **checkpoint**. Chết respawn tại đây.

**StoryManager** kích hoạt quest đầu — **Quest Tracker** góc phải. **ChapterBoundary** — ranh giới chương."

---

### 1.2 — Di chuyển cơ bản & Skill Tree giới thiệu (1.5 phút) ⏱ 3:00

**[A]:** `[DEMO]`

"Đầu game, người chơi chỉ có kỹ năng di chuyển cơ bản:
- **Walk, Run, Crouch, Slide, Jump, Climb Ladder**

Thanh **Stamina** dùng chung cho chạy."

**[B]:** `[DEMO — mở Skill Tree panel]`

"**Skill Tree đã có sẵn từ đầu game** — người chơi mở ra xem bất cứ lúc nào. **3 nhánh — 15 node** — mở khóa dần bằng điểm kỹ năng:"

`[CHỈ từng nhánh]`

"**① Movement** (5 node): Walk Speed → Run Speed → Air Control → **Dash → Wall Run/Bounce → Double Jump + Grapple**

**② Aim** (5 node): Giảm Recoil → Crit 10% → Crit 20% → Crit x1.5 → **One-shot Crook + Bonus Special**

**③ Intelligence** (5 node): XP Radius → XP x1.1 → Radius +10 → XP x1.15 → **Radius +15 + Highlight vật phẩm**

Mỗi node cộng dồn **+Stamina** (Move), **+Damage** (Aim), **+HP** (Intel). Chi phí 2 → 3 → 5 → 8 → 12 SP.

**Các kỹ năng như Dash, Wall Run, Double Jump, Grapple — không có sẵn, phải nâng skill tree mới dùng được.**"

---

### 1.3 — Vũ khí Chương 1 (1 phút) ⏱ 4:30

**[A]:** `[DEMO — nhặt súng đầu game]`

"**Chương 1**, người chơi tìm thấy **Pistol** — vũ khí cơ bản."

"Giới thiệu cơ chế vũ khí:
- **ADS** — ngắm bắn
- **Reload** — nạp đạn
- **Recoil + Spread** — độ giật, độ xoáy
- **Muzzle Flash** — lửa nòng
- **Bullet Holes** — khác nhau trên gỗ, kim loại, bùn, cỏ
- **Hitmarker** — dấu X trúng đích

[Các vũ khí còn lại — SMG, Shotgun, Rocket, Revolver, Katana, Burst Rifle — sẽ được nhặt ở các chương sau]

---

### 1.4 — Zombie Crook & AI (1.5 phút) ⏱ 5:30

**[B]:** `[DEMO — zombie xuất hiện]`

"**Crook** — zombie thường. **28+ biến thể**: Biker, Clown, Cop, Bride... Cao 1.5-2m ngẫu nhiên."

"AI:
- **NavMesh** di chuyển
- Phát hiện qua tầm nhìn + khoảng cách
- **Lunge/feint** — giả vờ rồi lao
- Mất dấu → **wander** tìm
- Có **headshot** riêng

**AIDirector**: 4 trạng thái **Calm → BuildUp → Attack → Recovery**. Camper 10s → spawn sau lưng.

**Spawner**: **Object Pooling** — 60 con, check NavMesh."

---

### 1.5 — HUD (30 giây) ⏱ 7:00

**[A]:** `[CHỈ nhanh]`

"**Health Bar** — viền đỏ khi yếu
**Stamina + Ammo + Dash Cooldown**
**Crosshair** — giãn theo spread
**Compass + Quest Beacon**
**Damage Direction + Combat Feedback** (DMG + kill feed)
**FPS + Threat Level + Interact Prompt**
**Boss Health Bar + Wave Announcer** — tự động xuất hiện"

---

### 1.6 — Quest đầu & Cutscene (30 giây) ⏱ 7:30

**[B]:** "Hơn **12 main quest** xuyên 5 chương. Hoàn thành = đến điểm / tiêu diệt / thu thập / tương tác. Thưởng **EXP + Journal**."

[DEMO — hoàn thành quest → Cutscene: "CHƯƠNG 2 — BỆNH VIỆN"]

"DayNight Cycle snap sang **Noon (12h)**."

---

## ☀️ CHƯƠNG 2 — BUỔI TRƯA (3.5 phút) ⏱ 8:00
### Bệnh viện — Noon (12h) — Nâng cấp skill + vũ khí mới

---

### 2.1 — Nâng cấp Skill Tree (1.5 phút) ⏱ 8:00

**[A]:** "**Chương 2 — Buổi Trưa tại Bệnh Viện**. Đã tích lũy đủ EXP — nâng skill tree lần đầu."

[DEMO — mở skill tree, click upgrade]

"Ví dụ: nâng **Movement node 1 — tăng Walk Speed**, hoặc **Aim node 1 — giảm Recoil**, hoặc **Intelligence node 1 — tăng XP Radius**.

**PlayerUpgradeManager** tự động áp dụng chỉ số vào nhân vật.

Người chơi tự chọn hướng build — không có lối đi đúng sai."

---

### 2.2 — Vũ khí mới: SMG & Shotgun (1 phút) ⏱ 9:30

**[B]:** `[DEMO — nhặt SMG]`

"**Chương 2** mở rộng kho vũ khí: tìm thấy **SMG** và **Shotgun**."

[DEMO — bắn SMG (tốc độ cao), Shotgun (sát thương gần)]

"Lúc này người chơi đã có 3 vũ khí: Pistol, SMG, Shotgun."

---

### 2.3 — Side Quest & Journal (1 phút) ⏱ 10:30

**[A]:** "Sau main quest, **8 Side Quest** mở khóa: Church, Auto Repair, Motel, Quarantine, HighRise Base, Mother's Story, Lighthouse."

**[B]:** `[DEMO — nhặt journal]`

"**39 journal** — 7 nhóm: Soldier (3), Neighbor (21), Military Record (8), Experiment Report (3), Doctor Journal (3), Cure Record (4), Brother Journal (5)."

[DEMO — popup journal: text + hình + voice log]

"Mỗi journal có hình, nội dung, voice log — ghép lại thành câu chuyện."

---

## 🏗️ CHƯƠNG 3 — HOÀNG HÔN (3.5 phút) ⏱ 11:30
### Công trường — Dusk (18h) — Kỹ năng mở rộng + vũ khí mới

---

### 3.1 — Wave System (1.5 phút) ⏱ 11:30

**[B]:** "**Chương 3 — Hoàng Hôn (18h)**.

**Wave System**: **WaveManager** (wave đầu 10 kill, +5 mỗi wave). **WaveQuestInteractable**: khóa vùng, sống sót N wave, teleport về nếu ra ngoài. **Wave Announcer**."

[DEMO — Wave đang chạy, boundary lock]

---

### 3.2 — Kỹ năng đã mở khóa (1 phút) ⏱ 13:00

**[A]:** "Nếu tập trung Movement, đến chương 3 người chơi đã mở được **Air Control** và **Dash**."

[DEMO — Dash né đòn, điều khiển trên không]

"**Dash** có cooldown — quản lý thời gian hồi là chiến thuật."

---

### 3.3 — Vũ khí mới: Rocket Launcher & Burst Rifle (30 giây) ⏱ 14:00

**[B]:** `[DEMO — nhặt Rocket]`

"**Chương 3** thêm **Rocket Launcher** và **Burst Rifle**."

[DEMO — bắn rocket (nổ diện rộng), burst (3 phát)]

"Tổng cộng đã có 5 vũ khí."

---

### 3.4 — Boomer & SpecialEnemyDirector (30 giây) ⏱ 14:30

**[B]:** `[DEMO — Boomer]`

"**Boomer** (wave 3+). 100 HP, kêu rít → **phát nổ** → sát thương vùng + **vũng acid**.

**SpecialEnemyDirector** spawn có NavMesh check, scale stat."

### 3.5 — Loot & Enemies phụ (30 giây) ⏱ 15:00

**[A]:** "**Loot System**: LootDropHelper, **LootPop**, **LootTrail**. Rơi Coin, EXP, Healthpack.

**Ceiling Zombie** (trần rơi), **Snatcher, Hooker**."

---

## 🌃 CHƯƠNG 4 — MÀN ĐÊM (2.5 phút) ⏱ 15:00
### Khu dân cư — Night (22h) — Kỹ năng cao + súng mạnh

---

### 4.1 — Kỹ năng cao cấp (1 phút) ⏱ 15:00

**[B]:** "**Chương 4 — Màn Đêm (22h)**. Đã đầu tư sâu Skill Tree."

[DEMO — skill tree đã nâng nhiều node]

"Nếu đủ điểm:"

[DEMO — Wall Run + Wall Bounce]

"**Wall Run + Wall Bounce** — chạy và bật tường."

[DEMO — Double Jump + Grappling Hook]

"**Double Jump + Grappling Hook** (node cuối Movement).

**Aim cuối**: **One-shot Crook** + bonus special.

**Intelligence cuối**: **Outline** — highlight journal trong bán kính."

---

### 4.2 — Vũ khí mới: Revolver & Katana (30 giây) ⏱ 16:00

**[A]:** `[DEMO — Revolver, Katana]`

"**Chương 4** thêm **Revolver** (sát thương cao, đạn ít) và **Katana** (cận chiến). Tổng cộng đã có 8 vũ khí."

---

### 4.3 — Tank Boss (1 phút) ⏱ 16:30

**[A]:** `[DEMO — Tank gầm, Boss Health Bar]`

"**Tank** (wave 5+). **500+ HP** scale wave. Punch / Swipe / Jump Attack. Gầm toàn map. **Boss Health Bar** + **Flashlight** (phím F)."

---

## 🌆 CHƯƠNG 5 — ĐÊM KHUYA + KẾT THÚC (2.5 phút) ⏱ 17:30
### Chung cư — Deep Night (2h) — Full vũ khí + trận cuối

---

### 5.2 — Witch & Big Guy (1 phút) ⏱ 18:00

**[A]:** "**Đêm Khuya (2h)** — tối nhất, Flashlight thiết yếu."

[DEMO — Witch]

"**Witch** (60 HP): ngồi khóc → lại gần → gào thét → **lao 6.5 m/s**. Mất dấu → quay lại khóc."

[DEMO — Big Guy]

"**Big Guy** (80 HP): đứng choáng → kích động → gầm → rượt chậm, đấm mạnh."

---

### 5.3 — Trận cuối & Ending (1 phút) ⏱ 18:30

**[B]:** "**Quest 12 — Escape Town**."

[DEMO — WaveQuestInteractable]

"**WaveQuest**: khóa vùng → nhiều wave → **Tank boss** → kích hoạt bom."

[DEMO — để cutscene chạy]

"**EndingSequenceManager**:
1. **BombExplosionCutscene**: Camera tạm → VFX nuke → SFX → Fade to Black
2. **EpilogueSlide**: 'Dịch bệnh đã được kiểm soát...'
3. **CreditsSequence**: Credit cuộn
4. Quay về **Main Menu**"

---

### 5.4 — Game Over & Achievement (30 giây) ⏱ 19:30

**[B]:** "**Game Over**: thống kê chapter, quest, journal, score. 3 lựa chọn.

**5 Achievement**:
1. Speedrunner — 5 chương <11 phút
2. Hell Slayer — 130 Crook kills
3. At Ease, Cooper — 20 kills wall run
4. Tank Slayer — hạ Tank đầu
5. Close Call — trong 5m Boomer nổ

**PlayFab**: Cloud save, auto-save, leaderboard."

---

## 🎯 KẾT LUẬN (1 phút) ⏱ 20:00

**Màn hình: Slide tổng kết**

---

**[A]:** "Tổng kết:"

| Hạng mục | Con số |
|---|---|
| Engine | Unity — C# |
| Scene | 3 |
| Script | 80+ |
| Chương | 5 |
| Main quest | 12+ |
| Side quest | 8 |
| Journal | 39 (7 nhóm) |
| Loại zombie | 7 |
| Vũ khí | 9 (thu thập dần qua 5 chương) |
| Skill tree | 3 nhánh / 15 node |
| Kỹ năng mở khóa | Dash, Wall Run, Double Jump, Grapple, Crit, Highlight, One-shot |
| Achievement | 5 |
| Cloud | PlayFab |

**[B]:** "Điểm nhấn: **vũ khí thu thập dần qua từng chương** — mỗi chương mở ra vũ khí mạnh hơn. **Skill Tree mở từ đầu** — người chơi chủ động build nhân vật."

**[A]:** "Nhóm em cảm ơn thầy cô và các bạn. Rất mong nhận được góp ý!"

**[CẢ NHÓM CÚI CHÀO]**

---

## 📋 CHECKLIST — SOÁT TRƯỚC NGÀY THUYẾT TRÌNH

### ▢ Chuẩn bị
- [ ] Unity mở sẵn scene Main Menu, sẵn sàng Play
- [ ] Âm thanh loa hoạt động
- [ ] Đồng hồ bấm giờ
- [ ] Script in / mở màn hình phụ

### ▢ Chương 1 — Súng: Pistol + Rifle. Skill tree: giới thiệu
- [ ] Main Menu — Play, Quit, Best Score
- [ ] PlayFab Login + Leaderboard
- [ ] DayNight Cycle — 6h sáng
- [ ] SaveRoom — hồi máu, checkpoint
- [ ] Walk, Run, Crouch, Slide, Jump, Climb Ladder
- [ ] Stamina System
- [ ] Camera FOV + Speed Lines + Camera Shake
- [ ] **Mở Skill Tree panel** — 3 nhánh, 15 node, chi phí
- [ ] **Nhấn mạnh**: kỹ năng nâng cao bị khóa
- [ ] **Pistol + Rifle** — bắn, chuyển đổi
- [ ] ADS, Reload, Recoil, Spread
- [ ] Muzzle Flash, Bullet Holes, Hitmarker
- [ ] Attachments
- [ ] Crook — 28 variants, AI, NavMesh, headshot
- [ ] AIDirector — 4 trạng thái, camper punish
- [ ] Object Pooling — spawner
- [ ] HUD — 12 widget
- [ ] Quest hoàn thành + Cutscene chuyển chương

### ▢ Chương 2 — Súng thêm: SMG + Shotgun. Skill: nâng lần đầu
- [ ] DayNight → Noon
- [ ] **SMG + Shotgun** — nhặt, bắn thử
- [ ] **Nâng skill tree node đầu tiên** (click upgrade)
- [ ] 8 Side Quest
- [ ] Journal đầu — popup text + hình + voice log

### ▢ Chương 3 — Súng thêm: Rocket + BurstRifle. Skill: Dash, Air Control
- [ ] DayNight → Dusk
- [ ] **Rocket Launcher + Burst Rifle** — nhặt, bắn
- [ ] **Dash + Air Control** — demo (đã mở khóa)
- [ ] WaveManager + WaveQuest + boundary lock
- [ ] Boomer — nổ, acid pool
- [ ] Ceiling Zombie, Snatcher, Hooker
- [ ] Loot System

### ▢ Chương 4 — Súng thêm: Revolver + Katana. Skill: Wall Run, Double Jump, Grapple
- [ ] DayNight → Night
- [ ] Flashlight
- [ ] **Revolver + Katana** — nhặt, chém
- [ ] **Wall Run + Wall Bounce** — demo
- [ ] **Double Jump + Grappling Hook** — demo
- [ ] Aim cuối: One-shot Crook
- [ ] Intel cuối: Highlight journal (Outline)
- [ ] Tank — gầm, 3 đòn, Boss Health Bar

### ▢ Chương 5 — Full 9 súng + Turret. Kết thúc
- [ ] DayNight → Deep Night (2h)
- [ ] **Turret** — vũ khí cuối
- [ ] **Chuyển nhanh 9 súng** — show full arsenal
- [ ] Witch — khóc, lao 6.5m/s
- [ ] Big Guy — choáng, gầm, rượt
- [ ] Quest 12 — Escape Town
- [ ] Wave Quest cuối + Tank boss
- [ ] Bomb Explosion Cutscene
- [ ] Epilogue + Credits
- [ ] Game Over + 5 Achievement
- [ ] PlayFab — cloud save, leaderboard
