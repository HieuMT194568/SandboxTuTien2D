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
    /// Đối tượng vật phẩm trong túi đồ của người chơi.
    /// </summary>
    public class InventoryItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // CONSUMABLE hoặc HIDDEN_WEAPON
        public int Quantity { get; set; }

        [JsonIgnore]
        public ConsumableData? Consumable { get; set; }

        [JsonIgnore]
        public HiddenWeaponData? HiddenWeapon { get; set; }
    }

    /// <summary>
    /// Component quản lý Túi đồ của nhân vật.
    /// Thiết kế theo Component-Based Architecture.
    /// </summary>
    public class InventoryComponent
    {
        private readonly List<InventoryItem> _items = new();
        private readonly EventManager _eventManager;

        /// <summary>Danh sách vật phẩm ở chế độ chỉ đọc.</summary>
        public IReadOnlyList<InventoryItem> Items => _items;

        /// <summary>Ám khí đang được trang bị.</summary>
        public HiddenWeaponData? EquippedWeapon { get; private set; }

        /// <summary>Sự kiện xảy ra khi túi đồ thay đổi (để cập nhật HUD).</summary>
        public event Action? OnInventoryChanged;

        public InventoryComponent(EventManager eventManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        /// <summary>
        /// Thêm Thực Phẩm vào túi đồ.
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
                    Type = "CONSUMABLE",
                    Quantity = quantity,
                    Consumable = data
                });
            }

            Console.WriteLine($"[Túi Đồ] +{quantity} {data.Name} đã thêm vào túi đồ.");
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Thêm Ám khí vào túi đồ.
        /// </summary>
        public void AddHiddenWeapon(HiddenWeaponData data, int quantity = 1)
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
                    Type = "HIDDEN_WEAPON",
                    Quantity = quantity,
                    HiddenWeapon = data
                });
            }

            Console.WriteLine($"[Túi Đồ] +{quantity} Ám khí '{data.Name}' đã thêm vào túi đồ.");
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Xóa vật phẩm khỏi túi đồ.
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

        /// <summary>
        /// Trang bị Ám Khí.
        /// </summary>
        public bool EquipWeapon(string itemId)
        {
            var item = _items.FirstOrDefault(i => i.ItemId == itemId && i.Type == "HIDDEN_WEAPON");
            if (item == null || item.HiddenWeapon == null)
            {
                Console.WriteLine($"[Túi Đồ] Không tìm thấy Ám khí '{itemId}' trong túi đồ để trang bị.");
                return false;
            }

            EquippedWeapon = item.HiddenWeapon;
            Console.WriteLine($"[Túi Đồ] ⚔ Đã trang bị Ám khí: {EquippedWeapon.Name}");
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Sử dụng/Tiêu thụ vật phẩm.
        /// </summary>
        public bool UseItem(string itemId, Player player)
        {
            var item = _items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null || item.Quantity <= 0)
            {
                Console.WriteLine($"[Túi Đồ] Không tìm thấy vật phẩm '{itemId}' trong túi.");
                return false;
            }

            if (item.Type == "CONSUMABLE" && item.Consumable != null)
            {
                var consumable = item.Consumable;

                // Kiểm tra cảnh giới tối thiểu
                int playerRealmIndex = (int)player.Cultivation.CurrentRealm;
                if (playerRealmIndex < consumable.TierRequired)
                {
                    Console.WriteLine($"[Túi Đồ] Cảnh giới của {player.Name} quá thấp ({player.Cultivation.CurrentRealm}), " +
                                      $"yêu cầu cấp cảnh giới {consumable.TierRequired} để dùng {consumable.Name}!");
                    return false;
                }

                // Thực hiện sử dụng thực phẩm
                Console.WriteLine($"[Túi Đồ] {player.Name} sử dụng: {consumable.Name}");
                foreach (var effect in consumable.Effects)
                {
                    if (effect.Type == "HEAL_HP")
                    {
                        float healAmount = effect.ValuePercentage > 0
                            ? player.Cultivation.MaxHP * (effect.ValuePercentage / 100f)
                            : effect.Value;
                        player.Cultivation.Heal(healAmount);
                    }
                    else if (effect.Type == "RECOVER_SOUL_POWER")
                    {
                        float recoverAmount = effect.ValuePercentage > 0
                            ? player.Cultivation.MaxSoulPower * (effect.ValuePercentage / 100f)
                            : effect.Value;
                        player.Cultivation.RecoverSoulPower(recoverAmount);
                    }
                }

                // Giảm số lượng
                RemoveItem(itemId, 1);
                return true;
            }
            else if (item.Type == "HIDDEN_WEAPON" && item.HiddenWeapon != null)
            {
                // Sử dụng Ám khí đồng nghĩa với việc Trang bị nó
                return EquipWeapon(itemId);
            }

            return false;
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

                // Load danh sách dữ liệu mẫu từ DataLoader để so khớp và khôi phục tham chiếu
                var hiddenWeapons = loader.LoadHiddenWeapons();
                var consumables = loader.LoadConsumables();

                if (root.TryGetProperty("items", out var itemsProperty) && itemsProperty.ValueKind == JsonValueKind.Array)
                {
                    foreach (var itemElem in itemsProperty.EnumerateArray())
                    {
                        string itemId = itemElem.GetProperty("itemId").GetString() ?? string.Empty;
                        string type = itemElem.GetProperty("type").GetString() ?? string.Empty;
                        int quantity = itemElem.GetProperty("quantity").GetInt32();

                        if (type == "CONSUMABLE")
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
                        else if (type == "HIDDEN_WEAPON")
                        {
                            var data = hiddenWeapons.FirstOrDefault(w => w.ItemId == itemId);
                            if (data != null)
                            {
                                _items.Add(new InventoryItem
                                {
                                    ItemId = itemId,
                                    Name = data.Name,
                                    Type = type,
                                    Quantity = quantity,
                                    HiddenWeapon = data
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
                        var weapon = hiddenWeapons.FirstOrDefault(w => w.ItemId == equippedId);
                        if (weapon != null)
                        {
                            EquippedWeapon = weapon;
                        }
                    }
                }

                Console.WriteLine($"[Túi Đồ] Đã khôi phục trạng thái túi đồ (Đang có {_items.Count} vật phẩm).");
                OnInventoryChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Túi Đồ] LỖI khi giải tuần tự hóa túi đồ: {ex.Message}");
            }
        }
    }
}
