# TÀI LIỆU KIẾN TRÚC VÀ ĐẶC TẢ LOGIC: DỰ ÁN SANDBOX TU TIÊN 2D PIXEL

## QUY TẮC TỐI CAO CHO AGENT
*   Tuyệt đối tuân thủ nguyên lý **Data-Driven**. Tách biệt hoàn toàn logic dữ liệu khỏi hệ thống hiển thị.
*   Sử dụng **Component-Based Architecture** và **Finite State Machine (FSM)**. Không lạm dụng kế thừa sâu.
*   Mã nguồn, biến số và cấu hình phải bám sát các chỉ số, quy luật thế giới trong tài liệu này.
*   Mục đánh dấu **[Đã triển khai]** đã có trong code; **[Kế hoạch]** là định hướng chưa làm.

---

## 1. THẾ GIỚI QUAN

### A. Bối cảnh
*   **Tên thế giới**: Thương Lan Giới — một đại lục tu tiên nơi linh khí thiên địa là nguồn gốc mọi sức mạnh.
*   **Năng lượng**: Tu sĩ dùng **Linh Căn** bẩm sinh để dẫn linh khí nhập thể, tích lũy **tu vi** và **Linh Lực**. Người không có Linh Căn là phàm nhân.
*   **Xung đột cốt lõi**: Tu sĩ cần săn **Yêu Thú** lấy Yêu Đan và nguyên liệu để tu luyện, luyện đan, luyện khí; Yêu Thú lâu năm hình thành **Yêu Vương** thù hận nhân loại. Các tông môn tranh giành tài nguyên và cơ duyên. **[Kế hoạch: tông môn]**

### B. Trục thời gian & sự kiện
*   Thế giới tự vận hành: **1 phút đời thực = 1 ngày trong game**. **[Đã triển khai]**
*   **Yêu Thú tiến hóa**: tuổi tăng liên tục, vượt mốc thì tấn thăng phẩm giai. **[Đã triển khai]**
*   **Uy áp Yêu Vương**: Yêu Thú ≥ 10.000 năm khiến Yêu Thú yếu (< 1.000 năm) bỏ chạy. **[Đã triển khai]**
*   **Thú triều**: tàn sát quá nhiều Yêu Thú có tỷ lệ đánh thức Yêu Vương tấn công Động Phủ. **[Kế hoạch]**

---

## 2. NHÂN VẬT & TIẾN TRÌNH TU LUYỆN

### A. Linh Căn **[Đã triển khai]**

| Loại Linh Căn | Hệ số tu luyện | Đặc điểm |
| :--- | :--- | :--- |
| Thiên Linh Căn | ≥ 1.8 | Thiên tài hiếm có, tu luyện cực nhanh |
| Chân Linh Căn | ≥ 1.2 | Tư chất tốt |
| Tạp Linh Căn | ≥ 0.5 | Tư chất bình thường |
| Ngụy Linh Căn | > 0 | Tu luyện rất chậm |
| Phế Linh Căn | 0 | Không thể dẫn khí nhập thể — chỉ là phàm nhân |

Mỗi Linh Căn có một **hệ** (Hỏa / Mộc / Băng / Vô) quyết định Pháp Thuật lĩnh ngộ khi đột phá.

### B. Cảnh giới **[Đã triển khai]**
Tu vi gồm 100 tầng, mỗi đại cảnh giới 10 tầng:

Luyện Khí (1-10) → Trúc Cơ (11-20) → Kim Đan (21-30) → Nguyên Anh (31-40) → Hóa Thần (41-50) → Luyện Hư (51-60) → Hợp Thể (61-70) → Đại Thừa (71-80) → Độ Kiếp (81-90) → Chân Tiên (91-99) → Tiên Đế (100).

### C. FSM tu luyện **[Đã triển khai]**
`[IDLE, MEDITATING (Đả Tọa), BREAKTHROUGH_READY (Bình Cảnh), BREAKTHROUGH (Đột Phá), DEAD]`

1.  **Tích lũy**: Khi Đả Tọa, tu vi tăng `5 × Hệ_Số_Linh_Căn × Δt` mỗi giây. Tu vi cần lên tầng = `100 × (1 + tầng × 0.5)`.
2.  **Bình cảnh**: Tại tầng 10, 20, 30... tu vi bị khóa, FSM chuyển sang `BREAKTHROUGH_READY`. Không thể Đả Tọa hay luyện hóa Yêu Đan cho tới khi đột phá.
3.  **Đột phá** (người chơi chủ động kích hoạt):
    *   **Xung quan** (bình cảnh dưới tầng 30): 5 đợt linh lực phản phệ nội tại, không thể né.
    *   **Thiên Kiếp** (từ tầng 30 trở lên): `2 + tầng/10` đợt lôi kiếp. Mỗi đạo thiên lôi có vòng báo hiệu 0.8 giây; người chơi được di chuyển để né hoặc chịu sát thương.

