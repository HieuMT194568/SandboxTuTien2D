# Sandbox Tu Tiên 2D Pixel

Chào mừng bạn đến với **Sandbox Tu Tiên 2D Pixel** — game nhập vai tu tiên góc nhìn từ trên xuống, viết bằng **C#** trên **MonoGame framework**. Người chơi chọn một trong 4 **lưu phái**, đả tọa tích lũy tu vi, săn Yêu Thú lấy Yêu Đan, luyện đan luyện khí, bày trận pháp và vượt qua bình cảnh — từ xung kích kinh mạch ở Luyện Khí cho tới độ Thiên Kiếp từ Kim Đan trở lên.

Mã nguồn theo ba nguyên lý: **Data-Driven**, **Component-Based** và **Event-Driven**, giúp mở rộng dễ dàng và tránh code chồng chéo.

---

## 🗺️ Kiến Trúc Hệ Thống

### 1. Data-Driven (Hướng dữ liệu)
*   Toàn bộ Đan Dược, Vật Liệu, Pháp Khí, Lưu Phái, Chiêu Thức và công thức luyện chế được định nghĩa trong JSON (`Content/Data/*`).
*   `DataLoader` dùng `System.Text.Json` để chuyển các file thành POCO trong `Data/Models/` (`ConsumableData`, `MagicWeaponData`, `ClassData`, `SkillData`...). `GameDataValidator` kiểm tra chéo `classes.json`/`skills.json` lúc khởi động (chiêu tồn tại, đúng lưu phái, đúng ô, cảnh giới hợp lệ) và chỉ log cảnh báo thay vì crash nếu có lỗi đánh máy.
*   Lò Luyện tự gom công thức từ trường `crafting_recipe`; Chiêu Thức kiểu `projectile` phóng theo `pattern` (`fan` / `barrage` / `nova`) — thêm nội dung mới không cần sửa code C#.

### 2. Component-Based (Hướng thành phần)
*   `Player` là thùng chứa dữ liệu mỏng, sở hữu `CultivationComponent`, `InventoryComponent` và `SkillLoadoutComponent`.
*   `CultivationComponent` là máy trạng thái hữu hạn (**FSM**), chỉ lo tu vi/đột phá:
    *   `Idle` — Nhàn rỗi
    *   `Meditating` — Đả Tọa, tích lũy tu vi
    *   `BreakthroughReady` — Chạm bình cảnh (tầng 10, 20, 30...), tu vi bị khóa
    *   `Breakthrough` — Đang đột phá (xung quan hoặc độ Thiên Kiếp)
    *   `Dead` — Tử vong do Yêu Thú tấn công
*   `SkillLoadoutComponent` tách riêng: lưu phái đã chọn quyết định bộ 6 ô chiêu (đánh thường/chiêu 1-3/lướt/tuyệt kỹ), mở khóa theo cảnh giới (`unlock_realm` của từng chiêu so với `CultivationComponent.CurrentRealm`), và theo dõi độ thông thạo (số lần dùng) để lên bậc công pháp.
*   `StatusEffectComponent` (gắn trên `Monster`) quản lý hiệu ứng trạng thái dùng chung: Trói, Choáng, Đóng băng, Chậm, Thiêu đốt, Độc theo thời gian, Khiên, buff/debuff.

### 3. Event-Driven (Hướng sự kiện)
`EventManager` là EventBus strongly-typed. Các sự kiện chính:
*   `OnLevelUpEvent`, `OnRealmChangedEvent`, `OnBottleneckReachedEvent`
*   `OnBreakthroughSuccessEvent`, `OnBreakthroughFailedEvent`
*   `OnLightningStrikeEvent` — mỗi đợt lôi kiếp; `Game1` sinh tia sét có vòng báo hiệu
*   `OnDamageDealtEvent`, `OnStatusAppliedEvent`, `OnElementalReactionEvent` — do `CombatSystem` phát khi tính sát thương/áp trạng thái
*   `OnPlayerDiedEvent`

