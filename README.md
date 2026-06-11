# Sandbox Tu Tiên 2D Pixel — Đấu La Đại Lục (Foundation Codebase)

Chào mừng bạn đến với **Sandbox Tu Tiên 2D Pixel** (chủ đề Đấu La Đại Lục) — một dự án game nhập vai được xây dựng bằng ngôn ngữ **C#** trên nền tảng **MonoGame framework**. 

Mã nguồn này được thiết kế và triển khai theo tiêu chuẩn kiến trúc chuyên nghiệp: **Data-Driven (Hướng dữ liệu)**, **Component-Based (Hướng thành phần)**, và **Event-Driven (Hướng sự kiện)** nhằm đảm bảo tính mở rộng cao, tối ưu hóa hiệu năng, và tránh tình trạng mã nguồn chồng chéo (Spaghetti code).

---

## 🗺️ Kiến Trúc Hệ Thống (Architecture Design)

Hệ thống được thiết kế dựa trên 4 trụ cột kiến trúc cốt lõi:

### 1. Data-Driven Architecture (Kiến trúc Hướng Dữ liệu)
*   **Mô tả**: Mọi thông số vật phẩm, thuộc tính ám khí, hồn thú, hồn kỹ và thực phẩm tiêu thụ đều được định nghĩa trong các tệp cấu hình JSON (`Content/Data/*`).
*   **Triển khai**: `DataLoader` sử dụng `System.Text.Json` để tự động chuyển đổi các tệp cấu hình thành các POCO (Plain Old C# Object) nằm trong `Data/Models/` (`HiddenWeaponData`, `ConsumableData`, v.v.). Điều này cho phép Designer dễ dàng cân bằng game (Balance) mà không cần chỉnh sửa hoặc biên dịch lại mã nguồn C#.

### 2. Component-Based Architecture (Kiến trúc Hướng Thành phần)
*   **Mô tả**: Thực thể (`Entity`) đóng vai trò là các thùng chứa dữ liệu mỏng (data container) và liên kết các thành phần chức năng lại với nhau.
*   **Triển khai**: `Player` chứa thông tin cơ bản và sở hữu một `CultivationComponent`. Thành phần tu luyện này được triển khai dưới dạng một máy trạng thái hữu hạn (**FSM**) gồm các trạng thái:
    *   `Idle` (Nhàn rỗi)
    *   `Meditating` (Minh tưởng tích lũy EXP)
    *   `BreakthroughReady` (Chạm bình cảnh hồn lực ở các cấp 10, 20, 30..., chờ hấp thu Hồn Hoàn)
    *   `AbsorbingRing` (Đang hấp thu Hồn Hoàn từ Hồn Thú)
    *   `Dead` (Hấp thu hồn hoàn vượt quá giới hạn chịu đựng dẫn đến tử vong)

### 3. Event-Driven Architecture (Kiến trúc Hướng Sự kiện)
*   **Mô tả**: Các hệ thống giao tiếp với nhau một cách lỏng lẻo (decoupled) thông qua một kênh truyền tin trung tâm — **EventBus**.
*   **Triển khai**: `EventManager` quản lý việc đăng ký (`Subscribe`) và phát sự kiện (`Publish`) một cách an toàn về kiểu dữ liệu (strongly-typed). Các sự kiện chính bao gồm:
    *   `OnLevelUpEvent`: Phát ra khi Player tăng cấp Hồn Lực.
    *   `OnRealmChangedEvent`: Phát ra khi Đột phá Cảnh giới thành công (ví dụ: Hồn Sĩ → Hồn Sư).
    *   `OnBottleneckReachedEvent`: Phát ra khi Hồn Lực chạm ngưỡng giới hạn (cấp 10, 20, 30...) cần Hồn Hoàn.
    *   `OnBreakthroughSuccessEvent` & `OnBreakthroughFailedEvent`: Kết quả của quá trình hấp thu Hồn Hoàn.
    *   `OnSoulRingAbsorbedEvent`: Lưu thông tin Hồn Hoàn hấp thu thành công (tuổi hồn hoàn, kỹ năng nhận được).
    *   `OnPlayerDiedEvent`: Người chơi tử vong khi hấp thu Hồn Hoàn quá giới hạn cơ thể.

### 4. Time-Slicing & Game Time (Hệ thống tối ưu & Thời gian)
*   **Tối ưu hiệu năng (Time-Slicing)**: Hệ thống `CultivationSystem` quản lý cập nhật tu luyện của tất cả thực thể trong game. Nhằm tránh quá tải CPU khi số lượng thực thể tăng lên hàng ngàn, hệ thống áp dụng kỹ thuật *phân lát thời gian*, chỉ cập nhật tối đa 10 thực thể trên mỗi khung hình thông qua thuật toán Round-Robin Index.
*   **Hệ thống Thời gian (GameTimeManager)**: Đồng bộ hóa thời gian game theo tỷ lệ `1:1440` (1 giây thực tế bằng 24 phút trong game, tương ứng 1 phút thực tế bằng 1 ngày đêm trong game). Giúp dễ dàng đồng bộ các cơ chế sinh học, thiên tượng hoặc chu kỳ hồi phục hồn lực.

---

## 📁 Cấu Trúc Thư Mục Dự Án (Directory Structure)

Thư mục dự án được tổ chức phân lớp logic rõ ràng:

```text
game/
├── Core/                     # Các module cốt lõi của game engine
│   ├── EventManager.cs       # Trung tâm điều phối EventBus (Subscribe/Publish)
│   └── GameTimeManager.cs    # Quản lý thời gian trong thế giới game (1s thực = 24 phút game)
│
├── Data/                     # Quản lý cấu hình dữ liệu ngoài
│   ├── DataLoader.cs         # Đọc tệp JSON từ Content/ và chuyển thành Object C#
│   └── Models/               # Định nghĩa các Data Model POCO
│       ├── ConsumableData.cs       # Cấu hình thực phẩm/dược phẩm
│       ├── CraftingIngredient.cs   # Nguyên liệu chế tạo
│       ├── HiddenWeaponData.cs     # Cấu hình ám khí (Đường Môn)
│       └── ItemEffect.cs           # Định nghĩa hiệu ứng vật phẩm
│
├── Components/               # Các component logic độc lập gắn vào Entity
│   └── CultivationComponent.cs  # Máy trạng thái FSM tu luyện & tính toán hấp thu Hồn Hoàn
│
├── Systems/                  # Hệ thống quản lý toàn cục các Component
│   └── CultivationSystem.cs  # Quản lý vòng lặp tu luyện và áp dụng Time-Slicing (tối đa 10 entity/frame)
│
├── Entities/                 # Định nghĩa các thực thể trong game
│   └── Player.cs             # Thực thể người chơi chứa các dữ liệu thuộc tính
│
├── Content/                  # Tài nguyên tĩnh của game
│   └── Data/                 # File dữ liệu JSON cấu hình cân bằng
│       ├── consumables.json       # Dữ liệu thực phẩm
│       └── hidden_weapons.json    # Dữ liệu ám khí Đường Môn
│
├── Game1.cs                  # Entry Point chính của MonoGame, kết nối toàn bộ hệ thống & vẽ HUD
├── Program.cs                # Điểm khởi chạy chương trình C#
└── SandboxTuTien.csproj      # Cấu hình dự án .NET 8.0 SDK
```

---

## ⚔️ Cơ Chế Tính Toán Tu Luyện & Hồn Hoàn (Core Game Mechanics)

### 1. Phân chia Cảnh Giới (Realms)
Cảnh giới tu luyện dựa trên cấp độ hồn lực từ 1 đến 99:
*   Cấp 1 - 10: **Hồn Sĩ** (Hồn hoàn tối đa: 0)
*   Cấp 11 - 20: **Hồn Sư** (Hồn hoàn tối đa: 1)
*   Cấp 21 - 30: **Đại Hồn Sư** (Hồn hoàn tối đa: 2)
*   Cấp 31 - 40: **Hồn Tôn** (Hồn hoàn tối đa: 3)
*   Cấp 41 - 50: **Hồn Tông** (Hồn hoàn tối đa: 4)
*   Cấp 51 - 60: **Hồn Vương** (Hồn hoàn tối đa: 5)
*   Cấp 61 - 70: **Hồn Đế** (Hồn hoàn tối đa: 6)
*   Cấp 71 - 80: **Hồn Thánh** (Hồn hoàn tối đa: 7)
*   Cấp 81 - 90: **Hồn Đấu La** (Hồn hoàn tối đa: 8)
*   Cấp 91 - 99: **Phong Hào Đấu La** (Hồn hoàn tối đa: 9)

### 2. Giới Hạn Cơ Thể & Tỷ Lệ Đột Phá Hấp Thu Hồn Hoàn (Đấu La Đại Lục Rule)
Khi chạm mốc cấp độ bình cảnh (`10, 20, 30...`), người chơi phải hấp thu Hồn Hoàn từ Hồn Thú để đột phá. Giới hạn năm tuổi của Hồn Hoàn an toàn được tính toán theo công thức Đấu La Đại Lục:
*   Hồn Hoàn 1: **423 năm**
*   Hồn Hoàn 2: **764 năm**
*   Hồn Hoàn 3: **1,760 năm**
*   Hồn Hoàn 4: **5,000 năm**
*   Hồn Hoàn 5: **12,000 năm**
*   Hồn Hoàn 6: **20,000 năm**
*   Hồn Hoàn 7: **50,000 năm**
*   Hồn Hoàn 8: **100,000 năm**
*   Hồn Hoàn 9: **Giới hạn tối đa**

**Công thức tính Tỷ Lệ Đột Phá:**
$$\text{Tỷ Lệ Thành Công} = \frac{\text{Giới Hạn Cơ Thể}}{\text{Số Năm Tuổi Hồn Hoàn}} + \text{Buff Ý Chí}$$

### 3. Sát Thương Nhận Vào Khi Hấp Thu (Absorption Damage)
Khi bắt đầu hấp thu, trạng thái FSM chuyển sang `AbsorbingRing`. Quá trình diễn ra trong **3 giây thực** (tương đương 5 đợt xung kích năng lượng trong game).
*   Nếu Hồn Hoàn vượt quá giới hạn an toàn hoặc tỷ lệ thành công thấp, người chơi sẽ nhận sát thương xung kích mỗi đợt:
    $$\text{Sát Thương Đợt} = \text{Max HP} \times (1 - \text{Tỷ Lệ Thành Công}) \times \text{Random}(0.3 \to 0.5)$$
*   Nếu lượng HP giảm về `0`, người chơi sẽ rơi vào trạng thái `Dead` (Tử Vong), quá trình hấp thu thất bại. Nếu sống sót qua 5 đợt, đột phá thành công, cảnh giới tăng, và nhận được Hồn Kỹ tương ứng.

---

## 🎮 Hướng Dẫn Cài Đặt & Chạy Thử Nghiệm (Build & Run)

### 📋 Yêu cầu hệ thống
1. Đã cài đặt **.NET 8.0 SDK** (phiên bản khuyến nghị: 8.0.400 hoặc mới hơn).
2. Hệ điều hành Windows (phù hợp với môi trường hiện tại của Workspace).

### 🚀 Cách chạy Game
Mở PowerShell hoặc Command Prompt tại thư mục gốc của dự án (`d:\VSCODE\game`), chạy các lệnh sau:

```powershell
# 1. Biên dịch dự án (Đảm bảo 0 lỗi, 0 cảnh báo)
dotnet build

# 2. Khởi chạy game
dotnet run
```

### ⌨️ Phím Tắt Kiểm Thử (Keyboard Test Suite)
Khi chạy game, màn hình Console sẽ ghi lại chi tiết các Log sự kiện thời gian thực. Bạn có thể sử dụng các phím tắt sau trên cửa sổ MonoGame để kiểm tra các luồng nghiệp vụ:

*   **`[M]` (Minh Tưởng)**: Bật/Tắt chế độ Minh Tưởng. Khi bật, Hồn Sĩ sẽ tích lũy EXP tự động dựa trên tốc độ tu luyện.
*   **`[Space]` (+500 EXP)**: Cộng trực tiếp 500 EXP giúp đẩy nhanh tốc độ lên cấp để kiểm tra cơ chế chạm bình cảnh (Bottleneck) tại cấp 10.
*   **`[A]` (Hấp thu Hồn Hoàn 400 năm)**: Hấp thu Hồn Hoàn từ Hồn Thú 400 năm (nằm trong giới hạn an toàn 423 năm của Hồn Hoàn thứ nhất). Tỷ lệ thành công cực cao, an toàn.
*   **`[D]` (Hấp thu Hồn Hoàn 9999 năm)**: Hấp thu Hồn Hoàn siêu việt cực hạn cơ thể. Năng lượng Hồn Hoàn sẽ bạo phát gây sát thương cực lớn liên tục trong 3 giây. Có khả năng cao dẫn đến tử vong (`PlayerDeathEvent`).
*   **`[Esc]`**: Thoát game an toàn.

---

## 📊 Trạng Thái Hiện Tại & Báo Cáo Tiến Độ (Current Progress)

Dưới đây là bảng tổng hợp tiến độ hoàn thành các module chức năng:

| Module | Tên Tệp Tin / Đường Dẫn | Trạng Thái | Mô Tả Chi Tiết |
| :--- | :--- | :--- | :--- |
| **Cấu Trúc** | `SandboxTuTien.csproj` | ✅ **Hoàn thành** | Chuyển đổi thành công sang cấu hình .NET 8.0, kích hoạt Nullable. |
| **Hệ Sự Kiện** | [EventManager.cs](file:///d:/VSCODE/game/Core/EventManager.cs) | ✅ **Hoàn thành** | Triển khai EventBus Generic không đồng bộ và 7 sự kiện nghiệp vụ cốt lõi. |
| **Hệ Thời Gian** | [GameTimeManager.cs](file:///d:/VSCODE/game/Core/GameTimeManager.cs) | ✅ **Hoàn thành** | Đồng bộ thời gian thực 1:1440 với định dạng hiển thị trực quan. |
| **Data Models** | [Data/Models/](file:///d:/VSCODE/game/Data/Models/) | ✅ **Hoàn thành** | Định nghĩa cấu trúc POCO cho Ám Khí, Thực Phẩm, Thuộc Tính và Hiệu Ứng. |
| **Data Loader** | [DataLoader.cs](file:///d:/VSCODE/game/Data/DataLoader.cs) | ✅ **Hoàn thành** | Trình đọc tệp cấu hình JSON linh hoạt sử dụng `System.Text.Json`. |
| **FSM Tu Luyện** | [CultivationComponent.cs](file:///d:/VSCODE/game/Components/CultivationComponent.cs) | ✅ **Hoàn thành** | Xây dựng máy trạng thái FSM tu luyện, bộ lọc giới hạn hồn hoàn Đấu La và cơ chế sát thương bạo phát. |
| **Hệ Tu Luyện** | [CultivationSystem.cs](file:///d:/VSCODE/game/Systems/CultivationSystem.cs) | ✅ **Hoàn thành** | Tối ưu hóa cập nhật với giải thuật cắt lát thời gian (Time-Slicing). |
| **Thực Thể** | [Player.cs](file:///d:/VSCODE/game/Entities/Player.cs) | ✅ **Hoàn thành** | Triển khai thực thể người chơi, nạp dữ liệu nền tảng và gán component. |
| **Hồ Sơ Cấu Hình** | `Content/Data/*.json` | ✅ **Hoàn thành** | Thiết lập cấu hình mẫu cho Ám khí (Vô Ảnh Châm, Khổng Tước Lực) và Thực phẩm (Xúc Xích Phục Hồi). |
| **Giao Diện & HUD** | [Game1.cs](file:///d:/VSCODE/game/Game1.cs) | ✅ **Hoàn thành** | Kết nối toàn bộ hệ thống, tích hợp HUD (Pixel-Art Rectangles) vẽ thanh HP, EXP, State Indicator, 9 ô Hồn Hoàn, cập nhật tiêu đề cửa sổ thời gian thực và tích hợp bộ gõ phím test. |

---

## 🔮 Kế Hoạch Tiếp Theo (Next Steps)
1. **Mô-đun Chiến đấu & Ám khí**: Tích hợp các ám khí đã nạp từ JSON để người chơi có thể trang bị và tấn công (sử dụng thuộc tính trong `CombatStats`).
2. **Hệ thống Túi đồ (InventorySystem)**: Quản lý việc nhặt, lưu trữ và sử dụng thực phẩm hồi HP/Hồn lực từ dữ liệu JSON đã nạp.
3. **Mô-đun Đồ họa 2D Pixel**: Thay thế các thanh HUD phẳng bằng Sprite/Font thực tế và hiển thị mô hình nhân vật hoạt họa 2D di chuyển trong môi trường Sandbox.
