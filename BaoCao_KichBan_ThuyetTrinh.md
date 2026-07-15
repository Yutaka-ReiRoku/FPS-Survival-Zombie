# 🎬 KỊCH BẢN THUYẾT TRÌNH DỰ ÁN — FPS SURVIVAL ZOMBIE
## Trình bày theo cốt truyện — 5 chương — 20 phút

> **Cách dùng:**
> - `[A]/[B]` — người nói. Có thể chia cho người chơi game riêng
> - `[DEMO]` — thao tác trên game, màn hình chiếu focus vào game
> - `[NGẮT]` — điểm dừng gợi ý để chuyển người nói hoặc hỏi nhanh
> - ⏱ Đồng hồ chạy — **bám sát** để không quá giờ
> - **Nguyên tắc**: Chơi game từ đầu → cuối. Tính năng nào xuất hiện tới đâu thì giới thiệu tới đó

---

## ⏱ TỔNG QUAN THỜI GIAN

| Mốc | Phần | TG | Người nói |
|---|---|---|---|
| 0:00 | **Mở đầu** | 1' | A |
| 1:00 | **Main Menu → vào game** | 1' | A |
| 2:00 | **Chương 1 — Bình Minh** | 6' | A+B |
| 8:00 | **Chương 2 — Buổi Trưa** | 3' | A+B |
| 11:00 | **Chương 3 — Hoàng Hôn** | 3.5' | A+B |
| 14:30 | **Chương 4 — Màn Đêm** | 3' | A+B |
| 17:30 | **Chương 5 — Đêm Khuya + Kết thúc** | 2.5' | A+B |
| 20:00 | **Kết luận** | 1' | B |

---

## 🎬 MỞ ĐẦU (1 phút) ⏱ 0:00

**Màn hình: Main Menu**

---

**[A]:** "Kính thưa thầy cô và các bạn. Nhóm em xin trình bày đồ án **FPS Survival Zombie** — game bắn súng góc nhìn thứ nhất, kể về hành trình của một người sống sót qua ngày tận thế zombie.

Trò chơi có **cốt truyện 5 chương**, mỗi chương gắn với một mốc thời gian — từ bình minh đến đêm khuya. Người chơi sẽ chiến đấu, tích lũy kỹ năng, thu thập nhật ký, và khám phá sự thật đằng sau đại dịch.

Dự án phát triển trên **Unity**, sử dụng **Cowsins FPS Engine** nền tảng và **PlayFab** cho dữ liệu đám mây.

Sau đây nhóm em xin demo — chúng ta sẽ chơi từ đầu đến cuối, gặp gì giới thiệu nấy."

---

## 🎮 MAIN MENU (1 phút) ⏱ 1:00

**[A]:** `[CHỈ màn hình]`

"Đây là **Main Menu**. Có nút **Play** bắt đầu game, **Quit** thoát, và hiển thị **Best Score**.

Ở đây có **PlayFab Login** — đăng ký tài khoản, đăng nhập. Nhờ đó dữ liệu điểm số, achievement được đồng bộ lên cloud, chơi trên nhiều máy vẫn giữ thành tích.

Có **Leaderboard** — bảng xếp hạng toàn cầu, và **Auto-save** — tự động lưu mỗi 60 giây."

[CLICK Play — vào game]

---

## 🌅 CHƯƠNG 1 — BÌNH MINH (6 phút) ⏱ 2:00
### Khu vực đầu — Dawn (6h sáng)

---

### 2.1 — Vào game (1 phút) ⏱ 2:00

**[B]:** (nhân vật vừa spawn)

"Người chơi thức dậy tại một trại tạm — **Chương 1 — Bình Minh**. Trời vừa sáng."

`[CHỈ]`

"**DayNight Cycle** — chu kỳ ngày đêm đang ở mốc 6h sáng. Hệ thống này điều khiển mặt trời, bầu trời, sương mù, ánh sáng — mỗi chương một không khí khác nhau.

**SaveRoom** — vùng sáng này vừa hồi máu vừa là **checkpoint**. Chết sẽ respawn tại đây.

**StoryManager** đã kích hoạt quest đầu tiên — **Quest Tracker** ở góc phải hướng dẫn mục tiêu. Hệ thống **ChapterBoundary** quản lý ranh giới từng chương."

---

### 2.2 — Di chuyển (1 phút) ⏱ 3:00

**[A]:** `[DEMO — vừa chạy vừa nói]`

"Người chơi bắt đầu di chuyển — game có hệ thống di chuyển phong phú:

