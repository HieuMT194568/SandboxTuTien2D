using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SandboxTuTien.Core;
using SandboxTuTien.Data;
using SandboxTuTien.Data.Models;
using SandboxTuTien.Entities;

namespace SandboxTuTien.Components
{
    /// <summary>
    /// Đối tượng vật phẩm trong túi trữ vật của người chơi.
    /// </summary>
    public class InventoryItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // CONSUMABLE hoặc MAGIC_WEAPON
        public int Quantity { get; set; }

        [JsonIgnore]
        public ConsumableData? Consumable { get; set; }

        [JsonIgnore]
        public MagicWeaponData? MagicWeapon { get; set; }
    }

    /// <summary>
    /// Component quản lý Túi Trữ Vật của nhân vật.
    /// Thiết kế theo Component-Based Architecture.
    /// </summary>
    public class InventoryComponent
    {
        public const string TYPE_CONSUMABLE = "CONSUMABLE";
        public const string TYPE_MAGIC_WEAPON = "MAGIC_WEAPON";

        private readonly List<InventoryItem> _items = new();
        private readonly EventManager _eventManager;

        /// <summary>Danh sách vật phẩm ở chế độ chỉ đọc.</summary>
        public IReadOnlyList<InventoryItem> Items => _items;

        /// <summary>Pháp Khí đang được trang bị.</summary>
        public MagicWeaponData? EquippedWeapon { get; private set; }

        /// <summary>Sự kiện xảy ra khi túi đồ thay đổi (để cập nhật HUD).</summary>
        public event Action? OnInventoryChanged;

        public InventoryComponent(EventManager eventManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        /// <summary>
        /// Thêm Đan Dược / Vật Liệu vào túi.
        /// </summary>
        public void AddConsumable(ConsumableData data, int quantity = 1)
        {
            if (data == null || quantity <= 0) return;

            var existing = _items.FirstOrDefault(i => i.ItemId == data.ItemId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _items.Add(new InventoryItem
                {
                    ItemId = data.ItemId,
                    Name = data.Name,
                    Type = TYPE_CONSUMABLE,
                    Quantity = quantity,
                    Consumable = data
                });
            }

            Console.WriteLine($"[Túi Trữ Vật] +{quantity} {data.Name}.");
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Thêm Pháp Khí vào túi.
        /// </summary>
        public void AddMagicWeapon(MagicWeaponData data, int quantity = 1)
        {
            if (data == null || quantity <= 0) return;

            var existing = _items.FirstOrDefault(i => i.ItemId == data.ItemId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _items.Add(new InventoryItem
                {
                    ItemId = data.ItemId,
                    Name = data.Name,
                    Type = TYPE_MAGIC_WEAPON,
                    Quantity = quantity,
                    MagicWeapon = data
                });
            }

            Console.WriteLine($"[Túi Trữ Vật] +{quantity} Pháp Khí '{data.Name}'.");
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Xóa vật phẩm khỏi túi.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var existing = _items.FirstOrDefault(i => i.ItemId == itemId);
            if (existing == null || existing.Quantity < quantity)
            {
                return false;
            }

            existing.Quantity -= quantity;
            if (existing.Quantity <= 0)
            {
                _items.Remove(existing);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Số lượng một vật phẩm đang có.</summary>
        public int GetItemCount(string itemId)
        {
            return _items.FirstOrDefault(i => i.ItemId == itemId)?.Quantity ?? 0;
        }

        /// <summary>
        /// Trang bị Pháp Khí.
        /// </summary>
        public bool EquipWeapon(string itemId)
        {
            var item = _items.FirstOrDefault(i => i.ItemId == itemId && i.Type == TYPE_MAGIC_WEAPON);
            if (item == null || item.MagicWeapon == null)
            {
                Console.WriteLine($"[Túi Trữ Vật] Không tìm thấy Pháp Khí '{itemId}' để trang bị.");
                return false;
            }

            EquippedWeapon = item.MagicWeapon;
            Console.WriteLine($"[Túi Trữ Vật] ⚔ Đã tế luyện Pháp Khí: {EquippedWeapon.Name}");
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Sử dụng vật phẩm. Trả về false (và không tiêu hao) nếu không dùng được.
        /// </summary>
        public bool UseItem(string itemId, Player player)
        {
            var item = _items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null || item.Quantity <= 0)
            {
                Console.WriteLine($"[Túi Trữ Vật] Không tìm thấy vật phẩm '{itemId}'.");
                return false;
            }

            if (item.Type == TYPE_MAGIC_WEAPON && item.MagicWeapon != null)
            {
                // Dùng Pháp Khí đồng nghĩa với việc trang bị nó
                return EquipWeapon(itemId);
            }

            if (item.Type != TYPE_CONSUMABLE || item.Consumable == null)
            {
                return false;
            }

            var consumable = item.Consumable;
            var cult = player.Cultivation;

            if (consumable.Effects.Count == 0)
            {
                Console.WriteLine($"[Túi Trữ Vật] {consumable.Name} là nguyên liệu, không thể dùng trực tiếp.");
                return false;
            }

            // Kiểm tra cảnh giới tối thiểu
            if ((int)cult.CurrentRealm < consumable.TierRequired)
            {
                Console.WriteLine($"[Túi Trữ Vật] Cảnh giới của {player.Name} quá thấp để luyện hóa {consumable.Name} " +
                                  $"(yêu cầu {CultivationComponent.GetRealmDisplayName((CultivationRealm)consumable.TierRequired)})!");
                return false;
            }

            // Vật phẩm cộng tu vi không dùng được khi tu vi đang bị khóa
            if (consumable.Effects.Any(e => e.Type == "GAIN_CULTIVATION") && !cult.CanGainExp)
            {
                Console.WriteLine($"[Túi Trữ Vật] Không thể luyện hóa {consumable.Name} lúc này (tu vi đang bị khóa).");
                return false;
            }

            Console.WriteLine($"[Túi Trữ Vật] {player.Name} dùng: {consumable.Name}");
            foreach (var effect in consumable.Effects)
            {
                switch (effect.Type)
                {
                    case "HEAL_HP":
                        cult.Heal(effect.ValuePercentage > 0 ? cult.MaxHP * (effect.ValuePercentage / 100f) : effect.Value);
                        break;
                    case "HEAL_HP_OVER_TIME":
                        cult.ApplyHealOverTime(effect.ValuePercentage / 100f, effect.Duration);
                        break;
                    case "RECOVER_SPIRIT_POWER":
                        cult.RecoverSpiritPower(effect.ValuePercentage > 0 ? cult.MaxSpiritPower * (effect.ValuePercentage / 100f) : effect.Value);
                        break;
                    case "BREAKTHROUGH_BUFF":
                        cult.AddPillBuff(effect.ValuePercentage / 100f);
                        break;
                    case "GAIN_CULTIVATION":
                        cult.AddExp(effect.Value);
                        break;
                }
            }

            RemoveItem(itemId, 1);
            return true;
        }

        // ====================================================================
        // SERIALIZATION & DESERIALIZATION (Save/Load)
        // ====================================================================

        /// <summary>
        /// Tuần tự hóa túi đồ thành chuỗi JSON.
        /// </summary>
        public string Serialize()
        {
            var data = new
            {
                equippedWeaponId = EquippedWeapon?.ItemId ?? string.Empty,
                items = _items.Select(i => new
                {
                    itemId = i.ItemId,
                    type = i.Type,
                    quantity = i.Quantity
                }).ToArray()
            };

            return JsonSerializer.Serialize(data);
        }

        /// <summary>
        /// Giải tuần tự hóa túi đồ từ chuỗi JSON và khôi phục dữ liệu bằng DataLoader.
        /// </summary>
        public void Deserialize(string json, DataLoader loader)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                _items.Clear();
                EquippedWeapon = null;

                var magicWeapons = loader.LoadMagicWeapons();
                var consumables = loader.LoadConsumables();

                if (root.TryGetProperty("items", out var itemsProperty) && itemsProperty.ValueKind == JsonValueKind.Array)
                {
                    foreach (var itemElem in itemsProperty.EnumerateArray())
                    {
                        string itemId = itemElem.GetProperty("itemId").GetString() ?? string.Empty;
                        string type = itemElem.GetProperty("type").GetString() ?? string.Empty;
                        int quantity = itemElem.GetProperty("quantity").GetInt32();

                        if (type == TYPE_CONSUMABLE)
                        {
                            var data = consumables.FirstOrDefault(c => c.ItemId == itemId);
                            if (data != null)
                            {
                                _items.Add(new InventoryItem
                                {
                                    ItemId = itemId,
                                    Name = data.Name,
                                    Type = type,
                                    Quantity = quantity,
                                    Consumable = data
                                });
                            }
                        }
                        else if (type == TYPE_MAGIC_WEAPON)
                        {
                            var data = magicWeapons.FirstOrDefault(w => w.ItemId == itemId);
                            if (data != null)
                            {
                                _items.Add(new InventoryItem
                                {
                                    ItemId = itemId,
                                    Name = data.Name,
                                    Type = type,
                                    Quantity = quantity,
                                    MagicWeapon = data
                                });
                            }
                        }
                    }
                }

                if (root.TryGetProperty("equippedWeaponId", out var eqWeaponProperty))
                {
                    string equippedId = eqWeaponProperty.GetString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(equippedId))
                    {
                        EquippedWeapon = magicWeapons.FirstOrDefault(w => w.ItemId == equippedId);
                    }
                }

                Console.WriteLine($"[Túi Trữ Vật] Đã khôi phục túi đồ ({_items.Count} vật phẩm).");
                OnInventoryChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Túi Trữ Vật] LỖI khi giải tuần tự hóa túi đồ: {ex.Message}");
            }
        }
    }
}
