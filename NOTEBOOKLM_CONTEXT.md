# Sandbox Tu Tiên 2D Pixel — Tài Liệu Kiến Trúc & Nghiệp Vụ (NotebookLM Context)

Tài liệu nghiệp vụ của dự án **Sandbox Tu Tiên 2D Pixel** — hệ thống giả lập tu tiên và chiến đấu thời gian thực viết bằng **C#** trên framework **MonoGame**.

---

## 🗺️ 1. Thiết Kế Kiến Trúc

1.  **Component-Based**: Thực thể (`Player`, `Monster`, `FormationArray`) chỉ chứa dữ liệu. Logic nghiệp vụ nằm trong Component chuyên biệt (`CultivationComponent`, `InventoryComponent`).
2.  **Data-Driven**: Đan dược, vật liệu, pháp khí, pháp thuật và công thức luyện chế định nghĩa trong JSON (`Content/Data/*`), đồng bộ vào **MySQL** khi có kết nối, tự động dùng JSON khi offline.
3.  **Event-Driven**: `EventManager` là EventBus trung tâm, strongly-typed, giúp các hệ thống không phụ thuộc trực tiếp vào nhau.

---

## ⚙️ 2. Máy Trạng Thái Tu Luyện (FSM)

`CultivationComponent.cs` quản lý các trạng thái:

*   `Idle`: Nhàn rỗi.
*   `Meditating` (Đả Tọa): Dẫn linh khí nhập thể, tu vi tăng $5 \times \text{Linh Căn} \times \Delta t$ mỗi giây.
*   `BreakthroughReady` (Bình Cảnh): Tu vi đóng băng tại tầng $10, 20, 30...$ cho tới khi đột phá.
*   `Breakthrough` (Đột Phá): Dưới tầng 30 là **xung quan** (5 đợt phản phệ, không né được, 0.6s/đợt). Từ tầng 30 là **độ Thiên Kiếp** ($2 + \text{tầng}/10$ đợt lôi kiếp, 1.2s/đợt, có thể né).
*   `Dead` (Tử Vong): HP về 0 do Yêu Thú tấn công. Thất bại khi đột phá **không** gây tử vong.

---

## 🧪 3. Công Thức & Logic Nghiệp Vụ

### A. Cảnh Giới
Luyện Khí (1-10) → Trúc Cơ (11-20) → Kim Đan (21-30) → Nguyên Anh (31-40) → Hóa Thần (41-50) → Luyện Hư (51-60) → Hợp Thể (61-70) → Đại Thừa (71-80) → Độ Kiếp (81-90) → Chân Tiên (91-99) → Tiên Đế (100).

### B. Tỷ Lệ Đột Phá
$$\text{Tỷ lệ} = \text{clamp}\left(\text{Gốc} + \text{Đan Dược} + 0.02 \times \text{Space} - \text{Tâm Ma},\ 0.05,\ 1.0\right)$$
*   **Gốc**: $\max(0.2,\ 0.85 - 0.15 \times (\text{tầng}/10 - 1))$ → tầng 10: 85%, tầng 20: 70%, tầng 30: 55%...
*   **Đan Dược**: Phá Cảnh Đan +15%/viên, tối đa +50%, tiêu hết sau mỗi lần đột phá.
*   **Tâm Ma**: +10% mỗi lần thất bại (tối đa 50%), về 0 khi đột phá thành công.

### C. Sát Thương Khi Đột Phá
*   **Xung quan** (không né được): $\text{MaxHP} \times (1 - \text{Tỷ lệ}) \times \text{Random}(0.3 \to 0.5)$ mỗi đợt.
*   **Lôi kiếp** (né được): mỗi đợt giáng $1 + (\text{tầng}/10 - 2)/2$ đạo sét, mỗi đạo gây $\text{MaxHP} \times (0.12 + 0.4 \times (1 - \text{Tỷ lệ}))$. Vòng đỏ báo hiệu 0.8s trước khi sét đánh; đạo đầu nhắm vào vị trí người chơi, các đạo sau rải trong bán kính 70px.

