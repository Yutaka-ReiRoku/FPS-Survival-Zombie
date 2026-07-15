# 🎬 KỊCH BẢN THUYẾT TRÌNH DỰ ÁN — FPS SURVIVAL ZOMBIE

> **Hướng dẫn sử dụng:**
> - Phần `[NGƯỜI A]` / `[NGƯỜI B]` — có thể phân chia theo số lượng thành viên
> - `[CLICK]` — nhấp chuột chuyển màn hình
> - `[DEMO]` — thao tác trực tiếp trên game
> - `[CHỈ MÀN HÌNH]` — chỉ tay / trỏ vào thành phần trên màn chiếu
> - Phần trong `“...”` là lời nói trực tiếp

---

## 🎯 PHẦN MỞ ĐẦU — GIỚI THIỆU (2 phút)

**Màn hình chiếu: Main Menu — dừng lại**

---

**[NGƯỜI A]:**

"Kính thưa quý thầy cô và các bạn,

Nhóm em xin được trình bày đồ án môn học: **FPS Survival Zombie** — một tựa game bắn súng góc nhìn thứ nhất kết hợp sinh tồn, lấy bối cảnh ngày tận thế zombie."

**[NGƯỜI B]:**

"Trò chơi kể về hành trình của một người sống sót duy nhất, từ lúc dịch bệnh bùng phát, vượt qua 5 chương với 5 mốc thời gian khác nhau — từ bình minh cho đến đêm khuya. Người chơi sẽ đối mặt với nhiều loại zombie, tích lũy kỹ năng, thu thập nhật ký để khám phá sự thật đằng sau đại dịch."

**[NGƯỜI A]:**

"Dự án được phát triển trên nền tảng **Unity**, sử dụng **Cowsins FPS Engine** làm hệ thống vũ khí và di chuyển nền tảng, kết hợp với **PlayFab** — dịch vụ đám mây của Microsoft — để lưu dữ liệu và bảng xếp hạng."

**[NGƯỜI B]:**

"Sau đây, nhóm em xin phép demo luồng chơi từ đầu đến cuối để quý thầy cô và các bạn có cái nhìn tổng quan nhất về dự án."

[CLICK vào Play]

---

## 🎮 PHẦN 1 — MAIN MENU & ĐĂNG NHẬP (1 phút)

**Màn hình chiếu: Main Menu**

---

**[NGƯỜI A]:**

"Đây là màn hình **Main Menu** của game."

`[CHỈ MÀN HÌNH]` "Chúng ta có nút **Play** — bắt đầu game, nút **Quit** — thoát, và hiển thị **Best Score** — điểm cao nhất đã đạt được."

"Ngoài ra, ở góc màn hình có **PlayFab Login** — người chơi có thể đăng ký tài khoản và đăng nhập. Dữ liệu sẽ được đồng bộ lên đám mây, cho phép chơi trên nhiều máy khác nhau mà không mất thành tích."

[CLICK vào Play — game bắt đầu load]

"Bây giờ, nhấn Play để vào game."

---

## 🌅 PHẦN 2 — CHƯƠNG 1: BÌNH MINH (4 phút)

**Màn hình chiếu: Game bắt đầu — Chapter 1 — Dawn**

---

### 2.1 — KHỞI ĐẦU & MÔI TRƯỜNG

**[NGƯỜI B]:**

"Người chơi bắt đầu tại **Chương 1 — Bình Minh**. Chúng ta hãy quan sát một số hệ thống nền tảng ngay từ đầu."

`[CHỈ MÀN HÌNH]`

"Đầu tiên là **DayNight Cycle** — chu kỳ ngày đêm. Lúc này đang là 6 giờ sáng, ánh sáng bình minh. Hệ thống này kiểm soát toàn bộ: màu sắc bầu trời, ánh sáng mặt trời, sương mù, ambient, và PostProcess — tạo không khí cho từng chương."

"Tiếp theo là **Checkpoint / SaveRoom** — vùng an toàn, người chơi đứng trong đó sẽ được hồi máu dần và tự động lưu checkpoint. Nếu chết, người chơi sẽ respawn tại checkpoint gần nhất."

