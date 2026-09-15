# Sandbox Tu Tiên 2D Pixel

Chào mừng bạn đến với **Sandbox Tu Tiên 2D Pixel** — game nhập vai tu tiên góc nhìn từ trên xuống, viết bằng **C#** trên **MonoGame framework**. Người chơi đả tọa tích lũy tu vi, săn Yêu Thú lấy Yêu Đan, luyện đan luyện khí, bày trận pháp và vượt qua bình cảnh — từ xung kích kinh mạch ở Luyện Khí cho tới độ Thiên Kiếp từ Kim Đan trở lên.

Mã nguồn theo ba nguyên lý: **Data-Driven**, **Component-Based** và **Event-Driven**, giúp mở rộng dễ dàng và tránh code chồng chéo.

---

## 🗺️ Kiến Trúc Hệ Thống

### 1. Data-Driven (Hướng dữ liệu)
*   Toàn bộ Đan Dược, Vật Liệu, Pháp Khí, Pháp Thuật và công thức luyện chế được định nghĩa trong JSON (`Content/Data/*`).
*   `DataLoader` dùng `System.Text.Json` để chuyển các file thành POCO trong `Data/Models/` (`ConsumableData`, `MagicWeaponData`, `TechniqueData`...).
*   Lò Luyện tự gom công thức từ trường `crafting_recipe`; Pháp Thuật phóng theo `pattern` (`fan` / `barrage` / `nova`) — thêm nội dung mới không cần sửa code C#.

### 2. Component-Based (Hướng thành phần)
*   `Player` là thùng chứa dữ liệu mỏng, sở hữu `CultivationComponent` và `InventoryComponent`.
*   `CultivationComponent` là máy trạng thái hữu hạn (**FSM**):
    *   `Idle` — Nhàn rỗi
    *   `Meditating` — Đả Tọa, tích lũy tu vi
    *   `BreakthroughReady` — Chạm bình cảnh (tầng 10, 20, 30...), tu vi bị khóa
    *   `Breakthrough` — Đang đột phá (xung quan hoặc độ Thiên Kiếp)
    *   `Dead` — Tử vong do Yêu Thú tấn công

