# Sandbox Tu Tiên 2D Pixel — Tài Liệu Kiến Trúc & Nghiệp Vụ (NotebookLM Context)

Chào mừng NotebookLM đến với tài liệu hướng dẫn nghiệp vụ của dự án **Sandbox Tu Tiên 2D Pixel** (Đấu La Đại Lục Version). Đây là hệ thống giả lập tu tiên và chiến đấu thời gian thực bằng ngôn ngữ **C#** trên framework **MonoGame**.

---

## 🗺️ 1. Thiết Kế Kiến Trúc Hệ Thống (Architecture Design)

Dự án được xây dựng dựa trên 3 nguyên lý thiết kế phần mềm tiêu chuẩn:

1.  **Component-Based (Hướng thành phần)**: Thực thể (`Player`, `Monster`, `AutoLauncher`) đóng vai trò là container chứa dữ liệu. Mọi logic nghiệp vụ độc lập được gói gọn trong các Component chuyên biệt (như `CultivationComponent`).
2.  **Data-Driven (Hướng dữ liệu)**: Các thông số cân bằng game (Sát thương, Tầm bắn, Hệ số, Loại vật phẩm, Công thức chế tạo) được định nghĩa ban đầu trong các file JSON ngoại vi (`Content/Data/*`), sau đó được di cư và quản lý tập trung bằng cơ sở dữ liệu **MySQL**, hỗ trợ nạp động thời gian thực hoặc dự phòng tự động bằng tệp JSON (Offline JSON Fallback).
3.  **Event-Driven (Decoupled)**: `EventManager` đóng vai trò là một **EventBus trung tâm**, cho phép các thực thể và hệ thống truyền thông điệp không đồng bộ một cách an sau về kiểu dữ liệu (Strongly-typed Events), giảm thiểu spaghetti code.

---

## ⚙️ 2. Máy Trạng Thái Tu Luyện (FSM - Finite State Machine)

Trạng thái tu luyện của một thực thể được quản lý bởi `CultivationComponent.cs` thông qua FSM:

*   `Idle`: Trạng thái nhàn rỗi, không tích lũy hồn lực.
*   `Meditating` (Minh Tưởng): Tự động hấp thu linh khí thế giới, tăng EXP động theo thời gian thực ($5 \times \text{Tiên Thiên Hồn Lực} \times \Delta t$).
*   `BreakthroughReady` (Chạm Bình Cảnh): EXP bị đóng băng ở các mốc cấp độ $10, 20, 30...$ Người chơi phải săn lùng quái thú để lấy Hồn Hoàn nhằm đột phá sang cảnh giới tiếp theo.
*   `AbsorbingRing` (Hấp Thu Hồn Hoàn): Trạng thái chịu đựng áp lực khi dòng chảy linh khí của Hồn Hoàn truyền vào cơ thể. Diễn ra trong **3 giây thực** (chia làm 5 đợt xung kích sát thương). Nếu HP về $0$, chuyển sang `Dead`. Nếu sống sót, đột phá thành công và chuyển lại `Meditating`.
*   `Dead` (Tử Vong): HP về 0 hoặc bạo thể do hấp thu hồn hoàn vượt quá giới hạn chịu đựng của cơ thể.

---

## 🧪 3. Các Công Thức Toán Học & Logic Nghiệp Vụ Cốt Lõi

### A. Giới Hạn Cơ Thể & Tỷ Lệ Đột Phá
Mỗi hồn hoàn thứ $N$ có một cực hạn chịu đựng (Body Limit) tính theo năm tuổi (Đấu La Đại Lục Rule):
*   **Hồn Hoàn 1 (Cấp 10)**: $423$ năm.
*   **Hồn Hoàn 2 (Cấp 20)**: $760$ năm.
*   **Hồn Hoàn 3 (Cấp 30)**: $1700$ năm.
*   *Công thức tính Tỷ Lệ Thành Công Hấp Thu:*
    $$\text{Success Rate} = \min\left(1.0, \frac{\text{Body Limit}}{\text{Tuổi Hồn Hoàn}} + \text{Buff Ý Chí}\right)$$

### B. Sát Thương Nội Tại Khi Hấp Thu (HP Shock Damage)
Nếu tuổi Hồn Hoàn vượt quá cực hạn chịu đựng, người chơi sẽ nhận sát thương xung kích nội tại ở mỗi đợt trong 5 đợt hấp thu:
$$\text{Damage Per Wave} = \text{Max HP} \times (1.0 - \text{Success Rate}) \times \text{Random}(0.3 \to 0.5)$$