"Trên góc phải màn hình là **Quest Tracker** — chỉ dẫn mục tiêu hiện tại. Hệ thống **StoryManager** đang quản lý toàn bộ cốt truyện, kích hoạt quest đầu tiên."

---

### 2.2 — DI CHUYỂN

**[NGƯỜI A]:**

"Về hệ thống di chuyển — ngoài đi bộ và chạy cơ bản, người chơi có thể:"

`[DEMO — vừa nói vừa chơi]`

"**Crouch và Slide** — ngồi và trượt để né đạn.
**Dash** — lướt nhanh về phía trước, có cooldown.
**Wall Run và Wall Bounce** — chạy trên tường và bật ra.
**Grappling Hook** — móc câu, bắn vào bề mặt để kéo người lại.
**Double Jump** — nhảy đôi.
Và **Climb Ladder** — leo thang."

"Tất cả các kỹ năng này đều dùng chung thanh **Stamina** — thể lực. Hết stamina thì không thể chạy, dash hay wall run được — người chơi phải quản lý stamina một cách chiến thuật."

[DEMO — chạy, dash, wall run, grappling hook trong 30 giây]

---

### 2.3 — VŨ KHÍ & CHIẾN ĐẤU

**[NGƯỜI B]:**

"Về vũ khí — game hỗ trợ đa dạng:"

`[CHỈ MÀN HÌNH — bấm chuyển vũ khí]`

"**Pistol, Rifle, SMG, Shotgun, Rocket Launcher, Revolver, Katana, Burst Rifle** — mỗi loại có chỉ số riêng về sát thương, tốc độ bắn, độ giật, dung lượng băng đạn."

"Vũ khí có cơ chế **nạp đạn**, **độ giật**, **độ xoáy đạn**, và **ngắm bắn ADS** — giống các tựa game bắn súng chuyên nghiệp."

[DEMO — bắn vài phát, nạp đạn, ngắm bắn]

"Khi bắn, chúng ta thấy **Muzzle Flash** — lửa nòng súng, và **Hitmarker** — dấu X báo hiệu trúng đích. Nếu bắn vào các bề mặt khác nhau, **lỗ đạn** cũng khác nhau — gỗ, kim loại, bùn, cỏ đều có hiệu ứng riêng."

---

### 2.4 — ZOMBIE CROOK & AI

**[NGƯỜI A]:**

"Bây giờ chúng ta gặp kẻ địch đầu tiên."

[DEMO — zombie xuất hiện, bắn]

"Đây là **Crook** — zombie thường, có hơn **28 biến thể ngoại hình**: Biker, Clown, Cop, Businessman, Bride, Cheerleader... Mỗi con có chiều cao ngẫu nhiên từ 1.5 đến 2 mét — tạo sự đa dạng."

"Về **AI**: Zombie di chuyển bằng **NavMesh**, phát hiện người chơi qua tầm nhìn và khoảng cách. Có hành vi **lung lay** — giả vờ tấn công, lao bất ngờ — tạo cảm giác tự nhiên. Nếu mất dấu người chơi, chúng sẽ **wander** — đi lang thang tìm kiếm."

"Hệ thống **AIDirector** theo dõi mức đe dọa từ 0 đến 100 với 4 trạng thái:
- **Calm**: yên tĩnh
- **BuildUp**: căng thẳng tăng
- **Attack**: tấn công dồn dập
- **Recovery**: hạ nhiệt

Nếu người chơi đứng yên quá 10 giây — cắm trại — zombie sẽ spawn ngay sau lưng để chống camp."

"Spawner sử dụng **Object Pooling** — pool tối đa 60 zombie, tái sử dụng thay vì tạo mới — tối ưu hiệu năng. Mỗi spawner có kiểm tra NavMesh hợp lệ, tránh spawn trong tường."

---

### 2.5 — HUD & GIAO DIỆN

**[NGƯỜI B]:**

"Bây giờ chúng ta nhìn vào giao diện HUD — đây là thành phần hiển thị xuyên suốt game."

`[CHỈ TỪNG WIDGET TRÊN MÀN HÌNH]`