**Đi bộ, chạy, ngồi, trượt** — cơ bản.
**Nhảy, Double Jump** — sau khi nâng cấp.
**Dash** — lướt nhanh.
**Wall Run + Wall Bounce** — chạy trên tường và bật ra.
**Grappling Hook** — bắn móc kéo người.
**Climb Ladder** — leo thang.

Tất cả dùng chung thanh **Stamina** — quản lý thể lực là yếu tố chiến thuật quan trọng.

**Camera** có **FOV thay đổi** khi chạy nhanh, hiệu ứng **Speed Lines**, **Camera Shake** khi bị đòn."

[DEMO 20s: chạy → dash → wall run → grapple → leo thang]

---

### 2.3 — Vũ khí đầu tay (1 phút) ⏱ 4:00

**[B]:** "Người chơi nhặt vũ khí đầu tiên."

`[DEMO — bấm chuyển súng, bắn]`

"Game có **9 loại vũ khí**: Pistol, Rifle, SMG, Shotgun, Rocket Launcher, Revolver, Katana, Burst Rifle, và Turret.

Các cơ chế:
- **ADS** — ngắm bắn
- **Reload** — nạp đạn
- **Recoil + Spread** — độ giật và độ xoáy
- **Muzzle Flash** — lửa nòng
- **Bullet Holes** — lỗ đạn trên gỗ, kim loại, bùn, cỏ
- **Hitmarker** — dấu X xác nhận trúng đích
- **Có Attachments** — Barrel, Grip, Scope, Stock, Laser, Flashlight, Magazine"

---

### 2.4 — Zombie Crook đầu tiên (1.5 phút) ⏱ 5:00

**[A]:** `[DEMO — zombie xuất hiện, bắn]`

"Đây là **Crook** — zombie thường. Có **28+ biến thể** ngoại hình: Biker, Clown, Cop, Bride, Cheerleader, Businessman... Mỗi con chiều cao 1.5-2m ngẫu nhiên."

`[Bắn chết 1 con]`

"**AI** của Crook:
- Di chuyển bằng **NavMesh**
- Phát hiện qua tầm nhìn + khoảng cách
- Hành vi **lunge/feint** — giả vờ rồi lao
- Mất dấu thì **wander** tìm
- Có **headshot** — crit headshot riêng

**AIDirector** — hệ thống độ khó động: 4 trạng thái **Calm → BuildUp → Attack → Recovery**, theo dõi tỉ lệ kill, thời gian sống, phát hiện camper. Đứng yên 10s → bị spawn sau lưng.

**Spawner**: **Object Pooling** — pool 60 con, kiểm tra NavMesh hợp lệ. Camper punishment: spawn gần."

---

### 2.5 — HUD (30 giây) ⏱ 6:30

**[B]:** `[CHỈ nhanh từng widget]`

"Toàn bộ giao diện HUD:
- **Health Bar** — máu, viền đỏ khi yếu
- **Stamina + Ammo + Dash Cooldown**
- **Crosshair** — giãn theo độ xoáy
- **Compass + Quest Beacon** — la bàn + điểm mục tiêu
- **Damage Direction** — chỉ hướng trúng đòn
- **Combat Feedback** — số DMG + kill feed
- **FPS Counter + Threat Level**
- **Interact Prompt** — Nhấn E
- **Boss Health Bar**, **Wave Announcer** — tự động xuất hiện khi cần"

---

### 2.6 — Quest đầu & Cutscene chuyển chương (1 phút) ⏱ 7:00

**[A]:** "Hệ thống **Quest**: hơn **12 main quest** xuyên 5 chương. Hoàn thành bằng: di chuyển đến vị trí, tiêu diệt đủ zombie, thu thập vật phẩm, tương tác."

[DEMO — QuestTrigger, hoàn thành quest]

"Mỗi quest thưởng **EXP + Journal**.

Hết chương 1 — **SaveRoom** kích hoạt **Cutscene**:"

[DEMO — Cutscene: "CHƯƠNG 2 — BỆNH VIỆN"]

"DayNight Cycle snap sang **Noon (12h)**."

**[NGẮT] — Hỏi nhanh: "Thầy cô có câu hỏi gì về phần này không?"**

---

## ☀️ CHƯƠNG 2 — BUỔI TRƯA (3 phút) ⏱ 8:00
### Bệnh viện — Noon (12h)

---

### 3.1 — Môi trường & Side Quest (2 phút) ⏱ 8:00

