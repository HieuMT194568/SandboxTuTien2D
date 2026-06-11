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

    private List<HiddenWeaponData> _hiddenWeapons = null!;
    private List<ConsumableData> _consumables = null!;

    // ========================================================================
    // TRẠNG THÁI GIAO DIỆN
    // ========================================================================
    private bool _isInventoryOpen = false;
    private KeyboardState _previousKeyState;
    private MouseState _previousMouseState;

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

        UpdatePlayerMovement(currentKeyState, deltaTime);

        foreach (var ring in _soulRings)
        {
            ring.Update(gameTime);
        }

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

        // 2. Vẽ Hồn Thú
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
                
                _spriteBatch.Draw(tex, monster.Position, null, Color.White, 0f, 
                                  new Vector2(16, 16), 1.5f, SpriteEffects.None, 0f);

                // Vẽ thanh máu quái
                int hpBarW = 32;
                int hpBarH = 4;
                int hpX = (int)monster.Position.X - 16;
                int hpY = (int)monster.Position.Y - 24;
                float hpRatio = monster.HP / monster.MaxHP;

                DrawRect(hpX, hpY, hpBarW, hpBarH, Color.Black);
                DrawRect(hpX, hpY, (int)(hpBarW * hpRatio), hpBarH, Color.Red);
                
                string ageLabel = $"{monster.Age}N";
                Vector2 labelSize = _font.MeasureString(ageLabel) * 0.7f;
                _spriteBatch.DrawString(_font, ageLabel, new Vector2(monster.Position.X - labelSize.X / 2f, monster.Position.Y - 38), 
                                        Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }

        // 3. Vẽ đạn ám khí
        foreach (var proj in _projectilePool.Projectiles)
        {
            if (proj.Active)
            {
                Texture2D tex = proj.Element == Element.Fire ? _boltTexture : _needleTexture;
                float rotation = (float)Math.Atan2(proj.Velocity.Y, proj.Velocity.X);
                _spriteBatch.Draw(tex, proj.Position, null, Color.White, rotation, 
                                  new Vector2(4, 4), 1.5f, SpriteEffects.None, 0f);
            }
        }

        // 4. Vẽ Player
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

        // 5. Vẽ chữ nổi
        foreach (var ft in _floatingTexts)
        {
            float alpha = 1f - (ft.Elapsed / ft.Lifetime);
            _spriteBatch.DrawString(_font, ft.Text, ft.Position, ft.Color * alpha, 
                                    0f, Vector2.Zero, ft.Scale, SpriteEffects.None, 0f);
        }

        // 6. Vẽ HUD
        DrawHUD();

        // 7. Vẽ túi đồ
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
            _player.PositionY = Math.Clamp(_player.PositionY, 175f, 600f - 16f); // Tránh đè lên HUD trên
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

        // Sử dụng phím tắt 1->5 để ăn xúc xích hoặc trang bị ám khí
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

        // Click chuột trái bắn đạn
        if (mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            // Không bắn nếu đang click vào khu vực túi đồ khi túi đồ đang mở
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
    // COMBAT & SYSTEM INTEGRATIONS
    // ====================================================================

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
        Random rand = new Random();
        int monsterType = rand.Next(3);

        string name;
        int age;
        float hp;
        Element elem;

        switch (monsterType)
        {
            case 0:
                name = "Lam Ngan Thao";
                age = 400;
                hp = 200f;
                elem = Element.Wood;
                break;
            case 1:
                name = "Hoa Ke";
                age = 1500;
                hp = 500f;
                elem = Element.Fire;
                break;
            case 2:
            default:
                name = "Bang Tam";
                age = 9999;
                hp = 1200f;
                elem = Element.Ice;
                break;
        }

        var m = new Monster(name, age, hp, spawnPos, elem);
        
        m.OnKilled += monster =>
        {
            var ring = new SoulRingEntity(monster.Position, monster.Age);
            _soulRings.Add(ring);

            _floatingTexts.Add(new FloatingText(monster.Position, $"Dropped Soul Ring {monster.Age}N!", Color.Yellow, 2.5f, 1.2f));
            _monsters.Remove(monster);
        };

        _monsters.Add(m);
        _floatingTexts.Add(new FloatingText(spawnPos, $"Spawn: {name} ({age}N)", Color.Tomato));
        Console.WriteLine($"[Hệ Thống] Đã sinh Hồn Thú '{name}' {age} năm ({elem}) tại {spawnPos}");
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

            cult.StartAbsorbingSoulRing(ring.Age);
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
    // RENDER METADATA MAPPING (NO ACCENTS IN FONT RENDERING)
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
    // HUD DRAWING
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
        DrawRect(startX - 10, startY - 5, 305, 140, new Color(20, 22, 38, 220));
        DrawRect(startX - 10, startY - 5, 305, 140, new Color(65, 75, 110), true);

        // Tên và cảnh giới (đã loại bỏ dấu tiếng Việt để vẽ font an toàn)
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

        // Ám khí hiện tại
        int wY = spY + spacing;
        string activeWeaponName = GetHUDItemName(_player.Inventory.EquippedWeapon?.ItemId ?? string.Empty);
        _spriteBatch.DrawString(_font, $"Am Khi: {activeWeaponName}", new Vector2(startX, wY + 5), Color.Khaki, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

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

        // Cảnh báo khi đang hấp thu
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
        _spriteBatch.DrawString(_font, "[WASD/Arrows]: Move | [Left Click]: Fire Weapon | [Right Click]: Spawn Monster", new Vector2(35, guideY + 5), Color.Silver, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, "[I]: Toggle Inventory | [E]: Switch Weapon | [1-5]: Use Quick Item | [R]: Absorb Ring", new Vector2(35, guideY + 26), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
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

        // Vẽ slots vật phẩm
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
