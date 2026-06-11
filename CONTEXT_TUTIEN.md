TÀI LIỆU KIẾN TRÚC VÀ ĐỊNH TẢ LOGIC: DỰ ÁN SANDBOX TU TIÊN 2D PIXEL (BẢN CHUYỂN THỂ ĐẤU LA ĐẠI LỤC)
QUY TẮC TỐI CAO CHO AGENT (ANTIGRAVITY):
Tuyệt đối tuân thủ nguyên lý Data-Driven. Tách biệt hoàn toàn Logic Dữ liệu khỏi Hệ thống Hiển thị (Visual).
Sử dụng Component-Based Architecture và Finite State Machine (FSM). Không lạm dụng kế thừa sâu.
Toàn bộ mã nguồn, biến số, và cấu hình phải bám sát các chỉ số, quy luật thế giới định nghĩa trong tài liệu này.
1. THẾ GIỚI QUAN & CỐT TRUYỆN ĐẦY ĐỦ (WORLD-BUILDING)
A. Bối Cảnh Tổng Quan
Tên Thế Giới: Đấu La Đại Lục
.
Trạng Thái Năng Lượng (Thay thế Linh Khí): Thế giới này không có ma pháp hay võ thuật truyền thống, mà vận hành dựa trên hệ thống Vũ Hồn và Hồn Lực
. Năng lượng phân bổ dựa trên tố chất bẩm sinh, mỗi người lên 6 tuổi sẽ được thức tỉnh Vũ Hồn (Khí vũ hồn hoặc Thú vũ hồn), nhưng chỉ những người sở hữu "Hồn lực" mới có khả năng tu luyện để trở thành Hồn sư - nghề nghiệp cao quý nhất đại lục
.
Xung Đột Cốt Lõi: Xung đột đa chiều giữa tổ chức lớn nhất đại lục là Vũ Hồn Điện cùng hai đại đế quốc (Thiên Đấu, Tinh La) và các đại tông môn ẩn thế (Hạo Thiên Tông, Thất Bảo Lưu Ly Tông)
. Đồng thời, tồn tại mâu thuẫn sinh tồn gay gắt giữa Hồn Sư (người cần săn giết Hồn thú để thăng cấp) và Hồn Thú (những sinh vật nắm giữ Hồn hoàn và sự thù hận với loài người)
.
B. Tiến Trình Lịch Sử & Sự Kiện Định Kỳ (Dynamic Events)
Thế giới tự vận hành theo trục thời gian: 1 Phút đời thực = 1 Ngày trong game.
Mở Rộng Khu Vực Săn Bắt (Liệp Hồn Sâm Lâm / Tinh Đấu Đại Sâm Lâm): Thay vì Bí cảnh, các khu rừng Hồn Thú sẽ là nơi người chơi phải tiến vào để săn giết Hồn thú khi đạt bình cảnh
. Tại các khu rừng nguy hiểm như Tinh Đấu Đại Sâm Lâm, hồn thú càng đi sâu vào trung tâm càng cường đại (từ mười năm, trăm năm đến vạn năm, mười vạn năm)
.
Hồn Thú Bạo Động (Monster Spawn Wave): Xảy ra khi Hồn sư lạm sát quá nhiều khiến Hồn thú thù hận
. Các Hồn thú vương giả mang sức mạnh khủng khiếp như Thái Thản Cự Viên hoặc Thiên Thanh Ngưu Mãng có thể bất ngờ xuất hiện uy hiếp người chơi và các NPC Hồn sư, bất chấp cấp độ
.
2. HƯỚNG XÂY DỰNG NHÂN VẬT & TIẾN TRÌNH (CHARACTER & CULTIVATION)
A. Hệ Thống Khởi Tạo (Vũ Hồn & Tiên Thiên Hồn Lực)
Khái niệm "Linh Căn" được thay thế bằng Tiên Thiên Hồn Lực bẩm sinh và Phẩm Chất Vũ Hồn được quyết định ngay khi thức tỉnh lúc 6 tuổi
.
Loại Thức Tỉnh
Cấp Hồn Lực Ban Đầu
Tốc Độ Hấp Thu & Tu Luyện
Đặc Điểm Logic
Tiên Thiên Mãn Hồn Lực
Đạt ngay cấp 10 (Max)
200%
Tốc độ tu luyện cực nhanh, là thiên tài hiếm có (như Đường Tam, Áo Tư Tạp), lập tức cần săn Hồn hoàn đầu tiên để đột phá
.
Tiên Thiên Hồn Lực Thường
Từ Cấp 1 đến 9
Tỷ lệ thuận với cấp bẩm sinh
Tốc độ tu luyện trung bình, yêu cầu thông qua minh tưởng để tăng cấp dần dần
.
Biến Dị / Song Sinh Vũ Hồn
Tùy biến (Thường là cấp 10)
Biến đổi đặc biệt
Rất hiếm gặp, có thể sở hữu cùng lúc 2 vũ hồn (yêu cầu không gắn hồn hoàn cho vũ hồn thứ 2 sớm) hoặc vũ hồn biến dị cực mạnh/cực yếu
.
Phế Vũ Hồn (Không Hồn Lực)
Cấp 0
0%
Cả đời không thể trở thành Hồn sư, chỉ là NPC bình thường
.
B. Hệ Thống Cảnh Giới & Cơ Chế Đột Phá (State Machine)
Mỗi nhân vật chịu sự quản lý của CultivationStateMachine với các trạng thái tương ứng với logic Đấu La Đại Lục: [IDLE, MEDITATING (Minh Tưởng), HUNTING_RING (Săn Hồn Hoàn), ABSORBING_RING (Hấp thu Hồn Hoàn), DEAD].
1. Hệ thống Cảnh Giới (Hồn Lực từ 1 đến 100):
Hồn Sĩ (Cấp 1-10) → Hồn Sư (Cấp 11-20) → Đại Hồn Sư (Cấp 21-30) → Hồn Tôn (Cấp 31-40) → Hồn Tông (Cấp 41-50) → Hồn Vương (Cấp 51-60) → Hồn Đế (Cấp 61-70) → Hồn Thánh (Cấp 71-80) → Hồn Đấu La (Cấp 81-90) → Phong Hào Đấu La (Cấp 91-99) → Thần (Cấp 100)
.
2. Cơ Chế Đột Phá (Bình Cảnh & Hồn Hoàn):
Cứ mỗi 10 cấp, nhân vật sẽ đạt bình cảnh (nút thắt)
.
Để đột phá trạng thái MEDITATING sang cảnh giới tiếp theo, FSM phải chuyển sang HUNTING_RING để đích thân giết Hồn Thú, sau đó chuyển sang ABSORBING_RING
.
Quy tắc giới hạn Hồn Hoàn (Cực hạn hấp thu): Việc hấp thu phải tuân thủ giới hạn chịu đựng của cơ thể dựa trên số năm tu vi của Hồn thú:
Hồn hoàn 1 (Bạch/Hoàng sắc): Thập niên - Bách niên
.
Hồn hoàn 3 (Tử sắc): Tối đa khoảng 1.700 năm
.
Hồn hoàn 4: Tối đa 5.000 năm
.
Hồn hoàn 5: Tối đa 12.000 năm
.
Hồn hoàn 6: Tối đa 20.000 năm
.
Hồn hoàn 7 (Hắc sắc): Từ 30.000 đến 50.000 năm
.
Hồn hoàn 8 & 9 (Hắc/Hồng sắc): Trên 50.000 năm đến 10 vạn năm
.
Thất bại khi đột phá: Nếu hấp thu Hồn hoàn vượt quá cực hạn hoặc xung đột thuộc tính, FSM sẽ chuyển sang trạng thái DEAD do cơ thể bạo liệt
.
Lợi ích đột phá: Mỗi Hồn hoàn cung cấp một Hồn Kỹ (Skill) duy nhất và nâng cao toàn diện chỉ số thuộc tính cơ thể
. Đặc biệt, có tỷ lệ 1/1000 rớt ra Hồn Cốt (Soul Bone) cung cấp thêm kỹ năng vĩnh viễn không bị giới hạn cấp độ
.
Logic Đột Phá và Thiên Kiếp:


