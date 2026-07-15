# 📋 FLOW TRÌNH BÀY & SCRIPT THUYẾT TRÌNH — DỰ ÁN FPS SURVIVAL ZOMBIE

---

## 🎯 PHẦN 1: GIỚI THIỆU (1-2 phút)

**Màn hình: Main Menu**

**Script:**
> "Xin chào thầy cô và các bạn. Nhóm em xin trình bày đồ án: **FPS Survival Zombie** – tựa game bắn súng góc nhìn thứ nhất kết hợp sinh tồn, lấy bối cảnh ngày tận thế zombie.
>
> Trò chơi kể về hành trình của một người sống sót từ lúc bùng phát dịch bệnh, vượt qua 5 chương, đối mặt với nhiều loại zombie, tích lũy kỹ năng, thu thập nhật ký và khám phá sự thật đằng sau đại dịch.
>
> Dự án được phát triển trên Unity, sử dụng Cowsins FPS Engine làm nền tảng, kết hợp với PlayFab cho lưu đám mây và bảng xếp hạng.
>
> Sau đây, nhóm em xin demo luồng chơi từ đầu đến cuối."

---

## 🎮 PHẦN 2: MAIN MENU & HỆ THỐNG ĐĂNG NHẬP (1 phút)

**Màn hình: Main Menu → click Play**

| Thành phần | Miêu tả |
|---|---|
| Nút Play | Bắt đầu game |
| Nút Quit | Thoát |
| Best Score | Điểm cao nhất |
| Đăng nhập PlayFab | Username/Password |

**Script:**
> "Đầu tiên là màn hình Main Menu. Tại đây người chơi có thể xem điểm cao nhất, đăng nhập PlayFab để lưu dữ liệu lên đám mây. Nhấn Play để bắt đầu."

---

## 🧟 PHẦN 3: CHƯƠNG 1 — BÌNH MINH (3 phút)

### 3.1 Spawn & Bắt Đầu

**Màn hình: Player spawn tại khu vực Chapter 1 (dawn)**

| Thành phần | Chi tiết |
|---|---|
| ChapterBoundary | Ranh giới chương, kích hoạt spawner |
| SaveRoom (Checkpoint) | Hồi máu, lưu checkpoint |
| DayNightCycle | Thời gian: Dawn (6h) |
| StoryManager | Kích hoạt Quest đầu tiên |
| QuestTracker HUD | Hiển thị mục tiêu |

**Script:**
> "Người chơi bắt đầu ở Chương 1 — Bình Minh. Chúng ta thấy:
> - **DayNightCycle** với ánh sáng bình minh
> - **SaveRoom** đầu tiên: vùng an toàn hồi máu và lưu checkpoint
> - **Quest Tracker** trên HUD chỉ dẫn mục tiêu
> - Hệ thống **StoryManager** quản lý cốt truyện"

### 3.2 Chiến Đấu với Zombie Cơ Bản

**Màn hình: Player di chuyển, gặp zombie → bắn**

| Loại Zombie | HP | Đặc điểm |
|---|---|---|
| **Crook (Zombie thường)** | 100 | 28+ biến thể (Biker, Clown, Cop,...), cao ngẫu nhiên 1.5-2m |
| Hành vi | — | Phát hiện bằng tầm nhìn + khoảng cách, di chuyển NavMesh, lunge/feint ngẫu nhiên |

**Script:**
> "Zombie thường — gọi là Crook — có hơn 28 biến thể ngoại hình. Chúng di chuyển bằng **NavMesh**, phát hiện người chơi qua tầm nhìn và khoảng cách. Hành vi có độ trễ: giả vờ tấn công, lao lên bất ngờ, tạo cảm giác tự nhiên.
>
> Hệ thống **AIDirector** theo dõi mức đe dọa của người chơi (0-100) với 4 trạng thái: Calm → BuildUp → Attack → Recovery. Nếu người chơi đứng yên quá 10 giây, zombie sẽ spawn sau lưng — chống camp."