**[B]:** "**Chương 2 — Buổi Trưa tại Bệnh Viện**. DayNight Cycle chuyển sang 12h — ánh sáng gay gắt hơn, sương mù tan.

Sau main quest, **8 Side Quest** mở khóa: Church, Auto Repair, Motel, Quarantine, HighRise Base, Mother's Story, Lighthouse.

**SideQuestManager** quản lý các nhiệm vụ phụ này — độc lập với cốt truyện chính."

### 3.2 — Journal đầu tiên (1 phút) ⏱ 10:00

**[A]:** `[DEMO — nhặt journal]`

"Đây là **Journal** — nhật ký. Xuyên suốt game có **39 journal** thuộc 7 nhóm:"

`[CHỈ]`

- Soldier Journal (3)
- Neighbor Journal (21) — nhiều nhất
- Military Record (8)
- Experiment Report (3)
- Doctor Journal (3)
- Cure Record (4)
- Brother Journal (5)

[DEMO — popup journal: text + hình + voice log]

"Mỗi journal có **hình ảnh, nội dung văn bản, và voice log**. Ghép chúng lại để hiểu toàn bộ câu chuyện đằng sau đại dịch."

**[NGẮT]**

---

## 🏗️ CHƯƠNG 3 — HOÀNG HÔN (3.5 phút) ⏱ 11:00
### Công trường — Dusk (18h)

---

### 4.1 — Wave System (1.5 phút) ⏱ 11:00

**[B]:** "**Chương 3 — Hoàng Hôn tại Công Trường**. Thời gian 18h, trời tối dần.

Đây là chương giới thiệu **Wave System**:

- **WaveManager**: wave đầu 10 kill, mỗi wave +5
- **Wave Announcer** — thông báo wave bắt đầu/kết thúc

Đặc biệt có **WaveQuestInteractable** — một dạng quest đặc biệt: người chơi bị **khóa trong khu vực**, cổng đóng, phải sống sót qua N wave liên tiếp. Cố gắng ra ngoài sẽ bị **teleport về**."

[DEMO — Wave đang chạy, chỉ boundary lock]

---

### 4.2 — Boomer (1 phút) ⏱ 12:30

**[A]:** `[DEMO — Boomer]`

"**Boomer** — zombie đặc biệt đầu tiên, xuất hiện từ wave 3.

- 100 HP
- Kêu rít cảnh báo → lao vào → **phát nổ**
- Gây sát thương vùng + để lại **vũng acid**
- Có thể bắn chết từ xa

Boomer được spawn bởi **SpecialEnemyDirector** — hệ thống spawn quái đặc biệt, có kiểm tra NavMesh, scale stat theo wave."

### 4.3 — Loot System (30 giây) ⏱ 13:30

**[B]:** `[DEMO — zombie chết rơi đồ]`

"Khi zombie chết — **Loot System**:

- **LootDropHelper** — roll loot từ bảng
- **LootPop** — hiệu ứng nảy
- **LootTrail** — vệt sáng
- Rơi ra: **Coin, EXP, Healthpack, Ammo, PowerUp**"

### 4.4 — Enemies phụ (30 giây) ⏱ 14:00

**[A]:** "Thêm 2 loại enemy:
- **Ceiling Zombie** — ẩn trên trần, rơi xuống khi người chơi đi qua
- **Snatcher + Hooker** — enemy đặc biệt khác"

**[NGẮT]**

---

## 🌃 CHƯƠNG 4 — MÀN ĐÊM (3 phút) ⏱ 14:30
### Khu dân cư — Night (22h)

---

### 5.1 — Skill Tree (1.5 phút) ⏱ 14:30

**[B]:** "**Chương 4 — Màn Đêm tại Khu Dân Cư**. DayNight Cycle snap sang **22h** — trời tối.

Đến chương này người chơi đã đủ điểm để mở **Skill Tree**."

[DEMO — mở Skill Tree panel]

"**3 nhánh — 15 node**:"

`[CHỈ từng nhánh]`

"**① Movement** (5 node): Walk Speed → Run Speed → Air Control + Dash → Wall Run/Bounce → Double Jump + Grapple. Mỗi node: **+Stamina**.

**② Aim** (5 node): Recoil → Crit 10% → Crit 20% → Crit x1.5 → One-shot Crook + Bonus Specials. Mỗi node: **+Damage**.

**③ Intelligence** (5 node): XP Radius → XP x1.1 → Radius +10 → XP x1.15 → Radius +15 + Highlight. Mỗi node: **+HP**.

