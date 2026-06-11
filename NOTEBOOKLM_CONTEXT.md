# Sandbox Tu Tiên 2D Pixel — Tài Liệu Kiến Trúc & Nghiệp Vụ (NotebookLM Context)

Chào mừng NotebookLM đến với tài liệu hướng dẫn nghiệp vụ của dự án **Sandbox Tu Tiên 2D Pixel** (Đấu La Đại Lục Version). Đây là hệ thống giả lập tu tiên và chiến đấu thời gian thực bằng ngôn ngữ **C#** trên framework **MonoGame**.

---

## 🗺️ 1. Thiết Kế Kiến Trúc Hệ Thống (Architecture Design)

Dự án được xây dựng dựa trên 3 nguyên lý thiết kế phần mềm tiêu chuẩn:

1.  **Component-Based (Hướng thành phần)**: Thực thể (`Player`, `Monster`, `AutoLauncher`) đóng vai trò là container chứa dữ liệu. Mọi logic nghiệp vụ độc lập được gói gọn trong các Component chuyên biệt (như `CultivationComponent`).
2.  **Data-Driven (Hướng dữ liệu)**: Các thông số cân bằng game (Sát thương, Tầm bắn, Hệ số, Loại vật phẩm, Công thức chế tạo) được định nghĩa hoàn toàn trong các file JSON ngoại vi (`Content/Data/*`) và được nạp vào bộ nhớ qua `DataLoader` sử dụng `System.Text.Json`.
3.  **Event-Driven (Decoupled)**: `EventManager` đóng vai trò là một **EventBus trung tâm**, cho phép các thực thể và hệ thống truyền thông điệp không đồng bộ một cách an toàn về kiểu dữ liệu (Strongly-typed Events), giảm thiểu spaghetti code.

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
2.  **Bệ Phóng Ám Khí Tự Động (Auto-Turret)**:
    *   Đặt bằng phím `[T]` tại vị trí người chơi (tối đa 3 bệ phóng trên màn hình).
    *   Tự động quét tìm mục tiêu Hồn thú gần nhất trong phạm vi $220\text{px}$.
    *   Bắn ra đạn cơ quan hệ thường (Neutral/None) gây 30 sát thương mỗi $1.5$ giây.

---

## 🎨 5. Bộ Nhận Diện Đồ Họa & Cảm Giác Game (Juiciness)

*   **Hệ thống hạt (Particle System)**: Hỗ trợ tạo bụi di chuyển (Dust), hào quang bay lên khi Minh Tưởng (Aura), tàn lửa bay bập bùng (Ember), lá mộc rơi chao lượn (Leaf), tuyết băng lửng lơ (Snow), tia lửa xẹt (Spark), và vòng nổ linh lực khi đột phá (Burst).
*   **Rung giật màn hình (Screenshake)**: Lắc camera động khi bắn đạn, trúng chiêu, quái chết, hoặc bạo phát năng lượng khi đột phá.
*   **Hoạt ảnh co giãn (Recoil)**: Sprite nòng nỏ tự động nén dọc khi bắn và hồi vị từ từ trong 0.2s.
*   **Thiền định bay bổng (Hover)**: Sprite Đường Tam nhấp nhô nhịp nhàng dạng sóng sin khi đang Minh Tưởng.
*   **Thanh máu Glassy 3D**: HP/EXP/SP hiển thị dạng ống trụ thủy tinh 3D có highlight bóng sáng và viền kính kim loại 1px bên trong (Beveled Panels).
*   **Point-Clamp Sharpness**: Tăng chất lượng hình ảnh retro bằng cách cố định lưới lọc điểm sắc nét, loại bỏ nhòe ảnh pixel.

---

## ⌨️ 6. Bộ Phím Điều Khiển Kiểm Thử (Developer testing Suite)

*   `[W, A, S, D] / [Phím Mũi Tên]`: Di chuyển nhân vật.
*   `[Chuột Trái]`: Bắn đạn thường của ám khí cầm tay (Nạp từ JSON).
*   `[Chuột Phải]`: Triệu hồi quái vật ngẫu nhiên tại vị trí con trỏ chuột.
*   `[T]`: Đặt bệ phóng tự động (Tối đa 3 bệ).
*   `[Q] / [W]`: Thi triển Hồn Kỹ 1 / 2 hướng về con trỏ chuột.
*   `[I]`: Mở / đóng túi đồ (Inventory).
*   `[E]`: Đổi vũ khí ám khí trang bị (Vô Thanh Tụ Tiễn <=> Chư Cát Thần Nỗ).
*   `[R]`: Hấp thu Hồn Hoàn rơi dưới đất (khi đứng gần).
*   `[1-5]`: Phím tắt ăn Xúc Xích hương tràng để hồi HP & SP trong túi đồ.
*   `[Space]`: Hack +500 EXP (dành cho Tester).
*   `[Esc]`: Thoát game.