### 3.3 Vũ Khí & Bắn Súng

**Màn hình: Chuyển đổi vũ khí, bắn, nạp đạn**

| Hệ thống | Chi tiết |
|---|---|
| WeaponController | Pistol, Rifle, SMG, Shotgun, Rocket, Revolver, Katana, BurstRifle |
| Shoot Styles | Hitscan, Projectile, Melee, Custom |
| Reload | Magazine-based |
| Recoil & Spread | Có độ giật và độ xoáy |
| ADS (Aim Down Sights) | Ngắm bắn |
| Attachments | Barrel, Grip, Scope, Stock, Laser, Flashlight, Magazine |
| Bullet Holes | Wood, Metal, Mud, Grass — mỗi loại khác nhau |
| Muzzle Flash | VFX khi bắn |
| Hitmarker | Dấu hiệu trúng đạn |

**Script:**
> "Hệ thống vũ khí được kế thừa từ Cowsins Engine, hỗ trợ đa dạng: súng lục, rifle, shotgun, rocket, và cả katana cận chiến. Có cơ chế nạp đạn, độ giật, ngắm bắn ADS, và hiệu ứng nòng sáng khi bắn.
>
> Đạn bắn vào các bề mặt khác nhau — gỗ, kim loại, bùn, cỏ — cho hiệu ứng lỗ đạn khác nhau."

### 3.4 Kỹ Năng Di Chuyển

**Màn hình: Player chạy, dash, wall run, grappling hook**

| Kỹ năng | Mô tả |
|---|---|
| Walk / Run | Cơ bản |
| Crouch / Slide | Ngồi / Trượt |
| Jump + Double Jump | Nhảy đơn / kép (skill tree) |
| Dash | Lao nhanh |
| Wall Run / Wall Bounce | Chạy tường / Bật tường |
| Grappling Hook | Móc câu |
| Climb Ladder | Leo thang |
| Stamina System | Thanh thể lực |

**Script:**
> "Về di chuyển, ngoài chạy nhảy cơ bản, người chơi có thể:
> - **Dash** (lướt nhanh)
> - **Wall Run & Wall Bounce** (chạy và bật tường)
> - **Grappling Hook** (móc câu di chuyển)
> - **Double Jump** (nhảy đôi)
>
> Tất cả đều dùng chung thanh Stamina — quản lý thể lực là yếu tố chiến thuật quan trọng."

### 3.5 HUD & Giao Diện

**Màn hình: Chỉ vào từng widget trên HUD**

| Widget | Chức năng |
|---|---|
| Health + Low Health Vignette | Máu + viền đỏ khi yếu |
| Stamina | Thể lực |
| Ammo | Đạn |
| Dash Cooldown | Hồi dash |
| Crosshair | Độ chính xác |
| Compass + Quest Beacon | La bàn + điểm đánh dấu |
| Damage Direction | Hướng bị tấn công |
| Combat Feedback | Số sát thương + kill feed |
| FPS Counter | Chỉ số FPS |
| Threat Indicator | Mức đe dọa |
| Interact Prompt | "Nhấn E" |
| Boss Health Bar | Thanh máu boss |
| Wave Announcer | Thông báo wave |

**Script:**
> "Giao diện HUD được thiết kế đầy đủ với: thanh máu, thể lực, đạn, crosshair, la bàn, điểm đánh dấu nhiệm vụ, chỉ thị hướng bị tấn công, phản hồi chiến đấu và chỉ số FPS."

### 3.6 Quest & Cốt Truyện Chương 1

**Màn hình: Quest trigger → hoàn thành quest → chuyến cảnh**

| Thành phần | Mô tả |
|---|---|
| QuestData (ScriptableObject) | 12+ quest chính |
| QuestTrigger | Trigger volume hoặc manual |
| QuestReward | EXP + Journal |
| CutscenePlayer | Cutscene chuyển chương |
| Collectible (Journal) | Nhật ký đầu tiên |