Tích lũy: Khi Linh khí đạt tối đa ($Max_Linh_Khi$), nhân vật chuyển sang trạng thái BREAKTHROUGH.

Tính toán tỷ lệ: $Tỷ_Lệ_Thành_Công = Base_Rate + Đan_Dược_Buff - Tâm_Ma_Debuff$.

Cơ chế Thiên Kiếp (Áp dụng từ Kim Đan trở lên):

Kích hoạt trạng thái TRIALS. Hệ thống sinh ra $N$ đợt Sét đánh ngẫu nhiên (Projectile gây sát thương Lôi hệ).

Nhân vật phải sống sót trong thời gian $T$ hoặc sử dụng Pháp bảo phòng ngự để triệt tiêu sát thương.

Nếu Vực máu ($HP$) về 0 ➔ Chuyển sang trạng thái DEAD (NPC xóa dữ liệu, Player chịu phạt tổn hao tu vi).





3. LỐI CHƠI SANDBOX CỐT LÕI (CORE GAMEPLAY MECHANICS)

A. Hệ Thống Tương Tác NPC & Nhân Quả (Dynamic AI Relationship)

NPC không đứng yên. Mỗi NPC có một MindSystem chạy ngầm.



Hệ thống Danh Vọng / Thiện Ác: Hành động của người chơi (Giúp đỡ, Đồ sát, Trộm cắp) sẽ cộng/trừ điểm Karma.

