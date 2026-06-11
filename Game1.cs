using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SandboxTuTien.Core;
using SandboxTuTien.Core.Combat;
using SandboxTuTien.Components;
using SandboxTuTien.Data;
using SandboxTuTien.Data.Models;
using SandboxTuTien.Entities;
using SandboxTuTien.Systems;

namespace SandboxTuTien;

/// <summary>
/// Đối tượng chữ nổi phục vụ hiển thị sát thương hoặc thông báo (chỉ dùng ASCII).
/// </summary>
public class FloatingText
{
    public Vector2 Position;
    public string Text;
    public Color Color;
    public float Scale;
    public float Lifetime;
    public float Elapsed;
    public bool Active => Elapsed < Lifetime;

    public FloatingText(Vector2 position, string text, Color color, float lifetime = 1.2f, float scale = 1.0f)
    {
        Position = position;
        Text = text;
        Color = color;
        Lifetime = lifetime;
        Scale = scale;
        Elapsed = 0f;
    }

    public void Update(float deltaTime)
    {
        Elapsed += deltaTime;
        Position.Y -= 40f * deltaTime; // Bay lên trên
    }
}

/// <summary>
/// Game1 — Điểm tích hợp giao diện đồ họa và tất cả các hệ thống.
/// </summary>
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    // ========================================================================
    // ĐỐI TƯỢNG ĐỒ HỌA
    // ========================================================================
    private SpriteFont _font = null!;
    private Texture2D _pixelTexture = null!;
    private Texture2D _playerTexture = null!;
    private Texture2D _monsterPlantTexture = null!;
    private Texture2D _monsterFireTexture = null!;
    private Texture2D _monsterIceTexture = null!;
    private Texture2D _needleTexture = null!;
    private Texture2D _boltTexture = null!;
    private Texture2D _sausageTexture = null!;
    private Texture2D _ringTexture = null!;
    private Texture2D _turretTexture = null!;
    private Texture2D _oscarTexture = null!;
    private Texture2D _anvilTexture = null!;

    // ========================================================================
    // HỆ THỐNG CỐT LÕI
    // ========================================================================
    private EventManager _eventManager = null!;
    private MySqlDbManager _dbManager = null!;
    private GameTimeManager _gameTimeManager = null!;
    private DataLoader _dataLoader = null!;
    private CultivationSystem _cultivationSystem = null!;

    // ========================================================================
    // ĐỐI TƯỢNG VÀ COMBAT
    // ========================================================================
    private Player _player = null!;
    private ProjectilePool _projectilePool = null!;
    private readonly List<Monster> _monsters = new();
    private readonly List<SoulRingEntity> _soulRings = new();
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly List<AutoLauncher> _launchers = new();
    private readonly List<Particle> _particles = new();
    private ConsumableSpawner _oscarSpawner = null!;
    private ConsumableSpawner _anvilSpawner = null!;
    private readonly List<DroppedItem> _droppedItems = new();
    private int _nextTurretTypeToPlace = 1;
    private float _contactDamageTimer = 0f;

    private float _shakeTime = 0f;
    private float _shakeIntensity = 0f;
    private Vector2 _absorbingRingPosition = Vector2.Zero;
    private Vector2 _cameraPosition = Vector2.Zero;
    private const float MAP_WIDTH = 2000f;
    private const float MAP_HEIGHT = 2000f;

    private List<HiddenWeaponData> _hiddenWeapons = null!;
    private List<ConsumableData> _consumables = null!;

    // ========================================================================
    // TRẠNG THÁI GIAO DIỆN
    // ========================================================================
    private bool _isInventoryOpen = false;
    private KeyboardState _previousKeyState;
    private MouseState _previousMouseState;
    private readonly Random _random = new();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
        _graphics.ApplyChanges();

        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     SANDBOX TU TIÊN 2D PIXEL — ĐẤU LA ĐẠI LỤC         ║");
        Console.WriteLine("║     Mô-đun Chiến đấu, Túi đồ & Đồ họa v2.0               ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 1. Khởi tạo EventManager
        _eventManager = new EventManager();
        SubscribeToEvents();

        // 2. Khởi tạo GameTimeManager
        _gameTimeManager = new GameTimeManager();

        // 3. Khởi tạo DataLoader
        string contentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
        _dataLoader = new DataLoader(contentPath);

        // 3.5. Khởi tạo MySQL Database Manager
        _dbManager = new MySqlDbManager();
        _dbManager.InitializeDatabase();

        LoadGameData();

        // 4. Khởi tạo Hệ thống Tu luyện
        _cultivationSystem = new CultivationSystem();

        // 5. Khởi tạo Projectile Pool
        _projectilePool = new ProjectilePool();

        // 6. Tạo Player
        _player = new Player(
            name: "Đường Tam",
            eventManager: _eventManager,
            innateLevel: 10,
            innateMultiplier: 2.0f
        );
        _cultivationSystem.RegisterComponent(_player.Cultivation);

        // 7. Cấp phát túi đồ mặc định
        GiveInitialInventoryItems();

        _oscarSpawner = new ConsumableSpawner(new Vector2(250, 380), 15f, "food_huong_trang_01", "Sausage", "CONSUMABLE", 1);
        _anvilSpawner = new ConsumableSpawner(new Vector2(550, 380), 10f, "item_thiet_mau_01", "Thiet Mau Ammo", "CONSUMABLE", 1);

        _oscarSpawner.OnSpawn += (pos, id, name, type, qty) => {
            _droppedItems.Add(new DroppedItem(pos, id, name, type, qty));
            _floatingTexts.Add(new FloatingText(pos - new Vector2(0, 15), $"+ Dropped {name}", Color.Orange, 1.5f));
        };
        _anvilSpawner.OnSpawn += (pos, id, name, type, qty) => {
            _droppedItems.Add(new DroppedItem(pos, id, name, type, qty));
            _floatingTexts.Add(new FloatingText(pos - new Vector2(0, 15), $"+ Dropped {name}", Color.Gray, 1.5f));
        };

        _previousKeyState = Keyboard.GetState();
        _previousMouseState = Mouse.GetState();

        // Sinh ngẫu nhiên một số Hồn Thú ban đầu rải rác trên bản đồ để thế giới sống động
        for (int i = 0; i < 15; i++)
        {
            Vector2 randomPos = new Vector2(
                _random.Next(100, 1900),
                _random.Next(100, 1900)
            );
            // Không sinh quá gần người chơi (tọa độ xuất phát 400, 240)
            if (Vector2.Distance(randomPos, new Vector2(400, 240)) > 200f)
            {
                SpawnRandomMonster(randomPos);
            }
        }

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Fonts/Arial");

        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _playerTexture = LoadTextureFromFile("Content/Sprites/player.png", () => PixelArtGenerator.CreatePlayerTexture(GraphicsDevice));
        _monsterPlantTexture = LoadTextureFromFile("Content/Sprites/monster_plant.png", () => PixelArtGenerator.CreateMonsterPlantTexture(GraphicsDevice));
        _monsterFireTexture = LoadTextureFromFile("Content/Sprites/monster_fire.png", () => PixelArtGenerator.CreateMonsterFireTexture(GraphicsDevice));
        _monsterIceTexture = LoadTextureFromFile("Content/Sprites/monster_ice.png", () => PixelArtGenerator.CreateMonsterIceTexture(GraphicsDevice));
        _needleTexture = LoadTextureFromFile("Content/Sprites/needle.png", () => PixelArtGenerator.CreateProjectileNeedleTexture(GraphicsDevice));
        _boltTexture = LoadTextureFromFile("Content/Sprites/bolt.png", () => PixelArtGenerator.CreateProjectileBoltTexture(GraphicsDevice));
        _sausageTexture = LoadTextureFromFile("Content/Sprites/sausage.png", () => PixelArtGenerator.CreateSausageTexture(GraphicsDevice));
        _ringTexture = LoadTextureFromFile("Content/Sprites/ring.png", () => PixelArtGenerator.CreateRingTexture(GraphicsDevice, 32));
        _turretTexture = LoadTextureFromFile("Content/Sprites/turret.png", () => PixelArtGenerator.CreateTurretTexture(GraphicsDevice));
        _oscarTexture = LoadTextureFromFile("Content/Sprites/oscar.png", () => PixelArtGenerator.CreatePlayerTexture(GraphicsDevice));
        _anvilTexture = LoadTextureFromFile("Content/Sprites/anvil.png", () => PixelArtGenerator.CreateTurretTexture(GraphicsDevice));
    }

    private Texture2D LoadTextureFromFile(string relativePath, Func<Texture2D> fallbackGenerator)
    {
        try
        {
            string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (File.Exists(absolutePath))
            {
                using (var stream = File.OpenRead(absolutePath))
                {
                    return Texture2D.FromStream(GraphicsDevice, stream);
                }
            }
            else
            {
                Console.WriteLine($"[Đồ Họa] File không tồn tại: {absolutePath}. Sử dụng tạo tự động.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Đồ Họa] Không thể tải {relativePath}: {ex.Message}. Sử dụng tạo tự động.");
        }
        return fallbackGenerator();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentKeyState = Keyboard.GetState();
        MouseState currentMouseState = Mouse.GetState();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (currentKeyState.IsKeyDown(Keys.Escape))
            Exit();

        _gameTimeManager.Update(gameTime);
        _cultivationSystem.Update(gameTime);
        _projectilePool.Update(deltaTime);

        // Cập nhật chuyển động người chơi
        UpdatePlayerMovement(currentKeyState, deltaTime);

        Vector2 playerPos = new Vector2(_player.PositionX, _player.PositionY);

        // Cập nhật Camera bám theo nhân vật một cách mượt mà (Lerp)
        Vector2 targetCamera = new Vector2(_player.PositionX - 400f, _player.PositionY - 300f);
        _cameraPosition = Vector2.Lerp(_cameraPosition, targetCamera, 0.1f);
        _cameraPosition.X = Math.Clamp(_cameraPosition.X, 0f, MAP_WIDTH - 800f);
        _cameraPosition.Y = Math.Clamp(_cameraPosition.Y, 0f, MAP_HEIGHT - 600f);

        // 1. Cập nhật Hồn Thú tự động tiến hóa theo tuổi thọ (1s thực = 24 phút game)
        for (int i = _monsters.Count - 1; i >= 0; i--)
        {
            _monsters[i].UpdateEvolution(deltaTime, 1440f, playerPos);
        }

        // 1.5. Boss Territorial Aura (Uy Áp) fear logic on low-level monsters
        var bosses = _monsters.Where(m => m.Active && m.Age >= 10000).ToList();
        foreach (var m in _monsters)
        {
            if (!m.Active || m.Age >= 1000) continue;
            var nearBoss = bosses.FirstOrDefault(b => Vector2.Distance(m.Position, b.Position) <= 150f);
            if (nearBoss != null)
            {
                m.PanicTimer = 1.0f;
                m.PanicSource = nearBoss.Position;
            }
        }

        // 2. Cập nhật hoạt động của Bệ Phóng Ám Khí tự động
        foreach (var launcher in _launchers)
        {
            float oldCd = launcher.CooldownTimer;
            launcher.Update(deltaTime, _monsters, _projectilePool);
            // Kích nổ hạt cơ quan xẹt lửa và rung màn nhẹ khi bệ phóng khai hỏa
            if (launcher.CooldownTimer > oldCd)
            {
                SpawnElementalBurst(launcher.Position, Element.None, 8);
                TriggerShake(0.08f, 1.5f);
            }
        }

        // Cập nhật spawners và dropped items
        _oscarSpawner.Update(deltaTime);
        _anvilSpawner.Update(deltaTime);

        for (int i = _droppedItems.Count - 1; i >= 0; i--)
        {
            var item = _droppedItems[i];
            item.Update(deltaTime);

            if (Vector2.Distance(playerPos, item.Position) <= 25f)
            {
                bool pickedUp = false;
                if (item.Type == "CONSUMABLE")
                {
                    var data = _consumables.FirstOrDefault(c => c.ItemId == item.ItemId);
                    if (data != null)
                    {
                        _player.Inventory.AddConsumable(data, item.Quantity);
                        pickedUp = true;
                    }
                }
                else if (item.Type == "HIDDEN_WEAPON")
                {
                    var data = _hiddenWeapons.FirstOrDefault(w => w.ItemId == item.ItemId);
                    if (data != null)
                    {
                        _player.Inventory.AddHiddenWeapon(data, item.Quantity);
                        pickedUp = true;
                    }
                }

                if (pickedUp)
                {
                    string nameHUD = GetHUDItemName(item.ItemId);
                    _floatingTexts.Add(new FloatingText(playerPos - new Vector2(0, 25), $"+ {nameHUD} ({item.Quantity})", Color.LimeGreen));
                    _droppedItems.RemoveAt(i);
                }
            }
        }

        // Tích lũy sát thương va chạm quái vật (Contact damage 8 HP/sec)
        if (_player.Cultivation.CurrentState != CultivationState.Dead && _player.Cultivation.CurrentState != CultivationState.AbsorbingRing)
        {
            _contactDamageTimer += deltaTime;
            if (_contactDamageTimer >= 0.5f)
            {
                _contactDamageTimer = 0f;
                float totalContactDmg = 0f;
                foreach (var monster in _monsters)
                {
                    if (monster.Active && monster.CheckCollision(playerPos, 12f))
                    {
                        totalContactDmg += 4f;
                    }
                }
                if (totalContactDmg > 0f)
                {
                    _player.Cultivation.Heal(-totalContactDmg);
                }
            }
        }

        // Cập nhật hệ thống hạt (Particles) & Rung màn hình (Screenshake)
        UpdateParticles(deltaTime);
        UpdateScreenshake(deltaTime);
        UpdateProjectileTrails();
        UpdateMeditationVFX();
        UpdateAbsorptionVFX();

        // Cập nhật Hồn Hoàn rơi
        foreach (var ring in _soulRings)
        {
            ring.Update(gameTime);
        }

        // Cập nhật chữ nổi
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            _floatingTexts[i].Update(deltaTime);
            if (!_floatingTexts[i].Active)
            {
                _floatingTexts.RemoveAt(i);
            }
        }

        HandleProjectileCollisions();
        HandleInput(currentKeyState, currentMouseState);
        UpdateWindowTitle();

        _previousKeyState = currentKeyState;
        _previousMouseState = currentMouseState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 18, 32));

        // Rung giật màn hình
        Vector2 shakeOffset = Vector2.Zero;
        if (_shakeTime > 0)
        {
            shakeOffset.X = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
            shakeOffset.Y = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
        }

        // Ma trận camera + màn hình rung
        Matrix worldTransform = Matrix.CreateTranslation(-_cameraPosition.X + shakeOffset.X, -_cameraPosition.Y + shakeOffset.Y, 0);

        float gameTimeForDraw = (float)gameTime.TotalGameTime.TotalSeconds;

        // ====================================================================
        // PASS 1: RENDER WORLD SPRITES (PointClamp để giữ pixel art sắc nét)
        // ====================================================================
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, worldTransform);

        // 0. Vẽ Nền Đất Tiled Background cao cấp
        int tileSize = 64;
        int startTileX = (int)(_cameraPosition.X / tileSize) - 1;
        int startTileY = (int)(_cameraPosition.Y / tileSize) - 1;
        int endTileX = (int)((_cameraPosition.X + 800f) / tileSize) + 1;
        int endTileY = (int)((_cameraPosition.Y + 600f) / tileSize) + 1;

        startTileX = Math.Max(0, startTileX);
        startTileY = Math.Max(0, startTileY);
        endTileX = Math.Min((int)(MAP_WIDTH / tileSize), endTileX);
        endTileY = Math.Min((int)(MAP_HEIGHT / tileSize), endTileY);

        for (int tx = startTileX; tx <= endTileX; tx++)
        {
            for (int ty = startTileY; ty <= endTileY; ty++)
            {
                Rectangle tileRect = new Rectangle(tx * tileSize, ty * tileSize, tileSize, tileSize);
                Color tileColor = ((tx + ty) % 2 == 0) ? new Color(18, 22, 36) : new Color(14, 17, 30);
                _spriteBatch.Draw(_pixelTexture, tileRect, tileColor);

                // Viền lưới nhẹ nhàng
                DrawRect(tx * tileSize, ty * tileSize, tileSize, 1, new Color(28, 33, 52) * 0.4f);
                DrawRect(tx * tileSize, ty * tileSize, 1, tileSize, new Color(28, 33, 52) * 0.4f);

                // Các vì sao tinh tú/linh khí phát sáng ngẫu nhiên định sẵn
                int hash = (tx * 17 + ty * 31) % 100;
                if (hash < 12)
                {
                    int starOffset = (tx * 7 + ty * 13) % (tileSize - 20) + 10;
                    Vector2 starPos = new Vector2(tx * tileSize + starOffset, ty * tileSize + starOffset);
                    Color starColor = (hash % 3 == 0) ? Color.Cyan * 0.25f : 
                                      (hash % 3 == 1) ? Color.Gold * 0.2f : Color.MediumPurple * 0.25f;
                    _spriteBatch.Draw(_pixelTexture, new Rectangle((int)starPos.X, (int)starPos.Y, 2, 2), starColor);
                }
            }
        }

        // 1. Vẽ Hồn Hoàn dưới đất
        foreach (var ring in _soulRings)
        {
            if (ring.Active)
            {
                float ringScaleFactor = 64f / _ringTexture.Width;
                float scale = (0.5f + 0.08f * (float)Math.Sin(ring.PulseTimer * 5f)) * ringScaleFactor;
                float rotation = ring.PulseTimer * 0.7f;
                Vector2 ringOrigin = new Vector2(_ringTexture.Width / 2f, _ringTexture.Height / 2f);
                _spriteBatch.Draw(_ringTexture, ring.Position, null, ring.GetColor() * 0.8f,
                                  rotation, ringOrigin, scale, SpriteEffects.None, 0f);
            }
        }

        // 1.5. Vẽ NPC Oscar và Anvil bằng các model .png xianxia premium mới
        // Oscar NPC
        Vector2 oscarPos = new Vector2(250, 380);
        Vector2 oscarOrigin = new Vector2(_oscarTexture.Width / 2f, _oscarTexture.Height / 2f);
        float oscarScale = 32f / _oscarTexture.Width;
        _spriteBatch.Draw(_oscarTexture, oscarPos, null, Color.White, 0f, oscarOrigin, 1.5f * oscarScale, SpriteEffects.None, 0f);

        // Anvil Spawner
        Vector2 anvilPos = new Vector2(550, 380);
        Vector2 anvilOrigin = new Vector2(_anvilTexture.Width / 2f, _anvilTexture.Height / 2f);
        float anvilScale = 32f / _anvilTexture.Width;
        _spriteBatch.Draw(_anvilTexture, anvilPos, null, Color.White, 0f, anvilOrigin, 1.5f * anvilScale, SpriteEffects.None, 0f);

        // 2. Vẽ các Dropped Item (chỉ phần hình ảnh)
        foreach (var item in _droppedItems)
        {
            if (item.Active)
            {
                Texture2D tex = item.ItemId == "food_huong_trang_01" ? _sausageTexture : _needleTexture;
                float hoverY = (float)Math.Sin(item.HoverTimer * 4f) * 3f;
                Vector2 drawPos = new Vector2(item.Position.X, item.Position.Y + hoverY);
                Vector2 itemOrigin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                float itemScaleFactor = (item.ItemId == "food_huong_trang_01" ? 16f : 8f) / tex.Width;
                _spriteBatch.Draw(tex, drawPos, null, Color.White, 0f, itemOrigin, 1.5f * itemScaleFactor, SpriteEffects.None, 0f);
            }
        }

        // 3. Vẽ các Bệ Phóng Ám Khí tự động xuống đất (chỉ phần hình ảnh + vòng quét)
        foreach (var launcher in _launchers)
        {
            if (launcher.Active)
            {
                float recoil = 1.0f;
                if (launcher.CooldownTimer > launcher.FireRate - 0.2f)
                {
                    float t = (launcher.CooldownTimer - (launcher.FireRate - 0.2f)) / 0.2f;
                    recoil = 1.0f - 0.25f * t;
                }

                Vector2 origin = new Vector2(_turretTexture.Width / 2f, _turretTexture.Height / 2f);
                float turretScaleFactor = 24f / _turretTexture.Width;
                Vector2 drawScale = new Vector2(1.5f * turretScaleFactor, 1.5f * recoil * turretScaleFactor);

                _spriteBatch.Draw(_turretTexture, launcher.Position, null, Color.White, 0f, 
                                  origin, drawScale, SpriteEffects.None, 0f);
                
                Vector2 ringOrigin = new Vector2(_ringTexture.Width / 2f, _ringTexture.Height / 2f);
                _spriteBatch.Draw(_ringTexture, launcher.Position, null, Color.White * 0.15f, 
                                  0f, ringOrigin, launcher.Range / (ringOrigin.X), SpriteEffects.None, 0f);
            }
        }

        // 4. Vẽ Hồn Thú (chỉ hình ảnh + vòng hào quang)
        foreach (var monster in _monsters)
        {
            if (monster.Active)
            {
                Texture2D tex = monster.Element switch
                {
                    Element.Wood => _monsterPlantTexture,
                    Element.Fire => _monsterFireTexture,
                    Element.Ice => _monsterIceTexture,
                    _ => _monsterPlantTexture
                };
                
                Vector2 monsterOrigin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                float scale = (monster.Radius / (tex.Width / 2f)) * 1.2f;

                Color drawColor = Color.White;
                if (monster.Age >= 100000)
                {
                    drawColor = Color.Red;
                }

                _spriteBatch.Draw(tex, monster.Position, null, drawColor, 0f, 
                                  monsterOrigin, scale, SpriteEffects.None, 0f);

                if (monster.Age >= 10000)
                {
                    Color auraColor = monster.Age >= 100000 ? Color.Red * 0.15f : Color.Purple * 0.12f;
                    Vector2 ringOrigin = new Vector2(_ringTexture.Width / 2f, _ringTexture.Height / 2f);
                    _spriteBatch.Draw(_ringTexture, monster.Position, null, auraColor, 
                                      0f, ringOrigin, 150f / (ringOrigin.X), SpriteEffects.None, 0f);
                }
            }
        }

        // 5. Vẽ đạn ám khí (chỉ phần hình ảnh)
        foreach (var proj in _projectilePool.Projectiles)
        {
            if (proj.Active)
            {
                Texture2D tex = proj.Element == Element.Fire ? _boltTexture : _needleTexture;
                Color bulletColor = proj.Element == Element.None ? Color.LightGray : Color.White;
                float rotation = (float)Math.Atan2(proj.Velocity.Y, proj.Velocity.X);
                Vector2 projOrigin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                float projScaleFactor = 8f / tex.Width;
                
                _spriteBatch.Draw(tex, proj.Position, null, bulletColor, rotation, 
                                  projOrigin, 1.5f * projScaleFactor, SpriteEffects.None, 0f);
            }
        }

        // 6. Vẽ Player (chỉ phần hình ảnh)
        if (_player.Cultivation.CurrentState != CultivationState.Dead)
        {
            float hover = 0f;
            if (_player.Cultivation.CurrentState == CultivationState.Meditating)
            {
                hover = (float)Math.Sin(gameTimeForDraw * 0.15f) * 4f;
            }

            Vector2 playerOrigin = new Vector2(_playerTexture.Width / 2f, _playerTexture.Height / 2f);
            float playerScaleFactor = 32f / _playerTexture.Width;
            _spriteBatch.Draw(_playerTexture, new Vector2(_player.PositionX, _player.PositionY + hover), null, Color.White, 0f, 
                              playerOrigin, 1.5f * playerScaleFactor, SpriteEffects.None, 0f);
        }
        else
        {
            Vector2 playerOrigin = new Vector2(_playerTexture.Width / 2f, _playerTexture.Height / 2f);
            float playerScaleFactor = 32f / _playerTexture.Width;
            _spriteBatch.Draw(_playerTexture, new Vector2(_player.PositionX, _player.PositionY), null, Color.DimGray, (float)Math.PI / 2f, 
                              playerOrigin, 1.5f * playerScaleFactor, SpriteEffects.None, 0f);
        }

        // 7. Vẽ các hạt năng lượng (Particles)
        foreach (var p in _particles)
        {
            float alpha = 1f - (p.Elapsed / p.Lifetime);
            Color drawColor = p.Color * alpha;
            _spriteBatch.Draw(_pixelTexture, p.Position, null, drawColor, p.Rotation, new Vector2(0.5f, 0.5f), p.Size, SpriteEffects.None, 0f);
        }

        _spriteBatch.End();

        // ====================================================================
        // PASS 2: RENDER WORLD UI/TEXT/BARS (LinearClamp để chữ KHÔNG bị nhòe/mất dòng)
        // ====================================================================
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, worldTransform);

        // Nhãn tên NPC Oscar & Anvil
        _spriteBatch.DrawString(_font, "Oscar", oscarPos - new Vector2(18, _oscarTexture.Height * 1.5f * oscarScale / 2f + 15f), Color.Pink, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, "Anvil", anvilPos - new Vector2(16, _anvilTexture.Height * 1.5f * anvilScale / 2f + 15f), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        // Nhãn tên Dropped Item
        foreach (var item in _droppedItems)
        {
            if (item.Active)
            {
                Texture2D tex = item.ItemId == "food_huong_trang_01" ? _sausageTexture : _needleTexture;
                float hoverY = (float)Math.Sin(item.HoverTimer * 4f) * 3f;
                Vector2 drawPos = new Vector2(item.Position.X, item.Position.Y + hoverY);
                _spriteBatch.DrawString(_font, item.Name, drawPos - new Vector2(20, 15), Color.White * 0.8f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }
        }

        // Thông tin đạn bệ phóng
        foreach (var launcher in _launchers)
        {
            if (launcher.Active)
            {
                string ammoText = launcher.AmmoCount <= 0 ? "EMPTY" : $"{launcher.AmmoCount}/{launcher.MaxAmmo}";
                Color ammoColor = launcher.AmmoCount <= 0 ? Color.Red : Color.LimeGreen;
                Vector2 textSz = _font.MeasureString(ammoText) * 0.6f;
                _spriteBatch.DrawString(_font, ammoText, launcher.Position - new Vector2(textSz.X / 2f, 25f), 
                                        ammoColor, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }
        }

        // Nhãn HP bar & tên Hồn Thú
        foreach (var monster in _monsters)
        {
            if (monster.Active)
            {
                int hpBarW = (int)(monster.Radius * 1.8f);
                int hpBarH = 4;
                int hpX = (int)monster.Position.X - hpBarW / 2;
                int hpY = (int)monster.Position.Y - (int)(monster.Radius + 6);
                float hpRatio = monster.HP / monster.MaxHP;

                DrawRect(hpX, hpY, hpBarW, hpBarH, Color.Black);
                DrawRect(hpX, hpY, (int)(hpBarW * hpRatio), hpBarH, Color.Red);
                
                string rankName = monster.Name;
                Vector2 labelSize = _font.MeasureString(rankName) * 0.7f;
                _spriteBatch.DrawString(_font, rankName, new Vector2(monster.Position.X - labelSize.X / 2f, hpY - 14), 
                                        Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }

        // Floating texts (Damage numbers, Exp popups)
        foreach (var ft in _floatingTexts)
        {
            float alpha = 1f - (ft.Elapsed / ft.Lifetime);
            _spriteBatch.DrawString(_font, ft.Text, ft.Position, ft.Color * alpha, 
                                    0f, Vector2.Zero, ft.Scale, SpriteEffects.None, 0f);
        }

        _spriteBatch.End();

        // ====================================================================
        // PASS 3: RENDER SCREEN SPACE UI/HUD (LinearClamp, Tọa độ màn hình tĩnh)
        // ====================================================================
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, null);

        // Vẽ HUD góc trên trái
        DrawHUD();

        // Vẽ túi đồ nếu đang mở
        if (_isInventoryOpen)
        {
            DrawInventoryUI();
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // ====================================================================
    // MOVEMENT & KEYBOARD
    // ====================================================================

    private void UpdatePlayerMovement(KeyboardState keys, float deltaTime)
    {
        if (_player.Cultivation.CurrentState == CultivationState.Dead || 
            _player.Cultivation.CurrentState == CultivationState.AbsorbingRing)
            return;

        float speed = 180f;
        Vector2 dir = Vector2.Zero;

        if (keys.IsKeyDown(Keys.W) || keys.IsKeyDown(Keys.Up)) dir.Y -= 1f;
        if (keys.IsKeyDown(Keys.S) || keys.IsKeyDown(Keys.Down)) dir.Y += 1f;
        if (keys.IsKeyDown(Keys.A) || keys.IsKeyDown(Keys.Left)) dir.X -= 1f;
        if (keys.IsKeyDown(Keys.D) || keys.IsKeyDown(Keys.Right)) dir.X += 1f;

        if (dir != Vector2.Zero)
        {
            dir.Normalize();
            _player.PositionX += dir.X * speed * deltaTime;
            _player.PositionY += dir.Y * speed * deltaTime;

            _player.PositionX = Math.Clamp(_player.PositionX, 16f, MAP_WIDTH - 16f);
            _player.PositionY = Math.Clamp(_player.PositionY, 16f, MAP_HEIGHT - 16f);

            // Thêm bụi di chuyển chân người chơi
            if (_random.NextDouble() < 0.15)
            {
                var dustPos = new Vector2(_player.PositionX + _random.Next(-6, 6), _player.PositionY + 12);
                var dustVel = new Vector2(-dir.X * 25f + _random.Next(-5, 5), -dir.Y * 10f + _random.Next(-5, 5));
                _particles.Add(new Particle(dustPos, dustVel, new Color(130, 115, 100, 150), _random.Next(2, 4), 0.6f, ParticleType.Dust));
            }
        }
    }

    private void HandleInput(KeyboardState keys, MouseState mouse)
    {
        Vector2 mouseWorldPos = mouse.Position.ToVector2() + _cameraPosition;

        if (IsKeyJustPressed(keys, Keys.M))
        {
            var cult = _player.Cultivation;
            if (cult.CurrentState == CultivationState.Meditating)
                cult.StopMeditating();
            else
                cult.StartMeditating();
        }

        if (IsKeyJustPressed(keys, Keys.Space))
        {
            if (_player.Cultivation.CurrentState == CultivationState.AbsorbingRing)
            {
                _player.Cultivation.QTEPressCount++;
                SpawnElementalBurst(new Vector2(_player.PositionX, _player.PositionY), Element.None, 5);
            }
            else
            {
                _player.Cultivation.AddExp(500f);
                _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "+500 EXP (Cheat)", Color.SkyBlue));
            }
        }

        if (IsKeyJustPressed(keys, Keys.I))
        {
            _isInventoryOpen = !_isInventoryOpen;
            Console.WriteLine($"[Giao Diện] Bật/Tắt túi đồ: " + (_isInventoryOpen ? "MỞ" : "ĐÓNG"));
        }

        if (IsKeyJustPressed(keys, Keys.E))
        {
            CycleEquippedWeapon();
        }

        if (IsKeyJustPressed(keys, Keys.R))
        {
            TryAbsorbNearestSoulRing();
        }

        // --- Bấm phím [F5] để Lưu game vào MySQL ---
        if (IsKeyJustPressed(keys, Keys.F5))
        {
            if (_dbManager.IsConnected)
            {
                bool saved = _dbManager.SavePlayerState(_player, _monsters, _droppedItems, _launchers, _soulRings);
                if (saved)
                {
                    _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 40), "Game Saved to MySQL!", Color.Lime, 2.0f, 1.1f));
                }
                else
                {
                    _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 40), "Save Failed!", Color.Red));
                }
            }
            else
            {
                _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 40), "MySQL Offline - Cannot Save!", Color.OrangeRed));
            }
        }

        // --- Bấm phím [F9] để Tải game từ MySQL ---
        if (IsKeyJustPressed(keys, Keys.F9))
        {
            if (_dbManager.IsConnected)
            {
                string monstersJson, droppedItemsJson, launchersJson, soulRingsJson;
                bool loaded = _dbManager.LoadPlayerState(_player, _dataLoader, out monstersJson, out droppedItemsJson, out launchersJson, out soulRingsJson);
                if (loaded)
                {
                    // 1. Dọn dẹp thế giới hiện tại
                    _monsters.Clear();
                    _soulRings.Clear();
                    _launchers.Clear();
                    _droppedItems.Clear();
                    _projectilePool.Clear();
                    _particles.Clear();

                    // 2. Phục hồi quái vật (Hồn thú)
                    if (!string.IsNullOrEmpty(monstersJson))
                    {
                        try
                        {
                            var list = JsonSerializer.Deserialize<List<SavedMonsterData>>(monstersJson);
                            if (list != null)
                            {
                                foreach (var data in list)
                                {
                                    if (Enum.TryParse<Element>(data.element, true, out var elem))
                                    {
                                        var m = new Monster(data.name, data.age, data.maxHp, new Vector2(data.x, data.y), elem);
                                        m.HP = data.hp;
                                        m.MaxHP = data.maxHp;
                                        m.BaseMaxHP = data.maxHp / (1f + data.age / 1500f);
                                        
                                        // Gán lại sự kiện OnKilled
                                        m.OnKilled += monster =>
                                        {
                                            TriggerShake(0.35f, 6.0f);
                                            SpawnElementalBurst(monster.Position, monster.Element, 25);
                                            var ring = new SoulRingEntity(monster.Position, monster.Age, monster.Element);
                                            _soulRings.Add(ring);
                                            _floatingTexts.Add(new FloatingText(monster.Position, $"Dropped Soul Ring {monster.Age}N ({monster.Element})!", Color.Yellow, 2.5f, 1.2f));
                                            _monsters.Remove(monster);
                                        };
                                        _monsters.Add(m);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Lỗi Tải Game] Không thể phục hồi Hồn Thú: {ex.Message}");
                        }
                    }

                    // 3. Phục hồi vật phẩm rơi
                    if (!string.IsNullOrEmpty(droppedItemsJson))
                    {
                        try
                        {
                            var list = JsonSerializer.Deserialize<List<SavedDroppedItemData>>(droppedItemsJson);
                            if (list != null)
                            {
                                foreach (var data in list)
                                {
                                    var item = new DroppedItem(new Vector2(data.x, data.y), data.itemId, data.name, data.type, data.quantity);
                                    _droppedItems.Add(item);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Lỗi Tải Game] Không thể phục hồi vật phẩm rơi: {ex.Message}");
                        }
                    }

                    // 4. Phục hồi bệ phóng tự động (turrets)
                    if (!string.IsNullOrEmpty(launchersJson))
                    {
                        try
                        {
                            var list = JsonSerializer.Deserialize<List<SavedLauncherData>>(launchersJson);
                            if (list != null)
                            {
                                foreach (var data in list)
                                {
                                    var l = new AutoLauncher(new Vector2(data.x, data.y), data.type);
                                    l.AmmoCount = data.ammo;
                                    l.MaxAmmo = data.maxAmmo;
                                    _launchers.Add(l);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Lỗi Tải Game] Không thể phục hồi bệ phóng: {ex.Message}");
                        }
                    }

                    // 5. Phục hồi hồn hoàn rơi trên đất
                    if (!string.IsNullOrEmpty(soulRingsJson))
                    {
                        try
                        {
                            var list = JsonSerializer.Deserialize<List<SavedSoulRingData>>(soulRingsJson);
                            if (list != null)
                            {
                                foreach (var data in list)
                                {
                                    if (Enum.TryParse<Element>(data.element, true, out var elem))
                                    {
                                        var r = new SoulRingEntity(new Vector2(data.x, data.y), data.age, elem);
                                        _soulRings.Add(r);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Lỗi Tải Game] Không thể phục hồi hồn hoàn: {ex.Message}");
                        }
                    }

                    _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 40), "Game Loaded from MySQL!", Color.Cyan, 2.0f, 1.1f));
                }
                else
                {
                    _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 40), "Load Failed!", Color.Red));
                }
            }
            else
            {
                _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 40), "MySQL Offline - Cannot Load!", Color.OrangeRed));
            }
        }

        // --- Bấm phím [Q] để kích hoạt Hồn kỹ 1 ---
        if (IsKeyJustPressed(keys, Keys.Q))
        {
            TriggerActiveSkill(1, mouseWorldPos);
        }

        // --- Bấm phím [W] để kích hoạt Hồn kỹ 2 ---
        if (IsKeyJustPressed(keys, Keys.W))
        {
            TriggerActiveSkill(2, mouseWorldPos);
        }

        // --- Bấm phím [T] để đặt bệ phóng ám khí tự động (Auto-Turret) ---
        if (IsKeyJustPressed(keys, Keys.T))
        {
            PlaceAutoLauncher();
        }

        // --- Bấm phím [F] để reload bệ phóng ám khí gần nhất ---
        if (IsKeyJustPressed(keys, Keys.F))
        {
            TryReloadNearestTurret();
        }

        // --- Bấm phím [Y] để chuyển đổi loại bệ phóng đặt tiếp theo ---
        if (IsKeyJustPressed(keys, Keys.Y))
        {
            _nextTurretTypeToPlace = _nextTurretTypeToPlace % 3 + 1;
            string turretName = _nextTurretTypeToPlace switch
            {
                1 => "Vo Thanh Tu Tien",
                2 => "Chu Cat Than No",
                3 => "Ham Sa Xa Anh",
                _ => "Unknown"
            };
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), $"Next Turret: {turretName}", Color.Gold));
            Console.WriteLine($"[Cơ Quan] Chọn loại bệ phóng tiếp theo: {turretName}");
        }

        // --- Bấm phím [U] để mở khóa Bát Chu Mâu lập tức ---
        if (IsKeyJustPressed(keys, Keys.U))
        {
            _player.Cultivation.UnlockBatChuMau();
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 40), "Bat Chu Mau Unlocked! (Cheat)", Color.Magenta));
        }

        // --- Bấm phím [H] để tạo Hồn Hoàn 99k năm ---
        if (IsKeyJustPressed(keys, Keys.H))
        {
            Vector2 playerPos = new Vector2(_player.PositionX, _player.PositionY);
            var ring = new SoulRingEntity(playerPos, 99000, Element.Wood);
            _soulRings.Add(ring);
            _floatingTexts.Add(new FloatingText(playerPos - new Vector2(0, 15), "Spawned 99k-year Ring! (Cheat)", Color.Yellow, 2f));
        }

        // Sử dụng phím tắt 1->5
        for (int i = 0; i < 5; i++)
        {
            Keys key = Keys.D1 + i;
            if (IsKeyJustPressed(keys, key))
            {
                var items = _player.Inventory.Items;
                if (i < items.Count)
                {
                    string itemId = items[i].ItemId;
                    string nameHUD = GetHUDItemName(itemId);
                    bool success = _player.Inventory.UseItem(itemId, _player);
                    if (success)
                    {
                        _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), $"Dung Vat Pham: {nameHUD}", Color.Lime));
                    }
                }
            }
        }

        // Click chuột trái bắn đạn thường
        if (mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            bool clickInInventory = _isInventoryOpen && mouse.X >= 510 && mouse.Y <= 520;
            
            if (!clickInInventory &&
                _player.Cultivation.CurrentState != CultivationState.Dead && 
                _player.Cultivation.CurrentState != CultivationState.AbsorbingRing)
            {
                FireActiveWeapon(mouseWorldPos);
            }
        }

        // Click chuột phải spawn quái
        if (mouse.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
        {
            SpawnRandomMonster(mouseWorldPos);
        }
    }

    // ====================================================================
    // ADVANCED COMBAT & TURRETS IMPLEMENTATIONS
    // ====================================================================

    private void TriggerActiveSkill(int skillNumber, Vector2 targetPos)
    {
        var cult = _player.Cultivation;
        if (cult.CurrentState == CultivationState.Dead) return;

        SoulSkill? skill = skillNumber == 1 ? cult.Skill1 : cult.Skill2;

        if (skill == null)
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 35), $"Hon ky {skillNumber} chua mo khoa!", Color.OrangeRed));
            return;
        }

        // Kiểm tra Hồn Lực SP
        if (cult.SoulPower < skill.SPCost)
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 35), "Khong du Hon luc (SP)!", Color.Red));
            return;
        }

        // Tiêu hao SP và kích hoạt bắn đạn hồn kỹ đặc biệt
        cult.ConsumeSoulPower(skill.SPCost);
        CastSoulSkillProjectiles(skill, targetPos);

        _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 55), $"Kich Hoat: {skill.Name}!", Color.YellowGreen, 1.5f, 1.2f));
    }

    private void CastSoulSkillProjectiles(SoulSkill skill, Vector2 targetPos)
    {
        Vector2 playerCenter = new Vector2(_player.PositionX, _player.PositionY);
        Vector2 dir = targetPos - playerCenter;
        if (dir == Vector2.Zero) dir = new Vector2(1, 0);
        else dir.Normalize();

        float baseAngle = (float)Math.Atan2(dir.Y, dir.X);

        if (skill.Name == "Lam Ngan Quan Quanh")
        {
            // Bắn 3 tia độc hệ Wood tốc độ cao sát thương 60
            float spread = 0.15f;
            float startAngle = baseAngle - spread;
            for (int i = 0; i < 3; i++)
            {
                float angle = startAngle + spread * i;
                Vector2 bulletDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                _projectilePool.Spawn(playerCenter, bulletDir, skill.Damage, 350f, 500f, Element.Wood);
            }
        }
        else if (skill.Name == "Phuong Hoang Hoa Tuyen")
        {
            // Bắn chùm lửa liên tiếp 8 phát (Fire) sát thương 100
            for (int i = 0; i < 8; i++)
            {
                float angle = baseAngle + (float)(_random.NextDouble() - 0.5) * 0.08f;
                Vector2 bulletDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 spawnOffset = bulletDir * (i * 8f);
                _projectilePool.Spawn(playerCenter + spawnOffset, bulletDir, skill.Damage, 400f, 450f, Element.Fire);
            }
        }
        else if (skill.Name == "Bang Tam Ket Gioi")
        {
            // Tạo khiên băng bắn ra 12 tia xung quanh 360 độ (Ice) sát thương 80
            float step = (float)(Math.PI * 2 / 12);
            for (int i = 0; i < 12; i++)
            {
                float angle = step * i;
                Vector2 bulletDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                _projectilePool.Spawn(playerCenter, bulletDir, skill.Damage, 250f, 300f, Element.Ice);
            }
        }
        else if (skill.Name == "Lam Ngan Tu Lung")
        {
            // Mộc Hồn hoàn 2: Cùm trói diện rộng (5 tia bắn cực mạnh, sát thương 150)
            float spread = 0.4f;
            float step = spread / 4f;
            float startAngle = baseAngle - spread / 2f;
            for (int i = 0; i < 5; i++)
            {
                float angle = startAngle + step * i;
                Vector2 bulletDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                _projectilePool.Spawn(playerCenter, bulletDir, skill.Damage, 380f, 480f, Element.Wood);
            }
        }
        else if (skill.Name == "Phuong Hoang Huyen Oa")
        {
            // Hỏa Hồn hoàn 2: Vòng xoáy lửa địa ngục (5 cầu lửa sát thương 200)
            float spread = 0.5f;
            float step = spread / 4f;
            float startAngle = baseAngle - spread / 2f;
            for (int i = 0; i < 5; i++)
            {
                float angle = startAngle + step * i;
                Vector2 bulletDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                _projectilePool.Spawn(playerCenter, bulletDir, skill.Damage, 300f, 360f, Element.Fire);
            }
        }
        else if (skill.Name == "Huyen Bang Xung Kich")
        {
            // Băng Hồn hoàn 2: Sóng xung kích huyền băng (10 tia quạt rộng sát thương 160)
            float spread = 0.7f;
            float step = spread / 9f;
            float startAngle = baseAngle - spread / 2f;
            for (int i = 0; i < 10; i++)
            {
                float angle = startAngle + step * i;
                Vector2 bulletDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                _projectilePool.Spawn(playerCenter, bulletDir, skill.Damage, 320f, 420f, Element.Ice);
            }
        }
        else
        {
            // Kỹ năng mặc định
            _projectilePool.Spawn(playerCenter, dir, skill.Damage, 300f, 350f, skill.Element);
        }

        Console.WriteLine($"[Hồn Kỹ] ⚡ Đường Tam thi triển: {skill.Name} (Sát thương: {skill.Damage}, Hồn lực: {skill.SPCost})");
    }

    private void PlaceAutoLauncher()
    {
        if (_player.Cultivation.CurrentState == CultivationState.Dead) return;

        // Giới hạn tối đa 3 bệ phóng ám khí
        if (_launchers.Count >= 3)
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "Dat Toi Da 3 Be Phong!", Color.OrangeRed));
            Console.WriteLine("[Cơ Quan] Cảnh báo: Đã đạt giới hạn tối đa 3 bệ phóng ám khí!");
            return;
        }

        // Kiểm tra xem người chơi có đạn Thiết Mẫu không
        var ammoItem = _player.Inventory.Items.FirstOrDefault(i => i.ItemId == "item_thiet_mau_01");
        if (ammoItem == null || ammoItem.Quantity <= 0)
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "Can 1 Thiet Mau Ammo de dat!", Color.Red));
            Console.WriteLine("[Cơ Quan] Thất bại: Không có đạn Thiết Mẫu trong túi đồ để đặt bệ phóng!");
            return;
        }

        // Tiêu hao 1 Thiết Mẫu
        _player.Inventory.RemoveItem("item_thiet_mau_01", 1);

        Vector2 playerPos = new Vector2(_player.PositionX, _player.PositionY);
        var turret = new AutoLauncher(playerPos, _nextTurretTypeToPlace);
        _launchers.Add(turret);

        // Hiệu ứng khói bụi xẹt lửa khi đặt bệ phóng cơ quan
        TriggerShake(0.15f, 3.0f);
        SpawnElementalBurst(playerPos, Element.None, 15);

        string turretName = _nextTurretTypeToPlace switch
        {
            1 => "Vo Thanh Tu Tien",
            2 => "Chu Cat Than No",
            3 => "Ham Sa Xa Anh",
            _ => "Turret"
        };

        _floatingTexts.Add(new FloatingText(playerPos - new Vector2(0, 15), $"+ {turretName}", Color.Gold));
        Console.WriteLine($"[Cơ Quan] ⚙ Đã đặt bệ phóng {turretName} tại vị trí {playerPos.X:F0},{playerPos.Y:F0} (Tổng số: {_launchers.Count}/3).");
    }

    private void TryReloadNearestTurret()
    {
        if (_player.Cultivation.CurrentState == CultivationState.Dead) return;

        Vector2 playerPos = new Vector2(_player.PositionX, _player.PositionY);
        
        // Find nearest turret within 60px
        var nearestTurret = _launchers
            .Where(t => t.Active && Vector2.Distance(playerPos, t.Position) <= 60f)
            .OrderBy(t => Vector2.Distance(playerPos, t.Position))
            .FirstOrDefault();

        if (nearestTurret == null)
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "Khong co Be Phong gan day!", Color.Gray));
            return;
        }

        if (nearestTurret.AmmoCount >= nearestTurret.MaxAmmo)
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "Be Phong da day dan!", Color.Yellow));
            return;
        }

        // Check if player has Thiết Mẫu Ammo
        var ammoItem = _player.Inventory.Items.FirstOrDefault(i => i.ItemId == "item_thiet_mau_01");
        if (ammoItem == null || ammoItem.Quantity <= 0)
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "Khong du Thiet Mau Ammo!", Color.Red));
            return;
        }

        // Consume 1 Thiết Mẫu and reload
        _player.Inventory.RemoveItem("item_thiet_mau_01", 1);
        nearestTurret.Reload();
        
        _floatingTexts.Add(new FloatingText(nearestTurret.Position - new Vector2(0, 20), "RELOADED!", Color.LimeGreen));
        Console.WriteLine($"[Cơ Quan] ⚙ Đã nạp lại đạn cho Bệ phóng tại {nearestTurret.Position.X:F0},{nearestTurret.Position.Y:F0}. Tiêu hao 1 Thiết Mẫu.");
    }

    private void FireActiveWeapon(Vector2 targetPos)
    {
        Vector2 playerCenter = new Vector2(_player.PositionX, _player.PositionY);
        Vector2 dir = targetPos - playerCenter;

        if (dir == Vector2.Zero) dir = new Vector2(1, 0);
        else dir.Normalize();

        var weapon = _player.Inventory.EquippedWeapon;

        float damage = 25f;
        float range = 250f;
        int count = 1;
        Element element = Element.None;

        if (weapon != null)
        {
            damage = weapon.CombatStats?.BaseDamage ?? 25f;
            range = (weapon.CombatStats?.Range ?? 25f) * 8f;
            count = weapon.CombatStats?.ProjectileCount ?? 1;

            if (weapon.ItemId == "am_khi_tu_tien_01")
            {
                element = Element.Wood;
            }
            else if (weapon.ItemId == "am_khi_chu_cat_02")
            {
                element = Element.Fire;
            }
        }

        float baseAngle = (float)Math.Atan2(dir.Y, dir.X);
        float speed = 400f;

        if (count == 1)
        {
            _projectilePool.Spawn(playerCenter, dir, damage, range, speed, element);
        }
        else
        {
            float spreadAngle = count == 16 ? 0.6f : 0.2f;
            float step = spreadAngle / (count - 1);
            float startAngle = baseAngle - spreadAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;
                Vector2 bulletDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                _projectilePool.Spawn(playerCenter, bulletDir, damage, range, speed, element);
            }
        }

        string wHUDName = GetHUDItemName(weapon?.ItemId ?? string.Empty);
        Console.WriteLine($"[Chiến Đấu] ⚔ Phóng {count} tia ám khí {wHUDName} (Hệ: {element})");
    }

    private void SpawnRandomMonster(Vector2 spawnPos)
    {
        int monsterType = _random.Next(3);

        string name;
        int age;
        float hp;
        Element elem;

        switch (monsterType)
        {
            case 0:
                name = "Lam Ngan Thao";
                age = 90; // Tạo quái nhỏ để test tiến hóa sinh trưởng lên Bách Niên
                hp = 180f;
                elem = Element.Wood;
                break;
            case 1:
                name = "Hoa Ke";
                age = 800; // Sắp đạt Thiên Niên
                hp = 400f;
                elem = Element.Fire;
                break;
            case 2:
            default:
                name = "Bang Tam";
                age = 8000; // Sắp đạt Vạn Niên
                hp = 900f;
                elem = Element.Ice;
                break;
        }

        var m = new Monster(name, age, hp, spawnPos, elem);
        
        m.OnKilled += monster =>
        {
            // Rung lắc màn hình lớn và vụ nổ năng lượng bộc phát
            TriggerShake(0.35f, 6.0f);
            SpawnElementalBurst(monster.Position, monster.Element, 25);

            // Rơi Hồn Hoàn lưu trữ tuổi thọ và hệ thuộc tính của quái vật!
            var ring = new SoulRingEntity(monster.Position, monster.Age, monster.Element);
            _soulRings.Add(ring);

            _floatingTexts.Add(new FloatingText(monster.Position, $"Dropped Soul Ring {monster.Age}N ({monster.Element})!", Color.Yellow, 2.5f, 1.2f));
            _monsters.Remove(monster);
        };

        _monsters.Add(m);
        _floatingTexts.Add(new FloatingText(spawnPos, $"Spawn: {m.Name} ({age}N)", Color.Tomato));
        Console.WriteLine($"[Hệ Thống] Đã sinh Hồn Thú '{m.Name}' {age} năm ({elem}) tại {spawnPos}");
    }

    private void HandleProjectileCollisions()
    {
        foreach (var proj in _projectilePool.Projectiles)
        {
            if (!proj.Active) continue;

            foreach (var monster in _monsters)
            {
                if (!monster.Active) continue;

                if (monster.CheckCollision(proj.Position))
                {
                    proj.Active = false;

                    // Apply aggro if projectile is not silent
                    if (!proj.IsSilent)
                    {
                        monster.IsAggroed = true;
                    }

                    // Apply root if Wood skill and monster is Speed role
                    if (proj.Element == Element.Wood && proj.Damage > 30f)
                    {
                        float rootDur = (monster.Role == "Speed") ? 3.5f : 1.5f;
                        monster.RootTimer = rootDur;
                        _floatingTexts.Add(new FloatingText(monster.Position - new Vector2(0, 30), $"ROOTED ({rootDur:F1}s) VN!", Color.LimeGreen, 1.5f));
                    }

                    // Apply poison if player has Bát Chu Mâu (25% chance on hit)
                    if (_player.Cultivation.HasBatChuMau && _random.NextDouble() < 0.25)
                    {
                        monster.PoisonTimer = 5.0f;
                        _floatingTexts.Add(new FloatingText(monster.Position - new Vector2(0, 45), "POISON VN!", Color.Purple, 1.2f));
                    }

                    monster.TakeDamage(proj.Damage, proj.Element, out float finalDmg, out bool isCounter);

                    Color txtColor = isCounter ? Color.Red : Color.Orange;
                    string dmgText = isCounter ? $"-{finalDmg:F0} COUNTER!" : $"-{finalDmg:F0}";
                    _floatingTexts.Add(new FloatingText(monster.Position - new Vector2(0, 15), dmgText, txtColor, 1.3f, isCounter ? 1.25f : 1.0f));

                    // Rung giật màn hình và sinh tia nổ thuộc tính va chạm
                    TriggerShake(isCounter ? 0.18f : 0.1f, isCounter ? 4.5f : 2.5f);
                    SpawnElementalBurst(proj.Position, proj.Element, isCounter ? 12 : 6);
                    break;
                }
            }
        }
    }

    private void TryAbsorbNearestSoulRing()
    {
        var cult = _player.Cultivation;
        if (cult.CurrentState != CultivationState.BreakthroughReady && cult.CurrentState != CultivationState.Idle)
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "Chua den binh canh!", Color.OrangeRed));
            return;
        }

        Vector2 playerPos = new Vector2(_player.PositionX, _player.PositionY);
        
        var ring = _soulRings
            .Where(r => r.Active && Vector2.Distance(playerPos, r.Position) <= 60f)
            .OrderBy(r => Vector2.Distance(playerPos, r.Position))
            .FirstOrDefault();

        if (ring != null)
        {
            _absorbingRingPosition = ring.Position;
            ring.Active = false;
            _soulRings.Remove(ring);

            // Truyền cả TUỔI và HỆ của hồn thú để đột phá hồn kỹ tương ứng
            cult.StartAbsorbingSoulRing(ring.Age, ring.Element);
        }
        else
        {
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "Khong co Hon Hoan gan day!", Color.Gray));
        }
    }

    private void CycleEquippedWeapon()
    {
        var weapons = _player.Inventory.Items.Where(i => i.Type == "HIDDEN_WEAPON").ToList();
        if (weapons.Count <= 1) return;

        var current = _player.Inventory.EquippedWeapon;
        int index = 0;
        
        if (current != null)
        {
            var currItem = weapons.FirstOrDefault(w => w.ItemId == current.ItemId);
            if (currItem != null)
            {
                index = (weapons.IndexOf(currItem) + 1) % weapons.Count;
            }
        }

        _player.Inventory.EquipWeapon(weapons[index].ItemId);
        string nameHUD = GetHUDItemName(weapons[index].ItemId);
        _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 35), $"Equip: {nameHUD}", Color.Gold));
    }

    // ====================================================================
    // METADATA HUD
    // ====================================================================

    private string GetHUDItemName(string itemId)
    {
        return itemId switch
        {
            "food_huong_trang_01" => "Sausage (Phuc Hoi)",
            "am_khi_tu_tien_01" => "Vo Thanh Tu Tien",
            "am_khi_chu_cat_02" => "Chu Cat Than No",
            "item_thiet_mau_01" => "Thiet Mau Ammo",
            _ => "Tay khong"
        };
    }

    private string GetRealmHUDName(CultivationRealm realm)
    {
        return realm switch
        {
            CultivationRealm.HonSi => "Hon Si",
            CultivationRealm.HonSu => "Hon Su",
            CultivationRealm.DaiHonSu => "Dai Hon Su",
            CultivationRealm.HonTon => "Hon Ton",
            CultivationRealm.HonTong => "Hon Tong",
            CultivationRealm.HonVuong => "Hon Vuong",
            CultivationRealm.HonDe => "Hon De",
            CultivationRealm.HonThanh => "Hon Thanh",
            CultivationRealm.HonDauLa => "Hon Dau La",
            CultivationRealm.PhongHaoDauLa => "Phong Hao Dau La",
            CultivationRealm.Than => "Than",
            _ => "Khong Ro"
        };
    }

    // ====================================================================
    // HUD & INVENTORY
    // ====================================================================

    private void DrawHUD()
    {
        var cult = _player.Cultivation;
        int barW = 200;
        int barH = 12;
        int startX = 20;
        int startY = 15;
        int spacing = 18;

        // Vẽ Khung đen nền HUD (Chứa beveled border hiệu ứng 3D)
        DrawRect(startX - 10, startY - 5, 305, 155, new Color(20, 22, 38, 220));
        DrawRect(startX - 10, startY - 5, 305, 155, new Color(65, 75, 110), true);
        DrawRect(startX - 9, startY - 4, 303, 1, new Color(100, 115, 160, 150)); // top inner highlight

        // Tên và cảnh giới
        string realmHUD = GetRealmHUDName(cult.CurrentRealm);
        _spriteBatch.DrawString(_font, $"Duong Tam | {realmHUD} (Cap {cult.CurrentLevel})", 
                                new Vector2(startX, startY), Color.Gold);

        // HP bar (Premium render)
        int hpY = startY + spacing + 6;
        float hpRatio = cult.HP / cult.MaxHP;
        DrawPremiumBar(startX, hpY, barW, barH, hpRatio, new Color(50, 15, 15), new Color(230, 45, 45), new Color(120, 40, 40), $"HP: {cult.HP:F0}/{cult.MaxHP:F0}", Color.Tomato);

        // EXP bar (Premium render)
        int expY = hpY + spacing;
        float expRatio = cult.CurrentExp / cult.MaxExpForCurrentLevel;
        expRatio = Math.Clamp(expRatio, 0f, 1f);
        DrawPremiumBar(startX, expY, barW, barH, expRatio, new Color(15, 15, 50), new Color(60, 130, 255), new Color(40, 90, 180), $"EXP: {cult.CurrentExp:F0}/{cult.MaxExpForCurrentLevel:F0}", Color.LightSkyBlue);

        // Hồn Lực bar (Premium render)
        int spY = expY + spacing;
        float spRatio = cult.SoulPower / cult.MaxSoulPower;
        DrawPremiumBar(startX, spY, barW, barH, spRatio, new Color(10, 45, 20), new Color(45, 210, 110), new Color(30, 130, 70), $"SP: {cult.SoulPower:F0}/{cult.MaxSoulPower:F0}", Color.MediumSpringGreen);

        // Ám khí hiện tại + Danh sách bệ phóng
        int wY = spY + spacing;
        string activeWeaponName = GetHUDItemName(_player.Inventory.EquippedWeapon?.ItemId ?? string.Empty);
        _spriteBatch.DrawString(_font, $"Am Khi: {activeWeaponName} | Turrets: {_launchers.Count}/3", new Vector2(startX, wY + 5), Color.Khaki, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        // Next turret cycling info
        string nextTurretName = _nextTurretTypeToPlace switch
        {
            1 => "Vo Thanh Tu Tien",
            2 => "Chu Cat Than No",
            3 => "Ham Sa Xa Anh",
            _ => "Unknown"
        };
        _spriteBatch.DrawString(_font, $"Next: {nextTurretName} (Y)", new Vector2(startX, wY + 20), Color.Tan, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

        // Vẽ 9 ô Hồn Hoàn Hấp Thu dưới góc trái HUD
        int ringsStartY = wY + spacing + 12;
        int ringSize = 20;
        int ringSpacing = 26;
        for (int i = 0; i < 9; i++)
        {
            int rx = startX + i * ringSpacing;
            bool activeRing = i < cult.SoulRingsCount;

            DrawRect(rx, ringsStartY, ringSize, ringSize, new Color(25, 25, 45));
            if (activeRing)
            {
                Color ringColor = (i + 1) switch
                {
                    1 => Color.White,
                    2 => Color.Yellow,
                    3 => Color.Purple,
                    4 or 5 or 6 or 7 => new Color(30, 30, 30),
                    8 or 9 => Color.Red,
                    _ => Color.Gray
                };
                
                float pulse = 0.5f + 0.1f * (float)Math.Sin(gameTimeForDraw * 6f);
                _spriteBatch.Draw(_ringTexture, new Vector2(rx + ringSize / 2f, ringsStartY + ringSize / 2f), null, 
                                  ringColor, (float)gameTimeForDraw, new Vector2(32, 32), 0.28f, SpriteEffects.None, 0f);
            }
            DrawRect(rx, ringsStartY, ringSize, ringSize, new Color(75, 85, 120), true);
        }

        // --- CỬA SỔ HIỂN THỊ HỒN KỸ CHỦ ĐỘNG (Q / W) ---
        int skillX = 335;
        int skillY = 15;
        DrawRect(skillX, skillY, 165, 140, new Color(20, 22, 38, 220));
        DrawRect(skillX, skillY, 165, 140, new Color(65, 75, 110), true);
        DrawRect(skillX + 1, skillY + 1, 163, 1, new Color(100, 115, 160, 150)); // top inner highlight
        _spriteBatch.DrawString(_font, "HON KY CHU DONG", new Vector2(skillX + 10, skillY + 8), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        DrawRect(skillX + 8, skillY + 24, 149, 1, new Color(65, 75, 110));

        string qLabel = cult.Skill1 != null ? $"Q: {cult.Skill1.Name}\n   ({cult.Skill1.SPCost} SP)" : "Q: [Chua hoc]";
        Color qColor = cult.Skill1 != null ? Color.MediumSpringGreen : Color.DarkGray;
        _spriteBatch.DrawString(_font, qLabel, new Vector2(skillX + 10, skillY + 32), qColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        string wLabel = cult.Skill2 != null ? $"W: {cult.Skill2.Name}\n   ({cult.Skill2.SPCost} SP)" : "W: [Chua hoc]";
        Color wColor = cult.Skill2 != null ? Color.Gold : Color.DarkGray;
        _spriteBatch.DrawString(_font, wLabel, new Vector2(skillX + 10, skillY + 80), wColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        // Bát Chu Mâu stats banner below skills box
        if (cult.HasBatChuMau)
        {
            DrawRect(skillX, skillY + 145, 165, 30, new Color(30, 15, 45, 220));
            DrawRect(skillX, skillY + 145, 165, 30, Color.Purple, true);
            _spriteBatch.DrawString(_font, "[Bat Chu Mau Passive]", new Vector2(skillX + 8, skillY + 147), Color.Magenta, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, "+50HP +30SP 25% Poison", new Vector2(skillX + 8, skillY + 159), Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        }

        // Cảnh báo khi hấp thu & QTE Willpower bar
        if (cult.CurrentState == CultivationState.AbsorbingRing)
        {
            float pulse = (float)(Math.Sin(gameTimeForDraw * 8.0) * 0.4 + 0.6);
            string warnMsg = "!!! WARNING: ABSORBING SOUL RING - Press [1] to eat Sausage !!!";
            _spriteBatch.DrawString(_font, warnMsg, new Vector2(100, 195), Color.Red * pulse, 0f, Vector2.Zero, 1.05f, SpriteEffects.None, 0f);

            string qteInstructions = "TAP SPACEBAR FAST TO BUILD WILLPOWER!";
            _spriteBatch.DrawString(_font, qteInstructions, new Vector2(100, 220), Color.Gold, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

            float willpowerRatio = Math.Clamp(cult.QTEPressCount * 0.05f, 0f, 1f);
            DrawPremiumBar(100, 245, 250, 15, willpowerRatio, new Color(30, 30, 45), Color.Gold, Color.Orange, $"Willpower: {cult.QTEPressCount} (Buff: +{cult.QTEPressCount * 2}%)", Color.Gold);
        }

        // Vẽ Bảng hướng dẫn ở đáy màn hình
        int guideY = 540;
        DrawRect(20, guideY, 760, 50, new Color(18, 18, 30, 220));
        DrawRect(20, guideY, 760, 50, new Color(55, 60, 85), true);
        DrawRect(21, guideY + 1, 758, 1, new Color(90, 100, 135, 150)); // top inner highlight
        _spriteBatch.DrawString(_font, "[WASD]: Move | [Left Click]: Normal Fire | [Right Click]: Spawn Monster | [T]: Place Turret", new Vector2(35, guideY + 5), Color.Silver, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, "[I]: Inventory | [E]: Switch Weapon | [1-5]: Quick Eat | [R]: Absorb Ring | [Q/W]: Cast Skills | [F]: Reload | [Y]: Cycle Turret | [F5]: Save DB | [F9]: Load DB", new Vector2(35, guideY + 26), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
    }

    private void DrawInventoryUI()
    {
        int startX = 515;
        int startY = 15;
        int width = 265;
        int height = 505;

        // Vẽ Nền
        DrawRect(startX, startY, width, height, new Color(24, 26, 45, 230));
        DrawRect(startX, startY, width, height, new Color(80, 95, 140), true);
        DrawRect(startX + 1, startY + 1, width - 2, 1, new Color(110, 130, 190, 150)); // top inner highlight

        // Vẽ Tiêu đề
        _spriteBatch.DrawString(_font, "TUI DO (INVENTORY)", new Vector2(startX + 38, startY + 15), Color.Gold, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
        DrawRect(startX + 15, startY + 40, width - 30, 2, new Color(80, 95, 140));

        // Vẽ slots
        var items = _player.Inventory.Items;
        int slotStartX = startX + 15;
        int slotStartY = startY + 55;
        int slotHeight = 65;
        int maxSlots = 6;

        for (int i = 0; i < maxSlots; i++)
        {
            int sY = slotStartY + i * (slotHeight + 8);
            DrawRect(slotStartX, sY, width - 30, slotHeight, new Color(15, 16, 28));

            if (i < items.Count)
            {
                var item = items[i];
                bool isEquipped = _player.Inventory.EquippedWeapon != null && 
                                  _player.Inventory.EquippedWeapon.ItemId == item.ItemId;

                if (isEquipped)
                {
                    DrawRect(slotStartX, sY, width - 30, slotHeight, Color.LimeGreen * 0.15f);
                    DrawRect(slotStartX, sY, width - 30, slotHeight, Color.Gold, true);
                }
                else
                {
                    DrawRect(slotStartX, sY, width - 30, slotHeight, new Color(60, 70, 100), true);
                }

                Texture2D? iconTex = item.Type switch
                {
                    "CONSUMABLE" when item.ItemId == "item_thiet_mau_01" => _needleTexture,
                    "CONSUMABLE" => _sausageTexture,
                    "HIDDEN_WEAPON" when item.ItemId == "am_khi_tu_tien_01" => _needleTexture,
                    "HIDDEN_WEAPON" when item.ItemId == "am_khi_chu_cat_02" => _boltTexture,
                    _ => _sausageTexture
                };

                if (iconTex != null)
                {
                    float maxIconDim = 32f;
                    float iconScale = maxIconDim / Math.Max(iconTex.Width, iconTex.Height);
                    _spriteBatch.Draw(iconTex, new Vector2(slotStartX + 25, sY + slotHeight / 2f), null, Color.White, 0f, 
                                      new Vector2(iconTex.Width / 2f, iconTex.Height / 2f), iconScale, SpriteEffects.None, 0f);
                }

                string nameHUD = GetHUDItemName(item.ItemId);
                string numKey = $"[{i + 1}] ";
                _spriteBatch.DrawString(_font, numKey + nameHUD, new Vector2(slotStartX + 52, sY + 8), isEquipped ? Color.Gold : Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, $"SL: {item.Quantity}", new Vector2(slotStartX + 52, sY + 28), Color.LimeGreen, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                
                string typeHUD = item.Type == "CONSUMABLE" ? "Duoc pham / Thuc pham" : "Am Khi (Weapon)";
                _spriteBatch.DrawString(_font, typeHUD, new Vector2(slotStartX + 52, sY + 44), Color.Gray, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);
            }
            else
            {
                DrawRect(slotStartX, sY, width - 30, slotHeight, new Color(40, 43, 60), true);
                _spriteBatch.DrawString(_font, "Slot Trong", new Vector2(slotStartX + 52, sY + 25), Color.DarkGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            }
        }
    }

    private void DrawRect(int x, int y, int width, int height, Color color, bool borderOnly = false)
    {
        if (width <= 0 || height <= 0) return;

        if (borderOnly)
        {
            int t = 1;
            DrawRect(x, y, width, t, color);
            DrawRect(x, y + height - t, width, t, color);
            DrawRect(x, y, t, height, color);
            DrawRect(x + width - t, y, t, height, color);
        }
        else
        {
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, width, height), color);
        }
    }

    private double gameTimeForDraw;
    
    private void UpdateWindowTitle()
    {
        var cult = _player.Cultivation;
        string stateVN = cult.CurrentState switch
        {
            CultivationState.Idle => "Nhàn Rỗi",
            CultivationState.Meditating => "Minh Tưởng ✦",
            CultivationState.BreakthroughReady => "⚠ CẦN HỒN HOÀN",
            CultivationState.AbsorbingRing => "⚡ ĐANG HẤP THU",
            CultivationState.Dead => "☠ TỬ VONG",
            _ => "???"
        };

        string realm = CultivationComponent.GetRealmDisplayName(cult.CurrentRealm);
        gameTimeForDraw = _gameTimeManager.TotalGameDays * 24.0 * 60.0;

        Window.Title = $"Đấu La | Duong Tam | {realm} Cấp {cult.CurrentLevel} | " +
                       $"HP: {cult.HP:F0}/{cult.MaxHP:F0} | " +
                       $"SP: {cult.SoulPower:F0}/{cult.MaxSoulPower:F0} | " +
                       $"HH: {cult.SoulRingsCount}/9 | " +
                       $"[{stateVN}] | {_gameTimeManager.GetFormattedTime()}";
    }

    // ====================================================================
    // DATA & PRE-LOADS
    // ====================================================================

    private void LoadGameData()
    {
        // Tải trước dữ liệu mẫu từ tệp JSON tĩnh để phòng hờ trường hợp MySQL offline
        var localWeapons = _dataLoader.LoadHiddenWeapons();
        var localConsumables = _dataLoader.LoadConsumables();

        if (_dbManager != null && _dbManager.IsConnected)
        {
            // Thực hiện di cư (Migrate) dữ liệu tĩnh từ JSON vào MySQL nếu các bảng trong MySQL trống
            _dbManager.MigrateJsonToMySql(localConsumables, localWeapons);

            // Nạp dữ liệu trực tiếp từ MySQL làm nguồn chính thức (Source of truth)
            _hiddenWeapons = _dbManager.LoadHiddenWeaponsFromDb();
            _consumables = _dbManager.LoadConsumablesFromDb();

            // Nếu nạp từ MySQL bị trống (lỗi hiếm gặp), sử dụng dữ liệu JSON tĩnh làm dự phòng
            if (_hiddenWeapons == null || _hiddenWeapons.Count == 0) _hiddenWeapons = localWeapons;
            if (_consumables == null || _consumables.Count == 0) _consumables = localConsumables;
        }
        else
        {
            // Dự phòng: Nạp trực tiếp từ JSON khi MySQL offline
            _hiddenWeapons = localWeapons;
            _consumables = localConsumables;
            Console.WriteLine("[Hệ Thống] Đã nạp dữ liệu từ file JSON tĩnh (Offline Mode).");
        }
    }

    private void GiveInitialInventoryItems()
    {
        var food = _consumables.FirstOrDefault(c => c.ItemId == "food_huong_trang_01");
        if (food != null)
        {
            _player.Inventory.AddConsumable(food, 5);
        }

        var ammo = _consumables.FirstOrDefault(c => c.ItemId == "item_thiet_mau_01");
        if (ammo != null)
        {
            _player.Inventory.AddConsumable(ammo, 5);
        }

        var needle = _hiddenWeapons.FirstOrDefault(w => w.ItemId == "am_khi_tu_tien_01");
        if (needle != null)
        {
            _player.Inventory.AddHiddenWeapon(needle, 1);
        }

        var bolt = _hiddenWeapons.FirstOrDefault(w => w.ItemId == "am_khi_chu_cat_02");
        if (bolt != null)
        {
            _player.Inventory.AddHiddenWeapon(bolt, 1);
        }

        _player.Inventory.EquipWeapon("am_khi_tu_tien_01");
    }

    private bool IsKeyJustPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _previousKeyState.IsKeyUp(key);
    }

    private void SubscribeToEvents()
    {
        _eventManager.Subscribe<OnLevelUpEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ⬆ TĂNG CẤP: {e.PlayerName} Cấp {e.OldLevel} → Cấp {e.NewLevel}");
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 50), "LEVEL UP!", Color.Yellow, 2.0f, 1.2f));
            if (_dbManager.IsConnected)
            {
                _dbManager.SavePlayerState(_player, _monsters, _droppedItems, _launchers, _soulRings);
                _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 70), "Auto-Saved!", Color.Lime * 0.7f, 1.5f, 0.9f));
            }
        });

        _eventManager.Subscribe<OnBottleneckReachedEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ⚠ BÌNH CẢNH: {e.PlayerName} tại Cấp {e.Level} ({e.CurrentRealm}) — Cần Hồn Hoàn!");
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 50), "BINH CANH! CAN HON HOAN", Color.OrangeRed, 3.0f, 1.2f));
        });

        _eventManager.Subscribe<OnBreakthroughSuccessEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ★ ĐỘT PHÁ THÀNH CÔNG: {e.PlayerName} → {e.NewRealm} Cấp {e.NewLevel} (Hồn Hoàn #{e.SoulRingNumber})");
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 60), $"SUCCESS #{e.SoulRingNumber}!", Color.Gold, 3.0f, 1.3f));
            if (_dbManager.IsConnected)
            {
                _dbManager.SavePlayerState(_player, _monsters, _droppedItems, _launchers, _soulRings);
                _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 80), "Auto-Saved!", Color.Lime * 0.7f, 1.5f, 0.9f));
            }
        });

        _eventManager.Subscribe<OnBreakthroughFailedEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ✗ ĐỘT PHÁ THẤT BẠI: {e.PlayerName} — {e.Reason} (Tỷ lệ: {e.SuccessRate:P1})");
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 60), "BREAKTHROUGH FAILED!", Color.Red, 3.0f, 1.3f));
        });

        _eventManager.Subscribe<OnPlayerDiedEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ☠ TỬ VONG: {e.PlayerName} — {e.CauseOfDeath}");
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 60), "DEAD!", Color.DarkRed, 4.0f, 1.4f));
            TriggerShake(0.8f, 10.0f);
            SpawnExplosion(new Vector2(_player.PositionX, _player.PositionY), Color.DarkRed, 40);
        });
    }

    // ====================================================================
    // HIỆU ỨNG HẠT, RUNG MÀN HÌNH VÀ THANH MÁU CAO CẤP
    // ====================================================================

    private void TriggerShake(float duration, float intensity)
    {
        _shakeTime = duration;
        _shakeIntensity = intensity;
    }

    private void UpdateParticles(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            _particles[i].Update(deltaTime);
            if (!_particles[i].Active)
            {
                _particles.RemoveAt(i);
            }
        }
    }

    private void UpdateScreenshake(float deltaTime)
    {
        if (_shakeTime > 0)
        {
            _shakeTime -= deltaTime;
            if (_shakeTime <= 0)
            {
                _shakeIntensity = 0f;
            }
        }
    }

    private void UpdateProjectileTrails()
    {
        foreach (var proj in _projectilePool.Projectiles)
        {
            if (!proj.Active) continue;

            if (_random.NextDouble() < 0.35)
            {
                Vector2 spawnPos = proj.Position - proj.Velocity * 0.02f; // Phía sau đạn một chút
                Vector2 vel = new Vector2((float)(_random.NextDouble() * 10 - 5), (float)(_random.NextDouble() * 10 - 5));

                Color pColor;
                ParticleType pType;
                float size = (float)(_random.NextDouble() * 2 + 1.5);

                switch (proj.Element)
                {
                    case Element.Wood:
                        pColor = new Color(50, 220, 120, 200);
                        pType = ParticleType.Leaf;
                        break;
                    case Element.Fire:
                        pColor = new Color(255, 120, 20, 220);
                        pType = ParticleType.Ember;
                        break;
                    case Element.Ice:
                        pColor = new Color(130, 220, 255, 200);
                        pType = ParticleType.Snow;
                        break;
                    default:
                        pColor = new Color(200, 200, 200, 180);
                        pType = ParticleType.Spark;
                        size = (float)(_random.NextDouble() * 1.5 + 1.0);
                        break;
                }

                _particles.Add(new Particle(spawnPos, vel, pColor, size, (float)(_random.NextDouble() * 0.4 + 0.3), pType));
            }
        }
    }

    private void UpdateMeditationVFX()
    {
        var cult = _player.Cultivation;
        if (cult.CurrentState == CultivationState.Meditating)
        {
            if (_random.NextDouble() < 0.12)
            {
                // Hạt khí bay lên từ xung quanh cơ thể
                Vector2 pos = new Vector2(_player.PositionX + _random.Next(-12, 12), _player.PositionY + _random.Next(-4, 12));
                Vector2 vel = new Vector2(0, -25f);
                Color auraColor = _random.Next(2) == 0 ? Color.Gold * 0.8f : Color.SkyBlue * 0.8f;
                _particles.Add(new Particle(pos, vel, auraColor, (float)(_random.NextDouble() * 2.5 + 1.5), (float)(_random.NextDouble() * 0.8 + 0.6), ParticleType.Aura));
            }
        }
    }

    private void UpdateAbsorptionVFX()
    {
        var cult = _player.Cultivation;
        if (cult.CurrentState == CultivationState.AbsorbingRing)
        {
            if (_random.NextDouble() < 0.25)
            {
                // Hạt khí cuộn tròn từ bệ hồn hoàn bay về phía người chơi
                Vector2 playerCenter = new Vector2(_player.PositionX, _player.PositionY);
                Vector2 spawnPos = _absorbingRingPosition + new Vector2((float)(_random.NextDouble() * 40 - 20), (float)(_random.NextDouble() * 40 - 20));
                
                Vector2 dir = playerCenter - spawnPos;
                float dist = dir.Length();
                if (dist > 5f)
                {
                    dir.Normalize();
                    Vector2 vel = dir * (dist * 1.5f + 50f);
                    
                    Color ringColor = cult.HP < cult.MaxHP * 0.4f ? Color.Red * 0.9f : Color.Purple * 0.9f; 
                    if (_random.Next(3) == 0) ringColor = Color.Gold * 0.9f;

                    _particles.Add(new Particle(spawnPos, vel, ringColor, (float)(_random.NextDouble() * 2 + 1.5), 0.7f, ParticleType.Spark));
                }
            }
        }
    }

    private void SpawnElementalBurst(Vector2 position, Element element, int count)
    {
        for (int i = 0; i < count; i++)
        {
            double angle = _random.NextDouble() * Math.PI * 2;
            float speed = (float)(_random.NextDouble() * 120 + 40);
            Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
            
            Color pColor;
            ParticleType pType;
            float size = (float)(_random.NextDouble() * 3.0 + 1.5);
            float lifetime = (float)(_random.NextDouble() * 0.5 + 0.3);

            switch (element)
            {
                case Element.Wood:
                    pColor = new Color(50, 220, 120, 220);
                    pType = ParticleType.Leaf;
                    break;
                case Element.Fire:
                    pColor = new Color(255, 120, 20, 240);
                    pType = ParticleType.Ember;
                    break;
                case Element.Ice:
                    pColor = new Color(130, 220, 255, 220);
                    pType = ParticleType.Snow;
                    break;
                default:
                    pColor = new Color(220, 220, 220, 200);
                    pType = ParticleType.Spark;
                    size = (float)(_random.NextDouble() * 2.0 + 1.0);
                    break;
            }

            _particles.Add(new Particle(position, vel, pColor, size, lifetime, pType));
        }
    }

    private void SpawnExplosion(Vector2 position, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            double angle = _random.NextDouble() * Math.PI * 2;
            float speed = (float)(_random.NextDouble() * 150 + 60);
            Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
            float size = (float)(_random.NextDouble() * 4.0 + 2.0);
            float lifetime = (float)(_random.NextDouble() * 0.7 + 0.4);

            _particles.Add(new Particle(position, vel, color, size, lifetime, ParticleType.Burst));
        }
    }

    private void SpawnBreakthroughBurst(Vector2 position)
    {
        int count = 50;
        for (int i = 0; i < count; i++)
        {
            double angle = _random.NextDouble() * Math.PI * 2;
            float speed = (float)(_random.NextDouble() * 180 + 80);
            Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
            float size = (float)(_random.NextDouble() * 4.5 + 2.5);
            float lifetime = (float)(_random.NextDouble() * 0.9 + 0.5);

            Color pColor = (i % 3) switch
            {
                0 => Color.Gold,
                1 => Color.Yellow,
                _ => Color.DeepSkyBlue
            };

            _particles.Add(new Particle(position, vel, pColor, size, lifetime, ParticleType.Burst));
        }
    }

    private void DrawPremiumBar(int x, int y, int width, int height, float ratio, Color bgColor, Color barColor, Color borderColor, string label, Color textColor)
    {
        // Vẽ Nền
        DrawRect(x, y, width, height, bgColor);
        
        // Vẽ phần thanh tiến trình
        int fillWidth = (int)(width * Math.Clamp(ratio, 0f, 1f));
        if (fillWidth > 0)
        {
            DrawRect(x, y, fillWidth, height, barColor);
            
            // Hiệu ứng Glassy 3D - Nửa trên sáng bóng
            DrawRect(x, y, fillWidth, Math.Max(1, height / 3), Color.White * 0.22f);
            
            // Hiệu ứng Glassy 3D - Nửa dưới bóng mờ
            DrawRect(x, y + height - Math.Max(1, height / 3), fillWidth, Math.Max(1, height / 3), Color.Black * 0.18f);
        }
        
        // Vẽ viền ngoài
        DrawRect(x, y, width, height, borderColor, true);
        
        // Vẽ nhãn văn bản
        if (!string.IsNullOrEmpty(label))
        {
            _spriteBatch.DrawString(_font, label, new Vector2(x + width + 10, y - 2), textColor, 0f, Vector2.Zero, 0.82f, SpriteEffects.None, 0f);
        }
    }

    private struct SavedMonsterData
    {
        public string name { get; set; }
        public int age { get; set; }
        public float hp { get; set; }
        public float maxHp { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public string element { get; set; }
    }

    private struct SavedDroppedItemData
    {
        public string itemId { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public int quantity { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }

    private struct SavedLauncherData
    {
        public int type { get; set; }
        public int ammo { get; set; }
        public int maxAmmo { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }

    private struct SavedSoulRingData
    {
        public int age { get; set; }
        public string element { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }
}