### 4. Time-Slicing & Thời gian game
*   `CultivationSystem` chỉ cập nhật tối đa 10 thực thể mỗi frame theo vòng xoay (Round-Robin).
*   `GameTimeManager` đồng bộ thời gian tỷ lệ `1:1440` (1 phút thực = 1 ngày trong game).

---

## 📁 Cấu Trúc Thư Mục

```text
SandboxTuTien2D/
├── Core/
│   ├── EventManager.cs          # EventBus + các sự kiện nghiệp vụ
│   ├── GameTimeManager.cs       # Thời gian thế giới (1s thực = 24 phút game)
│   ├── Particle.cs              # Hệ thống hạt
│   ├── PixelArtGenerator.cs     # Hình vẽ thủ tục dự phòng khi thiếu file .png
│   └── Combat/                  # Element (khắc hệ), Projectile, ProjectilePool
│
├── Data/
│   ├── DataLoader.cs            # Đọc JSON trong Content/Data
│   ├── GameDataValidator.cs     # Kiểm tra chéo classes.json / skills.json lúc khởi động
│   ├── MySqlDbManager.cs        # Tự tạo CSDL sandboxtutien_v2, lưu/tải toàn thế giới
│   └── Models/                  # ConsumableData, MagicWeaponData, ClassData, SkillData, ItemEffect, CraftingIngredient
│
├── Components/
│   ├── CultivationComponent.cs    # FSM tu luyện, Linh Căn, bình cảnh, xung quan, Thiên Kiếp, Tâm Ma
│   ├── SkillLoadoutComponent.cs   # Bộ 6 ô chiêu theo lưu phái, mở khóa theo cảnh giới, độ thông thạo
│   ├── StatusEffectComponent.cs   # Hiệu ứng trạng thái dùng chung (trói, độc, choáng, khiên, buff...)
│   ├── InventoryComponent.cs      # Túi trữ vật, dùng đan dược, trang bị pháp khí
│   └── ConsumableSpawner.cs       # Sinh vật phẩm theo chu kỳ (Đan Sư, Lò Luyện)
│
├── Systems/
│   ├── CultivationSystem.cs     # Time-Slicing tối đa 10 entity/frame
│   ├── CombatSystem.cs          # Va chạm đạn, tìm mục tiêu vùng, tính sát thương, đẩy lùi
│   ├── SkillSystem.cs           # Thi triển chiêu: mở khóa, hồi chiêu, Linh Lực, hệ số thông thạo
│   └── ZoneSystem.cs            # Vùng hiệu ứng tồn tại theo thời gian (chiêu kiểu "zone")
│
├── Entities/
│   ├── Player.cs                # Người chơi
│   ├── Monster.cs               # Yêu Thú (tuổi → phẩm giai, tiến hóa, độc, trói, uy áp)
│   ├── FormationArray.cs        # Trận Pháp tự động công kích
│   └── DroppedItem.cs           # Vật phẩm rơi trên đất
│
├── Content/
│   ├── Data/                    # consumables.json, phap_khi.json, classes.json, skills.json
│   ├── Fonts/Arial.spritefont   # Font có đủ ký tự tiếng Việt
│   └── Sprites/                 # Sprite .png (thiếu file nào sẽ dùng hình vẽ thủ tục)
│
├── Game1.cs                     # Vòng lặp chính MonoGame, màn chọn lưu phái, HUD, input, lôi kiếp, Lò Luyện
├── Program.cs
└── SandboxTuTien.csproj         # .NET 8.0
```

---

## ⚔️ Cơ Chế Tu Tiên

### 0. Lưu Phái
Chọn một trong 4 lưu phái khi bắt đầu game mới — cố định suốt ván, quyết định hệ số chỉ số, pháp khí dùng được, 2 nội tại và bộ 6 ô chiêu (đánh thường/chiêu 1-3/lướt/tuyệt kỹ):