### C. Cơ Chế Tiến Hóa Tự Động Của Hồn Thú (Monster Evolution)
Quái vật trong thế giới tự động tăng trưởng tuổi thọ tu vi theo thời gian:
$$\text{Age} = \text{Age} + \text{Random}(8 \to 15) \times \Delta t$$
*   **Bán kính va chạm (Radius)** nở rộng theo quy mô hàm logarit của tuổi thọ:
    $$\text{Radius} = 16.0 + \log_{10}(\text{Age}) \times 4.5$$
*   **Sinh mệnh tối đa (Max HP)** tăng tương ứng và tự động hồi phục tỷ lệ thuận (Proportional Healing):
    $$\text{Max HP} = \text{Base Max HP} \times \left(1.0 + \frac{\text{Age}}{1500}\right)$$
*   **Tiền tố Danh hiệu (Prefix Title)** tự động cập nhật theo mốc tuổi:
    *   `< 100` năm: **Thập Niên** (Thap Nien)
    *   `< 1,000` năm: **Bách Niên** (Bach Nien)
    *   `< 10,000` năm: **Thiên Niên** (Thien Nien)
    *   `>= 10,000` năm: **Vạn Niên** (Van Nien)

### D. Vòng Tròn Khắc Chế Ngũ Hành (Elemental Counter Loop)
Thiết lập 3 hệ chính: **Hỏa (Fire)** > **Mộc (Wood)** > **Băng (Ice)** > **Hỏa (Fire)**.
*   Nếu đạn ám khí/hồn kỹ khắc hệ của Hồn thú mục tiêu, sát thương tăng thêm **50%** ($\text{Final Damage} = \text{Base Damage} \times 1.5$) và hiển thị chữ nổi `COUNTER!`.

---

## ⚔️ 4. Hồn Kỹ & Cơ Quan Ám Khí Tự Động

1.  **Hồn Kỹ Chủ Động (Active Skills)**:
    *   Phím `[Q]`: Kích hoạt Hồn Kỹ 1 (học từ Hồn Hoàn thứ nhất, tiêu hao SP).
    *   Phím `[W]`: Kích hoạt Hồn Kỹ 2 (học từ Hồn Hoàn thứ hai, tiêu hao SP).
    *   *Các kỹ năng đặc trưng*: Hỏa hệ (*Phượng Hoàng Hỏa Tuyến*, *Phượng Hoàng Huyền Oa*), Mộc hệ (*Lam Ngân Quấn Quanh*, *Lam Ngân Tù Lồng*), Băng hệ (*Băng Tằm Kết Giới*, *Huyền Băng Xung Kích*).
2.  **Bệ Phóng Ám Khí Tự Động (Auto-Turret / Auto-Launcher)**:
    *   Đặt bằng phím `[T]` tại vị trí người chơi (tối đa 3 bệ phóng trên màn hình).
    *   Có 3 loại bệ phóng với thông số khác biệt:
        *   **Vô Thanh Tụ Tiễn** (Turret Type 1): Tốc độ bắn siêu nhanh ($0.5\text{s}$), tầm bắn $220\text{px}$, sát thương 10, bắn tỉa không gây Aggro quái (Silent).
        *   **Chư Cát Thần Nỗ** (Turret Type 2): Tốc độ bắn siêu chậm ($3.0\text{s}$), tầm bắn $260\text{px}$, sát thương bạo kích cực lớn (80), tốn 2 đạn mỗi viên nạp.
        *   **Hàm Sa Xạ Ảnh** (Turret Type 3): Tốc độ bắn vừa phải ($2.0\text{s}$), tầm bắn $180\text{px}$, sát thương 15, phun sương độc hại diện rộng (AoE) gây nhiễm độc DoT trong $5\text{s}$ cho quái vật trong phạm vi $80\text{px}$.

---

## 🎨 5. Bộ Nhận Diện Đồ Họa & Cảm Giác Game (Juiciness)