### D. Tỷ lệ đột phá **[Đã triển khai]**
```
Tỷ_Lệ_Thành_Công = clamp(Tỷ_Lệ_Gốc + Đan_Dược_Buff + Ổn_Định_Đạo_Tâm − Tâm_Ma_Debuff, 5%, 100%)
Tỷ_Lệ_Gốc        = max(20%, 85% − 15% × (tầng/10 − 1))
Đan_Dược_Buff    = tổng dược lực Phá Cảnh Đan đã dùng (tối đa 50%), tiêu hết sau mỗi lần đột phá
Ổn_Định_Đạo_Tâm  = 2% × số lần bấm Space trong lúc đột phá
Tâm_Ma_Debuff    = +10% mỗi lần thất bại (tối đa 50%), về 0 khi thành công
```

Sát thương:
```
Xung quan (mỗi đợt)   = MaxHP × (1 − Tỷ_Lệ) × Random(0.3 → 0.5)
Lôi kiếp (mỗi đạo)    = MaxHP × (0.12 + 0.4 × (1 − Tỷ_Lệ))
Số đạo mỗi đợt lôi kiếp = 1 + (tầng/10 − 2) / 2
```

### E. Kết quả đột phá **[Đã triển khai]**
*   **Thành công**: hồi đầy HP/Linh Lực; lần đột phá thứ 1 và 2 lĩnh ngộ Pháp Thuật theo hệ Linh Căn; 1% cơ duyên thức tỉnh thể chất **Vạn Độc Thể**.
*   **Thất bại** (HP về 0 khi đột phá): **đạo cơ tổn hại** — rớt 1 tầng tu vi, HP còn 20%, Tâm Ma tăng, dược lực tiêu tán. Không tử vong.
*   **Tử vong** chỉ xảy ra khi HP về 0 do Yêu Thú tấn công ngoài lúc đột phá.

---

## 3. LỐI CHƠI SANDBOX CỐT LÕI

### A. NPC & Nhân quả **[Kế hoạch]**
*   **FactionMindSystem**: điểm danh vọng theo tông môn; thân thiết với một phe làm phe đối địch thù ghét.
*   **Vendetta System**: giết NPC sẽ quét cây quan hệ (Sư phụ, Đạo lữ, Tộc nhân). Nếu có người cảnh giới cao hơn người chơi, bật cờ `REVENGE` và NPC đó truy sát theo tọa độ người chơi.
*   **Yêu Vương báo thù**: giết Yêu Thú nhỏ có tỷ lệ kích hoạt `BEAST_REVENGE`, Yêu Vương di chuyển tới Động Phủ.

### B. Động Phủ, Luyện Đan & Luyện Khí
*   **Lò Luyện** **[Đã triển khai]**: luyện Đan Dược và Pháp Khí từ nguyên liệu; công thức lấy từ `crafting_recipe` trong JSON, yêu cầu đủ nguyên liệu và cảnh giới (`tier_required`).
*   **Trận Pháp** **[Đã triển khai]**: cắm Trận Kỳ tự động công kích, tiêu hao Linh Thạch (Kim Châm Trận, Phá Quân Kiếm Trận, Vạn Độc Trận).
*   **Xây dựng Động Phủ** **[Kế hoạch]**: đặt Bồ Đoàn Thiền Định (tăng tốc đả tọa), Trận Nhãn Phòng Ngự.
*   **Linh Điền** **[Kế hoạch]**: ô đất cạnh Linh Mạch có chỉ số `Linh_Tích_Đất`; `Time_grow = Base_Time / Linh_Tích_Đất`.

### C. Chiến đấu & Khắc hệ **[Đã triển khai]**
*   **Hỏa khắc Mộc, Mộc khắc Băng, Băng khắc Hỏa** — khắc hệ tăng 50% sát thương.
*   **Hỏa tịnh độc**: đòn Hỏa hệ xóa độc tố trên mục tiêu.
*   **Mộc trói chân**: đòn Mộc hệ > 30 sát thương trói Yêu Thú (Yêu Thú tốc độ bị trói lâu hơn).
*   **Object Pooling**: toàn bộ phi kiếm, pháp thuật, đạn trận pháp dùng `ProjectilePool`.

### D. Yêu Thú **[Đã triển khai]**
*   Tuổi tăng 8–15 năm mỗi giây thực; phẩm giai: Nhất (<100), Nhị (<300), Tam (<1.000), Tứ (<3.000), Ngũ (<10.000), Lục (<30.000), Thất (<100.000), Bát (<300.000), Cửu Giai.
*   Trảm sát rơi Yêu Đan theo phẩm giai (Hạ phẩm 1-3, Trung phẩm 4-6, Thượng phẩm 7-9) và 40% nguyên liệu theo hệ.

---

## 4. ĐỊNH NGHĨA DỮ LIỆU (DATA DEFINITION FOR AGENT)