**Script:**
> "Hệ thống nhiệm vụ chính gồm hơn 12 quest xuyên suốt 5 chương. Người chơi hoàn thành quest bằng cách đến điểm, tiêu diệt đủ zombie, hoặc tương tác vật phẩm. Mỗi quest thưởng EXP và nhật ký.
>
> Khi hoàn thành Chương 1, một **Cutscene** chuyển cảnh xuất hiện: 'Chương 2 — Bệnh Viện'."

---

## 🌙 PHẦN 4: CHƯƠNG 2 — BUỔI TRƯA (2 phút)

**Màn hình: Chapter 2 — Noon (Hospital)**

| Đặc điểm | Mô tả |
|---|---|
| Time | Noon (12h) — DayNightCycle chuyển |
| Khu vực | Bệnh viện |
| Độ khó | Tăng dần |
| Side Quests | Mở khóa sau main quest |

**Script:**
> "Chương 2 — Buổi Trưa tại Bệnh Viện. Thời gian chuyển từ bình minh sang giữa trưa, ánh sáng thay đổi rõ rệt nhờ DayNightCycle. Zombie đông hơn và cứng hơn.
>
> Sau khi hoàn thành cốt truyện chính của chương, **Side Quest** bắt đầu mở khóa — các nhiệm vụ phụ với phần thưởng đặc biệt."

### 4.1 Side Quest System

**Màn hình: Nhặt side quest → hoàn thành**

| Side Quest | Mô tả |
|---|---|
| Church | Nhiệm vụ nhà thờ |
| Auto Repair | Tiệm sửa xe |
| Motel | Nhà nghỉ |
| Quarantine | Khu cách ly |
| HighRise Base | Căn cứ cao tầng |
| Mother's Story | Câu chuyện người mẹ |
| Lighthouse | Hải đăng |
| (8 tổng cộng) | Mỗi quest có mục tiêu riêng |

**Script:**
> "Side Quest là hệ thống song song, độc lập với cốt truyện chính. Có 8 side quest với các mục tiêu đa dạng: tiêu diệt, thu thập, tương tác."

---

## 🏗️ PHẦN 5: CHƯƠNG 3 — HOÀNG HÔN (2 phút)

### 5.1 Wave System

**Màn hình: Chapter 3 — Construction Site, wave đang diễn ra**

| Thành phần | Chi tiết |
|---|---|
| WaveManager | Vô hạn wave, base kill = 10 + wave × 5 |
| WaveQuestInteractable | Quest sóng: khóa ranh giới, spawn từng wave |
| SpecialEnemyDirector | Boomer từ wave 3+, Tank từ wave 5+ |
| Wave Announcer | UI thông báo wave start/end |

**Script:**
> "Chương 3 — Hoàng Hôn tại Công Trường, giới thiệu hệ thống **Wave**. Spawner có cơ chế **Object Pooling** với tối đa 60 zombie, spawn có kiểm tra NavMesh hợp lệ.
>
> Có dạng quest đặc biệt: **Wave Quest** — người chơi bị khóa trong khu vực, phải sống sót qua nhiều đợt sóng, bao gồm cả Boomer."

### 5.2 Boomer

**Màn hình: Boomer xuất hiện → kêu to → phát nổ**

| Zombie | HP | Đặc điểm |
|---|---|---|
| **Boomer** | 100 | Kêu rít → phát nổ → vũng acid, sát thương vùng |

**Script:**
> "Boomer là zombie đặc biệt: khi bị kích động, nó kêu to một hồi rồi phát nổ, để lại vũng acid sát thương. Người chơi có thể bắn chết từ xa để an toàn."

---

## 🌃 PHẦN 6: CHƯƠNG 4 — MÀN ĐÊM (2 phút)

### 6.1 Skill Tree

**Màn hình: Mở skill tree panel → nâng cấp**