### D. Kết Quả Đột Phá
*   **Thành công**: hồi đầy HP & Linh Lực, lĩnh ngộ Pháp Thuật theo hệ Linh Căn (lần 1 → Q, lần 2 → E), 1% thức tỉnh **Vạn Độc Thể** (+50 MaxHP, +30 Linh Lực, đòn đánh 25% tẩm độc).
*   **Thất bại**: rớt 1 tầng, HP còn 20%, Tâm Ma +10%, dược lực tiêu tán.

### E. Linh Căn
Hệ số tu luyện 0.0 → 2.0: Thiên Linh Căn (≥1.8), Chân Linh Căn (≥1.2), Tạp Linh Căn (≥0.5), Ngụy Linh Căn (>0), Phế Linh Căn (0 — không thể tu luyện). Hệ Linh Căn (Hỏa / Mộc / Băng) quyết định Pháp Thuật.

### F. Yêu Thú Tiến Hóa
$$\text{Age} \mathrel{+}= \text{Random}(8 \to 15) \times \Delta t \quad(\text{cộng dồn phần lẻ})$$
*   **Bán kính va chạm**: $16 + \log_{10}(\text{Age}) \times 4.5$
*   **Max HP**: $\text{Base} \times (1 + \text{Age}/1500)$, HP hiện tại scale theo tỷ lệ.
*   **Phẩm giai**: Nhất Giai (<100), Nhị (<300), Tam (<1.000), Tứ (<3.000), Ngũ (<10.000), Lục (<30.000), Thất (<100.000), Bát (<300.000), Cửu Giai.
*   **Uy áp**: Yêu Thú ≥10.000 năm khiến Yêu Thú <1.000 năm trong 150px bỏ chạy.
*   **Rơi đồ**: Yêu Đan Hạ phẩm (giai 1-3), Trung phẩm (4-6), Thượng phẩm (7-9); 40% rơi nguyên liệu theo hệ.

### G. Vòng Tròn Khắc Hệ
**Hỏa > Mộc > Băng > Hỏa**, khắc hệ +50% sát thương và hiện chữ `KHẮC HỆ!`.

---

## ⚔️ 4. Pháp Thuật, Pháp Khí & Trận Pháp

1.  **Pháp Thuật** (`techniques.json`), kiểu phóng `fan` / `barrage` / `nova`:
    *   Hỏa: *Liệt Hỏa Phi Tiễn* (chuỗi 8 tia), *Tam Muội Chân Hỏa* (quạt 5 tia).
    *   Mộc: *Thanh Mộc Triền Ti* (quạt 3 tia), *Vạn Mộc Khốn Trận* (quạt 5 tia).
    *   Băng: *Hàn Băng Hộ Thể* (tỏa tròn 12 tia), *Huyền Băng Liệt Phong* (quạt 10 tia).
    *   Vô thuộc tính: *Kim Quang Chỉ*, *Thiên Cương Quyền*.
2.  **Pháp Khí** (`phap_khi.json`): *Thanh Phong Phi Kiếm* (Mộc, 3 kiếm, không kinh động Yêu Thú), *Xích Diễm Kiếm Hạp* (Hỏa, 16 kiếm).
3.  **Trận Pháp** (phím `[T]`, tốn Linh Thạch, tối đa 3):
    *   **Kim Châm Trận**: 0.5s, tầm 220px, 10 sát thương, không kinh động Yêu Thú.
    *   **Phá Quân Kiếm Trận**: 3.0s, tầm 260px, 80 sát thương, tốn 2 phần linh lực.
    *   **Vạn Độc Trận**: 2.0s, tầm 180px, 15 sát thương, độc diện rộng 80px trong 5s.

---

## 🧪 5. Đan Dược & Lò Luyện