"**Health Bar** — thanh máu, khi máu thấp sẽ xuất hiện viền đỏ Low Health Vignette.
**Stamina Bar** — thể lực.
**Ammo** — số đạn còn lại.
**Dash Cooldown** — hồi chiêu dash.
**Crosshair** — tâm ngắm, giãn ra khi bắn.
**Compass** — la bàn, hiển thị hướng mục tiêu quest.
**Damage Direction** — chỉ thị hướng bị tấn công.
**Combat Feedback** — số sát thương hiện ra khi bắn trúng và kill feed.
**FPS Counter** — chỉ số khung hình.
**Threat Level** — mức đe dọa.
**Interact Prompt** — 'Nhấn E để tương tác'."

---

### 2.6 — QUEST & CHUYỂN CHƯƠNG

**[NGƯỜI A]:**

"Hệ thống nhiệm vụ — toàn bộ cốt truyện có hơn **12 quest chính** xuyên suốt 5 chương. Người chơi hoàn thành quest bằng cách: đến một vị trí, tiêu diệt đủ số zombie, thu thập đủ vật phẩm, hoặc tương tác với một vật thể."

"Mỗi quest hoàn thành được thưởng **EXP** để lên cấp và **Journal** — nhật ký để ghép câu chuyện."

[DEMO — hoàn thành quest, cutscene chuyển chương]

"Khi hoàn thành Chương 1, **Cutscene** chuyển cảnh xuất hiện với dòng chữ: 'Chương 2 — Bệnh Viện'. Lúc này DayNight Cycle snap chuyển từ Dawn sang Noon."

---

## ☀️ PHẦN 3 — CHƯƠNG 2: BUỔI TRƯA (2 phút)

**Màn hình chiếu: Chapter 2 — Noon — Hospital**

---

**[NGƯỜI B]:**

"Chương 2 — **Buổi Trưa tại Bệnh Viện**. Thời gian chuyển sang 12 giờ trưa — ánh sáng thay đổi rõ rệt nhờ DayNight Cycle."

"Độ khó tăng dần: zombie đông hơn, cứng hơn. Người chơi sẽ khám phá khu vực bệnh viện với các quest mới."

**[NGƯỜI A]:**

"Điểm đặc biệt ở chương này: sau khi hoàn thành main quest, **Side Quest** bắt đầu được mở khóa. Side Quest là hệ thống nhiệm vụ phụ song song, độc lập với cốt truyện chính."

"Có tổng cộng **8 Side Quest**:"
- Church — Nhà thờ
- Auto Repair — Tiệm sửa xe
- Motel — Nhà nghỉ
- Quarantine — Khu cách ly
- HighRise Base — Căn cứ cao tầng
- Mother's Story — Câu chuyện người mẹ
- Lighthouse — Hải đăng

"Mỗi side quest có mục tiêu riêng: tiêu diệt, thu thập, hoặc tương tác. Phần thưởng thường là EXP và journal hiếm."

---

## 🏗️ PHẦN 4 — CHƯƠNG 3: HOÀNG HÔN (2.5 phút)

**Màn hình chiếu: Chapter 3 — Dusk — Construction Site**

---

### 4.1 — WAVE SYSTEM

**[NGƯỜI B]:**

"Chương 3 — **Hoàng Hôn tại Công Trường**. Tại đây, game giới thiệu hệ thống **Wave**."

"Hệ thống này do **WaveManager** quản lý. Số zombie mỗi wave tăng dần: wave đầu 10 con, wave sau tăng thêm 5 con — wave thứ 10 sẽ có 55 zombie."

"Đặc biệt có dạng quest **Wave Quest** — người chơi bị khóa trong một khu vực, cổng đóng lại, phải sống sót qua nhiều đợt sóng. Nếu cố gắng ra ngoài, sẽ bị teleport trở lại."

"Ví dụ: quest generator ở công trường — người chơi phải sống sót 3 wave bao gồm cả Boomer."

---

### 4.2 — BOOMER

**[NGƯỜI A]:**

"Và đây là **Boomer** — zombie đặc biệt đầu tiên."

[DEMO — Boomer xuất hiện]

