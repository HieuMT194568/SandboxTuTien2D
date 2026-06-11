using System;
using System.Collections.Generic;
using System.Text.Json;
using MySqlConnector;
using SandboxTuTien.Data.Models;
using SandboxTuTien.Entities;
using SandboxTuTien.Components;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Data
{
    public class MySqlDbManager
    {
        // Cấu hình kết nối MySQL mặc định
        private readonly string _connectionStringWithoutDb = "Server=localhost;User ID=root;Password=;Port=3306;AllowUserVariables=True;UseAffectedRows=True;";
        private readonly string _connectionString = "Server=localhost;Database=sandboxtutien;User ID=root;Password=;Port=3306;AllowUserVariables=True;UseAffectedRows=True;";
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
                        cmd.CommandText = "CREATE DATABASE IF NOT EXISTS sandboxtutien CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                        cmd.ExecuteNonQuery();
                    }
                }

                // 2. Kết nối vào DB mới tạo và thiết lập các bảng
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        // Bảng Consumables (Vật phẩm tiêu thụ / Dược phẩm)
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS consumables (
                                item_id VARCHAR(50) PRIMARY KEY,
                                name VARCHAR(100) NOT NULL,
                                type VARCHAR(50) NOT NULL,
                                source VARCHAR(50),
                                tier_required INT DEFAULT 0,
                                effects_json TEXT,
                                spoilage_time INT DEFAULT -1
                            ) ENGINE=InnoDB;";
                        cmd.ExecuteNonQuery();

                        // Bảng Hidden Weapons (Ám khí Đường Môn)
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS hidden_weapons (
                                item_id VARCHAR(50) PRIMARY KEY,
                                name VARCHAR(100) NOT NULL,
                                type VARCHAR(50) NOT NULL,
                                tier_required INT DEFAULT 0,
                                combat_stats_json TEXT,
                                effects_json TEXT,
                                crafting_recipe_json TEXT
                            ) ENGINE=InnoDB;";
                        cmd.ExecuteNonQuery();

                        // Bảng Player Saves (Lưu trữ chỉ số, túi đồ người chơi và toàn bộ thế giới)
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS player_saves (
                                save_slot VARCHAR(50) PRIMARY KEY,
                                player_name VARCHAR(100) NOT NULL,
                                level INT NOT NULL,
                                current_exp FLOAT NOT NULL,
                                hp FLOAT NOT NULL,
                                max_hp FLOAT NOT NULL,
                                soul_power FLOAT NOT NULL,
                                max_soul_power FLOAT NOT NULL,
                                equipped_weapon_id VARCHAR(50),
                                inventory_json TEXT,
                                soul_rings_count INT DEFAULT 0,
                                skill1_name VARCHAR(100),
                                skill2_name VARCHAR(100),
                                has_bat_chu_mau TINYINT DEFAULT 0,
                                realm VARCHAR(50),
                                monsters_json TEXT,
                                dropped_items_json TEXT,
                                launchers_json TEXT,
                                soul_rings_json TEXT,
                                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                            ) ENGINE=InnoDB;";
                        cmd.ExecuteNonQuery();
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
        /// Di cư dữ liệu từ JSON sang MySQL nếu bảng dữ liệu rỗng.
        /// </summary>
        public void MigrateJsonToMySql(List<ConsumableData> consumables, List<HiddenWeaponData> weapons)
        {
            if (!IsConnected) return;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Kiểm tra và di cư Consumables
                    bool consumablesEmpty = false;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM consumables;";
                        consumablesEmpty = Convert.ToInt32(cmd.ExecuteScalar()) == 0;
                    }

                    if (consumablesEmpty && consumables != null && consumables.Count > 0)
                    {
                        Console.WriteLine("[MySQL] Bảng 'consumables' rỗng. Bắt đầu di cư dữ liệu từ JSON...");
                        foreach (var c in consumables)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    INSERT INTO consumables (item_id, name, type, source, tier_required, effects_json, spoilage_time)
                                    VALUES (@id, @name, @type, @source, @tier, @effects, @spoilage);";
                                cmd.Parameters.AddWithValue("@id", c.ItemId);
                                cmd.Parameters.AddWithValue("@name", c.Name);
                                cmd.Parameters.AddWithValue("@type", c.Type);
                                cmd.Parameters.AddWithValue("@source", c.Source);
                                cmd.Parameters.AddWithValue("@tier", c.TierRequired);
                                cmd.Parameters.AddWithValue("@effects", JsonSerializer.Serialize(c.Effects, _jsonOptions));
                                cmd.Parameters.AddWithValue("@spoilage", c.SpoilageTime);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        Console.WriteLine($"[MySQL] Đã di cư thành công {consumables.Count} vật phẩm tiêu thụ vào MySQL.");
                    }

                    // 2. Kiểm tra và di cư Hidden Weapons
                    bool weaponsEmpty = false;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM hidden_weapons;";
                        weaponsEmpty = Convert.ToInt32(cmd.ExecuteScalar()) == 0;
                    }

                    if (weaponsEmpty && weapons != null && weapons.Count > 0)
                    {
                        Console.WriteLine("[MySQL] Bảng 'hidden_weapons' rỗng. Bắt đầu di cư dữ liệu từ JSON...");
                        foreach (var w in weapons)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    INSERT INTO hidden_weapons (item_id, name, type, tier_required, combat_stats_json, effects_json, crafting_recipe_json)
                                    VALUES (@id, @name, @type, @tier, @stats, @effects, @recipe);";
                                cmd.Parameters.AddWithValue("@id", w.ItemId);
                                cmd.Parameters.AddWithValue("@name", w.Name);
                                cmd.Parameters.AddWithValue("@type", w.Type);
                                cmd.Parameters.AddWithValue("@tier", w.TierRequired);
                                cmd.Parameters.AddWithValue("@stats", w.CombatStats != null ? JsonSerializer.Serialize(w.CombatStats, _jsonOptions) : null);
                                cmd.Parameters.AddWithValue("@effects", JsonSerializer.Serialize(w.Effects, _jsonOptions));
                                cmd.Parameters.AddWithValue("@recipe", JsonSerializer.Serialize(w.CraftingRecipe, _jsonOptions));
                                cmd.ExecuteNonQuery();
                            }
                        }
                        Console.WriteLine($"[MySQL] Đã di cư thành công {weapons.Count} ám khí vào MySQL.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Lỗi trong quá trình di cư dữ liệu: {ex.Message}");
            }
        }

        /// <summary>
        /// Tải danh sách Consumables trực tiếp từ MySQL.
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
                        cmd.CommandText = "SELECT item_id, name, type, source, tier_required, effects_json, spoilage_time FROM consumables;";
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
                                    SpoilageTime = reader.GetInt32(6)
                                };

                                string effectsJson = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                                if (!string.IsNullOrEmpty(effectsJson))
                                {
                                    item.Effects = JsonSerializer.Deserialize<List<ItemEffect>>(effectsJson, _jsonOptions) ?? new();
                                }

                                list.Add(item);
                            }
                        }
                    }
                }
                Console.WriteLine($"[MySQL] Đã nạp {list.Count} Consumables từ MySQL DB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Lỗi nạp Consumables: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Tải danh sách Hidden Weapons trực tiếp từ MySQL.
        /// </summary>
        public List<HiddenWeaponData> LoadHiddenWeaponsFromDb()
        {
            var list = new List<HiddenWeaponData>();
            if (!IsConnected) return list;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT item_id, name, type, tier_required, combat_stats_json, effects_json, crafting_recipe_json FROM hidden_weapons;";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new HiddenWeaponData
                                {
                                    ItemId = reader.GetString(0),
                                    Name = reader.GetString(1),
                                    Type = reader.GetString(2),
                                    TierRequired = reader.GetInt32(3)
                                };

                                string statsJson = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                                if (!string.IsNullOrEmpty(statsJson))
                                {
                                    item.CombatStats = JsonSerializer.Deserialize<CombatStats>(statsJson, _jsonOptions);
                                }

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
                Console.WriteLine($"[MySQL] Đã nạp {list.Count} Hidden Weapons từ MySQL DB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Lỗi nạp Hidden Weapons: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Lưu trạng thái người chơi và toàn bộ thế giới vào MySQL.
        /// </summary>
        public bool SavePlayerState(
            Player player, 
            List<Monster> monsters,
            List<DroppedItem> droppedItems,
            List<AutoLauncher> launchers,
            List<SoulRingEntity> soulRings,
            string saveSlot = "slot_default")
        {
            if (!IsConnected) return false;

            try
            {
                var cult = player.Cultivation;
                string inventoryJson = player.Inventory.Serialize();

                // 1. Tuần tự hóa danh sách Quái vật
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

                // 3. Tuần tự hóa danh sách bệ phóng
                var launchersData = new List<object>();
                foreach (var l in launchers)
                {
                    if (l.Active)
                    {
                        launchersData.Add(new
                        {
                            type = l.TurretType,
                            ammo = l.AmmoCount,
                            maxAmmo = l.MaxAmmo,
                            x = l.Position.X,
                            y = l.Position.Y
                        });
                    }
                }
                string launchersJson = JsonSerializer.Serialize(launchersData);

                // 4. Tuần tự hóa danh sách hồn hoàn rơi dưới đất
                var soulRingsData = new List<object>();
                foreach (var r in soulRings)
                {
                    if (r.Active)
                    {
                        soulRingsData.Add(new
                        {
                            age = r.Age,
                            element = r.Element.ToString(),
                            x = r.Position.X,
                            y = r.Position.Y
                        });
                    }
                }
                string soulRingsJson = JsonSerializer.Serialize(soulRingsData);

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO player_saves (save_slot, player_name, level, current_exp, hp, max_hp, soul_power, max_soul_power, equipped_weapon_id, inventory_json, soul_rings_count, skill1_name, skill2_name, has_bat_chu_mau, realm, monsters_json, dropped_items_json, launchers_json, soul_rings_json)
                            VALUES (@slot, @name, @level, @exp, @hp, @maxHp, @sp, @maxSp, @weapon, @inventory, @ringsCount, @skill1, @skill2, @bat_chu_mau, @realm, @monsters, @droppedItems, @launchers, @soulRings)
                            ON DUPLICATE KEY UPDATE 
                                player_name = VALUES(player_name),
                                level = VALUES(level),
                                current_exp = VALUES(current_exp),
                                hp = VALUES(hp),
                                max_hp = VALUES(max_hp),
                                soul_power = VALUES(soul_power),
                                max_soul_power = VALUES(max_soul_power),
                                equipped_weapon_id = VALUES(equipped_weapon_id),
                                inventory_json = VALUES(inventory_json),
                                soul_rings_count = VALUES(soul_rings_count),
                                skill1_name = VALUES(skill1_name),
                                skill2_name = VALUES(skill2_name),
                                has_bat_chu_mau = VALUES(has_bat_chu_mau),
                                realm = VALUES(realm),
                                monsters_json = VALUES(monsters_json),
                                dropped_items_json = VALUES(dropped_items_json),
                                launchers_json = VALUES(launchers_json),
                                soul_rings_json = VALUES(soul_rings_json);";

                        cmd.Parameters.AddWithValue("@slot", saveSlot);
                        cmd.Parameters.AddWithValue("@name", player.Name);
                        cmd.Parameters.AddWithValue("@level", cult.CurrentLevel);
                        cmd.Parameters.AddWithValue("@exp", cult.CurrentExp);
                        cmd.Parameters.AddWithValue("@hp", cult.HP);
                        cmd.Parameters.AddWithValue("@maxHp", cult.MaxHP);
                        cmd.Parameters.AddWithValue("@sp", cult.SoulPower);
                        cmd.Parameters.AddWithValue("@maxSp", cult.MaxSoulPower);
                        cmd.Parameters.AddWithValue("@weapon", player.Inventory.EquippedWeapon?.ItemId ?? string.Empty);
                        cmd.Parameters.AddWithValue("@inventory", inventoryJson);
                        cmd.Parameters.AddWithValue("@ringsCount", cult.SoulRingsCount);
                        cmd.Parameters.AddWithValue("@skill1", cult.Skill1?.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@skill2", cult.Skill2?.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@bat_chu_mau", cult.HasBatChuMau ? 1 : 0);
                        cmd.Parameters.AddWithValue("@realm", cult.CurrentRealm.ToString());
                        cmd.Parameters.AddWithValue("@monsters", monstersJson);
                        cmd.Parameters.AddWithValue("@droppedItems", droppedItemsJson);
                        cmd.Parameters.AddWithValue("@launchers", launchersJson);
                        cmd.Parameters.AddWithValue("@soulRings", soulRingsJson);

                        cmd.ExecuteNonQuery();
                    }
                }
                Console.WriteLine($"[MySQL] Đã lưu trạng thái game & thế giới thành công vào MySQL (Slot: {saveSlot})!");
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
            DataLoader loader,
            out string monstersJson,
            out string droppedItemsJson,
            out string launchersJson,
            out string soulRingsJson,
            string saveSlot = "slot_default")
        {
            monstersJson = string.Empty;
            droppedItemsJson = string.Empty;
            launchersJson = string.Empty;
            soulRingsJson = string.Empty;

            if (!IsConnected) return false;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT player_name, level, current_exp, hp, max_hp, soul_power, max_soul_power, equipped_weapon_id, inventory_json, soul_rings_count, skill1_name, skill2_name, has_bat_chu_mau, realm, monsters_json, dropped_items_json, launchers_json, soul_rings_json
                            FROM player_saves 
                            WHERE save_slot = @slot;";
                        cmd.Parameters.AddWithValue("@slot", saveSlot);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string name = reader.GetString(0);
                                int level = reader.GetInt32(1);
                                float exp = reader.GetFloat(2);
                                float hp = reader.GetFloat(3);
                                float maxHp = reader.GetFloat(4);
                                float sp = reader.GetFloat(5);
                                float maxSp = reader.GetFloat(6);
                                string weaponId = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
                                string inventoryJson = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
                                int ringsCount = reader.GetInt32(9);
                                string skill1Name = reader.IsDBNull(10) ? string.Empty : reader.GetString(10);
                                string skill2Name = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
                                bool hasBatChuMau = reader.GetByte(12) == 1;
                                string realmStr = reader.GetString(13);

                                monstersJson = reader.IsDBNull(14) ? string.Empty : reader.GetString(14);
                                droppedItemsJson = reader.IsDBNull(15) ? string.Empty : reader.GetString(15);
                                launchersJson = reader.IsDBNull(16) ? string.Empty : reader.GetString(16);
                                soulRingsJson = reader.IsDBNull(17) ? string.Empty : reader.GetString(17);

                                // Phục hồi các chỉ số tu vi thông qua LoadState
                                var cult = player.Cultivation;
                                cult.LoadState(level, exp, hp, maxHp, sp, maxSp, ringsCount, hasBatChuMau, realmStr, skill1Name, skill2Name);

                                // Phục hồi túi đồ
                                if (!string.IsNullOrEmpty(inventoryJson))
                                {
                                    player.Inventory.Deserialize(inventoryJson, loader);
                                }

                                // Phục hồi trang bị vũ khí
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