| Lưu phái | Vai trò | HP / Linh lực / Tốc độ | Nội tại |
| :--- | :--- | :--- | :--- |
| **Kiếm Tu** | Sát thương đơn mục tiêu, cơ động | 100% / 90% / 110% | Kiếm Ý (đánh trúng liên tục +3%/lần, tối đa +30%), Kiếm Tâm Thông Minh (+15% chí mạng) |
| **Pháp Tu** | Sát thương diện rộng, khống chế — bộ chiêu chia theo hệ Linh Căn (Hỏa/Mộc/Băng) | 80% / 140% / 100% | Ngũ Hành Tương Khắc (khắc hệ +75% thay vì +50%), Linh Hải |
| **Thể Tu** | Cận chiến, chống chịu | 150% / 70% / 95% | Đồng Bì Thiết Cốt, Khí Huyết Cuồn Cuộn |
| **Phù Trận Sư** | Bẫy, hỗ trợ, trận pháp | 90% / 120% / 100% | Trận Đạo Tinh Thông, Phù Lục Tiết Kiệm |

Mỗi chiêu mở khóa theo cảnh giới (VD: đánh thường + chiêu 1 ở Luyện Khí, tuyệt kỹ ở Nguyên Anh) và có 4 bậc công pháp (Nhập Môn → Tiểu Thành → Đại Thành → Viên Mãn), lên bậc bằng số lần dùng, mỗi bậc cộng thêm % hiệu quả.

> **Hiện trạng:** Kiếm Tu chơi được trọn vẹn (cả 6 ô chiêu: bắn đạn, lướt, mưa kiếm tuyệt kỹ). Pháp Tu/Thể Tu/Phù Trận Sư đã có đủ dữ liệu và chọn được, nhưng các chiêu kiểu cận chiến (`melee_arc`), đòn xuống đất (`ground_aoe`), buff bản thân (`self_buff`) và vận công (`channel`) **chưa được thi triển** — bấm sẽ báo "Chiêu này chưa hỗ trợ!" thay vì tiêu hao Linh Lực. Việc này sẽ hoàn thiện ở giai đoạn kế tiếp.

### 1. Cảnh Giới
Tu vi gồm 100 tầng, mỗi đại cảnh giới 10 tầng:

| Tầng | Cảnh giới | Tầng | Cảnh giới |
| :--- | :--- | :--- | :--- |
| 1 – 10 | **Luyện Khí** | 51 – 60 | **Luyện Hư** |
| 11 – 20 | **Trúc Cơ** | 61 – 70 | **Hợp Thể** |
| 21 – 30 | **Kim Đan** | 71 – 80 | **Đại Thừa** |
| 31 – 40 | **Nguyên Anh** | 81 – 90 | **Độ Kiếp** |
| 41 – 50 | **Hóa Thần** | 91 – 99 | **Chân Tiên** |
| | | 100 | **Tiên Đế** |

### 2. Linh Căn
Phẩm chất Linh Căn nhân trực tiếp vào tốc độ đả tọa (`5 × hệ số × Δt` tu vi/giây):

| Hệ số | Linh Căn |
| :--- | :--- |
| ≥ 1.8 | Thiên Linh Căn |
| ≥ 1.2 | Chân Linh Căn |
| ≥ 0.5 | Tạp Linh Căn |
| > 0 | Ngụy Linh Căn |
| 0 | Phế Linh Căn (không thể tu luyện) |

Pháp Tu có Thiên Linh Căn theo hệ đã chọn (Hỏa/Mộc/Băng) — quyết định luôn bộ chiêu. Các lưu phái khác có Chân Linh Căn vô thuộc tính (bộ chiêu của họ không phụ thuộc hệ). Nhân vật: **Lâm Phong**, khởi đầu Luyện Khí tầng 1.

### 3. Bình Cảnh & Đột Phá
Mỗi khi đạt tầng 10, 20, 30... tu vi bị khóa. Nhấn `[R]` để đột phá:

*   **Xung quan** (bình cảnh tầng 10, 20): 5 đợt linh lực phản phệ, không thể né.
    $$\text{Sát thương mỗi đợt} = \text{MaxHP} \times (1 - \text{Tỷ lệ}) \times \text{Random}(0.3 \to 0.5)$$
