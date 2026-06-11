using System;
using System.Collections.Generic;
using System.IO;
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

    // ========================================================================
    // HỆ THỐNG CỐT LÕI
    // ========================================================================
    private EventManager _eventManager = null!;
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

        _previousKeyState = Keyboard.GetState();
        _previousMouseState = Mouse.GetState();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Fonts/Arial");

        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _playerTexture = PixelArtGenerator.CreatePlayerTexture(GraphicsDevice);
        _monsterPlantTexture = PixelArtGenerator.CreateMonsterPlantTexture(GraphicsDevice);
        _monsterFireTexture = PixelArtGenerator.CreateMonsterFireTexture(GraphicsDevice);
        _monsterIceTexture = PixelArtGenerator.CreateMonsterIceTexture(GraphicsDevice);
        _needleTexture = PixelArtGenerator.CreateProjectileNeedleTexture(GraphicsDevice);
        _boltTexture = PixelArtGenerator.CreateProjectileBoltTexture(GraphicsDevice);
        _sausageTexture = PixelArtGenerator.CreateSausageTexture(GraphicsDevice);
        _ringTexture = PixelArtGenerator.CreateRingTexture(GraphicsDevice, 32);
        _turretTexture = PixelArtGenerator.CreateTurretTexture(GraphicsDevice);
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

        // 1. Cập nhật Hồn Thú tự động tiến hóa theo tuổi thọ (1s thực = 24 phút game)
        for (int i = _monsters.Count - 1; i >= 0; i--)
        {
            _monsters[i].UpdateEvolution(deltaTime, 1440f);
        }

        // 2. Cập nhật hoạt động của Bệ Phóng Ám Khí tự động
        foreach (var launcher in _launchers)
        {
            launcher.Update(deltaTime, _monsters, _projectilePool);
        }

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

        _spriteBatch.Begin();

        // 1. Vẽ Hồn Hoàn dưới đất
        foreach (var ring in _soulRings)
        {
            if (ring.Active)
            {
                float scale = 0.5f + 0.08f * (float)Math.Sin(ring.PulseTimer * 5f);
                float rotation = ring.PulseTimer * 0.7f;
                _spriteBatch.Draw(_ringTexture, ring.Position, null, ring.GetColor() * 0.8f,
                                  rotation, new Vector2(32, 32), scale, SpriteEffects.None, 0f);
            }
        }

        // 2. Vẽ các Bệ Phóng Ám Khí tự động xuống đất
        foreach (var launcher in _launchers)
        {
            if (launcher.Active)
            {
                _spriteBatch.Draw(_turretTexture, launcher.Position, null, Color.White, 0f, 
                                  new Vector2(12, 12), 1.5f, SpriteEffects.None, 0f);
                
                // Vẽ vòng tròn tầm bắn quét nhỏ mờ bao quanh bệ phóng
                _spriteBatch.Draw(_ringTexture, launcher.Position, null, Color.White * 0.15f, 
                                  0f, new Vector2(32, 32), launcher.Range / 32f, SpriteEffects.None, 0f);
            }
        }

        // 3. Vẽ Hồn Thú (Kích thước co dãn theo bán kính va chạm)
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
                
                // Quy mô vẽ co dãn dựa trên bán kính va chạm Radius (gốc là 16px -> nhân scale)
                float scale = (monster.Radius / 16f) * 1.2f;

                _spriteBatch.Draw(tex, monster.Position, null, Color.White, 0f, 
                                  new Vector2(16, 16), scale, SpriteEffects.None, 0f);

                // Vẽ thanh HP quái
                int hpBarW = (int)(monster.Radius * 1.8f);
                int hpBarH = 4;
                int hpX = (int)monster.Position.X - hpBarW / 2;
                int hpY = (int)monster.Position.Y - (int)(monster.Radius + 6);
                float hpRatio = monster.HP / monster.MaxHP;

                DrawRect(hpX, hpY, hpBarW, hpBarH, Color.Black);
                DrawRect(hpX, hpY, (int)(hpBarW * hpRatio), hpBarH, Color.Red);
                
                // Tên Hồn thú cải tiến theo tu vi tiến hóa
                string rankName = monster.Name;
                Vector2 labelSize = _font.MeasureString(rankName) * 0.7f;
                _spriteBatch.DrawString(_font, rankName, new Vector2(monster.Position.X - labelSize.X / 2f, hpY - 14), 
                                        Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }

        // 4. Vẽ đạn ám khí
        foreach (var proj in _projectilePool.Projectiles)
        {
            if (proj.Active)
            {
                Texture2D tex = proj.Element == Element.Fire ? _boltTexture : _needleTexture;
                
                // Đạn hệ thường của cơ quan vẽ màu trắng/bạc
                Color bulletColor = proj.Element == Element.None ? Color.LightGray : Color.White;
                float rotation = (float)Math.Atan2(proj.Velocity.Y, proj.Velocity.X);
                
                _spriteBatch.Draw(tex, proj.Position, null, bulletColor, rotation, 
                                  new Vector2(4, 4), 1.5f, SpriteEffects.None, 0f);
            }
        }

        // 5. Vẽ Player
        if (_player.Cultivation.CurrentState != CultivationState.Dead)
        {
            _spriteBatch.Draw(_playerTexture, new Vector2(_player.PositionX, _player.PositionY), null, Color.White, 0f, 
                              new Vector2(16, 16), 1.5f, SpriteEffects.None, 0f);
        }
        else
        {
            _spriteBatch.Draw(_playerTexture, new Vector2(_player.PositionX, _player.PositionY), null, Color.DimGray, (float)Math.PI / 2f, 
                              new Vector2(16, 16), 1.5f, SpriteEffects.None, 0f);
        }

        // 6. Vẽ chữ nổi
        foreach (var ft in _floatingTexts)
        {
            float alpha = 1f - (ft.Elapsed / ft.Lifetime);
            _spriteBatch.DrawString(_font, ft.Text, ft.Position, ft.Color * alpha, 
                                    0f, Vector2.Zero, ft.Scale, SpriteEffects.None, 0f);
        }

        // 7. Vẽ HUD
        DrawHUD();

        // 8. Vẽ túi đồ
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

            _player.PositionX = Math.Clamp(_player.PositionX, 16f, 800f - 16f);
            _player.PositionY = Math.Clamp(_player.PositionY, 175f, 600f - 16f); // Tránh đè HUD
        }
    }

    private void HandleInput(KeyboardState keys, MouseState mouse)
    {
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
            _player.Cultivation.AddExp(500f);
            _floatingTexts.Add(new FloatingText(new Vector2(_player.PositionX, _player.PositionY - 30), "+500 EXP (Cheat)", Color.SkyBlue));
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

        // --- Bấm phím [Q] để kích hoạt Hồn kỹ 1 ---
        if (IsKeyJustPressed(keys, Keys.Q))
        {
            TriggerActiveSkill(1, mouse.Position.ToVector2());
        }

        // --- Bấm phím [W] để kích hoạt Hồn kỹ 2 ---
        if (IsKeyJustPressed(keys, Keys.W))
        {
            TriggerActiveSkill(2, mouse.Position.ToVector2());
        }

        // --- Bấm phím [T] để đặt bệ phóng ám khí tự động (Auto-Turret) ---
        if (IsKeyJustPressed(keys, Keys.T))
        {
            PlaceAutoLauncher();
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
                FireActiveWeapon(mouse.Position.ToVector2());
            }
        }

        // Click chuột phải spawn quái
        if (mouse.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
        {
            SpawnRandomMonster(mouse.Position.ToVector2());
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

        Vector2 playerPos = new Vector2(_player.PositionX, _player.PositionY);
        var turret = new AutoLauncher(playerPos);
        _launchers.Add(turret);

        _floatingTexts.Add(new FloatingText(playerPos - new Vector2(0, 15), "+ Auto Turret", Color.Gold));
        Console.WriteLine($"[Cơ Quan] ⚙ Đã đặt Bệ Phóng Ám Khí tự động tại vị trí {playerPos.X:F0},{playerPos.Y:F0} (Tổng số: {_launchers.Count}/3).");
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
                    monster.TakeDamage(proj.Damage, proj.Element, out float finalDmg, out bool isCounter);

                    Color txtColor = isCounter ? Color.Red : Color.Orange;
                    string dmgText = isCounter ? $"-{finalDmg:F0} COUNTER!" : $"-{finalDmg:F0}";
                    _floatingTexts.Add(new FloatingText(monster.Position - new Vector2(0, 15), dmgText, txtColor, 1.3f, isCounter ? 1.25f : 1.0f));
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

        // Vẽ Khung đen nền HUD
        DrawRect(startX - 10, startY - 5, 305, 155, new Color(20, 22, 38, 220));
        DrawRect(startX - 10, startY - 5, 305, 155, new Color(65, 75, 110), true);

        // Tên và cảnh giới
        string realmHUD = GetRealmHUDName(cult.CurrentRealm);
        _spriteBatch.DrawString(_font, $"Duong Tam | {realmHUD} (Cap {cult.CurrentLevel})", 
                                new Vector2(startX, startY), Color.Gold);

        // HP bar
        int hpY = startY + spacing + 6;
        float hpRatio = cult.HP / cult.MaxHP;
        DrawRect(startX, hpY, barW, barH, new Color(50, 15, 15));
        DrawRect(startX, hpY, (int)(barW * hpRatio), barH, new Color(230, 45, 45));
        DrawRect(startX, hpY, barW, barH, new Color(120, 40, 40), true);
        _spriteBatch.DrawString(_font, $"HP: {cult.HP:F0}/{cult.MaxHP:F0}", new Vector2(startX + barW + 10, hpY - 2), Color.Tomato, 0f, Vector2.Zero, 0.82f, SpriteEffects.None, 0f);

        // EXP bar
        int expY = hpY + spacing;
        float expRatio = cult.CurrentExp / cult.MaxExpForCurrentLevel;
        expRatio = Math.Clamp(expRatio, 0f, 1f);
        DrawRect(startX, expY, barW, barH, new Color(15, 15, 50));
        DrawRect(startX, expY, (int)(barW * expRatio), barH, new Color(60, 130, 255));
        DrawRect(startX, expY, barW, barH, new Color(40, 90, 180), true);
        _spriteBatch.DrawString(_font, $"EXP: {cult.CurrentExp:F0}/{cult.MaxExpForCurrentLevel:F0}", new Vector2(startX + barW + 10, expY - 2), Color.LightSkyBlue, 0f, Vector2.Zero, 0.82f, SpriteEffects.None, 0f);

        // Hồn Lực bar
        int spY = expY + spacing;
        float spRatio = cult.SoulPower / cult.MaxSoulPower;
        DrawRect(startX, spY, barW, barH, new Color(10, 45, 20));
        DrawRect(startX, spY, (int)(barW * spRatio), barH, new Color(45, 210, 110));
        DrawRect(startX, spY, barW, barH, new Color(30, 130, 70), true);
        _spriteBatch.DrawString(_font, $"SP: {cult.SoulPower:F0}/{cult.MaxSoulPower:F0}", new Vector2(startX + barW + 10, spY - 2), Color.MediumSpringGreen, 0f, Vector2.Zero, 0.82f, SpriteEffects.None, 0f);

        // Ám khí hiện tại + Danh sách bệ phóng
        int wY = spY + spacing;
        string activeWeaponName = GetHUDItemName(_player.Inventory.EquippedWeapon?.ItemId ?? string.Empty);
        _spriteBatch.DrawString(_font, $"Am Khi: {activeWeaponName} | Turrets: {_launchers.Count}/3", new Vector2(startX, wY + 5), Color.Khaki, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

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
        _spriteBatch.DrawString(_font, "HON KY CHU DONG", new Vector2(skillX + 10, skillY + 8), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        DrawRect(skillX + 8, skillY + 24, 149, 1, new Color(65, 75, 110));

        string qLabel = cult.Skill1 != null ? $"Q: {cult.Skill1.Name}\n   ({cult.Skill1.SPCost} SP)" : "Q: [Chua hoc]";
        Color qColor = cult.Skill1 != null ? Color.MediumSpringGreen : Color.DarkGray;
        _spriteBatch.DrawString(_font, qLabel, new Vector2(skillX + 10, skillY + 32), qColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        string wLabel = cult.Skill2 != null ? $"W: {cult.Skill2.Name}\n   ({cult.Skill2.SPCost} SP)" : "W: [Chua hoc]";
        Color wColor = cult.Skill2 != null ? Color.Gold : Color.DarkGray;
        _spriteBatch.DrawString(_font, wLabel, new Vector2(skillX + 10, skillY + 80), wColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        // Cảnh báo khi hấp thu
        if (cult.CurrentState == CultivationState.AbsorbingRing)
        {
            float pulse = (float)(Math.Sin(gameTimeForDraw * 8.0) * 0.4 + 0.6);
            string warnMsg = "!!! WARNING: ABSORBING SOUL RING - Press [1] to eat Sausage !!!";
            _spriteBatch.DrawString(_font, warnMsg, new Vector2(100, 195), Color.Red * pulse, 0f, Vector2.Zero, 1.05f, SpriteEffects.None, 0f);
        }

        // Vẽ Bảng hướng dẫn ở đáy màn hình
        int guideY = 540;
        DrawRect(20, guideY, 760, 50, new Color(18, 18, 30, 220));
        DrawRect(20, guideY, 760, 50, new Color(55, 60, 85), true);
        _spriteBatch.DrawString(_font, "[WASD]: Move | [Left Click]: Normal Fire | [Right Click]: Spawn Monster | [T]: Place Turret", new Vector2(35, guideY + 5), Color.Silver, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, "[I]: Inventory | [E]: Switch Weapon | [1-5]: Quick Eat | [R]: Absorb Ring | [Q/W]: Cast Skills", new Vector2(35, guideY + 26), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
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
                    "CONSUMABLE" => _sausageTexture,
                    "HIDDEN_WEAPON" when item.ItemId == "am_khi_tu_tien_01" => _needleTexture,
                    "HIDDEN_WEAPON" when item.ItemId == "am_khi_chu_cat_02" => _boltTexture,
                    _ => _sausageTexture
                };

                if (iconTex != null)
                {
                    _spriteBatch.Draw(iconTex, new Vector2(slotStartX + 25, sY + slotHeight / 2f), null, Color.White, 0f, 
                                      new Vector2(iconTex.Width / 2f, iconTex.Height / 2f), 2.0f, SpriteEffects.None, 0f);
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
        _hiddenWeapons = _dataLoader.LoadHiddenWeapons();
        _consumables = _dataLoader.LoadConsumables();
    }

    private void GiveInitialInventoryItems()
    {
        var food = _consumables.FirstOrDefault(c => c.ItemId == "food_huong_trang_01");
        if (food != null)
        {
            _player.Inventory.AddConsumable(food, 5);
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
        });
    }
}