"Boomer có 100 máu. Khi phát hiện người chơi, nó kêu rít lên cảnh báo, sau đó lao vào và **phát nổ** — gây sát thương vùng lớn và để lại **vũng acid** trên mặt đất. Dẫm vào vũng acid sẽ bị sát thương liên tục."

"Người chơi có thể bắn chết Boomer từ xa trước khi nó kịp đến gần. Nếu tiêu diệt thành công, Boomer rơi ra loot."

"Boomer được spawn bởi **SpecialEnemyDirector** — hệ thống spawn quái đặc biệt, kích hoạt từ wave 3 trở đi."

---

## 🌃 PHẦN 5 — CHƯƠNG 4: MÀN ĐÊM (2.5 phút)

**Màn hình chiếu: Chapter 4 — Night — Residential Area**

---

### 5.1 — SKILL TREE

**[NGƯỜI B]:**

"Chương 4 — **Màn Đêm tại Khu Dân Cư**. Lúc này, người chơi đã tích lũy đủ điểm kỹ năng, và đây là lúc giới thiệu **Skill Tree**."

[DEMO — mở skill tree panel]

"Skill Tree có 3 nhánh, mỗi nhánh 5 node, tổng cộng 15 node:"

`[CHỈ VÀO TỪNG NHÁNH]`

"**Nhánh 1 — Movement**: tăng tốc đi bộ → tốc độ chạy → điều khiển trên không + dash mới → wall run / wall bounce → double jump + grappling hook. Cộng dồn Stamina mỗi node."

"**Nhánh 2 — Aim**: giảm độ giật → tỉ lệ crit 10% → tỉ lệ crit 20% → sát thương crit x1.5 → one-shot Crook + bonus sát thương special. Cộng dồn Damage mỗi node."

"**Nhánh 3 — Intelligence**: bán kính hút EXP → hệ số nhân EXP 1.1x → bán kính rộng hơn → hệ số 1.15x → bán kính tối đa + highlight vật phẩm. Cộng dồn HP mỗi node."

"Mỗi node có chi phí tăng dần: node 1 tốn 2 điểm, node cuối tốn 12 điểm. Người chơi phải cân nhắc lựa chọn chiến thuật."

---

### 5.2 — TANK

**[NGƯỜI A]:**

"Khi wave đạt đến mức 5 trở lên, **Tank** bắt đầu xuất hiện."

[DEMO — Tank gầm, xuất hiện]

"Tank là **boss chính** của game — 500 máu cơ bản, tỉ lệ theo wave. Nó có 3 đòn tấn công:
- **Punch** — đấm gần
- **Swipe** — vả diện rộng
- **Jump Attack** — nhảy từ xa đến chỗ người chơi"

"Khi Tank xuất hiện, nó gầm một tiếng rất to — âm thanh 2D vang toàn bộ map — cảnh báo người chơi. Thanh **Boss Health Bar** xuất hiện trên HUD hiển thị máu của Tank."

---

## 🌆 PHẦN 6 — CHƯƠNG 5: ĐÊM KHUYA & KẾT THÚC (3 phút)

**Màn hình chiếu: Chapter 5 — Deep Night — Apartment Building**

---

### 6.1 — WITCH & BIG GUY

**[NGƯỜI B]:**

"Chương 5 — **Đêm Khuya tại Chung Cư**. Đây là chương cuối, thời gian 2 giờ sáng — tối nhất trong game. Đèn pin là thiết yếu."

"Tại đây xuất hiện 2 mini-boss đặc biệt:"

[DEMO — Witch đang khóc]

"**Witch** — mô phỏng theo game Left 4 Dead. Witch ngồi khóc ở một vị trí cố định. Nếu người chơi đến gần hoặc bắn trúng, nó sẽ đứng dậy, gào thét và lao với tốc độ cực nhanh — **6.5 m/giây**. Tuy chỉ có 60 máu nhưng sát thương rất cao. Nếu người chơi chạy ra xa và mất dấu, Witch sẽ quay lại vị trí cũ và tiếp tục khóc."

[DEMO — Big Guy đứng choáng]