*   **Độ Thiên Kiếp** (từ tầng 30 trở lên): `2 + tầng/10` đợt lôi kiếp. Mỗi đạo sét hiện **vòng đỏ báo hiệu** 0.8 giây rồi mới đánh xuống — người chơi được di chuyển để né.
    $$\text{Sát thương mỗi đạo} = \text{MaxHP} \times (0.12 + 0.4 \times (1 - \text{Tỷ lệ}))$$

**Tỷ lệ thành công:**
$$\text{Tỷ lệ} = \text{Tỷ lệ gốc} + \text{Đan dược} + 2\% \times \text{số lần bấm Space} - \text{Tâm Ma}$$
*   Tỷ lệ gốc: tầng 10 = 85%, mỗi bình cảnh sau giảm 15%, tối thiểu 20%.
*   Đan dược: dùng **Phá Cảnh Đan** (+15% mỗi viên, tối đa +50%), tiêu hết sau mỗi lần đột phá.
*   Kết quả được giới hạn trong khoảng 5% – 100%.

**Kết quả:**
*   **Thành công**: hồi đầy HP/Linh Lực, Tâm Ma tiêu tan, 1% cơ duyên thức tỉnh **Vạn Độc Thể**. Cảnh giới mới có thể mở khóa thêm ô chiêu trong Thanh Chiêu (báo bằng chữ nổi "Lĩnh ngộ: ...").
*   **Thất bại** (HP về 0 khi đột phá): không chết, nhưng **đạo cơ tổn hại** — rớt 1 tầng tu vi, còn 20% HP, **Tâm Ma +10%** (tối đa 50%).

### 4. Yêu Thú & Yêu Đan
*   Tuổi Yêu Thú tăng 8–15 năm mỗi giây thực, quyết định **phẩm giai** (Nhất → Cửu Giai tại các mốc 100 / 300 / 1.000 / 3.000 / 10.000 / 30.000 / 100.000 / 300.000 năm), HP và kích thước.
*   Yêu Thú ≥ 10.000 năm tỏa **uy áp** khiến Yêu Thú dưới 1.000 năm hoảng sợ bỏ chạy.
*   Trảm sát Yêu Thú rơi **Yêu Đan** (Hạ / Trung / Thượng phẩm theo phẩm giai) và 40% rơi nguyên liệu theo hệ (Mộc → Linh Mộc Tâm, Hỏa → Hỏa Tinh Thạch, Băng → Hàn Thiết).
*   Luyện hóa Yêu Đan trực tiếp để tăng tu vi, hoặc dùng làm nguyên liệu luyện đan.

### 5. Luyện Đan & Luyện Khí
Đứng cạnh **Lò Luyện** và nhấn `[G]`. Công thức mặc định:

| Thành phẩm | Nguyên liệu | Yêu cầu |
| :--- | :--- | :--- |
| Thanh Phong Phi Kiếm | 1 Hàn Thiết, 1 Hỏa Tinh Thạch | Luyện Khí |
| Xích Diễm Kiếm Hạp | 3 Hàn Thiết, 5 Hỏa Tinh Thạch, 2 Linh Mộc Tâm | Trúc Cơ |
| Bổ Linh Đan | 1 Hạ Phẩm Yêu Đan, 1 Linh Mộc Tâm | Luyện Khí |
| Phá Cảnh Đan | 2 Hạ Phẩm Yêu Đan, 1 Hỏa Tinh Thạch | Luyện Khí |

NPC **Đan Sư** tự luyện Hồi Xuân Đan mỗi 15 giây; Lò Luyện sinh Linh Thạch mỗi 10 giây.

### 6. Trận Pháp
Tiêu hao 1 **Linh Thạch** để cắm Trận Kỳ (tối đa 3), nạp lại bằng Linh Thạch:
*   **Kim Châm Trận**: bắn nhanh (0.5s), 10 sát thương, không kinh động Yêu Thú.
*   **Phá Quân Kiếm Trận**: chậm (3s), 80 sát thương, tốn 2 phần linh lực.
*   **Vạn Độc Trận**: 2s, phun độc vụ diện rộng 80px, gây độc 5 giây.

