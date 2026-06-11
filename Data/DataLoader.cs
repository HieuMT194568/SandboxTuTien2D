using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SandboxTuTien.Data.Models;

namespace SandboxTuTien.Data
{
    /// <summary>
    /// Data-Driven loader: Đọc và deserialize các file JSON cấu hình
    /// thành C# objects sử dụng System.Text.Json.
    /// Hỗ trợ load từ đường dẫn tuyệt đối hoặc tương đối trong Content.
    /// </summary>
    public class DataLoader
    {
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>Đường dẫn gốc đến thư mục Content.</summary>
        private readonly string _contentRootPath;

        public DataLoader(string contentRootPath)
        {
            _contentRootPath = contentRootPath ?? throw new ArgumentNullException(nameof(contentRootPath));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
        }

        // ====================================================================
        // GENERIC LOADERS
        // ====================================================================

        /// <summary>
        /// Load danh sách object từ file JSON (JSON array).
        /// </summary>
        /// <typeparam name="T">Kiểu POCO class để deserialize.</typeparam>
        /// <param name="relativeFilePath">Đường dẫn tương đối từ Content root.</param>
        /// <returns>Danh sách objects, hoặc empty list nếu lỗi.</returns>
        public List<T> LoadList<T>(string relativeFilePath) where T : class
        {
            string fullPath = Path.Combine(_contentRootPath, relativeFilePath);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[DataLoader] CẢNH BÁO: Không tìm thấy file '{fullPath}'. " +
                                  "Trả về danh sách rỗng.");
                return new List<T>();
            }

            try
            {
                string jsonContent = File.ReadAllText(fullPath);
                var result = JsonSerializer.Deserialize<List<T>>(jsonContent, _jsonOptions);

                if (result == null)
                {
                    Console.WriteLine($"[DataLoader] CẢNH BÁO: Deserialize '{fullPath}' trả về null.");
                    return new List<T>();
                }

                Console.WriteLine($"[DataLoader] Đã load {result.Count} {typeof(T).Name} " +
                                  $"từ '{relativeFilePath}'.");
                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[DataLoader] LỖI JSON khi đọc '{fullPath}': {ex.Message}");
                return new List<T>();
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[DataLoader] LỖI IO khi đọc '{fullPath}': {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Load một object đơn lẻ từ file JSON.
        /// </summary>
        public T? LoadSingle<T>(string relativeFilePath) where T : class
        {
            string fullPath = Path.Combine(_contentRootPath, relativeFilePath);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[DataLoader] CẢNH BÁO: Không tìm thấy file '{fullPath}'.");
                return null;
            }

            try
            {
                string jsonContent = File.ReadAllText(fullPath);
                var result = JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);

                Console.WriteLine($"[DataLoader] Đã load {typeof(T).Name} từ '{relativeFilePath}'.");
                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[DataLoader] LỖI JSON khi đọc '{fullPath}': {ex.Message}");
                return null;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[DataLoader] LỖI IO khi đọc '{fullPath}': {ex.Message}");
                return null;
            }
        }

        // ====================================================================
        // TYPED CONVENIENCE LOADERS
        // ====================================================================

        /// <summary>Load danh sách Ám Khí từ hidden_weapons.json.</summary>
        public List<HiddenWeaponData> LoadHiddenWeapons()
        {
            return LoadList<HiddenWeaponData>(Path.Combine("Data", "hidden_weapons.json"));
        }

        /// <summary>Load danh sách Thực Phẩm từ consumables.json.</summary>
        public List<ConsumableData> LoadConsumables()
        {
            return LoadList<ConsumableData>(Path.Combine("Data", "consumables.json"));
        }
    }
}