Chi phí: node 1 (2SP) → node 5 (12SP). **PlayerUpgradeManager** áp dụng các chỉ số này vào nhân vật."

---

### 5.2 — Tank Boss (1 phút) ⏱ 16:00

**[A]:** `[DEMO — Tank gầm, xuất hiện, Boss Health Bar hiện]`

"Từ wave 5, **Tank** bắt đầu xuất hiện.

**Tank** — boss chính:
- **500+ HP** (scale theo wave)
- 3 đòn: Punch (gần), Swipe (diện rộng), Jump Attack (từ xa)
- Gầm toàn map cảnh báo
- **Boss Health Bar** trên HUD hiển thị máu

**Flashlight** (phím F) — đèn pin độc lập, không gắn vũ khí."

### 5.3 — Collectible Highlight (30 giây) ⏱ 17:00

**[B]:** "Khi nâng cấp Intelligence đủ, **Outline Effect** — viền phát sáng — sẽ highlight journal và vật phẩm tương tác trong bán kính, giúp người chơi dễ tìm."

**[NGẮT]**

---

## 🌆 CHƯƠNG 5 — ĐÊM KHUYA + KẾT THÚC (3 phút) ⏱ 17:30
### Chung cư — Deep Night (2h sáng)

---

### 6.1 — Witch & Big Guy (1 phút) ⏱ 17:30

**[A]:** "**Chương 5 — Đêm Khuya tại Chung Cư**. 2 giờ sáng — tối nhất game. Flashlight thiết yếu."

`[DEMO — Witch khóc]`

"**Witch** (60 HP): ngồi khóc tại chỗ. Lại gần hoặc bắn trúng → gào thét → **lao thẳng vào người chơi với tốc độ 6.5 m/s**. Mất dấu thì quay lại khóc.

`[DEMO — Big Guy]`

**Big Guy** (80 HP): đứng choáng, mặc váy công chúa. Kích động → gầm → rượt chậm nhưng đòn đấm rất mạnh."

---

### 6.2 — Trận cuối (1 phút) ⏱ 18:30

**[B]:** "**Quest 12 — Escape Town**: quest cuối."

[DEMO — WaveQuestInteractable kích hoạt]

"Dạng **Wave Quest**: khóa khu vực, sống sót qua nhiều wave. Wave cuối là **Tank boss**. Đánh bại Tank → tương tạo lần 2 → **kích hoạt bom hủy diệt**."

---

### 6.3 — Ending Sequence (1.5 phút) ⏱ 19:00

**[A]:** "**EndingSequenceManager** điều phối:"

[DEMO — để cutscene chạy]

"1. **BombExplosionCutscene**: Camera tạm thời → VFX nổ hạt nhân → SFX nổ → Fade to Black
2. **EpilogueSlide**: 'Dịch bệnh đã được kiểm soát, nhưng ai mang đến phương thuốc vẫn còn là bí ẩn.'
3. **CreditsSequence**: Credit cuộn — thành viên nhóm, trường, tài nguyên, Cowsins Engine, thanks
4. Tự động quay về **Main Menu**"

---

### 6.4 — Game Over & Achievement (30 giây) ⏱ 20:00

**[B]:** (khi chết / khi xem lại)

"**Game Over** — thống kê đầy đủ: chapter, quest, journal, score (Story) hoặc wave, kills, best score (Wave). 3 lựa chọn: Restart (checkpoint), Main Menu, Quit.

**5 Achievement**:
1. Speedrunner — 5 chương <11 phút
2. Hell Slayer — 130 Crook kill
3. At Ease, Cooper — 20 kill khi wall run
4. Tank Slayer — hạ Tank đầu tiên
5. Close Call — trong 5m khi Boomer nổ"

---

## 🎯 KẾT LUẬN (1 phút) ⏱ 20:00

**Màn hình: Slide tổng kết**

---

**[A]:** "Tổng kết dự án:"

| Hạng mục | Con số |
|---|---|
| Engine | Unity — C# |
| Tổng Scene | 3 |
| Tổng Script | 80+ |
| Số chương | 5 (Bình Minh → Đêm Khuya) |
| Main quest | 12+ |
| Side quest | 8 |
| Journal | 39 (7 nhóm) |
| Loại zombie | 7 (Crook, Boomer, Tank, Witch, BigGuy, Snatcher, Hooker) |
| Loại vũ khí | 9 |
| Skill tree | 3 nhánh / 15 node |
| Achievement | 5 |
| Cloud | PlayFab (Login, Save, Leaderboard) |

