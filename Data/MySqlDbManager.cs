using System;
using System.Collections.Generic;
using System.Text.Json;
using MySqlConnector;
using SandboxTuTien.Components;
using SandboxTuTien.Data.Models;
using SandboxTuTien.Entities;

namespace SandboxTuTien.Data
{
    public class MySqlDbManager
    {
        /// <summary>
        /// Tên CSDL. Bản tu tiên dùng CSDL mới, không đọc save của bản Đấu La cũ (sandboxtutien).
        /// </summary>
        private const string DATABASE_NAME = "sandboxtutien_v2";

        // Cấu hình kết nối MySQL mặc định
        private readonly string _connectionStringWithoutDb = "Server=localhost;User ID=root;Password=123456;Port=3306;AllowUserVariables=True;UseAffectedRows=True;";
        private readonly string _connectionString = $"Server=localhost;Database={DATABASE_NAME};User ID=root;Password=123456;Port=3306;AllowUserVariables=True;UseAffectedRows=True;";
        private readonly JsonSerializerOptions _jsonOptions;

        public bool IsConnected { get; private set; }

        public MySqlDbManager()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Khởi tạo Cơ sở dữ liệu và các bảng cần thiết.
        /// Trả về true nếu kết nối và khởi tạo thành công, false nếu MySQL bị offline.
        /// </summary>
        public bool InitializeDatabase()
        {
            try
            {
                // 1. Thử kết nối và tạo database nếu chưa có
                using (var conn = new MySqlConnection(_connectionStringWithoutDb))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS {DATABASE_NAME} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                        cmd.ExecuteNonQuery();
                    }
                }

                // 2. Kết nối vào DB và thiết lập các bảng
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        // Bảng Đan Dược / Vật Liệu
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS consumables (
                                item_id VARCHAR(50) PRIMARY KEY,
                                name VARCHAR(100) NOT NULL,
                                type VARCHAR(50) NOT NULL,
                                source VARCHAR(50),
                                tier_required INT DEFAULT 0,
                                effects_json TEXT,
                                crafting_recipe_json TEXT,
                                spoilage_time INT DEFAULT -1
                            ) ENGINE=InnoDB;";
                        cmd.ExecuteNonQuery();

                        // Bảng Pháp Khí
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS magic_weapons (
                                item_id VARCHAR(50) PRIMARY KEY,
                                name VARCHAR(100) NOT NULL,
                                type VARCHAR(50) NOT NULL,
                                element VARCHAR(20) DEFAULT 'None',
                                tier_required INT DEFAULT 0,
                                combat_stats_json TEXT,
                                effects_json TEXT,
                                crafting_recipe_json TEXT
                            ) ENGINE=InnoDB;";
                        cmd.ExecuteNonQuery();

                        // Bảng lưu game (chỉ số người chơi, lưu phái, chiêu thức, túi đồ và toàn bộ thế giới)
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS player_saves (
                                save_slot VARCHAR(50) PRIMARY KEY,
                                player_name VARCHAR(100) NOT NULL,
                                class_id VARCHAR(50),
                                class_element VARCHAR(20),
                                level INT NOT NULL,
                                current_exp FLOAT NOT NULL,
                                hp FLOAT NOT NULL,
                                max_hp FLOAT NOT NULL,
                                spirit_power FLOAT NOT NULL,
                                max_spirit_power FLOAT NOT NULL,
                                equipped_weapon_id VARCHAR(50),
                                inventory_json TEXT,
                                breakthrough_count INT DEFAULT 0,
                                skill_mastery_json TEXT,
                                has_van_doc_the TINYINT DEFAULT 0,
                                heart_demon FLOAT DEFAULT 0,
                                realm VARCHAR(50),
                                monsters_json TEXT,
                                dropped_items_json TEXT,
                                formations_json TEXT,
                                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                            ) ENGINE=InnoDB;";
                        cmd.ExecuteNonQuery();