*   **Chất Lượng Đồ Họa Mới**: Toàn bộ mô hình nhân vật, quái vật, vật phẩm tiêu thụ (xúc xích), ám khí (đạn tụ tiễn, thần nỗ), hồn hoàn, bệ phóng, NPC Oscar và Lò rèn Anvil đều được vẽ lại dưới dạng file ảnh `.png` nghệ thuật, sắc nét, có chiều sâu xianxia (tu tiên).
*   **Pipeline Vẽ 3-Pass Sắc Nét & Chống Nhòe Chữ**:
    *   *Pass 1 (World Sprites)*: Sử dụng `PointClamp` và ma trận biến đổi Camera để giữ nguyên nét thô mộc của Pixel Art khi phóng to/thu nhỏ.
    *   *Pass 2 (World UI/Texts)*: Sử dụng `LinearClamp` và ma trận biến đổi Camera để nhãn văn bản trôi nổi (máu, tuổi thọ, sát thương khắc hệ) hiển thị mượt mà không bị nhòe nét hay mất chi tiết.
    *   *Pass 3 (Screen UI)*: Sử dụng `LinearClamp` không kèm ma trận Camera để vẽ các bảng giao diện tĩnh (HUD, Túi đồ Inventory, menu phím tắt).
*   **Scale Icon Giao Diện**: Icon túi đồ tự động scale vừa vặn khung slot `32x32`, tránh hiện tượng vật phẩm che full màn hình.
*   **Camera Smooth Tracking**: Hỗ trợ camera cuộn mượt mà bám sát nhân vật, giới hạn trong bản đồ lớn `2000x2000`px.
*   **Hệ thống hạt (Particle System)**: Hỗ trợ tạo bụi di chuyển (Dust), hào quang bay lên khi Minh Tưởng (Aura), tàn lửa bay bập bùng (Ember), lá mộc rơi chao lượn (Leaf), tuyết băng lửng lơ (Snow), tia lửa xẹt (Spark), và vòng nổ linh lực khi đột phá (Burst).
*   **Rung giật màn hình (Screenshake)**: Lắc camera động khi bắn đạn, trúng chiêu, quái chết, hoặc bạo phát năng lượng khi đột phá.
*   **Hoạt ảnh co giãn (Recoil)**: Sprite nòng nỏ tự động nén dọc khi bắn và hồi vị từ từ trong 0.2s.
*   **Thiền định bay bổng (Hover)**: Sprite Đường Tam nhấp nhô nhịp nhàng dạng sóng sin khi đang Minh Tưởng.
*   **Thanh máu Glassy 3D**: HP/EXP/SP hiển thị dạng ống trụ thủy tinh 3D có highlight bóng sáng và viền kính kim loại 1px bên trong (Beveled Panels).

---

## ⌨️ 6. Bộ Phím Điều Khiển Kiểm Thử (Developer testing Suite)

*   `[W, A, S, D] / [Phím Mũi Tên]`: Di chuyển nhân vật.
*   `[Chuột Trái]`: Bắn đạn thường của ám khí cầm tay (Nạp từ JSON/DB).
*   `[Chuột Phải]`: Triệu hồi quái vật ngẫu nhiên tại vị trí con trỏ chuột.
*   `[T]`: Đặt bệ phóng tự động (Tối đa 3 bệ).
*   `[Y]`: Thay đổi loại bệ phóng đặt tiếp theo (Vô Thanh Tụ Tiễn <=> Chư Cát Thần Nỗ <=> Hàm Sa Xạ Ảnh).
*   `[F]`: Nạp lại đạn (Reload) cho bệ phóng gần nhất.
*   `[Q] / [W]`: Thi triển Hồn Kỹ 1 / 2 hướng về con trỏ chuột.
*   `[I]`: Mở / đóng túi đồ (Inventory).
*   `[E]`: Đổi vũ khí ám khí trang bị (Vô Thanh Tụ Tiễn <=> Chư Cát Thần Nỗ).
*   `[R]`: Hấp thu Hồn Hoàn rơi dưới đất (khi đứng gần).
*   `[1-5]`: Phím tắt ăn Xúc Xích hương tràng để hồi HP & SP trong túi đồ.
*   `[U]`: Mở khóa nhanh Hồn Kốt Bát Chu Mâu (Cheat).
*   `[H]`: Triệu hồi nhanh Hồn Hoàn 99.000 năm tuổi (Cheat).
*   `[F5]`: Lưu game thủ công vào CSDL MySQL.
*   `[F9]`: Nạp game thủ công từ CSDL MySQL.
*   `[Space]`: Hack +500 EXP (dành cho Tester).
*   `[Esc]`: Thoát game.

---