| Nhánh | Node | Hiệu ứng |
|---|---|---|
| **Movement** (5 nodes) | Walk Speed → Run Speed → Air Control + Dash → Wall Run/Bounce → Double Jump + Grapple | +Stamina mỗi node |
| **Aim** (5 nodes) | Recoil → Crit 10% → Crit 20% → Crit x1.5 → One-shot Crook + Bonus Special | +Damage mỗi node |
| **Intelligence** (5 nodes) | XP Radius → XP x1.1 → Radius 10 → XP x1.15 → Radius 15 + Highlight | +HP mỗi node |

**Script:**
> "Chương 4 — Màn Đêm, và cũng là lúc người chơi đã tích lũy đủ điểm kỹ năng. **Skill Tree** có 3 nhánh:
> - **Movement**: Tốc độ, dash, wall run, double jump, grappling hook
> - **Aim**: Giảm recoil, tăng crit, one-shot zombie thường
> - **Intelligence**: Bán kính hút EXP, nhân EXP, highlight vật phẩm
>
> Mỗi node có chi phí điểm kỹ năng tăng dần và cộng dồn chỉ số sinh tồn."

### 6.2 Tank

**Màn hình: Tank xuất hiện, gầm rú, chiến đấu**

| Boss | HP | Đặc điểm |
|---|---|---|
| **Tank** | 500+ (scale theo wave) | Đấm, vả, nhảy tấn công, gầm toàn map |

**Script:**
> "Tank là boss chính — 500 máu cơ bản, tỉ lệ theo wave. Nó có 3 đòn tấn công: đấm, tát, và nhảy từ xa. Tiếng gầm của Tank vang toàn map cảnh báo người chơi."

---

## 🌆 PHẦN 7: CHƯƠNG 5 — ĐÊM KHUYA & KẾT THÚC (2 phút)

### 7.1 Witch & Big Guy

**Màn hình: Witch ngồi khóc → kích động → lao nhanh**

| Mini-boss | HP | Đặc điểm |
|---|---|---|
| **Witch** | 60 | Ngồi khóc, bị động → lao nhanh 6.5m/s |
| **Big Guy** | 80 | Choáng tại chỗ, kích động → rượt chậm nhưng trâu |

**Script:**
> "Chương 5 — Đêm Khuya tại Chung Cư. Xuất hiện 2 mini-boss đặc biệt:
> - **Witch**: ngồi khóc, nếu lại gần hoặc bắn trúng, nó gào thét lao cực nhanh vào người chơi
> - **Big Guy**: đứng choáng váng, khi bị kích động nó gầm lên và rượt chậm nhưng rất trâu"

### 7.2 Journal & Voice Log

**Màn hình: Nhặt journal → popup với text + voice**

| Nhóm Journal | Số lượng |
|---|---|
| Soldier Journal | 3 |
| Neighbor Journal | 21 |
| Military Record | 8 |
| Experiment Report | 3 |
| Doctor Journal | 3 |
| Cure Record | 4 |
| Brother Journal | 5 |
| **Tổng cộng** | **39 nhật ký** |

**Script:**
> "Xuyên suốt game, người chơi thu thập **39 mẩu nhật ký** kể về câu chuyện đằng sau đại dịch — từ góc nhìn người lính, hàng xóm, bác sĩ, quân đội, và những thí nghiệm bí mật. Mỗi nhật ký có hình ảnh, nội dung, và voice log."

### 7.3 Trận Cuối & Bomb Explosion

**Màn hình: WaveQuest cuối → đánh Tank → kích hoạt bom → explosion**

| Thành phần | Mô tả |
|---|---|
| WaveQuestInteractable | Quest cuối: sống sót wave + đánh Tank |
| BombExplosionCutscene | Camera tạm, VFX nuke, SFX nổ |
| EpilogueSlide | "Dịch bệnh đã được kiểm soát..." |
| CreditsSequence | Credit cuộn với logo trường |
| EndingSequenceManager | Điều phối toàn bộ kết thúc |

**Script:**
> "Kết thúc game: người chơi phải sống sót qua trận cuối, đánh bại Tank, sau đó kích hoạt bom hủy diệt khu vực.
>
> **Bomb Explosion Cutscene** — camera chuyển cảnh, hiệu ứng nổ hạt nhân. Tiếp theo là **Epilogue** và **Credits** với tên các thành viên nhóm, trường, và sự tri ân."