**[B]:** "Một tựa game FPS Survival hoàn chỉnh: **cốt truyện 5 chương, 7 loại kẻ địch AI, 9 vũ khí, skill tree, wave, 39 journal, kết thúc cutscene, cloud save**."

**[A]:** "Nhóm em cảm ơn thầy cô và các bạn đã theo dõi. Rất mong nhận được góp ý!"

**[CẢ NHÓM CÚI CHÀO]**

---

## 📋 CHECKLIST TẤT CẢ TÍNH NĂNG — SOÁT TRƯỚC NGÀY THUYẾT TRÌNH

### ▢ Chuẩn bị kỹ thuật
- [ ] Unity mở sẵn scene Main Menu
- [ ] Chế độ Play sẵn sàng
- [ ] Âm thanh loa hoạt động
- [ ] Đồng hồ bấm giờ
- [ ] Script in ra / mở trên màn hình phụ

### ▢ Chương 1 — Bình Minh
- [ ] Main Menu — Play, Quit, Best Score
- [ ] PlayFab Login — form, leaderboard
- [ ] DayNight Cycle — chỉ góc nhìn 6h sáng
- [ ] SaveRoom / Checkpoint — vùng sáng, hồi máu
- [ ] StoryManager + QuestTracker — quest đầu
- [ ] Walk / Run / Crouch / Slide
- [ ] Jump / Double Jump
- [ ] Dash (có cooldown)
- [ ] Wall Run + Wall Bounce
- [ ] Grappling Hook
- [ ] Climb Ladder
- [ ] Stamina System (chạy đến khi hết)
- [ ] Camera FOV + Speed Lines
- [ ] Camera Shake (bị đòn)
- [ ] 9 vũ khí — chuyển đổi + bắn thử
- [ ] ADS — ngắm bắn
- [ ] Reload — nạp đạn
- [ ] Recoil + Spread
- [ ] Muzzle Flash
- [ ] Bullet Holes (bắn xuống nền)
- [ ] Hitmarker
- [ ] Attachments — chỉ menu
- [ ] Crook zombie — 28 variants, AI, NavMesh
- [ ] AIDirector — 4 trạng thái, camper punish
- [ ] Object Pooling — spawner
- [ ] HUD — chỉ đủ 12 widget
- [ ] Quest hoàn thành + cutscene chuyển

### ▢ Chương 2 — Buổi Trưa
- [ ] DayNight → Noon (ánh sáng thay đổi)
- [ ] 8 Side Quest — mở bảng
- [ ] Journal đầu tiên — popup text + hình
- [ ] Voice Log — âm thanh
- [ ] CollectibleManager — 39 journal

### ▢ Chương 3 — Hoàng Hôn
- [ ] DayNight → Dusk
- [ ] WaveManager — wave đang chạy
- [ ] WaveQuestInteractable — khóa vùng, teleport
- [ ] Wave Announcer
- [ ] Boomer — xuất hiện, nổ, acid pool
- [ ] SpecialEnemyDirector
- [ ] Loot — Coin, EXP, Healthpack, Pop, Trail
- [ ] Ceiling Zombie — từ trần rơi
- [ ] Snatcher / Hooker

### ▢ Chương 4 — Màn Đêm
- [ ] DayNight → Night (22h)
- [ ] Flashlight (phím F)
- [ ] Skill Tree — mở panel, 3 nhánh
- [ ] PlayerUpgradeManager — áp dụng
- [ ] Tank — gầm, xuất hiện, punch/swipe/jump
- [ ] Boss Health Bar
- [ ] Outline — highlight journal

### ▢ Chương 5 — Đêm Khuya + Kết thúc
- [ ] DayNight → Deep Night (2h)
- [ ] Witch — khóc, kích động, lao nhanh
- [ ] Big Guy — choáng, gầm, rượt
- [ ] Quest 12 — Escape Town
- [ ] Wave Quest cuối + Tank boss
- [ ] Bomb Explosion Cutscene
- [ ] Epilogue Slide
- [ ] Credits Sequence
- [ ] Game Over — stats + 3 options
- [ ] 5 Achievement
- [ ] PlayFab — cloud save, auto-save

### ▸ Bonus (nếu còn thời gian)
- [ ] PlayerStatsTracker — stats panel
- [ ] PauseManager — menu tạm dừng
- [ ] Achievement toast notification
- [ ] Outline Effect chi tiết
- [ ] Low Health Vignette + Health Flash
- [ ] Compass — quay hướng, beacon