*   **Hồi Xuân Đan**: hồi 20% HP ngay + 5% HP/giây trong 10 giây (Đan Sư luyện mỗi 15s).
*   **Bổ Linh Đan**: hồi 50% Linh Lực.
*   **Phá Cảnh Đan**: +15% tỷ lệ đột phá.
*   **Yêu Đan**: luyện hóa để tăng tu vi (150 / 600 / 2.500), không dùng được khi đang bình cảnh.
*   **Lò Luyện** (`[C]`): công thức gom tự động từ `crafting_recipe` trong JSON, yêu cầu đủ nguyên liệu và cảnh giới.

---

## 🎨 6. Đồ Họa & Cảm Giác Game

*   **Sprite .png ưu tiên, hình vẽ thủ tục dự phòng**: thiếu file nào thì `PixelArtGenerator` vẽ thay (Đan Dược, Yêu Đan, Nguyên Liệu, Trận Kỳ, Đan Sư, Lò Luyện...).
*   **Font tiếng Việt**: `Arial.spritefont` có đủ dải ký tự tiếng Việt, ký tự lạ thay bằng `?` thay vì crash.
*   **Pipeline vẽ 3-Pass**: PointClamp cho sprite, LinearClamp cho chữ trong thế giới và HUD.
*   **Lôi kiếp**: vòng đỏ đậm dần khi sắp đánh, tia sét gấp khúc trắng xanh, rung màn hình.
*   **Hạt**: bụi, linh khí khi đả tọa, tàn lửa, lá mộc, tuyết băng, tia lửa, vụ nổ khi đột phá.
*   **Camera** cuộn mượt trên bản đồ `2000x2000`px.

---

## ⌨️ 7. Phím Điều Khiển

*   `[W, A, S, D]` / mũi tên: Di chuyển (được di chuyển khi độ Thiên Kiếp).
*   `[Chuột Trái]`: Phóng pháp khí. `[Chuột Phải]`: Gọi Yêu Thú.
*   `[M]`: Đả Tọa. `[R]`: Đột phá. `[Space]`: Ổn định đạo tâm khi đột phá.
*   `[Q] / [E]`: Pháp Thuật 1 / 2.
*   `[I]`: Túi trữ vật. `[Tab]`: Đổi pháp khí. `[1-5]`: Dùng nhanh vật phẩm.
*   `[C]`: Lò Luyện (`[F1-F4]` chọn công thức).
*   `[T]`: Bày trận. `[Y]`: Đổi loại trận. `[F]`: Nạp Linh Thạch.
*   `[F5]` / `[F9]`: Lưu / Tải MySQL.
*   Cheat: `[Space]` +500 tu vi, `[U]` Vạn Độc Thể, `[H]` +3 Phá Cảnh Đan.
*   `[Esc]`: Thoát.

---

## 🗄️ 8. Cơ Sở Dữ Liệu MySQL

1.  **Tự khởi tạo** CSDL `sandboxtutien_v2` (tách biệt với save bản cũ).
2.  **Bảng**:
    *   `consumables`: đan dược / vật liệu (hiệu ứng, công thức dạng JSON).
    *   `magic_weapons`: pháp khí (hệ, chỉ số, hiệu ứng, công thức dạng JSON).
    *   `player_saves`: tầng, tu vi, HP/Linh Lực, túi đồ, số lần đột phá, pháp thuật đã lĩnh ngộ (theo `id`), Vạn Độc Thể, Tâm Ma, cảnh giới, và toàn bộ thế giới (`monsters_json`, `dropped_items_json`, `formations_json`).
3.  **Đồng bộ JSON → MySQL** mỗi lần khởi động; **offline fallback** khi không có MySQL.
4.  **Tự động lưu** khi tăng tầng hoặc đột phá thành công; `[F9]` dọn thế giới hiện tại, dựng lại từ save và gắn lại sự kiện `OnKilled` cho Yêu Thú.