---

## 🏆 PHẦN 8: GAME OVER, ACHIEVEMENT & PLAYFAB (1 phút)

### 8.1 Game Over

**Màn hình: Game Over panel**

| Thông tin | Chi tiết |
|---|---|
| Chế độ Story | Chapter, quest, journals, score |
| Chế độ Wave | Score, wave, kills, best score |
| Restart / Main Menu / Quit | 3 tùy chọn |

### 8.2 Achievement

| Achievement | Điều kiện |
|---|---|
| Speedrunner | 5 chương dưới 11 phút |
| Hell Slayer | 130 Crook kills |
| At Ease, Cooper | 20 kill khi wall run |
| Tank Slayer | Kill Tank đầu tiên |
| Close Call | Trong 5m khi Boomer nổ |

### 8.3 PlayFab

| Tính năng | Mô tả |
|---|---|
| Register/Login | Username + Password |
| Cloud Save | Best score, wave, achievement |
| Auto-save | 60 giây/lần |
| Leaderboard | Xếp hạng toàn cầu |

**Script:**
> "Cuối cùng, hệ thống **Game Over** thống kê toàn bộ thành tích. **Achievement** gồm 5 thành tựu. Và **PlayFab** đồng bộ dữ liệu lên đám mây — đăng ký, đăng nhập, auto-save, và bảng xếp hạng toàn cầu."

---

## 🎬 PHẦN 9: KẾT LUẬN (1 phút)

**Màn hình: Tổng kết**

| Hạng mục | Số lượng / Chi tiết |
|---|---|
| Ngôn ngữ | C# — Unity |
| Số scene | 3 (MainMenu, Sample, Story) |
| Số script | 80+ |
| Số chương | 5 |
| Số quest chính | 12+ |
| Số side quest | 8 |
| Số journal | 39 |
| Số loại zombie | 7 (Crook, Boomer, Tank, Witch, BigGuy, Snatcher, Hooker) |
| Số vũ khí | 9+ (Pistol, Rifle, SMG, Shotgun, Rocket, Revolver, Katana, Burst Rifle, Turret) |
| Số achievement | 5 |
| Backend | PlayFab |
| Asset Store | Cowsins Engine, Polygon Apocalypse, Polygon Zombies, Polygon Boss Zombies |

**Script:**
> "Tóm lại, dự án đã hoàn thiện một tựa game FPS Survival hoàn chỉnh với:
> - **5 chương** với cốt truyện xuyên suốt
> - **12+ nhiệm vụ chính**, **8 nhiệm vụ phụ**
> - **7 loại kẻ địch** với AI phức tạp
> - **Hệ thống skill tree 3 nhánh**
> - **39 nhật ký** kể chuyện
> - **Hệ thống wave, checkpoint, day/night cycle**
> - **Tích hợp PlayFab** lưu dữ liệu đám mây
> - **Kết thúc hoàn chỉnh** với cutscene, epilogue, credits
>
> Nhóm em xin cảm ơn thầy cô và các bạn đã lắng nghe. Rất mong nhận được góp ý để hoàn thiện sản phẩm hơn nữa!"

---

## ⏱ TỔNG THỜI GIAN DỰ KIẾN: 15-17 PHÚT

| Phần | Thời gian |
|---|---|
| 1. Giới thiệu | 2 phút |
| 2. Main Menu | 1 phút |
| 3. Chương 1 (Bình Minh) | 3 phút |
| 4. Chương 2 (Buổi Trưa) | 2 phút |
| 5. Chương 3 (Hoàng Hôn) | 2 phút |
| 6. Chương 4 (Màn Đêm) | 2 phút |
| 7. Chương 5 (Đêm Khuya + Kết thúc) | 2 phút |
| 8. Game Over & PlayFab | 1 phút |
| 9. Kết luận | 1 phút |
