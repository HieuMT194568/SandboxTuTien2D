using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SandboxTuTien.Core
{
    /// <summary>
    /// Bộ tạo ảnh Pixel-Art chuyên nghiệp trực tiếp vào bộ nhớ đồ họa khi chạy game.
    /// Thiết kế theo phong cách Đấu La Đại Lục, sử dụng đổ bóng (Shading), 
    /// chuyển màu (Gradients) và các chi tiết đặc trưng để tạo giao diện premium.
    /// </summary>
    public static class PixelArtGenerator
    {
        /// <summary>
        /// Kiếm tu áo lam (32x32):
        /// Tóc dài màu xanh lam bay trong gió, giáp vai vàng (gold pauldrons), 
        /// đai lưng hoàng kim và tà áo dài chuyển sắc từ xanh đậm sang lam nhạt.
        /// </summary>
        public static Texture2D CreatePlayerTexture(GraphicsDevice gd)
        {
            int w = 32, h = 32;
            Color[] pixels = new Color[w * h];
            Color transparent = Color.Transparent;

            // Palette màu kiếm tu áo lam
            Color skinBase = new Color(245, 195, 170);      // Da thường
            Color skinShadow = new Color(210, 160, 135);    // Bóng cổ/mặt
            Color hairBlue = new Color(30, 100, 220);       // Tóc xanh lam hoàng tộc
            Color hairHighlight = new Color(100, 180, 255); // Ánh sáng trên tóc
            Color hairShadow = new Color(15, 45, 120);      // Tóc vùng bóng tối
            Color armorGold = new Color(245, 190, 40);      // Giáp hoàng kim
            Color armorGoldShadow = new Color(165, 120, 15); // Giáp vàng đổ bóng
            Color robeDarkBlue = new Color(20, 40, 100);    // Áo xanh lam đậm
            Color robeMidBlue = new Color(40, 80, 180);     // Áo lam sáng
            Color robeLightBlue = new Color(90, 140, 245);  // Viền áo lam nhạt
            Color beltGold = new Color(255, 215, 0);        // Thắt lưng hoàng kim
            Color bootsBlack = new Color(25, 25, 28);
            Color bootsHighlight = new Color(80, 85, 95);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = transparent;

                    // 1. Tóc dài Lam Ngân Hoàng bay về bên trái (x từ 8 -> 22, y từ 2 -> 16)
                    // Tạo lọn tóc bay bay
                    bool isHair = false;
                    if (y >= 2 && y <= 6 && x >= 11 && x <= 20) isHair = true; // Đỉnh đầu
                    if (y >= 7 && y <= 15 && x >= 9 && x <= 14) isHair = true;  // Lọn tóc dài bay trái

                    // 2. Khuôn mặt và cổ
                    bool isFace = (y >= 7 && y <= 13 && x >= 14 && x <= 20);
                    bool isNeck = (y == 14 && x >= 15 && x <= 18);

                    // 3. Giáp vai Hoàng Kim (y từ 15 -> 17)
                    bool isPauldron = (y >= 15 && y <= 17 && ((x >= 9 && x <= 12) || (x >= 21 && x <= 24)));

                    // 4. Thân áo choàng
                    bool isRobe = (y >= 15 && y <= 27 && x >= 10 && x <= 23 && !isPauldron);

                    // 5. Giày
                    bool isBoots = (y >= 28 && y <= 29 && x >= 11 && x <= 22);

                    if (isHair)
                    {
                        c = hairBlue;
                        // Đổ bóng & ánh sáng cho tóc
                        if (x == 11 || y == 2 || x == 10) c = hairHighlight;
                        if (x == 14 || y >= 12 || x == 9) c = hairShadow;
                    }
                    else if (isFace)
                    {
                        c = skinBase;
                        // Vẽ mắt xanh ngọc bích thần bí
                        if (y == 9 && (x == 16 || x == 19)) c = new Color(30, 200, 150);
                        // Bóng mắt/tóc đè
                        if (y == 7 || y == 8 || x == 14) c = skinShadow;
                        // Miệng cười nhẹ
                        if (y == 12 && x >= 17 && x <= 18) c = new Color(220, 100, 100);
                    }
                    else if (isNeck)
                    {
                        c = skinShadow;
                    }
                    else if (isPauldron)
                    {
                        c = armorGold;
                        if (y == 17 || x == 9 || x == 24) c = armorGoldShadow; // Bóng giáp vai
                    }
                    else if (isRobe)
                    {
                        c = robeMidBlue;
                        // Gradient chuyển sắc theo chiều dọc
                        if (y >= 23) c = robeDarkBlue;
                        if (y <= 18) c = robeLightBlue;

                        // Hoa văn ánh kim/lam nhạt viền áo
                        if (x == 10 || x == 23 || y == 27) c = robeLightBlue;
                        
                        // Thắt lưng hoàng kim (y = 20-21)
                        if ((y == 20 || y == 21) && x >= 12 && x <= 21)
                        {
                            c = beltGold;
                            if (x == 16 || x == 17) c = Color.Red; // Hồng ngọc đính đai lưng
                        }
                    }
                    else if (isBoots)
                    {
                        if (x <= 14 || x >= 18)
                        {
                            c = bootsBlack;
                            if (y == 28 && (x == 12 || x == 20)) c = bootsHighlight;
                        }
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Lam Ngân Thảo Đột Biến (Mộc - 32x32):
        /// Các nhánh dây leo xoắn quấn màu xanh ngọc (neon teal-green) phát ra ánh huỳnh quang,
        /// đỉnh có hoa văn vân bạc rực rỡ và gai gai nhọn.
        /// </summary>
        public static Texture2D CreateMonsterPlantTexture(GraphicsDevice gd)
        {
            int w = 32, h = 32;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            // Palette màu Lam Ngân Thảo Đột Biến
            Color neonTeal = new Color(20, 200, 180);       // Thân cỏ neon phát sáng
            Color darkTeal = new Color(5, 80, 75);         // Vùng khuất tối
            Color silverLustre = new Color(220, 240, 255);   // Vân bạc rực rỡ
            Color thornPink = new Color(230, 80, 160);      // Gai độc màu hồng sen dị biệt

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;

                    // Tính toán hình dáng dây leo xoắn ốc bằng hàm Sin/Cos
                    // Khoảng cách đến trục xoắn ốc
                    float centerY = y - 16;
                    float centerX = x - 16;
                    
                    // Tạo thân xoắn giữa màn hình
                    float wave = (float)Math.Sin(y * 0.4f) * 4f;
                    float distToStem = Math.Abs(centerX - wave);

                    if (y >= 10 && distToStem < 3f)
                    {
                        c = neonTeal;
                        // Bóng tối phần rìa
                        if (centerX - wave < -1f) c = darkTeal;
                        // Vân bạc dọc thân
                        if (Math.Abs(centerX - wave) < 0.8f && y % 3 == 0) c = silverLustre;
                    }

                    // Nhánh phụ 1 tỏa sang trái (dưới)
                    float branch1 = (16 - y) * 0.6f + 8;
                    if (y >= 16 && y <= 26 && Math.Abs(x - (16 - branch1)) < 2f)
                    {
                        c = neonTeal;
                        if (x % 3 == 0) c = thornPink; // Gai độc
                    }

                    // Nhánh phụ 2 tỏa sang phải (trên)
                    float branch2 = (16 - y) * 0.8f - 6;
                    if (y >= 8 && y <= 18 && Math.Abs(x - (16 - branch2)) < 2f)
                    {
                        c = neonTeal;
                        if (y % 3 == 0) c = thornPink; // Gai độc
                    }

                    // Hoa Lam Ngân nở trên đỉnh (y từ 4 -> 9, x từ 12 -> 20)
                    float distToFlower = Vector2.Distance(new Vector2(x, y), new Vector2(16, 6));
                    if (distToFlower < 4.5f)
                    {
                        c = silverLustre;
                        if (distToFlower < 2.5f) c = neonTeal;
                        if (distToFlower < 1.0f) c = thornPink; // Nhụy hoa độc
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Tà Hỏa Phượng Hoàng (Hỏa - 32x32):
        /// Chú chim lửa dang rộng đôi cánh lông vũ chuyển màu lộng lẫy (crimson -> orange -> gold)
        /// với ngọn lửa xòe rộng rực rỡ và mắt thần vàng rực.
        /// </summary>
        public static Texture2D CreateMonsterFireTexture(GraphicsDevice gd)
        {
            int w = 32, h = 32;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            // Palette màu Tà Hỏa Phượng Hoàng
            Color crimson = new Color(180, 15, 30);      // Viền cánh đậm
            Color fireOrange = new Color(255, 90, 10);     // Thân lông vũ cam cháy
            Color goldFlame = new Color(255, 215, 20);     // Đầu và chóp cánh hoàng kim
            Color whiteHot = new Color(255, 255, 180);     // Lõi nhiệt độ cao nhất

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;

                    // Tâm đầu chim (16, 10)
                    float headDist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 9));
                    // Thân chim hình elip (16, 17)
                    float bodyX = (x - 16) * 1.2f;
                    float bodyY = (y - 17) * 0.8f;
                    float bodyDist = (float)Math.Sqrt(bodyX * bodyX + bodyY * bodyY);

                    // Đôi cánh dang rộng sang 2 bên (y từ 11 -> 22)
                    bool isWing = false;
                    if (y >= 11 && y <= 21)
                    {
                        // Phương trình mô phỏng đường cong cánh
                        float wingCurveL = 16 - (float)Math.Pow(y - 12, 1.3) * 1.5f;
                        float wingCurveR = 16 + (float)Math.Pow(y - 12, 1.3) * 1.5f;
                        
                        if (x >= wingCurveL && x <= 16) isWing = true;
                        if (x <= wingCurveR && x >= 16) isWing = true;
                    }

                    // 1. Vẽ thân chim
                    if (bodyDist < 6.5f)
                    {
                        c = fireOrange;
                        if (bodyDist < 4f) c = goldFlame;
                        if (bodyDist < 1.8f) c = whiteHot;
                    }
                    // 2. Vẽ cánh chim
                    else if (isWing)
                    {
                        c = crimson;
                        // Đổ bóng cánh theo khoảng cách đến mép
                        int distToCenter = Math.Abs(x - 16);
                        if (distToCenter < 10) c = fireOrange;
                        if (distToCenter < 5) c = goldFlame;

                        // Hạt lửa bập bùng ngẫu nhiên ở mép cánh
                        if (distToCenter > 11 && (x + y) % 3 == 0) c = trans;
                    }

                    // 3. Vẽ đầu chim
                    if (headDist < 3.5f)
                    {
                        c = goldFlame;
                        if (headDist < 2.0f) c = whiteHot;
                        // Mắt thần màu đỏ rực
                        if (y == 9 && (x == 15 || x == 17)) c = Color.Red;
                    }

                    // 4. Vẽ đuôi phượng dài chảy xuống (y từ 23 -> 30)
                    if (y >= 23 && y <= 30 && Math.Abs(x - 16) <= (30 - y) / 1.5f)
                    {
                        c = fireOrange;
                        if (y % 2 == 0) c = goldFlame;
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Thiên Băng Tàm (Băng - 32x32):
        /// Kén tằm băng tinh khiết, các đốt cơ thể phát sáng xanh ngọc bích,
        /// bao quanh bởi một lớp gai băng góc cạnh sắc nhọn phản quang tuyết trắng.
        /// </summary>
        public static Texture2D CreateMonsterIceTexture(GraphicsDevice gd)
        {
            int w = 32, h = 32;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            // Palette màu Thiên Băng Tàm
            Color glacierBlue = new Color(110, 220, 255);    // Xanh băng nhạt phát sáng
            Color coreCyan = new Color(20, 160, 210);       // Màu lõi băng xanh ngọc bích
            Color frostWhite = new Color(240, 250, 255);     // Màu tuyết trắng rực rỡ
            Color deepGlacier = new Color(10, 60, 120);      // Bóng tối sâu thẳm dưới băng

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;

                    // Kén tằm nằm chéo góc nhẹ (từ trên-trái xuống dưới-phải)
                    // Phương trình xoay tọa độ để vẽ cơ thể kén tằm phân đoạn
                    float rotX = (x - 16) * 0.707f + (y - 16) * 0.707f;
                    float rotY = -(x - 16) * 0.707f + (y - 16) * 0.707f;

                    // Thân kén tằm chính: rotX là trục dài, rotY là trục rộng
                    if (rotX >= -11f && rotX <= 11f && Math.Abs(rotY) < 6f)
                    {
                        c = glacierBlue;
                        
                        // Vân phân đoạn cơ thể (đốt tằm)
                        int segment = (int)((rotX + 11) / 3.5f);
                        if ((int)(rotX + 11) % 4 == 0)
                        {
                            c = coreCyan; // Đường phân đốt đậm màu
                        }
                        else if (rotY < -2f)
                        {
                            c = frostWhite; // Ánh sáng tuyết rọi trên lưng tằm
                        }
                        else if (rotY > 2f)
                        {
                            c = deepGlacier; // Bóng đổ dưới bụng tằm
                        }
                    }

                    // Gai băng nhọn đâm ra ngoài cơ thể (để trông giống quái tinh thể băng)
                    // Thiết lập tọa độ gai băng cố định
                    bool isSpike = false;
                    // Gai góc trên trái
                    if (x + y >= 10 && x + y <= 13 && x <= 10 && y <= 10 && (x - y) % 2 == 0) isSpike = true;
                    // Gai góc dưới phải
                    if (x + y >= 51 && x + y <= 54 && x >= 22 && y >= 22 && (x - y) % 2 == 0) isSpike = true;
                    // Gai hông góc trên phải
                    if (y - x >= 12 && y >= 20 && x <= 10) isSpike = true;
                    // Gai hông góc dưới trái
                    if (x - y >= 12 && x >= 20 && y <= 10) isSpike = true;

                    if (isSpike)
                    {
                        c = frostWhite;
                        if ((x + y) % 3 == 0) c = glacierBlue;
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Mũi tên Vô Thanh Tụ Tiễn (8x8):
        /// Kim tiễn thon gọn bằng hợp kim bạc xám, đầu tẩm độc xanh tím sẫm (Lam Ngân Độc)
        /// và phần đuôi phát ra hiệu ứng khí độc mờ bay phía sau.
        /// </summary>
        public static Texture2D CreateProjectileNeedleTexture(GraphicsDevice gd)
        {
            int w = 8, h = 8;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;
            Color silver = new Color(210, 220, 230);        // Thân kim ánh bạc
            Color silverDark = new Color(130, 140, 150);    // Bóng kim loại
            Color poisonPurple = new Color(160, 40, 200);   // Độc tố Lam Ngân tím sẫm
            Color poisonGreen = new Color(30, 210, 120);    // Ánh huỳnh quang của độc

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;

                    // Đường chéo chính là thân kim tiễn
                    if (x == y)
                    {
                        c = silver;
                        if (x >= 5) c = poisonPurple;   // Đầu tẩm độc tím
                        if (x == 7) c = poisonGreen;    // Mũi nhọn độc xanh cực độ
                    }
                    // Viền đổ bóng dưới mũi kim
                    else if (x == y - 1)
                    {
                        c = silverDark;
                        if (x >= 5) c = poisonPurple;
                    }
                    // Khói độc mờ ở đuôi kim (gốc trên trái)
                    else if (x == 0 && y == 1)
                    {
                        c = poisonGreen * 0.4f;
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Mũi tiễn Chư Cát Thần Nỗ (8x8):
        /// Mũi tiễn sắt đen nặng, đầu thép đục giáp và đuôi nỏ bùng nổ tia lửa màu cam rực do lực cơ quan.
        /// </summary>
        public static Texture2D CreateProjectileBoltTexture(GraphicsDevice gd)
        {
            int w = 8, h = 8;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;
            Color ironBlack = new Color(45, 45, 55);       // Sắt đen huyền thiết
            Color steelBright = new Color(200, 200, 215);   // Thép đục phá ánh sáng
            Color brassGold = new Color(220, 170, 30);      // Khớp cơ quan đồng vàng
            Color sparkOrange = new Color(255, 100, 10);    // Tia lửa cơ quan đỏ cam

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;

                    // Nỏ bay ngang (trục Y = 3 hoặc Y = 4)
                    if (y == 3 || y == 4)
                    {
                        c = ironBlack;
                        
                        // Đầu tiễn bằng thép sắc nhọn (x từ 6 -> 7)
                        if (x >= 6) c = steelBright;
                        
                        // Đai nỏ bằng đồng ở giữa (x = 3)
                        if (x == 3) c = brassGold;

                        // Đuôi nỏ phun lửa trợ lực (x từ 0 -> 1)
                        if (x <= 1) c = sparkOrange;
                    }
                    // Tia lửa xẹt văng ra xung quanh ở đuôi
                    else if (x == 1 && (y == 2 || y == 5))
                    {
                        c = sparkOrange * 0.7f;
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Đan Dược (16x16):
        /// Viên đan tròn màu ngọc bích đổ bóng 3D, có đan văn kim sắc uốn lượn
        /// và quầng linh quang mờ bao quanh.
        /// </summary>
        public static Texture2D CreatePillTexture(GraphicsDevice gd)
        {
            int w = 16, h = 16;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            Color jade = new Color(110, 225, 165);           // Thân đan ngọc bích
            Color jadeShadow = new Color(35, 135, 95);       // Bóng khối tròn
            Color highlight = new Color(235, 255, 245);      // Vết sáng phản quang
            Color pillVein = new Color(255, 210, 80);        // Đan văn kim sắc
            Color aura = new Color(130, 255, 195, 90);       // Linh quang mờ

            Vector2 center = new Vector2(7.5f, 7.5f);
            Vector2 shine = new Vector2(5.5f, 5.5f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);

                    if (dist <= 5.2f)
                    {
                        c = jade;
                        if (x + y > 17) c = jadeShadow;

                        // Đan văn uốn lượn ngang thân đan
                        float veinY = 7.5f + (float)Math.Sin(x * 0.9f) * 1.2f;
                        if (Math.Abs(y - veinY) < 0.5f && dist < 4.5f) c = pillVein;

                        if (Vector2.Distance(p, shine) <= 1.6f) c = highlight;
                    }
                    else if (dist <= 7.2f && (x * y) % 3 == 0)
                    {
                        c = aura;
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Yêu Đan (16x16):
        /// Tinh thể hình thoi màu huyết tử, lõi xoáy yêu khí sáng rực và viền hắc quang.
        /// </summary>
        public static Texture2D CreateBeastCoreTexture(GraphicsDevice gd)
        {
            int w = 16, h = 16;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            Color crystal = new Color(170, 45, 120);         // Thân tinh thể huyết tử
            Color crystalDark = new Color(90, 15, 60);       // Mặt khuất sáng
            Color coreGlow = new Color(255, 140, 200);       // Lõi yêu khí
            Color outline = new Color(40, 5, 30, 220);       // Viền hắc quang

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;
                    float manhattan = Math.Abs(x - 7.5f) + Math.Abs(y - 7.5f);

                    if (manhattan <= 6f)
                    {
                        c = crystal;
                        if (x > 7 && y > 7) c = crystalDark;
                        if (manhattan <= 2.5f) c = coreGlow;
                        if (manhattan > 5.2f) c = outline;
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Nguyên Liệu (16x16):
        /// Khối khoáng thạch trắng xám nhiều mặt cắt, được tô màu (tint) khi vẽ
        /// để phân biệt Linh Thạch, Hàn Thiết, Hỏa Tinh Thạch, Linh Mộc Tâm.
        /// </summary>
        public static Texture2D CreateMaterialTexture(GraphicsDevice gd)
        {
            int w = 16, h = 16;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            Color face = new Color(245, 245, 250);
            Color faceMid = new Color(190, 190, 200);
            Color faceDark = new Color(120, 120, 135);
            Color edge = new Color(60, 60, 70);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;

                    // Khối lục giác dẹt
                    bool inside = y >= 3 && y <= 13 && x >= 2 && x <= 13 &&
                                  Math.Abs(x - 7.5f) + Math.Abs(y - 8f) * 0.8f <= 7.5f;
                    if (inside)
                    {
                        c = faceMid;
                        if (x < 7 && y < 8) c = face;
                        if (x > 8 && y > 8) c = faceDark;
                        if (Math.Abs(x - 7.5f) + Math.Abs(y - 8f) * 0.8f > 6.6f) c = edge;
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Vòng Linh Quang (64x64):
        /// Vòng tròn phát sáng có hiệu ứng chuyển màu mềm mại từ ngoài vào trong (soft gradient glow),
        /// xuất hiện các ký tự runic ẩn hiện và các hạt bụi năng lượng lấp lánh xoay quanh.
        /// </summary>
        public static Texture2D CreateRingTexture(GraphicsDevice gd, int radius)
        {
            int size = radius * 2;
            Color[] pixels = new Color[size * size];
            Color trans = Color.Transparent;

            float rSquared = radius * radius;
            // Độ dày vòng chính
            float innerSquared = (radius - 2.8f) * (radius - 2.8f); 
            // Vùng phát sáng mờ bên ngoài
            float outerGlowSq = (radius + 2.5f) * (radius + 2.5f);
            // Vùng phát sáng mờ bên trong
            float innerGlowSq = (radius - 5.5f) * (radius - 5.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    float distSq = dx * dx + dy * dy;
                    Color c = trans;

                    // 1. Vòng linh quang lõi chính (Rất đặc)
                    if (distSq <= rSquared && distSq >= innerSquared)
                    {
                        // Tạo hoa văn runic đứt quãng tự nhiên (bản chất là các khe hở)
                        float angle = (float)Math.Atan2(dy, dx);
                        float deg = angle * (180f / (float)Math.PI);
                        if (deg < 0) deg += 360f;

                        // Cứ mỗi 45 độ, tạo một vạch khắc runic (sáng hơn hoặc trống)
                        if ((int)deg % 45 <= 4)
                        {
                            c = new Color(255, 255, 255, 255); // Runic sáng trắng
                        }
                        else
                        {
                            c = new Color(230, 230, 230, 230); // Màu cơ bản (nhân tint màu ngoài)
                        }
                    }
                    // 2. Hào quang phát sáng mềm mại bên ngoài (Soft Outer Glow)
                    else if (distSq > rSquared && distSq <= outerGlowSq)
                    {
                        float ratio = (outerGlowSq - distSq) / (outerGlowSq - rSquared);
                        c = new Color(200, 200, 200, (int)(160 * ratio));
                    }
                    // 3. Hào quang phát sáng mềm mại bên trong (Soft Inner Glow)
                    else if (distSq < innerSquared && distSq >= innerGlowSq)
                    {
                        float ratio = (distSq - innerGlowSq) / (innerSquared - innerGlowSq);
                        c = new Color(200, 200, 200, (int)(120 * ratio));
                    }
                    // 4. Các hạt năng lượng lấp lánh bay ngẫu nhiên rìa hào quang (Orbital Particles)
                    if (c == trans && distSq >= (radius - 8) * (radius - 8) && distSq <= (radius + 6) * (radius + 6))
                    {
                        if ((x * 17 + y * 23) % 113 == 0)
                        {
                            c = new Color(255, 255, 255, 180); // Hạt bụi trắng lấp lánh
                        }
                    }

                    pixels[y * size + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, size, size);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Trận Kỳ (24x24):
        /// Cán cờ gỗ cắm trên đế đá, lá cờ đỏ viền kim tuyến in trận văn đen,
        /// điểm linh quang lam sắc lấp lánh quanh cờ.
        /// </summary>
        public static Texture2D CreateFormationFlagTexture(GraphicsDevice gd)
        {
            int w = 24, h = 24;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            Color pole = new Color(125, 80, 40);           // Cán cờ gỗ
            Color stone = new Color(95, 100, 110);         // Đế đá
            Color stoneDark = new Color(60, 62, 70);       // Bóng đế đá
            Color banner = new Color(190, 35, 40);         // Lá cờ đỏ
            Color bannerGold = new Color(235, 190, 60);    // Viền kim tuyến
            Color rune = new Color(25, 15, 20);            // Trận văn
            Color spark = new Color(120, 210, 255, 200);   // Linh quang

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;

                    // 1. Cán cờ
                    if (x >= 6 && x <= 7 && y >= 2 && y <= 21) c = pole;

                    // 2. Lá cờ (x 8 -> 19, y 3 -> 13), mép dưới hình đuôi nheo
                    bool inBanner = x >= 8 && x <= 19 && y >= 3 && y <= 13 &&
                                    !(y == 13 && x >= 12 && x <= 15);
                    if (inBanner)
                    {
                        c = banner;
                        if (x == 8 || x == 19 || y == 3 || y == 13) c = bannerGold;

                        // Trận văn hình chữ thập kèm vòng
                        bool cross = (x == 13 || x == 14) && y >= 5 && y <= 11 ||
                                     (y == 8) && x >= 10 && x <= 17;
                        if (cross) c = rune;
                    }

                    // 3. Đế đá
                    if (y >= 21 && y <= 23 && x >= 3 && x <= 10)
                    {
                        c = y == 23 ? stoneDark : stone;
                    }

                    // 4. Linh quang lấp lánh
                    if (c == trans && (x * 7 + y * 11) % 29 == 0 && y < 20) c = spark;

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Đan Sư (32x32):
        /// Lão giả tóc bạc râu dài, đạo bào tím đai vàng, bên hông đeo hồ lô đan dược.
        /// </summary>
        public static Texture2D CreateAlchemistTexture(GraphicsDevice gd)
        {
            int w = 32, h = 32;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            Color skin = new Color(235, 195, 160);
            Color hair = new Color(230, 230, 235);         // Tóc/râu bạc
            Color robe = new Color(95, 60, 140);           // Đạo bào tím
            Color robeDark = new Color(60, 35, 95);
            Color sash = new Color(230, 185, 60);          // Đai vàng
            Color gourd = new Color(215, 120, 40);         // Hồ lô
            Color eye = new Color(30, 25, 30);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;
                    Vector2 p = new Vector2(x, y);

                    // 1. Búi tóc và đầu
                    if (Vector2.Distance(p, new Vector2(16, 3)) <= 2f) c = hair;
                    if (Vector2.Distance(p, new Vector2(16, 8)) <= 4f)
                    {
                        c = skin;
                        if (y <= 5) c = hair;
                        if (y == 8 && (x == 14 || x == 18)) c = eye;
                    }

                    // 2. Râu dài
                    if (y >= 10 && y <= 16 && Math.Abs(x - 16) <= 3 - (y - 10) / 3) c = hair;

                    // 3. Đạo bào hình thang
                    int halfWidth = 4 + (y - 12) / 2;
                    if (y >= 12 && y <= 30 && Math.Abs(x - 16) <= halfWidth && c == trans)
                    {
                        c = x < 16 ? robe : robeDark;
                        if (y == 19) c = sash;
                    }

                    // 4. Hồ lô bên hông
                    if (Vector2.Distance(p, new Vector2(24, 22)) <= 2.2f ||
                        Vector2.Distance(p, new Vector2(24, 19)) <= 1.2f)
                    {
                        c = gourd;
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }

        /// <summary>
        /// Lò Luyện Khí (32x32):
        /// Đỉnh lô ba chân bằng đồng xanh, nắp có núm, lỗ thông khí và lửa đỏ cháy dưới đáy.
        /// </summary>
        public static Texture2D CreateFurnaceTexture(GraphicsDevice gd)
        {
            int w = 32, h = 32;
            Color[] pixels = new Color[w * h];
            Color trans = Color.Transparent;

            Color bronze = new Color(170, 125, 60);
            Color bronzeDark = new Color(105, 70, 30);
            Color patina = new Color(90, 150, 125);        // Rỉ đồng xanh
            Color fire = new Color(255, 120, 30);
            Color fireCore = new Color(255, 225, 90);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = trans;

                    // 1. Lửa dưới đáy lô
                    if (y >= 25 && y <= 29 && x >= 12 && x <= 20)
                    {
                        c = (x + y) % 3 == 0 ? fireCore : fire;
                    }

                    // 2. Ba chân lô
                    if (y >= 23 && y <= 30 && (x == 8 || x == 9 || x == 15 || x == 16 || x == 22 || x == 23))
                    {
                        c = bronzeDark;
                    }

                    // 3. Thân lô hình elip
                    float ex = (x - 15.5f) / 11f;
                    float ey = (y - 17f) / 7f;
                    if (ex * ex + ey * ey <= 1f)
                    {
                        c = bronze;
                        if (y >= 20) c = bronzeDark;
                        if (y == 15 && x % 4 == 0) c = patina;           // Hoa văn rỉ đồng
                        if (y == 18 && (x == 11 || x == 15 || x == 19)) c = fireCore; // Lỗ thông khí
                    }

                    // 4. Nắp lô và núm
                    if (y >= 8 && y <= 10 && x >= 10 && x <= 21) c = bronzeDark;
                    if (y >= 5 && y <= 7 && x >= 14 && x <= 17) c = patina;

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(gd, w, h);
            texture.SetData(pixels);
            return texture;
        }
    }
}