### 3. Event-Driven (Hướng sự kiện)
`EventManager` là EventBus strongly-typed. Các sự kiện chính:
*   `OnLevelUpEvent`, `OnRealmChangedEvent`, `OnBottleneckReachedEvent`
*   `OnBreakthroughSuccessEvent`, `OnBreakthroughFailedEvent`
*   `OnLightningStrikeEvent` — mỗi đợt lôi kiếp; `Game1` sinh tia sét có vòng báo hiệu
*   `OnTechniqueLearnedEvent` — lĩnh ngộ Pháp Thuật mới
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
│   ├── MySqlDbManager.cs        # Tự tạo CSDL sandboxtutien_v2, lưu/tải toàn thế giới
│   └── Models/                  # ConsumableData, MagicWeaponData, TechniqueData, ItemEffect, CraftingIngredient
│
├── Components/
│   ├── CultivationComponent.cs  # FSM tu luyện, Linh Căn, bình cảnh, xung quan, Thiên Kiếp, Tâm Ma
│   ├── InventoryComponent.cs    # Túi trữ vật, dùng đan dược, trang bị pháp khí
│   └── ConsumableSpawner.cs     # Sinh vật phẩm theo chu kỳ (Đan Sư, Lò Luyện)
│
├── Systems/
│   └── CultivationSystem.cs     # Time-Slicing tối đa 10 entity/frame
│
├── Entities/
│   ├── Player.cs                # Người chơi
│   ├── Monster.cs               # Yêu Thú (tuổi → phẩm giai, tiến hóa, độc, trói, uy áp)
│   ├── FormationArray.cs        # Trận Pháp tự động công kích
│   └── DroppedItem.cs           # Vật phẩm rơi trên đất
│
├── Content/
│   ├── Data/                    # consumables.json, phap_khi.json, techniques.json
│   ├── Fonts/Arial.spritefont   # Font có đủ ký tự tiếng Việt
│   └── Sprites/                 # Sprite .png (thiếu file nào sẽ dùng hình vẽ thủ tục)
│
├── Game1.cs                     # Vòng lặp chính MonoGame, HUD, input, lôi kiếp, Lò Luyện
├── Program.cs
└── SandboxTuTien.csproj         # .NET 8.0
```

---

## ⚔️ Cơ Chế Tu Tiên

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
Phẩm chất Linh Căn nhân trực tiếp vào tốc độ đả tọa (`5 × hệ số × Δt` tu vi/giây), hệ Linh Căn quyết định Pháp Thuật lĩnh ngộ:

| Hệ số | Linh Căn |
| :--- | :--- |
| ≥ 1.8 | Thiên Linh Căn |
| ≥ 1.2 | Chân Linh Căn |
| ≥ 0.5 | Tạp Linh Căn |
| > 0 | Ngụy Linh Căn |
| 0 | Phế Linh Căn (không thể tu luyện) |

Nhân vật mặc định: **Lâm Phong**, Thiên Linh Căn hệ Hỏa, khởi đầu Luyện Khí tầng 1.

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
*   **Thành công**: hồi đầy HP/Linh Lực, Tâm Ma tiêu tan, lĩnh ngộ Pháp Thuật (lần 1 → phím Q, lần 2 → phím E), 1% cơ duyên thức tỉnh **Vạn Độc Thể**.
*   **Thất bại** (HP về 0 khi đột phá): không chết, nhưng **đạo cơ tổn hại** — rớt 1 tầng tu vi, còn 20% HP, **Tâm Ma +10%** (tối đa 50%).

### 4. Yêu Thú & Yêu Đan
*   Tuổi Yêu Thú tăng 8–15 năm mỗi giây thực, quyết định **phẩm giai** (Nhất → Cửu Giai tại các mốc 100 / 300 / 1.000 / 3.000 / 10.000 / 30.000 / 100.000 / 300.000 năm), HP và kích thước.
*   Yêu Thú ≥ 10.000 năm tỏa **uy áp** khiến Yêu Thú dưới 1.000 năm hoảng sợ bỏ chạy.
*   Trảm sát Yêu Thú rơi **Yêu Đan** (Hạ / Trung / Thượng phẩm theo phẩm giai) và 40% rơi nguyên liệu theo hệ (Mộc → Linh Mộc Tâm, Hỏa → Hỏa Tinh Thạch, Băng → Hàn Thiết).
*   Luyện hóa Yêu Đan trực tiếp để tăng tu vi, hoặc dùng làm nguyên liệu luyện đan.

### 5. Luyện Đan & Luyện Khí
Đứng cạnh **Lò Luyện** và nhấn `[C]`. Công thức mặc định:

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

### ⌨️ Phím Điều Khiển
*   **`[W][A][S][D]` / Mũi tên**: Di chuyển.
*   **Chuột trái**: Phóng pháp khí về phía con trỏ. **Chuột phải**: Gọi Yêu Thú ngẫu nhiên tại con trỏ.
*   **`[M]`**: Bật/tắt Đả Tọa.
*   **`[R]`**: Đột phá khi chạm bình cảnh. Trong lúc đột phá, bấm **`[Space]`** liên tục để ổn định đạo tâm (+2% mỗi lần); khi độ Thiên Kiếp, di chuyển để né vòng đỏ.
*   **`[Q]` / `[E]`**: Thi triển Pháp Thuật 1 / 2 (tiêu hao Linh Lực).
*   **`[Tab]`**: Đổi pháp khí. **`[I]`**: Túi trữ vật. **`[1]`–`[5]`**: Dùng nhanh vật phẩm.
*   **`[C]`**: Mở Lò Luyện khi đứng gần (chọn công thức bằng `[F1]`–`[F4]` hoặc chuột).
*   **`[T]`**: Bày trận (tốn 1 Linh Thạch). **`[Y]`**: Đổi loại trận. **`[F]`**: Nạp Linh Thạch cho trận gần nhất.
*   **`[F5]` / `[F9]`**: Lưu / Tải game từ MySQL.
*   **Phím gian lận (test)**: `[Space]` +500 tu vi (khi không đột phá), `[U]` thức tỉnh Vạn Độc Thể, `[H]` nhận 3 Phá Cảnh Đan.
*   **`[Esc]`**: Thoát game.

---

## 📊 Trạng Thái Hiện Tại

| Module | Tệp | Trạng thái |
| :--- | :--- | :--- |
| Hệ sự kiện | [EventManager.cs](Core/EventManager.cs) | ✅ Hoàn thành |
| Thời gian thế giới | [GameTimeManager.cs](Core/GameTimeManager.cs) | ✅ Hoàn thành |
| Dữ liệu JSON | [Content/Data/](Content/Data/) + [Data/Models/](Data/Models/) | ✅ Đan dược, pháp khí, pháp thuật, công thức |
| FSM tu luyện | [CultivationComponent.cs](Components/CultivationComponent.cs) | ✅ Linh Căn, bình cảnh, xung quan, Thiên Kiếp, Tâm Ma, Đan dược |
| Time-Slicing | [CultivationSystem.cs](Systems/CultivationSystem.cs) | ✅ Hoàn thành |
| Túi trữ vật | [InventoryComponent.cs](Components/InventoryComponent.cs) | ✅ Hoàn thành |
| Chiến đấu | [ProjectilePool.cs](Core/Combat/ProjectilePool.cs) | ✅ Pháp khí, pháp thuật data-driven, khắc hệ |
| Yêu Thú | [Monster.cs](Entities/Monster.cs) | ✅ Phẩm giai, tiến hóa, uy áp, độc, trói |
| Trận pháp | [FormationArray.cs](Entities/FormationArray.cs) | ✅ 3 loại trận |
| Lò Luyện | [Game1.cs](Game1.cs) | ✅ Luyện đan & luyện khí data-driven |
| CSDL MySQL | [MySqlDbManager.cs](Data/MySqlDbManager.cs) | ✅ Lưu/tải toàn bộ thế giới |
| Đồ họa | [Content/Sprites/](Content/Sprites/) + [PixelArtGenerator.cs](Core/PixelArtGenerator.cs) | 🟡 Đan dược, Yêu Đan, Trận Kỳ, Đan Sư đang dùng hình vẽ thủ tục — chờ sprite .png |

---

## 🔮 Kế Hoạch Tiếp Theo

1. **Động Phủ & Linh Điền**: đặt Bồ Đoàn, Trận Nhãn; trồng Linh Thảo với `Time_grow = Base_Time / Linh_Tích_Đất`.
2. **Nhân quả & Tông môn**: điểm danh vọng theo tông môn, hệ thống truy sát báo thù (Vendetta).
3. **Thú triều**: giết nhiều Yêu Thú có thể đánh thức Yêu Vương tấn công Động Phủ.
4. **Sprite .png** cho `pill.png`, `beast_core.png`, `material.png`, `formation_flag.png`, `alchemist.png`.