### 7. Khắc Hệ
**Hỏa > Mộc > Băng > Hỏa** — đòn khắc hệ gây thêm **50%** sát thương. Hỏa hệ thiêu sạch độc tố; Mộc hệ mạnh trói chân Yêu Thú (Yêu Thú tốc độ bị trói lâu hơn).

---

## 🎮 Cài Đặt & Chạy

### Yêu cầu
1. **.NET 8.0 SDK**.
2. Windows.
3. (Tùy chọn) **MySQL** tại `localhost:3306`, user `root`. Game tự tạo CSDL `sandboxtutien_v2`; nếu không kết nối được sẽ chạy offline bằng JSON (không lưu/tải được).

### Chạy game
Tại thư mục gốc `SandboxTuTien2D`:

```powershell
dotnet build
dotnet run
```

### 🧙 Màn Hình Chọn Lưu Phái
Mở game là vào thẳng màn chọn lưu phái: **`[1]`–`[4]`** chọn lưu phái, **`[Enter]`** xác nhận. Nếu chọn Pháp Tu, bước tiếp theo chọn hệ Linh Căn: **`[1]`** Hỏa, **`[2]`** Mộc, **`[3]`** Băng, **`[Enter]`** xác nhận, **`[Backspace]`** quay lại chọn lưu phái.

### ⌨️ Phím Điều Khiển (trong game)
*   **`[W][A][S][D]` / Mũi tên**: Di chuyển.
*   **Chuột trái**: Đánh thường (chiêu "basic" của lưu phái). **Chuột giữa**: Gọi Yêu Thú ngẫu nhiên tại con trỏ (phục vụ test).
*   **`[M]`**: Bật/tắt Đả Tọa.
*   **`[R]`**: Đột phá khi chạm bình cảnh. Trong lúc đột phá, bấm **`[Space]`** liên tục để ổn định đạo tâm (+2% mỗi lần); khi độ Thiên Kiếp, di chuyển để né vòng đỏ.
*   **`[Q]` / `[E]` / `[C]`**: Thi triển chiêu 1 / 2 / 3. **`[Shift]`**: Lướt. **`[X]`**: Tuyệt kỹ. (Tiêu hao Linh Lực; ô chưa mở khóa hoặc chiêu chưa hỗ trợ sẽ báo rõ lý do.)
*   **`[Tab]`**: Đổi pháp khí trang bị (không ảnh hưởng sát thương — đòn đánh thường lấy từ lưu phái). **`[I]`**: Túi trữ vật. **`[1]`–`[5]`**: Dùng nhanh vật phẩm.
*   **`[G]`**: Mở Lò Luyện khi đứng gần (chọn công thức bằng `[F1]`–`[F4]` hoặc chuột).
*   **`[T]`**: Bày trận (tốn 1 Linh Thạch). **`[Y]`**: Đổi loại trận. **`[F]`**: Nạp Linh Thạch cho trận gần nhất.
*   **`[F5]` / `[F9]`**: Lưu / Tải game từ MySQL (gồm cả độ thông thạo chiêu thức).
*   **Phím gian lận (test)**: `[Space]` +500 tu vi (khi không đột phá), `[U]` thức tỉnh Vạn Độc Thể, `[H]` nhận 3 Phá Cảnh Đan.
*   **`[Esc]`**: Thoát game.

---

## 📊 Trạng Thái Hiện Tại