### A. Pháp Khí — `Content/Data/phap_khi.json`
```json
{
  "item_id": "phi_kiem_thanh_phong",
  "name": "Thanh Phong Phi Kiếm",
  "type": "MAGIC_WEAPON",
  "element": "Wood",
  "tier_required": 0,
  "combat_stats": {
    "base_damage": 150,
    "range": 30,
    "projectile_count": 3,
    "silent_attack": true
  },
  "effects": [
    { "type": "APPLY_DEBUFF", "status": "POISON", "value": 10, "duration": 5 }
  ],
  "crafting_recipe": [
    { "item_id": "han_thiet", "quantity": 1 },
    { "item_id": "hoa_tinh_thach", "quantity": 1 }
  ]
}
```

### B. Đan Dược / Vật Liệu — `Content/Data/consumables.json`
```json
{
  "item_id": "dan_pha_canh",
  "name": "Phá Cảnh Đan",
  "type": "CONSUMABLE",
  "source": "CRAFTED",
  "tier_required": 0,
  "effects": [
    { "type": "BREAKTHROUGH_BUFF", "value_percentage": 15 }
  ],
  "crafting_recipe": [
    { "item_id": "yeu_dan_ha_pham", "quantity": 2 },
    { "item_id": "hoa_tinh_thach", "quantity": 1 }
  ],
  "spoilage_time": -1
}
```
Loại hiệu ứng hỗ trợ: `HEAL_HP`, `HEAL_HP_OVER_TIME`, `RECOVER_SPIRIT_POWER`, `BREAKTHROUGH_BUFF`, `GAIN_CULTIVATION`, `APPLY_DEBUFF`. Vật phẩm không có hiệu ứng là **nguyên liệu** (không dùng trực tiếp).

### C. Lưu Phái — `Content/Data/classes.json`
```json
{
  "id": "kiem_tu",
  "name": "Kiếm Tu",
  "stat_multipliers": { "hp": 1.0, "spirit_power": 0.9, "speed": 1.1 },
  "passives": [{ "id": "kiem_y", "name": "Kiếm Ý", "description": "..." }],
  "skills": { "basic": "kiemtu_ngu_kiem_thuat", "skill_1": "...", "dash": "...", "ultimate": "..." }
}
```
Chọn một lần khi bắt đầu game mới. `stat_multipliers` nhân vào HP/Linh Lực/Tốc độ gốc (`CultivationComponent`). `skills` tham chiếu 6 ô id sang `skills.json`; Pháp Tu dùng thêm `skills.by_element` (khóa `Fire`/`Wood`/`Ice`) thay vì các trường cố định vì bộ chiêu đổi theo Linh Căn.

### D. Chiêu Thức — `Content/Data/skills.json`
```json
{
  "id": "hoa_1",
  "class_id": "phap_tu",
  "slot": "skill_1",
  "type": "projectile",
  "element": "Fire",
  "unlock_realm": "LuyenKhi",
  "sp_cost": 25,
  "damage": 100,
  "pattern": "barrage",
  "count": 8,
  "spread": 0.08,
  "spacing": 8,
  "range": 400,
  "speed": 450,
  "cooldown": 4
}
```
*   `slot`: `basic` / `skill_1` / `skill_2` / `skill_3` / `dash` / `ultimate` — 6 ô cố định của mọi lưu phái.
*   `unlock_realm`: mở khóa khi `CultivationComponent.CurrentRealm` đạt cảnh giới này (thay cho hệ tier-theo-đột-phá cũ).
*   `type`: `projectile` (đã thi triển đầy đủ qua `SkillSystem`, dùng `pattern` `fan`/`barrage`/`nova`), `dash` (di chuyển tức thời + sát thương/đẩy lùi tại điểm đến, đã thi triển), `zone` (vùng sát thương/hồi máu theo thời gian qua `ZoneSystem`, đã thi triển). `melee_arc`, `ground_aoe`, `self_buff`, `channel` đã có đủ trường dữ liệu nhưng **chưa được thi triển** — `SkillSystem.TryCast` trả về `NotSupported`.
*   `GameDataValidator` kiểm tra mọi id lưu phái tham chiếu tồn tại, đúng `class_id`, đúng `slot` khai báo.

---

## 5. YÊU CẦU KỸ THUẬT & KIỂM SOÁT LỖI

*   **Time-Slicing Manager**: chỉ xử lý tối đa 10 thực thể logic mỗi frame để tránh lag spike. **[Đã triển khai cho CultivationSystem]**
*   **Robust Save/Load**: mọi thành phần có trạng thái (`CultivationComponent`, `InventoryComponent`, thực thể thế giới) phải có `Serialize()` / khôi phục trạng thái. Pháp thuật lưu theo `id`, không lưu theo tên. **[Đã triển khai — CSDL `sandboxtutien_v2`]**
*   **Strict Error Handling**: mọi hành vi dùng đan, dùng pháp thuật, dịch chuyển tọa độ phải kiểm tra điều kiện đầu vào (null-check, boundary-check); vật phẩm không dùng được thì không bị tiêu hao. Không để crash giữa chừng.
*   **Font**: mọi chuỗi hiển thị bằng `SpriteFont` chỉ dùng ký tự có trong `Arial.spritefont` (ASCII + tiếng Việt); ký tự ngoài bảng sẽ hiện `?`.