                        // Di cư an toàn cho bảng player_saves đã tồn tại từ phiên bản trước phần Lưu Phái
                        // (không xóa cột skill1_id/skill2_id cũ nếu có — chỉ ngừng đọc/ghi chúng).
                        // Dùng INFORMATION_SCHEMA thay vì "ADD COLUMN IF NOT EXISTS" vì cú pháp đó không
                        // được mọi phiên bản MySQL/MariaDB hỗ trợ.
                        foreach (var (column, definition) in new[]
                        {
                            ("class_id", "VARCHAR(50)"),
                            ("class_element", "VARCHAR(20)"),
                            ("skill_mastery_json", "TEXT")
                        })
                        {
                            cmd.CommandText = @"
                                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                                WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'player_saves' AND COLUMN_NAME = @col;";
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@db", DATABASE_NAME);
                            cmd.Parameters.AddWithValue("@col", column);
                            long exists = Convert.ToInt64(cmd.ExecuteScalar());

                            if (exists == 0)
                            {
                                cmd.Parameters.Clear();
                                cmd.CommandText = $"ALTER TABLE player_saves ADD COLUMN {column} {definition};";
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                IsConnected = true;
                Console.WriteLine("[MySQL] Khởi tạo Database và các bảng thành công!");
                return true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Console.WriteLine($"[MySQL] Không thể kết nối hoặc khởi tạo MySQL: {ex.Message}");
                Console.WriteLine("[MySQL] Game sẽ tự động chuyển sang chế độ Offline (Dùng file JSON tĩnh).");
                return false;
            }
        }

        /// <summary>
        /// Di cư dữ liệu từ JSON sang MySQL (nếu có cập nhật mới).
        /// </summary>
        public void MigrateJsonToMySql(List<ConsumableData> consumables, List<MagicWeaponData> weapons)
        {
            if (!IsConnected) return;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Đồng bộ Đan Dược / Vật Liệu
                    if (consumables != null && consumables.Count > 0)
                    {
                        Console.WriteLine("[MySQL] Bắt đầu đồng bộ dữ liệu Đan Dược từ JSON...");
                        int syncCount = 0;
                        foreach (var c in consumables)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    INSERT INTO consumables (item_id, name, type, source, tier_required, effects_json, crafting_recipe_json, spoilage_time)
                                    VALUES (@id, @name, @type, @source, @tier, @effects, @recipe, @spoilage)
                                    ON DUPLICATE KEY UPDATE
                                        name = VALUES(name),
                                        type = VALUES(type),
                                        source = VALUES(source),
                                        tier_required = VALUES(tier_required),
                                        effects_json = VALUES(effects_json),
                                        crafting_recipe_json = VALUES(crafting_recipe_json),
                                        spoilage_time = VALUES(spoilage_time);";
                                cmd.Parameters.AddWithValue("@id", c.ItemId);
                                cmd.Parameters.AddWithValue("@name", c.Name);
                                cmd.Parameters.AddWithValue("@type", c.Type);
                                cmd.Parameters.AddWithValue("@source", c.Source);
                                cmd.Parameters.AddWithValue("@tier", c.TierRequired);
                                cmd.Parameters.AddWithValue("@effects", JsonSerializer.Serialize(c.Effects, _jsonOptions));
                                cmd.Parameters.AddWithValue("@recipe", JsonSerializer.Serialize(c.CraftingRecipe, _jsonOptions));
                                cmd.Parameters.AddWithValue("@spoilage", c.SpoilageTime);
                                cmd.ExecuteNonQuery();
                                syncCount++;
                            }
                        }
                        Console.WriteLine($"[MySQL] Đã đồng bộ thành công {syncCount} Đan Dược / Vật Liệu.");
                    }