Hệ thống Truy Vết (Vendetta System): Nếu Người chơi giết NPC $A$, hệ thống sẽ quét cây quan hệ của $A$ (Sư phụ, Đạo lữ, Tộc nhân). Nếu tìm thấy nhân vật có cảnh giới cao hơn Người chơi, sẽ kích hoạt cờ REVENGE. NPC báo thù sẽ bắt đầu di chuyển về phía tọa độ của Người chơi trên bản đồ thế giới vĩ mô.


B. Hệ Thống Động Phủ & Xây Dựng (Sandbox Building & Farming)


Xây dựng: Người chơi có thể đặt các thực thể chức năng (Tile/Object) xuống bản đồ: Lò Luyện Đan, Lò Đúc Khí, Bồ Đoàn Thiền Định, Trận Nhãn Phòng Ngự.

Linh Điền (Farming): Ô đất được đặt cạnh Linh Mạch sẽ có chỉ số $Linh_Tích_Đất$. Khi gieo hạt giống Linh Thảo, thời gian chín của cây sẽ bằng: $Time_{grow} = Base_Time / Linh_Tích_Đất$.

C. Logic Đột Phá và Khảo Nghiệm Hấp Thu Hồn Hoàn (Thay thế Thiên Kiếp):
Trong Đấu La Đại Lục, không có "Thiên Kiếp" khi thăng cấp, sự hung hiểm đến từ việc Hấp thu Hồn Hoàn vượt cực hạn
.
Tích lũy & Chạm Bình Cảnh: Khi Hồn Lực đạt tối đa của một danh hiệu (VD: cấp 10, 20, 30...), điểm kinh nghiệm (EXP) bị khóa. Nhân vật chuyển sang trạng thái BREAKTHROUGH_READY
.
Săn giết & Kích hoạt: Nhân vật phải tự tay giết Hồn Thú để Hồn Hoàn xuất hiện (thời gian tồn tại 1 canh giờ) và chuyển FSM sang ABSORBING_RING
.
Cơ chế Phản Phệ (Hấp thu Hồn Hoàn):
Hệ thống so sánh Số năm tu vi của Hồn Hoàn với Cực hạn chịu đựng của cơ thể (Body_Limit)
.
Tỷ_Lệ_Thành_Công=Hồn_Hoàn_AgeBody_Limit+Willpower_Buff​×100%.
Hệ thống sinh ra N đợt sát thương nội tại (Internal Damage) trong thời gian T, mô phỏng năng lượng bạo lệ của Hồn Hoàn đang xé rách kinh mạch
.
Nếu Vực máu (HP) về 0 trong quá trình này ➔ Cơ thể bạo thể mà vong, chuyển sang trạng thái DEAD (Mất mạng vĩnh viễn)
. Nếu sống sót, Hồn Lực tự động đột phá sang cấp tiếp theo và nhận Hồn Kỹ mới
.
3. LỐI CHƠI SANDBOX CỐT LÕI (CORE GAMEPLAY MECHANICS)
A. Hệ Thống Tương Tác NPC & Nhân Quả (Dynamic AI/Faction Relationship)
NPC quản lý bởi FactionMindSystem.
Hệ thống Thế Lực (Faction Standing): Điểm danh vọng không chia Thiện/Ác mà chia theo Thế lực: Vũ Hồn Điện, Hạo Thiên Tông, Thất Bảo Lưu Ly Tông, Hai Đại Đế Quốc
. Giúp đỡ một phe sẽ tự động giảm danh vọng phe đối lập (VD: Thân thiết với Hạo Thiên Tông sẽ bị Vũ Hồn Điện thù ghét)
.
Hệ thống Truy Vết & Thú Triều Báo Thù (Vendetta System):
Với NPC Hồn Sư: Giết NPC sẽ quét cây quan hệ Sư đồ/Tông môn. Nếu kích hoạt cờ REVENGE, các Hồn sư cấp cao (VD: Phong Hào Đấu La) sẽ tổ chức truy sát
.
Với Hồn Thú: Giết Hồn Thú nhỏ (VD: Thỏ, Khỉ) có tỷ lệ % đánh thức các Vua Hồn Thú (VD: Thái Thản Cự Viên, Thiên Thanh Ngưu Mãng)
. Kích hoạt trạng thái BEAST_REVENGE, Boss sẽ di chuyển đến tọa độ của người chơi để đập phá Động phủ/Học viện
.
B. Hệ Thống Xây Dựng & Chế Tác Ám Khí (Sandbox Building & Blacksmithing)
Xây dựng Công xưởng (Workshop): Thay vì Lò Luyện Đan, người chơi sẽ đặt các Tile/Object như: Lò Rèn (Forge), Ống Bễ (Bellows), Bàn Chế Tác Ám Khí
.
Chế Tác Ám Khí & Rèn (Blacksmithing): Yêu cầu chỉ số Lực_Lượng và độ_chinh_xac Thông qua kỹ năng như Loạn Phi Phong Chùy Pháp, người chơi loại bỏ tạp chất của sắt thường thành "Thiết Mẫu" để chế tạo Ám Khí (Tụ Tiễn, Chư Cát Thần Nỗ)
Vườn Ươm Tiên Thảo (Farming): Ô đất đặt cạnh các mạch suối đặc biệt (VD: Băng Hỏa Lưỡng Nghi Nhãn) sẽ có chỉ số Năng_Lượng_Thổ_Nhưỡng.Tốc độ sinh trưởng của Tiên Thảo (như Bát Giác Huyền Băng Thảo):Timegrow​=Base_Time/Năng_Lượng_Thổ_Nhưỡng
C. Cơ Chế Chiến Đấu & Khắc Chế Thuộc Tính Vũ Hồn (Elemental Combat)
Chiến đấu dựa trên Tương khắc Vũ hồn và Phân loại hệ:
Khắc chế thuộc tính: Hỏa khắc Mộc (Phượng Hoàng Hỏa Tuyến thiêu rụi Lam Ngân Thảo)
, Băng khắc Hỏa (Huyền Thủy Ngưng Băng dập tắt lửa)
. Sát thương tăng 50% nếu khắc hệ.
Khắc chế Hệ Hồn Sư: Khống Chế hệ khắc Mẫn Công hệ; Mẫn Công hệ khắc Phụ Trợ hệ
.
Object Pooling: Tất cả đòn đánh tầm xa như Hồn Kỹ (Bạch Hổ Liệt Quang Ba, Phượng Hoàng Hỏa Tuyến)
 và hàng loạt Ám Khí (Phi tiêu, Chư Cát Thần Nỗ bắn ra 16 mũi tên cùng lúc)
 phải quản lý bởi ProjectilePool để tối ưu RAM.