"**Big Guy** — một người đàn ông mặc váy công chúa, đứng choáng váng tại chỗ. 80 máu. Nếu người chơi đến gần hoặc tấn công, nó gầm lên và bắt đầu rượt đuổi — chậm nhưng rất trâu, mỗi đòn punch gây sát thương nặng."

---

### 6.2 — JOURNAL & VOICE LOG

**[NGƯỜI A]:**

"Xuyên suốt game, người chơi thu thập **39 mẩu nhật ký** — chia thành 7 nhóm:"

`[CHỈ VÀO DANH SÁCH]`

"Nhật ký của Người Lính — 3 cái
Nhật ký của Người Hàng Xóm — 21 cái
Hồ sơ Quân Đội — 8 cái
Báo cáo Thí Nghiệm — 3 cái
Nhật ký Bác Sĩ — 3 cái
Hồ sơ Thuốc Chữa — 4 cái
Nhật ký Người Anh — 5 cái"

[DEMO — nhặt journal, popup hiện ra]

"Mỗi nhật ký gồm: **tiêu đề, hình ảnh, nội dung văn bản**, và **voice log** — giọng đọc. Âm thanh voice log sẽ phát khi người chơi nhặt journal. Thu thập càng nhiều, người chơi càng hiểu rõ câu chuyện đằng sau đại dịch."

---

### 6.3 — TRẬN CUỐI

**[NGƯỜI B]:**

"Kết thúc game: người chơi phải hoàn thành **Quest 12 — Escape Town**, nhiệm vụ cuối cùng."

[DEMO — WaveQuestInteractable cuối]

"Đây là **Wave Quest** đặc biệt — người chơi bị khóa trong khu vực, phải sống sót qua nhiều đợt sóng. Ở wave cuối, **Tank** xuất hiện — người chơi phải đánh bại nó."

"Sau khi Tank bị tiêu diệt, **WaveQuestInteractable** cho phép tương tác lần 2 — kích hoạt bom."

---

### 6.4 — ENDING SEQUENCE

**[NGƯỜI A]:**

"Sau khi kích hoạt bom, **EndingSequenceManager** điều phối toàn bộ kết thúc:"

[DEMO — để game chạy cutscene]

"**Bước 1 — Bomb Explosion Cutscene**"
"Camera chuyển sang một camera tạm thời, quay cảnh nổ bom — hiệu ứng VFX hạt nhân, âm thanh nổ lớn. Màn hình fade to black."

"**Bước 2 — Epilogue Slide**"
"Hiển thị dòng chữ: 'Dịch bệnh đã được kiểm soát, nhưng ai là người mang đến phương thuốc chữa — vẫn còn là một bí ẩn.'"

"**Bước 3 — Credits Sequence**"
"Credit cuộn lên với: Tên các thành viên nhóm phát triển, tên trường, các tài nguyên sử dụng, Cowsins Engine, lời cảm ơn."

"**Bước 4 — Return to Main Menu**"
"Tự động quay lại Main Menu."

---

## 🏆 PHẦN 7 — GAME OVER, ACHIEVEMENT & PLAYFAB (1.5 phút)

**Màn hình chiếu: Game Over panel**

---

### 7.1 — GAME OVER

**[NGƯỜI B]:**

"Nếu người chơi chết, **Game Over Manager** hiển thị bảng thống kê."

`[CHỈ MÀN HÌNH]`

"**Chế độ Story**: chapter đang chơi, số quest đã hoàn thành, số journal thu thập được, điểm số.
**Chế độ Wave**: điểm, wave đạt được, số kill, best score."

"Có 3 lựa chọn: **Restart** — respawn tại checkpoint gần nhất, **Main Menu** — về menu, **Quit** — thoát game."

---

### 7.2 — ACHIEVEMENTS

**[NGƯỜI A]:**

"Hệ thống **Achievement** gồm 5 thành tựu:"

"1. **Speedrunner** — Hoàn thành 5 chương trong vòng chưa đầy 11 phút.
2. **Hell Slayer** — Tiêu diệt 130 Crook.
3. **At Ease, Cooper** — Tiêu diệt 20 zombie khi đang wall run.
4. **Tank Slayer** — Lần đầu tiên hạ Tank.
5. **Close Call** — Ở trong bán kính 5 mét khi Boomer phát nổ."