## 🗄️ 7. Hệ Thống Cơ Sở Dữ Liệu MySQL & Tự Động Di Cư

Dự án tích hợp thư viện kết nối hiệu năng cao `MySqlConnector` để đồng bộ dữ liệu tĩnh và động:

1.  **Cơ chế Tự Khởi Tạo (Self-Initialization)**: Khi chạy game, hệ thống tự động kết nối và tạo cơ sở dữ liệu `sandboxtutien` cùng các bảng cấu trúc nếu chưa tồn tại.
2.  **Cấu Trúc Các Bảng (Tables Schema)**:
    *   `consumables`: Lưu trữ danh sách vật phẩm tiêu hao (id, tên, loại, nguồn rơi, hiệu ứng đặc biệt dạng JSON, thời gian hỏng).
    *   `hidden_weapons`: Lưu trữ danh sách các loại ám khí (id, tên, loại, chỉ số sát thương/tầm bắn dạng JSON, nguyên liệu chế tạo dạng JSON).
    *   `player_saves`: Lưu trữ thông tin lưu game (tên người chơi, cấp độ, EXP, HP/SP hiện tại và tối đa, ám khí đang trang bị, toàn bộ túi đồ dạng JSON, số hồn hoàn sở hữu, hồn kỹ đã học, trạng thái Bát Chu Mâu, cảnh giới và trạng thái thực thể thế giới).
3.  **Tự Động Di Cư (Auto-Migration)**: Nếu phát hiện bảng dữ liệu cấu hình rỗng, hệ thống tự động đọc tệp JSON tĩnh địa phương và chèn (Migrate) hàng loạt vào CSDL MySQL.
4.  **Cơ Chế Dự Phòng Ngoại Tuyến (Offline Fallback)**: Nếu không thể kết nối tới MySQL Server (MySQL bị tắt hoặc cấu hình sai), game sẽ tự động kích hoạt chế độ Offline, đọc các tệp JSON tĩnh để trải nghiệm chơi game không bao giờ bị gián đoạn.

---

## 💾 8. Cơ Chế Lưu Trữ & Phục Hồi Thế Giới (Full World Persistence)

Không chỉ lưu trữ thuộc tính của người chơi, hệ thống hỗ trợ lưu toàn bộ thực thể động trong thế giới game để tạo ra trải nghiệm Persistent World hoàn chỉnh:

1.  **Lưu Trạng Thái Thế Giới (Save State)**:
    *   Khi người chơi nhấn `[F5]`, **Tăng Cấp (Level Up)**, hoặc **Đột Phá Thành Công (Breakthrough)**, hệ thống sẽ tự động lưu trạng thái người chơi và tuần tự hóa (Serialize) toàn bộ thực thể động trên bản đồ thành chuỗi JSON TEXT để ghi vào CSDL MySQL:
        *   `monsters_json`: Lưu trữ tên, tuổi thọ tu vi, HP hiện tại, Max HP, vị trí X, Y và hệ thuộc tính của toàn bộ Hồn Thú đang hoạt động.
        *   `dropped_items_json`: Lưu trữ id, tên, số lượng, loại và vị trí của vật phẩm rơi dưới đất.
        *   `launchers_json`: Lưu trữ loại cơ quan, lượng đạn còn lại, lượng đạn tối đa và vị trí của các bệ phóng ám khí.
        *   `soul_rings_json`: Lưu trữ vị trí, tuổi thọ và hệ thuộc tính của các Hồn Hoàn chưa được hấp thu trôi nổi trên đất.
2.  **Khôi Phục Trạng Thái Thế Giới (Load State)**:
    *   Khi nhấn `[F9]`, game dọn sạch toàn bộ thực thể cũ hiện tại, xóa sạch danh sách bay của đạn và hạt.
    *   Giải tuần tự hóa (Deserialize) dữ liệu JSON từ MySQL và sinh lại chính xác các đối tượng thực thể thế giới.
3.  **Tái Liên Kết Sự Kiện (Event Re-binding)**:
    *   Để đảm bảo các thực thể được tái tạo hoạt động bình thường, game thực hiện đăng ký lại sự kiện `OnKilled` cho Hồn Thú. Khi quái vật phục hồi bị người chơi tiêu diệt, sự kiện này vẫn kích hoạt chuẩn xác hiệu ứng nổ nguyên tố, rung giật màn hình và sinh ra Hồn Hoàn rơi tại đúng tọa độ đó.