                    // 2. Đồng bộ Pháp Khí
                    if (weapons != null && weapons.Count > 0)
                    {
                        Console.WriteLine("[MySQL] Bắt đầu đồng bộ dữ liệu Pháp Khí từ JSON...");
                        int syncCount = 0;
                        foreach (var w in weapons)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    INSERT INTO magic_weapons (item_id, name, type, element, tier_required, combat_stats_json, effects_json, crafting_recipe_json)
                                    VALUES (@id, @name, @type, @element, @tier, @stats, @effects, @recipe)
                                    ON DUPLICATE KEY UPDATE
                                        name = VALUES(name),
                                        type = VALUES(type),
                                        element = VALUES(element),
                                        tier_required = VALUES(tier_required),
                                        combat_stats_json = VALUES(combat_stats_json),
                                        effects_json = VALUES(effects_json),
                                        crafting_recipe_json = VALUES(crafting_recipe_json);";
                                cmd.Parameters.AddWithValue("@id", w.ItemId);
                                cmd.Parameters.AddWithValue("@name", w.Name);
                                cmd.Parameters.AddWithValue("@type", w.Type);
                                cmd.Parameters.AddWithValue("@element", w.Element);
                                cmd.Parameters.AddWithValue("@tier", w.TierRequired);
                                cmd.Parameters.AddWithValue("@stats", w.CombatStats != null ? JsonSerializer.Serialize(w.CombatStats, _jsonOptions) : null);
                                cmd.Parameters.AddWithValue("@effects", JsonSerializer.Serialize(w.Effects, _jsonOptions));
                                cmd.Parameters.AddWithValue("@recipe", JsonSerializer.Serialize(w.CraftingRecipe, _jsonOptions));
                                cmd.ExecuteNonQuery();
                                syncCount++;
                            }
                        }
                        Console.WriteLine($"[MySQL] Đã đồng bộ thành công {syncCount} Pháp Khí.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Lỗi trong quá trình đồng bộ dữ liệu: {ex.Message}");
            }
        }

        /// <summary>
        /// Tải danh sách Đan Dược / Vật Liệu trực tiếp từ MySQL.
        /// </summary>
        public List<ConsumableData> LoadConsumablesFromDb()
        {
            var list = new List<ConsumableData>();
            if (!IsConnected) return list;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT item_id, name, type, source, tier_required, effects_json, crafting_recipe_json, spoilage_time FROM consumables;";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new ConsumableData
                                {
                                    ItemId = reader.GetString(0),
                                    Name = reader.GetString(1),
                                    Type = reader.GetString(2),
                                    Source = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    TierRequired = reader.GetInt32(4),
                                    SpoilageTime = reader.GetInt32(7)
                                };

                                string effectsJson = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                                if (!string.IsNullOrEmpty(effectsJson))
                                {
                                    item.Effects = JsonSerializer.Deserialize<List<ItemEffect>>(effectsJson, _jsonOptions) ?? new();
                                }

                                string recipeJson = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
                                if (!string.IsNullOrEmpty(recipeJson))
                                {
                                    item.CraftingRecipe = JsonSerializer.Deserialize<List<CraftingIngredient>>(recipeJson, _jsonOptions) ?? new();
                                }

                                list.Add(item);
                            }
                        }
                    }
                }
                Console.WriteLine($"[MySQL] Đã nạp {list.Count} Đan Dược / Vật Liệu từ MySQL DB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Lỗi nạp Đan Dược: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Tải danh sách Pháp Khí trực tiếp từ MySQL.
        /// </summary>
        public List<MagicWeaponData> LoadMagicWeaponsFromDb()
        {
            var list = new List<MagicWeaponData>();
            if (!IsConnected) return list;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT item_id, name, type, element, tier_required, combat_stats_json, effects_json, crafting_recipe_json FROM magic_weapons;";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new MagicWeaponData
                                {
                                    ItemId = reader.GetString(0),
                                    Name = reader.GetString(1),
                                    Type = reader.GetString(2),
                                    Element = reader.IsDBNull(3) ? "None" : reader.GetString(3),
                                    TierRequired = reader.GetInt32(4)
                                };

                                string statsJson = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                                if (!string.IsNullOrEmpty(statsJson))
                                {
                                    item.CombatStats = JsonSerializer.Deserialize<CombatStats>(statsJson, _jsonOptions);
                                }

                                string effectsJson = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
                                if (!string.IsNullOrEmpty(effectsJson))
                                {
                                    item.Effects = JsonSerializer.Deserialize<List<ItemEffect>>(effectsJson, _jsonOptions) ?? new();
                                }

                                string recipeJson = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
                                if (!string.IsNullOrEmpty(recipeJson))
                                {
                                    item.CraftingRecipe = JsonSerializer.Deserialize<List<CraftingIngredient>>(recipeJson, _jsonOptions) ?? new();
                                }

                                list.Add(item);
                            }
                        }
                    }
                }
                Console.WriteLine($"[MySQL] Đã nạp {list.Count} Pháp Khí từ MySQL DB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Lỗi nạp Pháp Khí: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Lưu trạng thái người chơi và toàn bộ thế giới vào MySQL.
        /// </summary>
        public bool SavePlayerState(
            Player player,
            SkillLoadoutComponent loadout,
            List<Monster> monsters,
            List<DroppedItem> droppedItems,
            List<FormationArray> formations,
            string saveSlot = "slot_default")
        {
            if (!IsConnected) return false;

            try
            {
                var cult = player.Cultivation;
                string inventoryJson = player.Inventory.Serialize();

                // 1. Tuần tự hóa danh sách Yêu Thú
                var monstersData = new List<object>();
                foreach (var m in monsters)
                {
                    if (m.Active)
                    {
                        monstersData.Add(new
                        {
                            name = m.BaseName,
                            age = m.Age,
                            hp = m.HP,
                            maxHp = m.MaxHP,
                            x = m.Position.X,
                            y = m.Position.Y,
                            element = m.Element.ToString()
                        });
                    }
                }
                string monstersJson = JsonSerializer.Serialize(monstersData);

                // 2. Tuần tự hóa danh sách vật phẩm rơi
                var droppedItemsData = new List<object>();
                foreach (var item in droppedItems)
                {
                    if (item.Active)
                    {
                        droppedItemsData.Add(new
                        {
                            itemId = item.ItemId,
                            name = item.Name,
                            type = item.Type,
                            quantity = item.Quantity,
                            x = item.Position.X,
                            y = item.Position.Y
                        });
                    }
                }
                string droppedItemsJson = JsonSerializer.Serialize(droppedItemsData);

                // 3. Tuần tự hóa danh sách trận pháp
                var formationsData = new List<object>();
                foreach (var f in formations)
                {
                    if (f.Active)
                    {
                        formationsData.Add(new
                        {
                            type = f.FormationType,
                            ammo = f.AmmoCount,
                            maxAmmo = f.MaxAmmo,
                            x = f.Position.X,
                            y = f.Position.Y
                        });
                    }
                }
                string formationsJson = JsonSerializer.Serialize(formationsData);

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO player_saves (save_slot, player_name, class_id, class_element, level, current_exp, hp, max_hp, spirit_power, max_spirit_power, equipped_weapon_id, inventory_json, breakthrough_count, skill_mastery_json, has_van_doc_the, heart_demon, realm, monsters_json, dropped_items_json, formations_json)
                            VALUES (@slot, @name, @classId, @classElement, @level, @exp, @hp, @maxHp, @sp, @maxSp, @weapon, @inventory, @breakthroughs, @mastery, @vanDocThe, @heartDemon, @realm, @monsters, @droppedItems, @formations)
                            ON DUPLICATE KEY UPDATE
                                player_name = VALUES(player_name),
                                class_id = VALUES(class_id),
                                class_element = VALUES(class_element),
                                level = VALUES(level),
                                current_exp = VALUES(current_exp),
                                hp = VALUES(hp),
                                max_hp = VALUES(max_hp),
                                spirit_power = VALUES(spirit_power),
                                max_spirit_power = VALUES(max_spirit_power),
                                equipped_weapon_id = VALUES(equipped_weapon_id),
                                inventory_json = VALUES(inventory_json),
                                breakthrough_count = VALUES(breakthrough_count),
                                skill_mastery_json = VALUES(skill_mastery_json),
                                has_van_doc_the = VALUES(has_van_doc_the),
                                heart_demon = VALUES(heart_demon),
                                realm = VALUES(realm),
                                monsters_json = VALUES(monsters_json),
                                dropped_items_json = VALUES(dropped_items_json),
                                formations_json = VALUES(formations_json);";

                        cmd.Parameters.AddWithValue("@slot", saveSlot);
                        cmd.Parameters.AddWithValue("@name", player.Name);
                        cmd.Parameters.AddWithValue("@classId", loadout.PlayerClass.Id);
                        cmd.Parameters.AddWithValue("@classElement", loadout.ChosenElement.ToString());
                        cmd.Parameters.AddWithValue("@level", cult.CurrentLevel);
                        cmd.Parameters.AddWithValue("@exp", cult.CurrentExp);
                        cmd.Parameters.AddWithValue("@hp", cult.HP);
                        cmd.Parameters.AddWithValue("@maxHp", cult.MaxHP);
                        cmd.Parameters.AddWithValue("@sp", cult.SpiritPower);
                        cmd.Parameters.AddWithValue("@maxSp", cult.MaxSpiritPower);
                        cmd.Parameters.AddWithValue("@weapon", player.Inventory.EquippedWeapon?.ItemId ?? string.Empty);
                        cmd.Parameters.AddWithValue("@inventory", inventoryJson);
                        cmd.Parameters.AddWithValue("@breakthroughs", cult.BreakthroughCount);
                        cmd.Parameters.AddWithValue("@mastery", loadout.SerializeMastery());
                        cmd.Parameters.AddWithValue("@vanDocThe", cult.HasVanDocThe ? 1 : 0);
                        cmd.Parameters.AddWithValue("@heartDemon", cult.HeartDemon);
                        cmd.Parameters.AddWithValue("@realm", cult.CurrentRealm.ToString());
                        cmd.Parameters.AddWithValue("@monsters", monstersJson);
                        cmd.Parameters.AddWithValue("@droppedItems", droppedItemsJson);
                        cmd.Parameters.AddWithValue("@formations", formationsJson);

                        cmd.ExecuteNonQuery();
                    }
                }
                Console.WriteLine($"[MySQL] Đã lưu trạng thái game & thế giới thành công (Slot: {saveSlot})!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Lỗi lưu trạng thái game: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tải trạng thái người chơi và thế giới từ MySQL.
        /// </summary>
        public bool LoadPlayerState(
            Player player,
            SkillLoadoutComponent loadout,
            DataLoader loader,
            out string monstersJson,
            out string droppedItemsJson,
            out string formationsJson,
            string saveSlot = "slot_default")
        {
            monstersJson = string.Empty;
            droppedItemsJson = string.Empty;
            formationsJson = string.Empty;

            if (!IsConnected) return false;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT player_name, class_id, class_element, level, current_exp, hp, max_hp, spirit_power, max_spirit_power, equipped_weapon_id, inventory_json, breakthrough_count, skill_mastery_json, has_van_doc_the, heart_demon, realm, monsters_json, dropped_items_json, formations_json
                            FROM player_saves
                            WHERE save_slot = @slot;";
                        cmd.Parameters.AddWithValue("@slot", saveSlot);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string name = reader.GetString(0);
                                string savedClassId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                                int level = reader.GetInt32(3);
                                float exp = reader.GetFloat(4);
                                float hp = reader.GetFloat(5);
                                float maxHp = reader.GetFloat(6);
                                float sp = reader.GetFloat(7);
                                float maxSp = reader.GetFloat(8);
                                string weaponId = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
                                string inventoryJson = reader.IsDBNull(10) ? string.Empty : reader.GetString(10);
                                int breakthroughCount = reader.GetInt32(11);
                                string masteryJson = reader.IsDBNull(12) ? string.Empty : reader.GetString(12);
                                bool hasVanDocThe = reader.GetByte(13) == 1;
                                float heartDemon = reader.IsDBNull(14) ? 0f : reader.GetFloat(14);
                                string realmStr = reader.IsDBNull(15) ? string.Empty : reader.GetString(15);

                                monstersJson = reader.IsDBNull(16) ? string.Empty : reader.GetString(16);
                                droppedItemsJson = reader.IsDBNull(17) ? string.Empty : reader.GetString(17);
                                formationsJson = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);

                                // Nếu bản lưu thuộc lưu phái khác lưu phái đang chơi (F9 chỉ nạp lại trong cùng
                                // phiên chơi, chưa hỗ trợ "tiếp tục" từ màn hình chọn lưu phái), chỉ cảnh báo.
                                if (!string.IsNullOrEmpty(savedClassId) && savedClassId != loadout.PlayerClass.Id)
                                {
                                    Console.WriteLine($"[MySQL] CẢNH BÁO: Bản lưu thuộc lưu phái '{savedClassId}', " +
                                                      $"khác lưu phái đang chơi '{loadout.PlayerClass.Id}'. Vẫn nạp chỉ số/thông thạo.");
                                }

                                // Phục hồi tu vi
                                player.Cultivation.LoadState(level, exp, hp, maxHp, sp, maxSp, breakthroughCount,
                                                             hasVanDocThe, heartDemon, realmStr);

                                // Phục hồi độ thông thạo chiêu thức
                                loadout.LoadMastery(masteryJson);

                                // Phục hồi túi đồ
                                if (!string.IsNullOrEmpty(inventoryJson))
                                {
                                    player.Inventory.Deserialize(inventoryJson, loader);
                                }

                                // Phục hồi Pháp Khí đang trang bị
                                if (!string.IsNullOrEmpty(weaponId))
                                {
                                    player.Inventory.EquipWeapon(weaponId);
                                }

                                Console.WriteLine($"[MySQL] Tải trạng thái của '{name}' thành công từ MySQL!");
                                return true;
                            }
                            else
                            {
                                Console.WriteLine($"[MySQL] Không tìm thấy bản lưu game nào ở slot: {saveSlot}.");
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Lỗi khi nạp dữ liệu người chơi: {ex.Message}");
                return false;
            }
        }
    }
}