| Module | Tệp | Trạng thái |
| :--- | :--- | :--- |
| Hệ sự kiện | [EventManager.cs](Core/EventManager.cs) | ✅ Hoàn thành |
| Thời gian thế giới | [GameTimeManager.cs](Core/GameTimeManager.cs) | ✅ Hoàn thành |
| Dữ liệu JSON | [Content/Data/](Content/Data/) + [Data/Models/](Data/Models/) | ✅ Đan dược, pháp khí, lưu phái, chiêu thức, công thức |
| FSM tu luyện | [CultivationComponent.cs](Components/CultivationComponent.cs) | ✅ Bình cảnh, xung quan, Thiên Kiếp, Tâm Ma, Đan dược, hệ số chỉ số theo lưu phái |
| Lưu phái & chiêu thức | [SkillLoadoutComponent.cs](Components/SkillLoadoutComponent.cs), [SkillSystem.cs](Systems/SkillSystem.cs) | 🟡 4 lưu phái chọn được; Kiếm Tu chơi trọn vẹn (projectile/dash/zone), 4 kiểu ra đòn còn lại chờ giai đoạn sau |
| Time-Slicing | [CultivationSystem.cs](Systems/CultivationSystem.cs) | ✅ Hoàn thành |
| Túi trữ vật | [InventoryComponent.cs](Components/InventoryComponent.cs) | ✅ Hoàn thành |
| Chiến đấu | [CombatSystem.cs](Systems/CombatSystem.cs) | ✅ Va chạm, tìm mục tiêu vùng/quạt, khắc hệ, chí mạng, đẩy lùi, hiệu ứng trạng thái |
| Yêu Thú | [Monster.cs](Entities/Monster.cs), [StatusEffectComponent.cs](Components/StatusEffectComponent.cs) | ✅ Phẩm giai, tiến hóa, uy áp, hiệu ứng trạng thái dùng chung |
| Trận pháp | [FormationArray.cs](Entities/FormationArray.cs) | ✅ 3 loại trận |
| Lò Luyện | [Game1.cs](Game1.cs) | ✅ Luyện đan & luyện khí data-driven |
| CSDL MySQL | [MySqlDbManager.cs](Data/MySqlDbManager.cs) | ✅ Lưu/tải toàn bộ thế giới + lưu phái + độ thông thạo chiêu thức |
| Đồ họa | [Content/Sprites/](Content/Sprites/) + [PixelArtGenerator.cs](Core/PixelArtGenerator.cs) | 🟡 Đan dược, Yêu Đan, Trận Kỳ, Đan Sư đang dùng hình vẽ thủ tục — chờ sprite .png |

---

## 🔮 Kế Hoạch Tiếp Theo

1. **Hoàn thiện 3 lưu phái còn lại**: thi triển 4 kiểu ra đòn còn thiếu — cận chiến hình quạt (`melee_arc`, Thể Tu), đòn xuống đất có báo hiệu (`ground_aoe`, Pháp Tu/Phù Trận Sư), buff bản thân + khiên (`self_buff`), vận công liên tục (`channel`, Hỏa Long Phệ). Wiring đầy đủ nội tại của Pháp Tu/Thể Tu/Phù Trận Sư (hiện chỉ Kiếm Tu có 2 nội tại hoạt động).
2. **StatsComponent**: chỉ số Công/Thủ tách khỏi HP/Linh Lực, để `DEFENSE_DOWN` (Kiếm Tâm Phá Giáp) và phòng thủ Yêu Thú có tác dụng thật.
3. **Pierce thật sự**: trường `pierce` trên Pháp Khí/Chiêu đã có trong dữ liệu nhưng `CombatSystem` hiện vẫn hủy đạn ngay khi trúng mục tiêu đầu tiên.
4. **Tiếp tục ván đã lưu từ màn chọn lưu phái**: hiện `[F9]` chỉ nạp lại trong cùng phiên chơi (đã chọn lưu phái); chưa hỗ trợ bỏ qua màn chọn lưu phái khi có save.
5. **Động Phủ & Linh Điền**: đặt Bồ Đoàn, Trận Nhãn; trồng Linh Thảo với `Time_grow = Base_Time / Linh_Tích_Đất`.
6. **Nhân quả & Tông môn**: điểm danh vọng theo tông môn, hệ thống truy sát báo thù (Vendetta).
7. **Thú triều**: giết nhiều Yêu Thú có thể đánh thức Yêu Vương tấn công Động Phủ.
8. **Sprite .png** cho `pill.png`, `beast_core.png`, `material.png`, `formation_flag.png`, `alchemist.png`.