1. CHI TIẾT NỘI DUNG DỮ LIỆU (DATA DEFINITION FOR AGENT)
Agent sử dụng cấu trúc dữ liệu này để sinh file cấu hình JSON. Chuyển đổi Đan dược thành cấu trúc Ám Khí (Hidden Weapon) và Vật Phẩm Phụ Trợ (Consumable/Sausage) phù hợp với Đấu La Đại Lục.
A. Cấu trúc Chế tác Ám Khí mẫu (Hidden Weapon Data Structure)
Dựa trên nguyên lý chế tạo "Vô Thanh Tụ Tiễn"
.
{
  "item_id": "am_khi_tu_tien_01",
  "name": "Vô Thanh Tụ Tiễn",
  "type": "HIDDEN_WEAPON",
  "tier_required": 1,
  "combat_stats": {
    "base_damage": 150,
    "range": 30,
    "projectile_count": 3,
    "silent_attack": true
  },
  "effects": [
    {
      "type": "APPLY_DEBUFF",
      "status": "POISON",
      "value": 10,
      "duration": 5
    }
  ],
  "crafting_recipe": [
    {"item_id": "mat_thi_thiet_mau", "quantity": 1}, 
    {"item_id": "lo_xo_co_quan", "quantity": 1}
  ]
}
B. Cấu trúc Thực Phẩm Phụ Trợ mẫu (Food/Consumable Data Structure)
Dựa trên hiệu năng của "Khôi Phục Hương Tràng" (Xúc xích hồi phục của Áo Tư Tạp)
.
{
  "item_id": "food_huong_trang_01",
  "name": "Khôi Phục Hương Tràng",
  "type": "CONSUMABLE",
  "source": "SOUL_SKILL_CREATED",
  "tier_required": 1,
  "effects": [
    {
      "type": "HEAL_HP",
      "value_percentage": 20
    },
    {
      "type": "RECOVER_SOUL_POWER",
      "value_percentage": 10
    }
  ],
  "spoilage_time": 43200 
}
(Ghi chú: spoilage_time là 12 canh giờ - thời gian tối đa hương tràng có thể tồn tại theo thiết lập nguyên tác
)
5. YÊU CẦU KỸ THUẬT & KIỂM SOÁT LỖI (TECHNICAL SPECIFICATIONS)
Time-Slicing Manager: Hệ thống quản lý thế giới (WorldManager) chỉ được phép xử lý tối đa 10 NPC di chuyển/tính toán logic trên một khung hình (Frame) để tránh hiện tượng nghẽn CPU (Lag Spike).

Robust Save/Load System: Tất cả các thành phần có trạng thái thay đổi (CultivationComponent, InventoryComponent, vị trí Block động phủ) bắt buộc phải triển khai hàm Serialize() trả về chuỗi JSON và Deserialize() để khôi phục trạng thái.

Strict Error Handling: Mọi hành vi cắn đan, dùng kỹ năng, dịch chuyển tọa độ phải được bọc trong các khối lệnh kiểm tra điều kiện đầu vào (Null-check, Boundary-check). Không để xảy ra lỗi crash game giữa chừng.