"Achievement được persistent — lưu lại dù có thoát game, và đồng bộ lên PlayFab."

---

### 7.3 — PLAYFAB CLOUD

**[NGƯỜI B]:**

"**PlayFab** là dịch vụ đám mây của Microsoft được tích hợp vào game với các tính năng:"

"**Đăng ký / Đăng nhập** bằng Username và Password.
**Cloud Save** — lưu best score, best wave, trạng thái achievement.
**Auto-save** — tự động lưu mỗi 60 giây và khi pause/quit game.
**Leaderboard** — bảng xếp hạng toàn cầu dựa trên Best Score.
**Merge dữ liệu** — nếu chơi trên nhiều máy, hệ thống tự động hợp nhất và lấy giá trị cao nhất."

---

## 🎬 PHẦN 8 — KẾT LUẬN (1 phút)

**Màn hình chiếu: Slide tổng kết**

---

**[NGƯỜI A]:**

"Xin phép tổng kết dự án bằng một số con số:"

`[CHỈ VÀO SLIDE]`

| Hạng mục | Con số |
|---|---|
| Engine | Unity — C# |
| Tổng số Scene | 3 |
| Tổng số Script | Hơn 80 |
| Số chương | 5 |
| Số main quest | Hơn 12 |
| Số side quest | 8 |
| Số journal | 39 |
| Số loại zombie | 7 |
| Số loại vũ khí | 9 |
| Số achievement | 5 |
| Skill tree nodes | 15 |
| Dịch vụ Cloud | PlayFab |

**[NGƯỜI B]:**

"Như vậy, dự án đã hoàn thiện một tựa game **FPS Survival hoàn chỉnh** với:
- Cốt truyện xuyên suốt 5 chương
- Hệ thống chiến đấu và di chuyển phong phú
- 7 loại kẻ địch với AI thông minh
- Hệ thống skill tree, wave, checkpoint
- 39 mẩu nhật ký kể chuyện
- Kết thúc hoàn chỉnh với cutscene và credits
- Và tích hợp PlayFab cho dữ liệu đám mây"

**[NGƯỜI A]:**

"Cuối cùng, nhóm em xin chân thành cảm ơn quý thầy cô và các bạn đã dành thời gian lắng nghe. Đây là sản phẩm của cả quá trình học tập và làm việc nhóm, chắc chắn còn nhiều thiếu sót. Nhóm em rất mong nhận được những góp ý từ thầy cô và các bạn để có thể hoàn thiện hơn nữa."

**[CẢ NHÓM CÚI CHÀO]**

"Xin cảm ơn!"

---

## ⏱ TỔNG THỜI GIAN DỰ KIẾN

| Phần | Thời gian | Người nói |
|---|---|---|
| Mở đầu — Giới thiệu | 2 phút | A + B |
| Main Menu & Đăng nhập | 1 phút | A |
| Chương 1 — Bình Minh (6 mục) | 4 phút | A + B |
| Chương 2 — Buổi Trưa + Side Quest | 2 phút | B + A |
| Chương 3 — Hoàng Hôn + Wave + Boomer | 2.5 phút | B + A |
| Chương 4 — Màn Đêm + Skill Tree + Tank | 2.5 phút | B + A |
| Chương 5 — Đêm Khuya + Ending | 3 phút | B + A |
| Game Over + Achievement + PlayFab | 1.5 phút | B + A |
| Kết luận | 1 phút | A + B |
| **Tổng cộng** | **~20 phút** | |

---

## 📋 CHECKLIST CHUẨN BỊ TRƯỚC KHI THUYẾT TRÌNH

- [ ] Mở Unity — load scene Main Menu
- [ ] Đảm bảo game ở chế độ Play sẵn sàng
- [ ] Kiểm tra âm thanh (loa / tai nghe)
- [ ] Chuẩn bị slide tổng kết (nếu có)
- [ ] Mở file kịch bản này trên màn hình phụ (nếu cần)
- [ ] Test trước luồng chơi để tránh bug
- [ ] Đảm bảo checkpoint được set đúng chapter cần demo
- [ ] Chuẩn bị cheats / developer mode để skip nếu cần
