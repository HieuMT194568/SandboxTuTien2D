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

/// <summary>Màn hình hiện tại: chọn lưu phái trước khi vào game, hoặc đang chơi.</summary>
public enum GameScreen
{
    ClassSelect,
    Playing
}

/// <summary>Bước trong màn hình chọn lưu phái: chọn lưu phái, rồi chọn hệ Linh Căn nếu cần (Pháp Tu).</summary>
public enum ClassSelectStep
{
    PickClass,
    PickElement
}

/// <summary>
/// Đối tượng chữ nổi phục vụ hiển thị sát thương hoặc thông báo.
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
/// Một đạo thiên lôi trong Thiên Kiếp: hiện vòng báo hiệu tại vị trí,
/// sau Delay giây thì đánh xuống; người chơi đứng trong vòng sẽ chịu sát thương.
/// </summary>
public class LightningStrike
{
    public Vector2 Position;
    public float Delay;
    public float Damage;
    public float Radius = 30f;
    public float FlashTimer;
    public bool Landed;
    public int Seed;

    public bool Finished => Landed && FlashTimer <= 0f;

    public LightningStrike(Vector2 position, float delay, float damage, int seed)
    {
        Position = position;
        Delay = delay;
        Damage = damage;
        Seed = seed;
    }
}

/// <summary>
/// Game1 — Điểm tích hợp giao diện đồ họa và tất cả các hệ thống.
/// </summary>
public class Game1 : Game
{
    private const string PLAYER_NAME = "Lâm Phong";
    private const int MAX_FORMATIONS = 3;
    private const string SPIRIT_STONE_ID = "linh_thach";
    private const float BASE_MOVE_SPEED = 180f;
    private const float DASH_IMPACT_RADIUS = 60f;

    private static readonly Element[] PhapTuElements = { Element.Fire, Element.Wood, Element.Ice };

    private static readonly Vector2 AlchemistPosition = new Vector2(250, 380);
    private static readonly Vector2 ForgePosition = new Vector2(550, 380);

    // Bố cục bảng Lò Luyện (dùng chung cho vẽ và xử lý click)
    private const int MAX_RECIPES_SHOWN = 4;
    private const int CRAFT_PANEL_X = 20;
    private const int CRAFT_PANEL_Y = 15;
    private const int CRAFT_PANEL_W = 300;
    private const int CRAFT_PANEL_H = 505;
    private const int CRAFT_SLOT_START_Y = CRAFT_PANEL_Y + 52;
    private const int CRAFT_SLOT_H = 88;
    private const int CRAFT_SLOT_GAP = 6;
    private const int CRAFT_BTN_Y = CRAFT_PANEL_Y + 440;
    private const int CRAFT_BTN_H = 50;

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
    private Texture2D _pillTexture = null!;
    private Texture2D _beastCoreTexture = null!;
    private Texture2D _materialTexture = null!;
    private Texture2D _ringTexture = null!;
    private Texture2D _formationTexture = null!;
    private Texture2D _alchemistTexture = null!;
    private Texture2D _forgeTexture = null!;

    // ========================================================================
    // HỆ THỐNG CỐT LÕI
    // ========================================================================
    private EventManager _eventManager = null!;
    private MySqlDbManager _dbManager = null!;
    private GameTimeManager _gameTimeManager = null!;
    private DataLoader _dataLoader = null!;
    private CultivationSystem _cultivationSystem = null!;
    private CombatSystem _combatSystem = null!;
    private SkillSystem _skillSystem = null!;
    private ZoneSystem _zoneSystem = null!;

    /// <summary>Nội tại Vạn Độc Thể: 25% tẩm độc 2% HP/giây trong 5 giây.</summary>
    private static readonly IReadOnlyList<OnHitEffect> VanDocTheOnHit = new[]
    {
        new OnHitEffect(StatusType.Poison, 5f, 0f, 2f, 0.25f)
    };

    // ========================================================================
    // ĐỐI TƯỢNG VÀ COMBAT
    // ========================================================================
    private Player _player = null!;
    private SkillLoadoutComponent _skillLoadout = null!;
    private ProjectilePool _projectilePool = null!;
    private readonly List<Monster> _monsters = new();
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly List<FormationArray> _formations = new();
    private readonly List<LightningStrike> _lightningStrikes = new();
    private readonly List<Particle> _particles = new();
    private ConsumableSpawner _alchemistSpawner = null!;
    private ConsumableSpawner _forgeSpawner = null!;
    private readonly List<DroppedItem> _droppedItems = new();
    private int _nextFormationType = 1;
    private float _contactDamageTimer = 0f;

    private float _shakeTime = 0f;
    private float _shakeIntensity = 0f;
    private Vector2 _cameraPosition = Vector2.Zero;
    private const float MAP_WIDTH = 2000f;
    private const float MAP_HEIGHT = 2000f;

    private List<MagicWeaponData> _magicWeapons = null!;
    private List<ConsumableData> _consumables = null!;
    private List<ClassData> _classes = null!;
    private List<SkillData> _skills = null!;
    private List<CraftingRecipe> _recipes = new();

    // ========================================================================
    // TRẠNG THÁI GIAO DIỆN
    // ========================================================================
    private bool _isInventoryOpen = false;
    private bool _isCraftingOpen = false;
    private int _selectedRecipeIndex = 0;
    private KeyboardState _previousKeyState;
    private MouseState _previousMouseState;
    private readonly Random _random = new();

    // ========================================================================
    // MÀN HÌNH CHỌN LƯU PHÁI
    // ========================================================================
    private GameScreen _screen = GameScreen.ClassSelect;
    private ClassSelectStep _classSelectStep = ClassSelectStep.PickClass;
    private int _selectedClassIndex = 0;
    private int _selectedElementIndex = 0;

    // ========================================================================
    // NỘI TẠI KIẾM TU — Kiếm Ý (cộng dồn sát thương khi đánh trúng liên tục)
    // ========================================================================
    private int _kiemYStacks = 0;
    private float _kiemYTimer = 0f;
    private const float KIEM_Y_DECAY_TIME = 3f;
    private const float KIEM_Y_PER_STACK = 0.03f;
    private const int KIEM_Y_MAX_STACKS = 10;

    /// <summary>Bộ chiêu unlocked ở lần kiểm tra gần nhất, dùng để phát hiện chiêu mới lĩnh ngộ khi lên cảnh giới.</summary>
    private readonly HashSet<string> _previouslyUnlockedSkillIds = new();

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
        Console.WriteLine("║            SANDBOX TU TIÊN 2D PIXEL                      ║");
        Console.WriteLine("║   Lưu Phái, Tu Luyện, Thiên Kiếp, Luyện Đan & Trận Pháp   ║");
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

        // 4. Khởi tạo các hệ thống lõi không phụ thuộc lưu phái (Player được tạo sau khi chọn lưu phái)
        _cultivationSystem = new CultivationSystem();
        _projectilePool = new ProjectilePool();
        _combatSystem = new CombatSystem(_monsters, _projectilePool, _eventManager);
        _skillSystem = new SkillSystem(_projectilePool);
        _zoneSystem = new ZoneSystem(_combatSystem);

        _previousKeyState = Keyboard.GetState();
        _previousMouseState = Mouse.GetState();

        // Bắt đầu ở màn hình chọn lưu phái; StartNewGame() sẽ tạo Player và thế giới khi đã chọn xong
        _screen = GameScreen.ClassSelect;

        base.Initialize();
    }

    /// <summary>
    /// Tạo Player theo lưu phái + hệ Linh Căn đã chọn, cấp phát túi đồ, sinh thế giới ban đầu,
    /// rồi chuyển sang màn hình chơi. Gọi một lần duy nhất từ màn hình chọn lưu phái.
    /// </summary>
    private void StartNewGame(ClassData chosenClass, Element chosenElement)
    {
        bool isPhapTu = chosenClass.HasElementalSkills;
        float spiritRootMultiplier = isPhapTu ? 2.0f : 1.2f; // Pháp Tu: Thiên Linh Căn; còn lại: Chân Linh Căn
        Element spiritRootElement = isPhapTu ? chosenElement : Element.None;

        _player = new Player(
            name: PLAYER_NAME,
            eventManager: _eventManager,
            innateLevel: 1,
            spiritRootMultiplier: spiritRootMultiplier,
            spiritRootElement: spiritRootElement,
            hpMultiplier: chosenClass.StatMultipliers.HP,
            spiritPowerMultiplier: chosenClass.StatMultipliers.SpiritPower,
            moveSpeedMultiplier: chosenClass.StatMultipliers.Speed
        );
        _cultivationSystem.RegisterComponent(_player.Cultivation);

        _skillLoadout = new SkillLoadoutComponent(chosenClass, chosenElement, _skills);
        _previouslyUnlockedSkillIds.Clear();
        foreach (var skill in _skillLoadout.GetUnlockedSkills(_player.Cultivation.CurrentRealm))
        {
            _previouslyUnlockedSkillIds.Add(skill.Id);
        }

        // Nội tại: Ngũ Hành Tương Khắc (Pháp Tu, khắc hệ +75% thay vì +50%), Kiếm Tâm Thông Minh (Kiếm Tu, +15% chí mạng)
        _combatSystem.PlayerCounterMultiplier = chosenClass.Passives.Any(p => p.Id == "ngu_hanh_tuong_khac")
            ? 1.75f : DamageCalculator.DEFAULT_COUNTER_MULTIPLIER;
        _combatSystem.PlayerCritChance = chosenClass.Passives.Any(p => p.Id == "kiem_tam_thong_minh") ? 0.15f : 0f;
        _kiemYStacks = 0;
        _kiemYTimer = 0f;

        // Cấp phát túi đồ mặc định
        GiveInitialInventoryItems();

        _alchemistSpawner = new ConsumableSpawner(AlchemistPosition, 15f, "dan_hoi_xuan", GetItemName("dan_hoi_xuan"), InventoryComponent.TYPE_CONSUMABLE, 1);
        _forgeSpawner = new ConsumableSpawner(ForgePosition, 10f, SPIRIT_STONE_ID, GetItemName(SPIRIT_STONE_ID), InventoryComponent.TYPE_CONSUMABLE, 1);

        _alchemistSpawner.OnSpawn += (pos, id, name, type, qty) => {
            _droppedItems.Add(new DroppedItem(pos, id, name, type, qty));
            _floatingTexts.Add(new FloatingText(pos - new Vector2(0, 15), $"+ {name}", Color.Orange, 1.5f));
        };
        _forgeSpawner.OnSpawn += (pos, id, name, type, qty) => {
            _droppedItems.Add(new DroppedItem(pos, id, name, type, qty));
            _floatingTexts.Add(new FloatingText(pos - new Vector2(0, 15), $"+ {name}", Color.LightSkyBlue, 1.5f));
        };

        // Sinh ngẫu nhiên một số Yêu Thú ban đầu rải rác trên bản đồ
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

        _screen = GameScreen.Playing;
        Console.WriteLine($"[Lưu Phái] Bắt đầu game mới: {chosenClass.Name}" +
                          (isPhapTu ? $" ({chosenElement.GetDisplayName()})" : "") + ".");
    }

    // ====================================================================
    // MÀN HÌNH CHỌN LƯU PHÁI
    // ====================================================================

    private void HandleClassSelectInput(KeyboardState keys)
    {
        if (_classes.Count == 0) return; // Lỗi nạp dữ liệu — không cho chọn để tránh crash

        if (_classSelectStep == ClassSelectStep.PickClass)
        {
            for (int i = 0; i < _classes.Count && i < 4; i++)
            {
                if (IsKeyJustPressed(keys, Keys.D1 + i))
                {
                    _selectedClassIndex = i;
                }
            }

            if (IsKeyJustPressed(keys, Keys.Enter))
            {
                var chosen = _classes[_selectedClassIndex];
                if (chosen.HasElementalSkills)
                {
                    _classSelectStep = ClassSelectStep.PickElement;
                    _selectedElementIndex = 0;
                }
                else
                {
                    StartNewGame(chosen, Element.None);
                }
            }
        }
        else // PickElement
        {
            for (int i = 0; i < PhapTuElements.Length; i++)
            {
                if (IsKeyJustPressed(keys, Keys.D1 + i))
                {
                    _selectedElementIndex = i;
                }
            }

            if (IsKeyJustPressed(keys, Keys.Back))
            {
                _classSelectStep = ClassSelectStep.PickClass;
            }
            else if (IsKeyJustPressed(keys, Keys.Enter))
            {
                StartNewGame(_classes[_selectedClassIndex], PhapTuElements[_selectedElementIndex]);
            }
        }
    }

    private void DrawClassSelectScreen()
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, null);

        _spriteBatch.DrawString(_font, "SANDBOX TU TIÊN 2D PIXEL", new Vector2(220, 35), Color.Gold, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 0f);

        if (_classes.Count == 0)
        {
            _spriteBatch.DrawString(_font, "LỖI: Không nạp được dữ liệu lưu phái (classes.json/skills.json).",
                                    new Vector2(60, 200), Color.Red, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            _spriteBatch.End();
            return;
        }

        if (_classSelectStep == ClassSelectStep.PickClass)
        {
            _spriteBatch.DrawString(_font, "Chọn Lưu Phái", new Vector2(300, 75), Color.LightSkyBlue, 0f, Vector2.Zero, 1.05f, SpriteEffects.None, 0f);

            const int cardW = 180, cardH = 370, gap = 10, startX = 30, startY = 110;
            for (int i = 0; i < _classes.Count; i++)
            {
                var cls = _classes[i];
                int x = startX + i * (cardW + gap);
                bool selected = i == _selectedClassIndex;

                DrawRect(x, startY, cardW, cardH, selected ? new Color(45, 35, 70, 230) : new Color(20, 22, 38, 220));
                DrawRect(x, startY, cardW, cardH, selected ? Color.Gold : new Color(65, 75, 110), true);

                float py = startY + 10;
                _spriteBatch.DrawString(_font, $"[{i + 1}] {cls.Name}", new Vector2(x + 10, py), selected ? Color.Gold : Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                py += 22;
                py += DrawWrappedText(cls.Role, x + 10, (int)py, cardW - 20, Color.LightSalmon, 0.6f, maxLines: 2);
                py += 4;
                py += DrawWrappedText(cls.Description, x + 10, (int)py, cardW - 20, Color.Silver, 0.58f, maxLines: 4);
                py += 8;

                _spriteBatch.DrawString(_font, "Chỉ số:", new Vector2(x + 10, py), Color.Gray, 0f, Vector2.Zero, 0.58f, SpriteEffects.None, 0f);
                py += 14;
                _spriteBatch.DrawString(_font, $"HP x{cls.StatMultipliers.HP:0.0}  LL x{cls.StatMultipliers.SpiritPower:0.0}  Tốc x{cls.StatMultipliers.Speed:0.0}",
                                        new Vector2(x + 10, py), Color.LightGreen, 0f, Vector2.Zero, 0.56f, SpriteEffects.None, 0f);
                py += 20;

                _spriteBatch.DrawString(_font, "Nội tại:", new Vector2(x + 10, py), Color.Gray, 0f, Vector2.Zero, 0.58f, SpriteEffects.None, 0f);
                py += 14;
                foreach (var passive in cls.Passives)
                {
                    _spriteBatch.DrawString(_font, $"- {passive.Name}", new Vector2(x + 10, py), Color.MediumSpringGreen, 0f, Vector2.Zero, 0.56f, SpriteEffects.None, 0f);
                    py += 14;
                }

                if (cls.HasElementalSkills)
                {
                    _spriteBatch.DrawString(_font, "(Chọn hệ Linh Căn tiếp theo)", new Vector2(x + 10, startY + cardH - 18), Color.Cyan, 0f, Vector2.Zero, 0.52f, SpriteEffects.None, 0f);
                }
            }

            _spriteBatch.DrawString(_font, "[1-4] Chọn lưu phái | [Enter] Xác nhận", new Vector2(30, 495), Color.Gold, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
        else
        {
            var cls = _classes[_selectedClassIndex];
            _spriteBatch.DrawString(_font, $"{cls.Name} — Chọn Hệ Linh Căn", new Vector2(220, 75), Color.LightSkyBlue, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

            string[] names = { "Hỏa", "Mộc", "Băng" };
            Color[] colors = { Color.OrangeRed, Color.LimeGreen, Color.LightSkyBlue };
            const int cardW = 200, cardH = 220, gap = 20, startX = 100, startY = 180;

            for (int i = 0; i < PhapTuElements.Length; i++)
            {
                int x = startX + i * (cardW + gap);
                bool selected = i == _selectedElementIndex;
                DrawRect(x, startY, cardW, cardH, selected ? new Color(45, 35, 70, 230) : new Color(20, 22, 38, 220));
                DrawRect(x, startY, cardW, cardH, selected ? Color.Gold : new Color(65, 75, 110), true);
                _spriteBatch.DrawString(_font, $"[{i + 1}] {names[i]}", new Vector2(x + 15, startY + 20), colors[i], 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);
            }

            _spriteBatch.DrawString(_font, "[1-3] Chọn hệ | [Enter] Xác nhận | [Backspace] Quay lại", new Vector2(30, 495), Color.Gold, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }

        _spriteBatch.End();
    }

    /// <summary>Vẽ văn bản tự xuống dòng trong một khung rộng maxWidth, tối đa maxLines dòng. Trả về chiều cao đã dùng.</summary>
    private float DrawWrappedText(string text, int x, int y, int maxWidth, Color color, float scale, int maxLines = int.MaxValue)
    {
        if (string.IsNullOrEmpty(text)) return 0f;

        float lineHeight = _font.MeasureString("Ag").Y * scale + 1f;
        string[] words = text.Split(' ');
        string line = string.Empty;
        float curY = y;
        int lineCount = 0;

        foreach (var word in words)
        {
            string testLine = string.IsNullOrEmpty(line) ? word : line + " " + word;
            if (_font.MeasureString(testLine).X * scale > maxWidth && !string.IsNullOrEmpty(line))
            {
                _spriteBatch.DrawString(_font, line, new Vector2(x, curY), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                curY += lineHeight;
                lineCount++;
                if (lineCount >= maxLines) return curY - y;
                line = word;
            }
            else
            {
                line = testLine;
            }
        }

        if (!string.IsNullOrEmpty(line))
        {
            _spriteBatch.DrawString(_font, line, new Vector2(x, curY), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            curY += lineHeight;
        }

        return curY - y;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Fonts/Arial");

        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // File .png nếu có sẽ được ưu tiên; nếu không, dùng hình vẽ thủ tục từ PixelArtGenerator
        _playerTexture = LoadTextureFromFile("Content/Sprites/player.png", () => PixelArtGenerator.CreatePlayerTexture(GraphicsDevice));
        _monsterPlantTexture = LoadTextureFromFile("Content/Sprites/monster_plant.png", () => PixelArtGenerator.CreateMonsterPlantTexture(GraphicsDevice));
        _monsterFireTexture = LoadTextureFromFile("Content/Sprites/monster_fire.png", () => PixelArtGenerator.CreateMonsterFireTexture(GraphicsDevice));
        _monsterIceTexture = LoadTextureFromFile("Content/Sprites/monster_ice.png", () => PixelArtGenerator.CreateMonsterIceTexture(GraphicsDevice));
        _needleTexture = LoadTextureFromFile("Content/Sprites/needle.png", () => PixelArtGenerator.CreateProjectileNeedleTexture(GraphicsDevice));
        _boltTexture = LoadTextureFromFile("Content/Sprites/bolt.png", () => PixelArtGenerator.CreateProjectileBoltTexture(GraphicsDevice));
        _pillTexture = LoadTextureFromFile("Content/Sprites/pill.png", () => PixelArtGenerator.CreatePillTexture(GraphicsDevice));
        _beastCoreTexture = LoadTextureFromFile("Content/Sprites/beast_core.png", () => PixelArtGenerator.CreateBeastCoreTexture(GraphicsDevice));
        _materialTexture = LoadTextureFromFile("Content/Sprites/material.png", () => PixelArtGenerator.CreateMaterialTexture(GraphicsDevice));
        _ringTexture = LoadTextureFromFile("Content/Sprites/ring.png", () => PixelArtGenerator.CreateRingTexture(GraphicsDevice, 32));
        _formationTexture = LoadTextureFromFile("Content/Sprites/formation_flag.png", () => PixelArtGenerator.CreateFormationFlagTexture(GraphicsDevice));
        _alchemistTexture = LoadTextureFromFile("Content/Sprites/alchemist.png", () => PixelArtGenerator.CreateAlchemistTexture(GraphicsDevice));
        _forgeTexture = LoadTextureFromFile("Content/Sprites/anvil.png", () => PixelArtGenerator.CreateFurnaceTexture(GraphicsDevice));
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
                Console.WriteLine($"[Đồ Họa] File không tồn tại: {absolutePath}. Sử dụng hình vẽ thủ tục.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Đồ Họa] Không thể tải {relativePath}: {ex.Message}. Sử dụng hình vẽ thủ tục.");
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

        if (_screen == GameScreen.ClassSelect)
        {
            HandleClassSelectInput(currentKeyState);
            _previousKeyState = currentKeyState;
            _previousMouseState = currentMouseState;
            base.Update(gameTime);
            return;
        }

        _gameTimeManager.Update(gameTime);
        _cultivationSystem.Update(gameTime);
        _projectilePool.Update(deltaTime);
        _skillSystem.Update(deltaTime);

        // Cập nhật chuyển động người chơi
        UpdatePlayerMovement(currentKeyState, deltaTime);

        Vector2 playerPos = _player.Position;

        // Camera bám theo nhân vật mượt mà (Lerp)
        Vector2 targetCamera = new Vector2(_player.PositionX - 400f, _player.PositionY - 300f);
        _cameraPosition = Vector2.Lerp(_cameraPosition, targetCamera, 0.1f);
        _cameraPosition.X = Math.Clamp(_cameraPosition.X, 0f, MAP_WIDTH - 800f);
        _cameraPosition.Y = Math.Clamp(_cameraPosition.Y, 0f, MAP_HEIGHT - 600f);

        // 1. Yêu Thú tự động tiến hóa theo tuổi thọ
        for (int i = _monsters.Count - 1; i >= 0; i--)
        {
            _monsters[i].UpdateEvolution(deltaTime, GameTimeManager.TIME_SCALE, playerPos);
        }

        // 1.5. Uy áp của Yêu Vương (>= 10.000 năm) khiến Yêu Thú yếu (< 1.000 năm) hoảng sợ bỏ chạy
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

        // 2. Trận Pháp tự động công kích
        foreach (var formation in _formations)
        {
            float oldCd = formation.CooldownTimer;
            formation.Update(deltaTime, _monsters, _projectilePool);
            if (formation.CooldownTimer > oldCd)
            {
                SpawnElementalBurst(formation.Position, Element.None, 8);
                TriggerShake(0.08f, 1.5f);
            }
        }

        // Cập nhật spawners và vật phẩm rơi
        _alchemistSpawner.Update(deltaTime);
        _forgeSpawner.Update(deltaTime);

        for (int i = _droppedItems.Count - 1; i >= 0; i--)
        {
            var item = _droppedItems[i];
            item.Update(deltaTime);

            if (Vector2.Distance(playerPos, item.Position) <= 25f && AddItemById(item.ItemId, item.Quantity))
            {
                _floatingTexts.Add(new FloatingText(playerPos - new Vector2(0, 25), $"+ {GetItemName(item.ItemId)} ({item.Quantity})", Color.LimeGreen));
                _droppedItems.RemoveAt(i);
            }
        }

        // Sát thương va chạm Yêu Thú (4 HP mỗi 0.5 giây cho mỗi con)
        var state = _player.Cultivation.CurrentState;
        if (state != CultivationState.Dead && state != CultivationState.Breakthrough)
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

        // Hệ thống hạt, rung màn hình, lôi kiếp
        UpdateParticles(deltaTime);
        UpdateScreenshake(deltaTime);
        UpdateProjectileTrails();
        UpdateMeditationVFX();
        UpdateBreakthroughVFX();
        UpdateLightningStrikes(deltaTime);

        // Cập nhật chữ nổi
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            _floatingTexts[i].Update(deltaTime);
            if (!_floatingTexts[i].Active)
            {
                _floatingTexts.RemoveAt(i);
            }
        }

        // Kiếm Ý (Kiếm Tu): cộng dồn sát thương khi đánh trúng, tự mất nếu ngừng đánh trúng
        if (_kiemYTimer > 0f)
        {
            _kiemYTimer -= deltaTime;
            if (_kiemYTimer <= 0f) _kiemYStacks = 0;
        }
        _combatSystem.PlayerDamageMultiplier = 1f + _kiemYStacks * KIEM_Y_PER_STACK;

        // Vùng hiệu ứng (chiêu kiểu "zone": mưa kiếm, vùng độc, vùng hồi máu...)
        _zoneSystem.Update(deltaTime, playerPos, _player.Cultivation.MaxHP, out float zoneHeal);
        if (zoneHeal > 0f) _player.Cultivation.Heal(zoneHeal);

        _combatSystem.PlayerBonusOnHit = _player.Cultivation.HasVanDocThe ? VanDocTheOnHit : Array.Empty<OnHitEffect>();
        _combatSystem.Update();
        HandleInput(currentKeyState, currentMouseState);
        UpdateWindowTitle();

        _previousKeyState = currentKeyState;
        _previousMouseState = currentMouseState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 18, 32));

        if (_screen == GameScreen.ClassSelect)
        {
            DrawClassSelectScreen();
            base.Draw(gameTime);
            return;
        }

        // Rung giật màn hình
        Vector2 shakeOffset = Vector2.Zero;
        if (_shakeTime > 0)
        {
            shakeOffset.X = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
            shakeOffset.Y = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
        }

        // Ma trận camera + màn hình rung
        Matrix worldTransform = Matrix.CreateTranslation(-_cameraPosition.X + shakeOffset.X, -_cameraPosition.Y + shakeOffset.Y, 0);

        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        Vector2 ringOrigin = new Vector2(_ringTexture.Width / 2f, _ringTexture.Height / 2f);

        // ====================================================================
        // PASS 1: RENDER WORLD SPRITES (PointClamp để giữ pixel art sắc nét)
        // ====================================================================
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, worldTransform);

        // 0. Nền đất lát gạch
        int tileSize = 64;
        int startTileX = Math.Max(0, (int)(_cameraPosition.X / tileSize) - 1);
        int startTileY = Math.Max(0, (int)(_cameraPosition.Y / tileSize) - 1);
        int endTileX = Math.Min((int)(MAP_WIDTH / tileSize), (int)((_cameraPosition.X + 800f) / tileSize) + 1);
        int endTileY = Math.Min((int)(MAP_HEIGHT / tileSize), (int)((_cameraPosition.Y + 600f) / tileSize) + 1);

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

                // Linh khí phát sáng ngẫu nhiên định sẵn
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

        // 1. Vòng báo hiệu lôi kiếp dưới đất
        foreach (var strike in _lightningStrikes)
        {
            if (strike.Landed) continue;
            float progress = 1f - Math.Clamp(strike.Delay / CultivationComponent.LIGHTNING_STRIKE_DELAY, 0f, 1f);
            _spriteBatch.Draw(_ringTexture, strike.Position, null, Color.Red * (0.25f + 0.55f * progress),
                              0f, ringOrigin, strike.Radius / ringOrigin.X, SpriteEffects.None, 0f);
        }

        // 1.2. Vùng hiệu ứng đang hoạt động (chiêu kiểu "zone")
        foreach (var zone in _zoneSystem.Zones)
        {
            Color zoneColor = zone.DamagePerTick > 0f ? Color.OrangeRed : Color.LimeGreen;
            float pulse = 0.5f + 0.15f * (float)Math.Sin(time * 5f);
            _spriteBatch.Draw(_ringTexture, zone.Position, null, zoneColor * (0.2f + 0.1f * pulse),
                              0f, ringOrigin, zone.Radius / ringOrigin.X, SpriteEffects.None, 0f);
        }

        // 1.5. NPC Đan Sư và Lò Luyện
        Vector2 alchemistOrigin = new Vector2(_alchemistTexture.Width / 2f, _alchemistTexture.Height / 2f);
        float alchemistScale = 32f / _alchemistTexture.Width;
        _spriteBatch.Draw(_alchemistTexture, AlchemistPosition, null, Color.White, 0f, alchemistOrigin, 1.5f * alchemistScale, SpriteEffects.None, 0f);

        Vector2 forgeOrigin = new Vector2(_forgeTexture.Width / 2f, _forgeTexture.Height / 2f);
        float forgeScale = 32f / _forgeTexture.Width;
        _spriteBatch.Draw(_forgeTexture, ForgePosition, null, Color.White, 0f, forgeOrigin, 1.5f * forgeScale, SpriteEffects.None, 0f);

        // 2. Vật phẩm rơi (chỉ phần hình ảnh)
        foreach (var item in _droppedItems)
        {
            if (!item.Active) continue;
            Texture2D tex = GetItemIcon(item.ItemId, out Color tint);
            float hoverY = (float)Math.Sin(item.HoverTimer * 4f) * 3f;
            Vector2 drawPos = new Vector2(item.Position.X, item.Position.Y + hoverY);
            Vector2 itemOrigin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            float itemSize = item.Type == InventoryComponent.TYPE_MAGIC_WEAPON ? 8f : 16f;
            _spriteBatch.Draw(tex, drawPos, null, tint, 0f, itemOrigin, 1.5f * itemSize / tex.Width, SpriteEffects.None, 0f);
        }

        // 3. Trận Pháp (Trận Kỳ + vòng phạm vi)
        foreach (var formation in _formations)
        {
            if (!formation.Active) continue;

            float recoil = 1.0f;
            if (formation.CooldownTimer > formation.FireRate - 0.2f)
            {
                float t = (formation.CooldownTimer - (formation.FireRate - 0.2f)) / 0.2f;
                recoil = 1.0f - 0.25f * t;
            }

            Vector2 origin = new Vector2(_formationTexture.Width / 2f, _formationTexture.Height / 2f);
            float flagScale = 24f / _formationTexture.Width;
            Vector2 drawScale = new Vector2(1.5f * flagScale, 1.5f * recoil * flagScale);

            _spriteBatch.Draw(_formationTexture, formation.Position, null, Color.White, 0f,
                              origin, drawScale, SpriteEffects.None, 0f);

            _spriteBatch.Draw(_ringTexture, formation.Position, null, Color.White * 0.15f,
                              0f, ringOrigin, formation.Range / ringOrigin.X, SpriteEffects.None, 0f);
        }

        // 4. Yêu Thú (hình ảnh + uy áp)
        foreach (var monster in _monsters)
        {
            if (!monster.Active) continue;

            Texture2D tex = monster.Element switch
            {
                Element.Wood => _monsterPlantTexture,
                Element.Fire => _monsterFireTexture,
                Element.Ice => _monsterIceTexture,
                _ => _monsterPlantTexture
            };

            Vector2 monsterOrigin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            float scale = (monster.Radius / (tex.Width / 2f)) * 1.2f;
            Color drawColor = monster.Age >= 100000 ? Color.Red : Color.White;
            if (monster.StatusEffects.Has(StatusType.Poison)) drawColor = Color.Lerp(drawColor, Color.MediumPurple, 0.45f);
            if (monster.StatusEffects.Has(StatusType.Burn)) drawColor = Color.Lerp(drawColor, Color.OrangeRed, 0.35f);
            if (!monster.StatusEffects.CanMove) drawColor = Color.Lerp(drawColor, Color.LimeGreen, 0.35f);

            _spriteBatch.Draw(tex, monster.Position, null, drawColor, 0f,
                              monsterOrigin, scale, SpriteEffects.None, 0f);

            if (monster.Age >= 10000)
            {
                Color auraColor = monster.Age >= 100000 ? Color.Red * 0.15f : Color.Purple * 0.12f;
                _spriteBatch.Draw(_ringTexture, monster.Position, null, auraColor,
                                  0f, ringOrigin, 150f / ringOrigin.X, SpriteEffects.None, 0f);
            }
        }

        // 5. Phi kiếm / pháp thuật đang bay
        foreach (var proj in _projectilePool.Projectiles)
        {
            if (!proj.Active) continue;

            Texture2D tex = proj.Element == Element.Fire ? _boltTexture : _needleTexture;
            Color bulletColor = proj.Element == Element.None ? Color.LightGray : Color.White;
            float rotation = (float)Math.Atan2(proj.Velocity.Y, proj.Velocity.X);
            Vector2 projOrigin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            float projScaleFactor = 8f / tex.Width;

            _spriteBatch.Draw(tex, proj.Position, null, bulletColor, rotation,
                              projOrigin, 1.5f * projScaleFactor, SpriteEffects.None, 0f);
        }

        // 6. Player
        Vector2 playerOrigin = new Vector2(_playerTexture.Width / 2f, _playerTexture.Height / 2f);
        float playerScaleFactor = 32f / _playerTexture.Width;
        if (_player.Cultivation.CurrentState != CultivationState.Dead)
        {
            float hover = 0f;
            if (_player.Cultivation.CurrentState == CultivationState.Meditating)
            {
                hover = (float)Math.Sin(time * 2f) * 4f;
            }

            _spriteBatch.Draw(_playerTexture, new Vector2(_player.PositionX, _player.PositionY + hover), null, Color.White, 0f,
                              playerOrigin, 1.5f * playerScaleFactor, SpriteEffects.None, 0f);
        }
        else
        {
            _spriteBatch.Draw(_playerTexture, _player.Position, null, Color.DimGray, (float)Math.PI / 2f,
                              playerOrigin, 1.5f * playerScaleFactor, SpriteEffects.None, 0f);
        }

        // 7. Tia sét đã giáng xuống
        foreach (var strike in _lightningStrikes)
        {
            if (strike.Landed && strike.FlashTimer > 0f)
            {
                DrawLightningBolt(strike);
            }
        }

        // 8. Hạt năng lượng (Particles)
        foreach (var p in _particles)
        {
            float alpha = 1f - (p.Elapsed / p.Lifetime);
            Color drawColor = p.Color * alpha;
            _spriteBatch.Draw(_pixelTexture, p.Position, null, drawColor, p.Rotation, new Vector2(0.5f, 0.5f), p.Size, SpriteEffects.None, 0f);
        }

        _spriteBatch.End();

        // ====================================================================
        // PASS 2: RENDER WORLD UI/TEXT/BARS (LinearClamp để chữ KHÔNG bị nhòe)
        // ====================================================================
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, worldTransform);

        DrawCenteredText("Đan Sư", AlchemistPosition - new Vector2(0, _alchemistTexture.Height * 1.5f * alchemistScale / 2f + 15f), Color.Plum, 0.7f);
        DrawCenteredText("Lò Luyện", ForgePosition - new Vector2(0, _forgeTexture.Height * 1.5f * forgeScale / 2f + 15f), Color.LightGray, 0.7f);

        if (!_isCraftingOpen && Vector2.Distance(_player.Position, ForgePosition) <= 60f)
        {
            DrawCenteredText("Nhấn [G] để Luyện Đan / Luyện Khí", ForgePosition - new Vector2(0, _forgeTexture.Height * 1.5f * forgeScale / 2f + 35f), Color.Gold, 0.65f);
        }

        // Nhãn tên vật phẩm rơi
        foreach (var item in _droppedItems)
        {
            if (!item.Active) continue;
            float hoverY = (float)Math.Sin(item.HoverTimer * 4f) * 3f;
            DrawCenteredText(item.Name, new Vector2(item.Position.X, item.Position.Y + hoverY - 18), Color.White * 0.8f, 0.6f);
        }

        // Linh lực còn lại của Trận Pháp
        foreach (var formation in _formations)
        {
            if (!formation.Active) continue;
            string chargeText = formation.AmmoCount <= 0 ? "Cạn linh lực" : $"{formation.AmmoCount}/{formation.MaxAmmo}";
            Color chargeColor = formation.AmmoCount <= 0 ? Color.Red : Color.LimeGreen;
            DrawCenteredText(chargeText, formation.Position - new Vector2(0, 25f), chargeColor, 0.6f);
        }

        // Thanh máu & tên Yêu Thú
        foreach (var monster in _monsters)
        {
            if (!monster.Active) continue;

            int hpBarW = (int)(monster.Radius * 1.8f);
            int hpBarH = 4;
            int hpX = (int)monster.Position.X - hpBarW / 2;
            int hpY = (int)monster.Position.Y - (int)(monster.Radius + 6);
            float hpRatio = monster.HP / monster.MaxHP;

            DrawRect(hpX, hpY, hpBarW, hpBarH, Color.Black);
            DrawRect(hpX, hpY, (int)(hpBarW * hpRatio), hpBarH, Color.Red);

            DrawCenteredText(monster.Name, new Vector2(monster.Position.X, hpY - 14), Color.Yellow, 0.7f);
        }

        // Chữ nổi (sát thương, thông báo)
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

        DrawHUD(time);

        if (_isInventoryOpen)
        {
            DrawInventoryUI();
        }

        if (_isCraftingOpen)
        {
            DrawCraftingUI();
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // ====================================================================
    // MOVEMENT & KEYBOARD
    // ====================================================================

    private void UpdatePlayerMovement(KeyboardState keys, float deltaTime)
    {
        var cult = _player.Cultivation;
        // Khi độ Thiên Kiếp vẫn được di chuyển để né lôi; xung quan thì phải ngồi yên
        bool dodgingTribulation = cult.CurrentState == CultivationState.Breakthrough && cult.IsHeavenlyTribulation;
        if (cult.CurrentState == CultivationState.Dead ||
            (cult.CurrentState == CultivationState.Breakthrough && !dodgingTribulation))
            return;

        float speed = BASE_MOVE_SPEED * cult.MoveSpeedMultiplier;
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

            // Bụi di chuyển dưới chân
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
        var cult = _player.Cultivation;

        // --- [M] Đả Tọa ---
        if (IsKeyJustPressed(keys, Keys.M))
        {
            if (cult.CurrentState == CultivationState.Meditating)
                cult.StopMeditating();
            else
                cult.StartMeditating();
        }

        // --- [Space] Ổn định đạo tâm khi đột phá / cheat +500 tu vi ---
        if (IsKeyJustPressed(keys, Keys.Space))
        {
            if (cult.CurrentState == CultivationState.Breakthrough)
            {
                cult.QTEPressCount++;
                SpawnElementalBurst(_player.Position, Element.None, 5);
            }
            else if (cult.AddExp(500f))
            {
                _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 30), "+500 Tu Vi (Cheat)", Color.SkyBlue));
            }
        }

        if (IsKeyJustPressed(keys, Keys.I))
        {
            _isInventoryOpen = !_isInventoryOpen;
        }

        if (IsKeyJustPressed(keys, Keys.Tab))
        {
            CycleEquippedWeapon();
        }

        // --- [R] Đột phá bình cảnh ---
        if (IsKeyJustPressed(keys, Keys.R))
        {
            TryStartBreakthrough();
        }

        // --- [G] Mở/Đóng Lò Luyện ---
        if (IsKeyJustPressed(keys, Keys.G))
        {
            if (Vector2.Distance(_player.Position, ForgePosition) <= 60f)
            {
                _isCraftingOpen = !_isCraftingOpen;
                if (_isCraftingOpen) _isInventoryOpen = true;
                Console.WriteLine($"[Lò Luyện] " + (_isCraftingOpen ? "MỞ" : "ĐÓNG"));
            }
            else
            {
                _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 40), "Hãy lại gần Lò Luyện!", Color.OrangeRed));
            }
        }

        // Tự động đóng Lò Luyện nếu đi quá xa
        if (_isCraftingOpen && Vector2.Distance(_player.Position, ForgePosition) > 75f)
        {
            _isCraftingOpen = false;
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 40), "Đã rời Lò Luyện", Color.OrangeRed));
        }

        // Chọn công thức bằng F1-F4 khi Lò Luyện mở
        if (_isCraftingOpen)
        {
            for (int i = 0; i < Math.Min(MAX_RECIPES_SHOWN, _recipes.Count); i++)
            {
                if (IsKeyJustPressed(keys, Keys.F1 + i))
                {
                    _selectedRecipeIndex = i;
                }
            }
        }

        // --- [F5] Lưu game vào MySQL ---
        if (IsKeyJustPressed(keys, Keys.F5))
        {
            if (_dbManager.IsConnected)
            {
                bool saved = _dbManager.SavePlayerState(_player, _skillLoadout, _monsters, _droppedItems, _formations);
                _floatingTexts.Add(saved
                    ? new FloatingText(_player.Position - new Vector2(0, 40), "Đã lưu game!", Color.Lime, 2.0f, 1.1f)
                    : new FloatingText(_player.Position - new Vector2(0, 40), "Lưu thất bại!", Color.Red));
            }
            else
            {
                _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 40), "MySQL offline - không thể lưu!", Color.OrangeRed));
            }
        }

        // --- [F9] Tải game từ MySQL ---
        if (IsKeyJustPressed(keys, Keys.F9))
        {
            LoadWorldFromDatabase();
        }

        // --- [Q] / [E] / [C] Chiêu 1-3, [Shift] Lướt, [X] Tuyệt kỹ ---
        if (IsKeyJustPressed(keys, Keys.Q))
        {
            TriggerSkill(SkillSlot.Skill1, mouseWorldPos);
        }

        if (IsKeyJustPressed(keys, Keys.E))
        {
            TriggerSkill(SkillSlot.Skill2, mouseWorldPos);
        }

        if (IsKeyJustPressed(keys, Keys.C))
        {
            TriggerSkill(SkillSlot.Skill3, mouseWorldPos);
        }

        if (IsKeyJustPressed(keys, Keys.LeftShift) || IsKeyJustPressed(keys, Keys.RightShift))
        {
            TriggerSkill(SkillSlot.Dash, mouseWorldPos);
        }

        if (IsKeyJustPressed(keys, Keys.X))
        {
            TriggerSkill(SkillSlot.Ultimate, mouseWorldPos);
        }

        // --- [T] Bày Trận Pháp ---
        if (IsKeyJustPressed(keys, Keys.T))
        {
            PlaceFormation();
        }

        // --- [F] Nạp Linh Thạch cho trận gần nhất ---
        if (IsKeyJustPressed(keys, Keys.F))
        {
            TryRechargeNearestFormation();
        }

        // --- [Y] Đổi loại trận bày tiếp theo ---
        if (IsKeyJustPressed(keys, Keys.Y))
        {
            _nextFormationType = _nextFormationType % 3 + 1;
            string formationName = FormationArray.GetFormationName(_nextFormationType);
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 30), $"Trận kế tiếp: {formationName}", Color.Gold));
            Console.WriteLine($"[Trận Pháp] Chọn trận bày tiếp theo: {formationName}");
        }

        // --- [U] Cheat: thức tỉnh Vạn Độc Thể ---
        if (IsKeyJustPressed(keys, Keys.U))
        {
            cult.UnlockVanDocThe();
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 40), "Vạn Độc Thể (Cheat)", Color.Magenta));
        }

        // --- [H] Cheat: nhận 3 Phá Cảnh Đan ---
        if (IsKeyJustPressed(keys, Keys.H) && AddItemById("dan_pha_canh", 3))
        {
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 15), "+3 Phá Cảnh Đan (Cheat)", Color.Yellow, 2f));
        }

        // Phím tắt dùng vật phẩm 1->5
        for (int i = 0; i < 5; i++)
        {
            if (IsKeyJustPressed(keys, Keys.D1 + i))
            {
                var items = _player.Inventory.Items;
                if (i < items.Count)
                {
                    string itemId = items[i].ItemId;
                    string itemName = GetItemName(itemId);
                    bool success = _player.Inventory.UseItem(itemId, _player);
                    _floatingTexts.Add(success
                        ? new FloatingText(_player.Position - new Vector2(0, 30), $"Dùng: {itemName}", Color.Lime)
                        : new FloatingText(_player.Position - new Vector2(0, 30), $"Không thể dùng {itemName}", Color.Gray));
                }
            }
        }

        // Chuột trái: đòn đánh thường (chiêu "basic" của lưu phái) hoặc thao tác Lò Luyện
        if (mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            bool clickInInventory = _isInventoryOpen && mouse.X >= 510 && mouse.Y <= 520;
            bool clickInCrafting = _isCraftingOpen &&
                                   mouse.X >= CRAFT_PANEL_X && mouse.X <= CRAFT_PANEL_X + CRAFT_PANEL_W &&
                                   mouse.Y >= CRAFT_PANEL_Y && mouse.Y <= CRAFT_PANEL_Y + CRAFT_PANEL_H;

            if (clickInCrafting)
            {
                HandleCraftingClick(mouse.X, mouse.Y);
            }
            else if (!clickInInventory)
            {
                TriggerSkill(SkillSlot.Basic, mouseWorldPos);
            }
        }

        // Chuột giữa: gọi Yêu Thú (phục vụ kiểm thử)
        if (mouse.MiddleButton == ButtonState.Pressed && _previousMouseState.MiddleButton == ButtonState.Released)
        {
            SpawnRandomMonster(mouseWorldPos);
        }
    }

    private void LoadWorldFromDatabase()
    {
        if (!_dbManager.IsConnected)
        {
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 40), "MySQL offline - không thể tải!", Color.OrangeRed));
            return;
        }

        bool loaded = _dbManager.LoadPlayerState(_player, _skillLoadout, _dataLoader, out string monstersJson, out string droppedItemsJson, out string formationsJson);
        if (!loaded)
        {
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 40), "Tải game thất bại!", Color.Red));
            return;
        }

        // 1. Dọn dẹp thế giới hiện tại
        _monsters.Clear();
        _formations.Clear();
        _droppedItems.Clear();
        _lightningStrikes.Clear();
        _projectilePool.Clear();
        _particles.Clear();
        _skillSystem.ResetCooldowns();
        _zoneSystem.Clear();
        _kiemYStacks = 0;
        _kiemYTimer = 0f;

        // Ghi nhận lại bộ chiêu đã mở khóa theo cảnh giới vừa nạp, tránh spam thông báo "lĩnh ngộ" sai
        _previouslyUnlockedSkillIds.Clear();
        foreach (var skill in _skillLoadout.GetUnlockedSkills(_player.Cultivation.CurrentRealm))
        {
            _previouslyUnlockedSkillIds.Add(skill.Id);
        }

        // 2. Phục hồi Yêu Thú
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
                            m.OnKilled += HandleMonsterKilled;
                            _monsters.Add(m);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi Tải Game] Không thể phục hồi Yêu Thú: {ex.Message}");
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
                        _droppedItems.Add(new DroppedItem(new Vector2(data.x, data.y), data.itemId, data.name, data.type, data.quantity));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi Tải Game] Không thể phục hồi vật phẩm rơi: {ex.Message}");
            }
        }

        // 4. Phục hồi Trận Pháp
        if (!string.IsNullOrEmpty(formationsJson))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<SavedFormationData>>(formationsJson);
                if (list != null)
                {
                    foreach (var data in list)
                    {
                        var f = new FormationArray(new Vector2(data.x, data.y), data.type);
                        f.AmmoCount = data.ammo;
                        f.MaxAmmo = data.maxAmmo;
                        _formations.Add(f);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi Tải Game] Không thể phục hồi Trận Pháp: {ex.Message}");
            }
        }

        _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 40), "Đã tải game!", Color.Cyan, 2.0f, 1.1f));
    }

    // ====================================================================
    // PHÁP THUẬT, PHÁP KHÍ & TRẬN PHÁP
    // ====================================================================

    /// <summary>
    /// Thử thi triển chiêu ở một ô kỹ năng. Chiêu "projectile" tự bắn ra bên trong SkillSystem;
    /// "dash" và "zone" được thi hành ở đây (ApplyDash / ZoneSystem.Spawn).
    /// </summary>
    private void TriggerSkill(SkillSlot slot, Vector2 targetPos)
    {
        var skill = _skillLoadout.GetSkill(slot);
        var result = _skillSystem.TryCast(_player.Cultivation, _skillLoadout, slot, _player.Position, targetPos, out var outcome);

        if (result == CastResult.Success)
        {
            ExecuteNonProjectileOutcome(skill!, targetPos, outcome);
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 55), $"{skill!.Name}!", Color.YellowGreen, 1.5f, 1.2f));
            return;
        }

        string? message = result switch
        {
            CastResult.NotLearned => "Ô chiêu trống!",
            CastResult.NotUnlocked when skill != null =>
                $"Cần đạt {CultivationComponent.GetRealmDisplayName(Enum.Parse<CultivationRealm>(skill.UnlockRealm, true))}!",
            CastResult.NotSupported => "Chiêu này chưa hỗ trợ!",
            CastResult.NotEnoughSpiritPower => "Không đủ Linh Lực!",
            CastResult.OnCooldown => $"Đang hồi chiêu ({_skillSystem.GetCooldownRemaining(skill!.Id):F1}s)",
            _ => null
        };
        if (message == null) return; // Incapacitated: im lặng, không spam thông báo

        _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 35), message, Color.OrangeRed));
    }

    /// <summary>Thi hành phần "dash" hoặc "zone" của chiêu vừa thi triển thành công (projectile đã tự bắn trong SkillSystem).</summary>
    private void ExecuteNonProjectileOutcome(SkillData skill, Vector2 targetPos, SkillCastOutcome outcome)
    {
        if (!SkillExecutionTypeExtensions.TryParse(skill.Type, out var execType)) return;

        if (execType == SkillExecutionType.Dash)
        {
            ApplyDash(skill, targetPos, outcome);
        }
        else if (execType == SkillExecutionType.Zone)
        {
            _zoneSystem.Spawn(targetPos, skill, outcome.EffectiveDamagePerTick);
            SpawnElementalBurst(targetPos, ElementExtensions.ParseElement(skill.Element), 10);
        }
    }

    /// <summary>Di chuyển tức thời theo hướng con trỏ, gây sát thương + đẩy lùi Yêu Thú quanh điểm đến.</summary>
    private void ApplyDash(SkillData skill, Vector2 targetPos, SkillCastOutcome outcome)
    {
        Vector2 origin = _player.Position;
        Vector2 dir = targetPos - origin;
        if (dir == Vector2.Zero) dir = new Vector2(1, 0);
        else dir.Normalize();

        Vector2 destination = origin + dir * skill.DashDistance;
        destination.X = Math.Clamp(destination.X, 16f, MAP_WIDTH - 16f);
        destination.Y = Math.Clamp(destination.Y, 16f, MAP_HEIGHT - 16f);

        _player.PositionX = destination.X;
        _player.PositionY = destination.Y;

        if (outcome.EffectiveDamage > 0f)
        {
            foreach (var monster in _combatSystem.QueryCircle(destination, DASH_IMPACT_RADIUS))
            {
                _combatSystem.ApplyHit(monster, outcome.EffectiveDamage, Element.None, isSilent: false, null, ProjectileOwner.Player, monster.Position);
                _combatSystem.ApplyKnockback(monster, origin, skill.Knockback);
            }
        }

        SpawnElementalBurst(origin, Element.None, 10);
        SpawnElementalBurst(destination, Element.None, 10);
        TriggerShake(0.1f, 2f);
    }

    /// <summary>So sánh bộ chiêu mở khóa trước/sau khi lên cảnh giới, thông báo chiêu mới lĩnh ngộ.</summary>
    private void AnnounceNewlyUnlockedSkills()
    {
        foreach (var skill in _skillLoadout.GetUnlockedSkills(_player.Cultivation.CurrentRealm))
        {
            if (_previouslyUnlockedSkillIds.Add(skill.Id))
            {
                _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 100), $"Lĩnh ngộ: {skill.Name}", Color.YellowGreen, 3.0f, 1.2f));
            }
        }
    }

    private void PlaceFormation()
    {
        var state = _player.Cultivation.CurrentState;
        if (state == CultivationState.Dead || state == CultivationState.Breakthrough) return;

        if (_formations.Count >= MAX_FORMATIONS)
        {
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 30), $"Tối đa {MAX_FORMATIONS} trận pháp!", Color.OrangeRed));
            return;
        }

        if (!_player.Inventory.RemoveItem(SPIRIT_STONE_ID, 1))
        {
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 30), "Cần 1 Linh Thạch để bày trận!", Color.Red));
            return;
        }

        var formation = new FormationArray(_player.Position, _nextFormationType);
        _formations.Add(formation);

        TriggerShake(0.15f, 3.0f);
        SpawnElementalBurst(_player.Position, Element.None, 15);

        _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 15), $"+ {formation.Name}", Color.Gold));
        Console.WriteLine($"[Trận Pháp] ⚙ Đã bày {formation.Name} tại {formation.Position.X:F0},{formation.Position.Y:F0} ({_formations.Count}/{MAX_FORMATIONS}).");
    }

    private void TryRechargeNearestFormation()
    {
        if (_player.Cultivation.CurrentState == CultivationState.Dead) return;

        Vector2 playerPos = _player.Position;
        var nearest = _formations
            .Where(f => f.Active && Vector2.Distance(playerPos, f.Position) <= 60f)
            .OrderBy(f => Vector2.Distance(playerPos, f.Position))
            .FirstOrDefault();

        if (nearest == null)
        {
            _floatingTexts.Add(new FloatingText(playerPos - new Vector2(0, 30), "Không có trận pháp gần đây!", Color.Gray));
            return;
        }

        if (nearest.AmmoCount >= nearest.MaxAmmo)
        {
            _floatingTexts.Add(new FloatingText(playerPos - new Vector2(0, 30), "Trận pháp đã đầy linh lực!", Color.Yellow));
            return;
        }

        if (!_player.Inventory.RemoveItem(SPIRIT_STONE_ID, 1))
        {
            _floatingTexts.Add(new FloatingText(playerPos - new Vector2(0, 30), "Không đủ Linh Thạch!", Color.Red));
            return;
        }

        nearest.Reload();
        _floatingTexts.Add(new FloatingText(nearest.Position - new Vector2(0, 20), "Nạp đầy linh lực!", Color.LimeGreen));
        Console.WriteLine($"[Trận Pháp] ⚙ Nạp Linh Thạch cho {nearest.Name} tại {nearest.Position.X:F0},{nearest.Position.Y:F0}.");
    }

    private void SpawnRandomMonster(Vector2 spawnPos)
    {
        string name;
        int age;
        float hp;
        Element elem;

        switch (_random.Next(3))
        {
            case 0:
                name = "Thanh Mộc Yêu Đằng";
                age = 90; // Sắp tấn thăng Nhị Giai
                hp = 180f;
                elem = Element.Wood;
                break;
            case 1:
                name = "Hỏa Vân Lang";
                age = 800; // Tam Giai
                hp = 400f;
                elem = Element.Fire;
                break;
            default:
                name = "Hàn Băng Mãng";
                age = 8000; // Ngũ Giai, sắp thành Yêu Vương
                hp = 900f;
                elem = Element.Ice;
                break;
        }

        var m = new Monster(name, age, hp, spawnPos, elem);
        m.OnKilled += HandleMonsterKilled;

        _monsters.Add(m);
        _floatingTexts.Add(new FloatingText(spawnPos, $"Xuất hiện: {m.Name}", Color.Tomato));
        Console.WriteLine($"[Hệ Thống] Đã sinh Yêu Thú '{m.Name}' {age} năm ({elem}) tại {spawnPos}");
    }

    private void TryStartBreakthrough()
    {
        var cult = _player.Cultivation;
        if (cult.CurrentState != CultivationState.BreakthroughReady)
        {
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 30), "Chưa chạm bình cảnh!", Color.OrangeRed));
            return;
        }

        if (cult.StartBreakthrough())
        {
            _isCraftingOpen = false;
            TriggerShake(0.3f, 4f);
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 50),
                cult.IsHeavenlyTribulation ? "THIÊN KIẾP GIÁNG LÂM!" : "BẮT ĐẦU XUNG QUAN!",
                cult.IsHeavenlyTribulation ? Color.Cyan : Color.Gold, 2.5f, 1.3f));
        }
    }

    private void CycleEquippedWeapon()
    {
        var weapons = _player.Inventory.Items.Where(i => i.Type == InventoryComponent.TYPE_MAGIC_WEAPON).ToList();
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
        _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 35), $"Trang bị: {weapons[index].Name}", Color.Gold));
    }

    // ====================================================================
    // THIÊN KIẾP
    // ====================================================================

    private void UpdateLightningStrikes(float deltaTime)
    {
        for (int i = _lightningStrikes.Count - 1; i >= 0; i--)
        {
            var strike = _lightningStrikes[i];

            if (!strike.Landed)
            {
                strike.Delay -= deltaTime;
                if (strike.Delay <= 0f)
                {
                    strike.Landed = true;
                    strike.FlashTimer = 0.25f;
                    TriggerShake(0.2f, 5f);
                    SpawnExplosion(strike.Position, Color.Cyan, 20);

                    if (Vector2.Distance(_player.Position, strike.Position) <= strike.Radius + 10f)
                    {
                        _player.Cultivation.TakeTribulationDamage(strike.Damage);
                        _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 30), $"-{strike.Damage:F0} Lôi Kiếp!", Color.Cyan, 1.3f, 1.1f));
                    }
                }
            }
            else
            {
                strike.FlashTimer -= deltaTime;
            }

            if (strike.Finished)
            {
                _lightningStrikes.RemoveAt(i);
            }
        }
    }

    /// <summary>Vẽ tia sét gấp khúc từ trên trời đánh xuống vị trí strike.</summary>
    private void DrawLightningBolt(LightningStrike strike)
    {
        var rng = new Random(strike.Seed);
        float alpha = Math.Clamp(strike.FlashTimer / 0.25f, 0f, 1f);
        const int segments = 8;
        Vector2 previous = strike.Position - new Vector2(0, 320f);

        for (int s = 1; s <= segments; s++)
        {
            Vector2 next = s == segments
                ? strike.Position
                : new Vector2(strike.Position.X + rng.Next(-14, 15), previous.Y + 320f / segments);

            DrawLine(previous, next, Color.DeepSkyBlue * (0.45f * alpha), 7f);
            DrawLine(previous, next, Color.White * alpha, 3f);
            previous = next;
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 delta = end - start;
        float angle = (float)Math.Atan2(delta.Y, delta.X);
        _spriteBatch.Draw(_pixelTexture, start, null, color, angle, new Vector2(0f, 0.5f),
                          new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
    }

    // ====================================================================
    // METADATA VẬT PHẨM
    // ====================================================================

    private string GetItemName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return "Tay không";
        return _consumables.FirstOrDefault(c => c.ItemId == itemId)?.Name
            ?? _magicWeapons.FirstOrDefault(w => w.ItemId == itemId)?.Name
            ?? itemId;
    }

    private Texture2D GetItemIcon(string itemId, out Color tint)
    {
        tint = Color.White;

        var weapon = _magicWeapons.FirstOrDefault(w => w.ItemId == itemId);
        if (weapon != null)
        {
            return ElementExtensions.ParseElement(weapon.Element) == Element.Fire ? _boltTexture : _needleTexture;
        }

        if (itemId.StartsWith("yeu_dan_")) return _beastCoreTexture;
        if (itemId.StartsWith("dan_")) return _pillTexture;

        tint = itemId switch
        {
            SPIRIT_STONE_ID => Color.LightSkyBlue,
            "han_thiet" => Color.Silver,
            "hoa_tinh_thach" => Color.OrangeRed,
            "linh_moc_tam" => Color.LimeGreen,
            _ => Color.White
        };
        return _materialTexture;
    }

    /// <summary>Thêm vật phẩm vào túi theo ID (tự tra Đan Dược hay Pháp Khí). False nếu ID không tồn tại.</summary>
    private bool AddItemById(string itemId, int quantity)
    {
        var consumable = _consumables.FirstOrDefault(c => c.ItemId == itemId);
        if (consumable != null)
        {
            _player.Inventory.AddConsumable(consumable, quantity);
            return true;
        }

        var weapon = _magicWeapons.FirstOrDefault(w => w.ItemId == itemId);
        if (weapon != null)
        {
            _player.Inventory.AddMagicWeapon(weapon, quantity);
            return true;
        }

        return false;
    }

    private void DropItem(string itemId, Vector2 position)
    {
        string type = _magicWeapons.Any(w => w.ItemId == itemId)
            ? InventoryComponent.TYPE_MAGIC_WEAPON
            : InventoryComponent.TYPE_CONSUMABLE;
        _droppedItems.Add(new DroppedItem(position, itemId, GetItemName(itemId), type, 1));
    }

    // ====================================================================
    // HUD & INVENTORY
    // ====================================================================

    private void DrawHUD(float time)
    {
        var cult = _player.Cultivation;
        int barW = 200;
        int barH = 12;
        int startX = 20;
        int startY = 15;
        int spacing = 18;

        // Khung nền HUD (viền vát 3D)
        DrawRect(startX - 10, startY - 5, 305, 170, new Color(20, 22, 38, 220));
        DrawRect(startX - 10, startY - 5, 305, 170, new Color(65, 75, 110), true);
        DrawRect(startX - 9, startY - 4, 303, 1, new Color(100, 115, 160, 150));

        // Đạo hiệu, lưu phái và cảnh giới
        string realmName = CultivationComponent.GetRealmDisplayName(cult.CurrentRealm);
        _spriteBatch.DrawString(_font, $"{_player.Name} | {_skillLoadout.PlayerClass.Name} | {realmName} tầng {cult.CurrentLevel}",
                                new Vector2(startX, startY), Color.Gold, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);

        int hpY = startY + spacing + 6;
        DrawPremiumBar(startX, hpY, barW, barH, cult.HP / cult.MaxHP, new Color(50, 15, 15), new Color(230, 45, 45), new Color(120, 40, 40), $"HP: {cult.HP:F0}/{cult.MaxHP:F0}", Color.Tomato);

        int expY = hpY + spacing;
        float expRatio = Math.Clamp(cult.CurrentExp / cult.MaxExpForCurrentLevel, 0f, 1f);
        DrawPremiumBar(startX, expY, barW, barH, expRatio, new Color(15, 15, 50), new Color(60, 130, 255), new Color(40, 90, 180), $"Tu vi: {cult.CurrentExp:F0}/{cult.MaxExpForCurrentLevel:F0}", Color.LightSkyBlue);

        int spY = expY + spacing;
        DrawPremiumBar(startX, spY, barW, barH, cult.SpiritPower / cult.MaxSpiritPower, new Color(10, 45, 20), new Color(45, 210, 110), new Color(30, 130, 70), $"Linh lực: {cult.SpiritPower:F0}/{cult.MaxSpiritPower:F0}", Color.MediumSpringGreen);

        // Pháp Khí + Trận Pháp
        int wY = spY + spacing;
        string weaponName = GetItemName(_player.Inventory.EquippedWeapon?.ItemId ?? string.Empty);
        _spriteBatch.DrawString(_font, $"Pháp khí: {weaponName} | Trận: {_formations.Count}/{MAX_FORMATIONS}", new Vector2(startX, wY + 5), Color.Khaki, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, $"Trận kế tiếp: {FormationArray.GetFormationName(_nextFormationType)} (Y)", new Vector2(startX, wY + 21), Color.Tan, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

        // Linh Căn và tỷ lệ đột phá
        _spriteBatch.DrawString(_font, $"Linh căn: {cult.GetSpiritRootName()}", new Vector2(startX, wY + 37), Color.LightSalmon, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, $"Đột phá: {cult.CalculateBreakthroughSuccessRate():P0} (Đan +{cult.PillBuff:P0}, Tâm ma -{cult.HeartDemon:P0})",
                                new Vector2(startX, wY + 53), cult.HeartDemon > 0 ? Color.Violet : Color.LightGreen, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

        // --- Thanh Chiêu (6 ô kỹ năng của lưu phái) ---
        int skillX = 335;
        int skillY = 15;
        int skillPanelH = 178;
        DrawRect(skillX, skillY, 300, skillPanelH, new Color(20, 22, 38, 220));
        DrawRect(skillX, skillY, 300, skillPanelH, new Color(65, 75, 110), true);
        DrawRect(skillX + 1, skillY + 1, 298, 1, new Color(100, 115, 160, 150));
        _spriteBatch.DrawString(_font, "THANH CHIÊU", new Vector2(skillX + 10, skillY + 8), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        DrawRect(skillX + 8, skillY + 24, 284, 1, new Color(65, 75, 110));

        (SkillSlot slot, string key)[] slotKeys =
        {
            (SkillSlot.Basic, "LMB"), (SkillSlot.Skill1, "Q"), (SkillSlot.Skill2, "E"),
            (SkillSlot.Skill3, "C"), (SkillSlot.Dash, "Shift"), (SkillSlot.Ultimate, "X")
        };

        int rowY = skillY + 30;
        foreach (var (slot, key) in slotKeys)
        {
            DrawSkillSlotRow(slot, key, skillX + 8, rowY, cult.CurrentRealm);
            rowY += 24;
        }

        int bannerY = skillY + skillPanelH + 5;

        // Thể chất Vạn Độc Thể
        if (cult.HasVanDocThe)
        {
            DrawRect(skillX, bannerY, 300, 28, new Color(30, 15, 45, 220));
            DrawRect(skillX, bannerY, 300, 28, Color.Purple, true);
            _spriteBatch.DrawString(_font, "[Vạn Độc Thể] +50HP +30LL 25% tẩm độc", new Vector2(skillX + 8, bannerY + 6), Color.Magenta, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);
            bannerY += 33;
        }

        // Kiếm Ý (nội tại Kiếm Tu)
        if (_skillLoadout.PlayerClass.Id == "kiem_tu" && _kiemYStacks > 0)
        {
            _spriteBatch.DrawString(_font, $"Kiếm Ý x{_kiemYStacks} (+{_kiemYStacks * 3}% sát thương)", new Vector2(skillX + 8, bannerY + 4), Color.Cyan, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        float pulse = (float)(Math.Sin(time * 8.0) * 0.4 + 0.6);

        // Nhắc nhở khi chạm bình cảnh
        if (cult.CurrentState == CultivationState.BreakthroughReady)
        {
            string hint = cult.CurrentLevel >= 30
                ? "BÌNH CẢNH! Nhấn [R] để độ Thiên Kiếp"
                : "BÌNH CẢNH! Nhấn [R] để xung kích bình cảnh";
            _spriteBatch.DrawString(_font, hint, new Vector2(100, 200), Color.Orange * pulse, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
        }

        // Cảnh báo khi đang đột phá + thanh ổn định đạo tâm
        if (cult.CurrentState == CultivationState.Breakthrough)
        {
            string warnMsg = cult.IsHeavenlyTribulation
                ? "!!! THIÊN KIẾP GIÁNG LÂM - Di chuyển né vòng đỏ !!!"
                : "!!! ĐANG XUNG QUAN - Nhấn [1-5] dùng Hồi Xuân Đan !!!";
            _spriteBatch.DrawString(_font, warnMsg, new Vector2(100, 195), (cult.IsHeavenlyTribulation ? Color.Cyan : Color.Red) * pulse, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

            _spriteBatch.DrawString(_font, "BẤM SPACE LIÊN TỤC ĐỂ ỔN ĐỊNH ĐẠO TÂM!", new Vector2(100, 220), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            float waveRatio = cult.TribulationTotalWaves > 0 ? (float)cult.TribulationWave / cult.TribulationTotalWaves : 0f;
            DrawPremiumBar(100, 245, 250, 15, waveRatio, new Color(30, 30, 45), Color.Gold, Color.Orange,
                           $"Đợt {cult.TribulationWave}/{cult.TribulationTotalWaves} | Tỷ lệ {cult.CalculateBreakthroughSuccessRate():P0} (Đạo tâm +{cult.QTEPressCount * 2}%)", Color.Gold);
        }

        // Bảng hướng dẫn ở đáy màn hình
        int guideY = 540;
        DrawRect(20, guideY, 760, 50, new Color(18, 18, 30, 220));
        DrawRect(20, guideY, 760, 50, new Color(55, 60, 85), true);
        DrawRect(21, guideY + 1, 758, 1, new Color(90, 100, 135, 150));
        _spriteBatch.DrawString(_font, "[WASD] Di chuyển | [Chuột trái] Đánh thường | [Chuột giữa] Gọi Yêu Thú | [T] Bày trận | [Y] Đổi trận | [F] Nạp Linh Thạch", new Vector2(35, guideY + 7), Color.Silver, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, "[M] Đả tọa | [R] Đột phá | [Q/E/C] Chiêu | [Shift] Lướt | [X] Tuyệt kỹ | [I] Túi | [G] Lò luyện | [1-5] Dùng | [F5/F9] Lưu/Tải", new Vector2(35, guideY + 27), Color.Gold, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);
    }

    /// <summary>Vẽ một dòng trong Thanh Chiêu: tên chiêu, và trạng thái (trống/khóa/hồi chiêu/sẵn sàng + bậc công pháp).</summary>
    private void DrawSkillSlotRow(SkillSlot slot, string key, int x, int y, CultivationRealm realm)
    {
        var skill = _skillLoadout.GetSkill(slot);
        if (skill == null)
        {
            _spriteBatch.DrawString(_font, $"[{key}] (Trống)", new Vector2(x, y), Color.DarkGray, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
            return;
        }

        string label;
        Color color;

        if (!_skillLoadout.IsUnlocked(slot, realm))
        {
            string requiredRealm = Enum.TryParse<CultivationRealm>(skill.UnlockRealm, true, out var required)
                ? CultivationComponent.GetRealmDisplayName(required)
                : skill.UnlockRealm;
            label = $"[{key}] {skill.Name} (Cần {requiredRealm})";
            color = Color.DarkGray;
        }
        else
        {
            float cooldown = _skillSystem.GetCooldownRemaining(skill.Id);
            if (cooldown > 0f)
            {
                label = $"[{key}] {skill.Name} ({cooldown:F1}s)";
                color = Color.OrangeRed;
            }
            else
            {
                int tier = _skillLoadout.GetMasteryTier(skill);
                label = tier > 0 ? $"[{key}] {skill.Name} · {SkillLoadoutComponent.GetMasteryTierName(tier)}" : $"[{key}] {skill.Name}";
                color = Color.MediumSpringGreen;
            }
        }

        _spriteBatch.DrawString(_font, label, new Vector2(x, y), color, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
    }

    private void DrawInventoryUI()
    {
        int startX = 515;
        int startY = 15;
        int width = 265;
        int height = 505;

        DrawRect(startX, startY, width, height, new Color(24, 26, 45, 230));
        DrawRect(startX, startY, width, height, new Color(80, 95, 140), true);
        DrawRect(startX + 1, startY + 1, width - 2, 1, new Color(110, 130, 190, 150));

        _spriteBatch.DrawString(_font, "TÚI TRỮ VẬT", new Vector2(startX + 70, startY + 15), Color.Gold, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
        DrawRect(startX + 15, startY + 40, width - 30, 2, new Color(80, 95, 140));

        var items = _player.Inventory.Items;
        int slotStartX = startX + 15;
        int slotStartY = startY + 55;
        int slotHeight = 65;
        int maxSlots = 6;

        for (int i = 0; i < maxSlots; i++)
        {
            int sY = slotStartY + i * (slotHeight + 8);
            DrawRect(slotStartX, sY, width - 30, slotHeight, new Color(15, 16, 28));

            if (i >= items.Count)
            {
                DrawRect(slotStartX, sY, width - 30, slotHeight, new Color(40, 43, 60), true);
                _spriteBatch.DrawString(_font, "Ô trống", new Vector2(slotStartX + 52, sY + 25), Color.DarkGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                continue;
            }

            var item = items[i];
            bool isEquipped = _player.Inventory.EquippedWeapon?.ItemId == item.ItemId;

            if (isEquipped)
            {
                DrawRect(slotStartX, sY, width - 30, slotHeight, Color.LimeGreen * 0.15f);
                DrawRect(slotStartX, sY, width - 30, slotHeight, Color.Gold, true);
            }
            else
            {
                DrawRect(slotStartX, sY, width - 30, slotHeight, new Color(60, 70, 100), true);
            }

            Texture2D iconTex = GetItemIcon(item.ItemId, out Color tint);
            float iconScale = 32f / Math.Max(iconTex.Width, iconTex.Height);
            _spriteBatch.Draw(iconTex, new Vector2(slotStartX + 25, sY + slotHeight / 2f), null, tint, 0f,
                              new Vector2(iconTex.Width / 2f, iconTex.Height / 2f), iconScale, SpriteEffects.None, 0f);

            _spriteBatch.DrawString(_font, $"[{i + 1}] {item.Name}", new Vector2(slotStartX + 52, sY + 8), isEquipped ? Color.Gold : Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, $"SL: {item.Quantity}", new Vector2(slotStartX + 52, sY + 28), Color.LimeGreen, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            string typeName = item.Type == InventoryComponent.TYPE_MAGIC_WEAPON
                ? "Pháp khí"
                : (item.Consumable?.Effects.Count ?? 0) > 0 ? "Đan dược" : "Nguyên liệu";
            _spriteBatch.DrawString(_font, typeName, new Vector2(slotStartX + 52, sY + 44), Color.Gray, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);
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

    private void DrawCenteredText(string text, Vector2 centerBottom, Color color, float scale)
    {
        Vector2 size = _font.MeasureString(text) * scale;
        _spriteBatch.DrawString(_font, text, new Vector2(centerBottom.X - size.X / 2f, centerBottom.Y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void UpdateWindowTitle()
    {
        var cult = _player.Cultivation;
        string stateName = cult.CurrentState switch
        {
            CultivationState.Idle => "Nhàn Rỗi",
            CultivationState.Meditating => "Đả Tọa ✦",
            CultivationState.BreakthroughReady => "⚠ BÌNH CẢNH",
            CultivationState.Breakthrough => cult.IsHeavenlyTribulation ? "⚡ ĐỘ THIÊN KIẾP" : "⚡ XUNG QUAN",
            CultivationState.Dead => "☠ TỬ VONG",
            _ => "???"
        };

        string realm = CultivationComponent.GetRealmDisplayName(cult.CurrentRealm);

        Window.Title = $"Tu Tiên | {_player.Name} | {realm} tầng {cult.CurrentLevel} | " +
                       $"HP: {cult.HP:F0}/{cult.MaxHP:F0} | " +
                       $"Linh lực: {cult.SpiritPower:F0}/{cult.MaxSpiritPower:F0} | " +
                       $"[{stateName}] | {_gameTimeManager.GetFormattedTime()}";
    }

    // ====================================================================
    // DATA & PRE-LOADS
    // ====================================================================

    private void LoadGameData()
    {
        // Tải trước dữ liệu từ JSON tĩnh để phòng hờ MySQL offline
        var localWeapons = _dataLoader.LoadMagicWeapons();
        var localConsumables = _dataLoader.LoadConsumables();

        if (_dbManager != null && _dbManager.IsConnected)
        {
            // Đồng bộ dữ liệu JSON vào MySQL
            _dbManager.MigrateJsonToMySql(localConsumables, localWeapons);

            // Nạp từ MySQL làm nguồn chính thức
            _magicWeapons = _dbManager.LoadMagicWeaponsFromDb();
            _consumables = _dbManager.LoadConsumablesFromDb();

            if (_magicWeapons.Count == 0) _magicWeapons = localWeapons;
            if (_consumables.Count == 0) _consumables = localConsumables;
        }
        else
        {
            _magicWeapons = localWeapons;
            _consumables = localConsumables;
            Console.WriteLine("[Hệ Thống] Đã nạp dữ liệu từ file JSON tĩnh (Offline Mode).");
        }

        BuildRecipes();

        // Nạp dữ liệu Lưu Phái / Chiêu Thức — kiểm tra tính toàn vẹn tham chiếu ngay lúc khởi động
        // để bắt lỗi đánh máy trong JSON sớm, trước khi người chơi vào màn hình chọn lưu phái.
        _classes = _dataLoader.LoadClasses();
        _skills = _dataLoader.LoadSkills();
        var classDataErrors = GameDataValidator.Validate(_classes, _skills);
        if (classDataErrors.Count == 0)
        {
            Console.WriteLine($"[Lưu Phái] Đã nạp {_classes.Count} lưu phái, {_skills.Count} chiêu thức — dữ liệu hợp lệ.");
        }
        else
        {
            Console.WriteLine($"[Lưu Phái] CẢNH BÁO: {classDataErrors.Count} lỗi dữ liệu trong classes.json/skills.json:");
            foreach (var error in classDataErrors)
            {
                Console.WriteLine($"           - {error}");
            }
        }
    }

    /// <summary>Gom công thức từ Pháp Khí và Đan Dược có crafting_recipe (data-driven).</summary>
    private void BuildRecipes()
    {
        _recipes = _magicWeapons
            .Where(w => w.CraftingRecipe.Count > 0)
            .Select(w => new CraftingRecipe(w.ItemId, w.Name, w.TierRequired, w.CraftingRecipe))
            .Concat(_consumables
                .Where(c => c.CraftingRecipe.Count > 0)
                .Select(c => new CraftingRecipe(c.ItemId, c.Name, c.TierRequired, c.CraftingRecipe)))
            .Take(MAX_RECIPES_SHOWN)
            .ToList();
    }

    private void GiveInitialInventoryItems()
    {
        AddItemById("dan_hoi_xuan", 5);
        AddItemById(SPIRIT_STONE_ID, 5);
        AddItemById("dan_pha_canh", 1);
        AddItemById("phi_kiem_thanh_phong", 1);
        AddItemById("kiem_hap_xich_diem", 1);

        _player.Inventory.EquipWeapon("phi_kiem_thanh_phong");
    }

    private bool IsKeyJustPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _previousKeyState.IsKeyUp(key);
    }

    private void AutoSave(float textOffsetY)
    {
        if (!_dbManager.IsConnected) return;
        _dbManager.SavePlayerState(_player, _skillLoadout, _monsters, _droppedItems, _formations);
        _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, textOffsetY), "Tự động lưu", Color.Lime * 0.7f, 1.5f, 0.9f));
    }

    private void SubscribeToEvents()
    {
        _eventManager.Subscribe<OnLevelUpEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ⬆ TĂNG TẦNG: {e.PlayerName} tầng {e.OldLevel} → tầng {e.NewLevel}");
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 50), $"Tầng {e.NewLevel}!", Color.Yellow, 2.0f, 1.2f));
            AutoSave(70);
        });

        _eventManager.Subscribe<OnRealmChangedEvent>(e =>
        {
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 90), $"Bước vào {e.NewRealm}!", Color.Gold, 3.0f, 1.3f));
            SpawnBreakthroughBurst(_player.Position);
            AnnounceNewlyUnlockedSkills();
        });

        _eventManager.Subscribe<OnBottleneckReachedEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ⚠ BÌNH CẢNH: {e.PlayerName} tại tầng {e.Level} ({e.CurrentRealm})");
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 50),
                e.IsHeavenlyTribulation ? "BÌNH CẢNH! [R] độ Thiên Kiếp" : "BÌNH CẢNH! [R] xung quan",
                Color.OrangeRed, 3.0f, 1.2f));
        });

        _eventManager.Subscribe<OnBreakthroughSuccessEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ★ ĐỘT PHÁ THÀNH CÔNG: {e.PlayerName} tầng {e.Level} (lần {e.BreakthroughNumber})");
            _lightningStrikes.Clear();
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 60), "ĐỘT PHÁ THÀNH CÔNG!", Color.Gold, 3.0f, 1.3f));
            SpawnBreakthroughBurst(_player.Position);
            TriggerShake(0.4f, 6f);
            AutoSave(80);
        });

        _eventManager.Subscribe<OnBreakthroughFailedEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ✗ ĐỘT PHÁ THẤT BẠI: {e.PlayerName} — {e.Reason} (Tỷ lệ: {e.SuccessRate:P1})");
            _lightningStrikes.Clear();
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 60), $"ĐỘT PHÁ THẤT BẠI! Rớt {e.LevelLost} tầng", Color.Red, 3.0f, 1.2f));
            TriggerShake(0.6f, 8f);
            SpawnExplosion(_player.Position, Color.MediumPurple, 30);
        });

        _eventManager.Subscribe<OnLightningStrikeEvent>(e =>
        {
            for (int i = 0; i < e.StrikeCount; i++)
            {
                // Đạo lôi đầu tiên nhắm thẳng vào người chơi, các đạo sau rải ngẫu nhiên quanh đó
                float spread = i == 0 ? 10f : 70f;
                Vector2 offset = new Vector2((float)(_random.NextDouble() * 2 - 1) * spread, (float)(_random.NextDouble() * 2 - 1) * spread);
                _lightningStrikes.Add(new LightningStrike(_player.Position + offset, CultivationComponent.LIGHTNING_STRIKE_DELAY, e.Damage, _random.Next()));
            }
        });

        _eventManager.Subscribe<OnDamageDealtEvent>(e =>
        {
            bool heavy = e.IsCounter || e.IsCrit;
            Color color = e.IsCounter ? Color.Red : e.IsCrit ? Color.Gold : Color.Orange;
            string text = $"-{e.Amount:F0}" + (e.IsCrit ? " CHÍ MẠNG!" : "") + (e.IsCounter ? " KHẮC HỆ!" : "");
            _floatingTexts.Add(new FloatingText(e.Position - new Vector2(0, 15), text, color, 1.3f, heavy ? 1.25f : 1.0f));
            TriggerShake(heavy ? 0.18f : 0.1f, heavy ? 4.5f : 2.5f);
            SpawnElementalBurst(e.ImpactPosition, e.Element, heavy ? 12 : 6);

            // Kiếm Ý (Kiếm Tu): mỗi đòn của người chơi đánh trúng +1 tầng, làm mới thời gian tự mất
            if (e.Owner == ProjectileOwner.Player && _skillLoadout?.PlayerClass.Id == "kiem_tu")
            {
                _kiemYStacks = Math.Min(KIEM_Y_MAX_STACKS, _kiemYStacks + 1);
                _kiemYTimer = KIEM_Y_DECAY_TIME;
            }
        });

        _eventManager.Subscribe<OnStatusAppliedEvent>(e =>
        {
            string label = StatusEffect.GetDisplayName(e.Status);
            if (StatusEffect.IsCrowdControl(e.Status)) label += $" ({e.Duration:F1}s)";
            Color color = e.Status switch
            {
                StatusType.Poison => Color.MediumPurple,
                StatusType.Burn => Color.OrangeRed,
                StatusType.Root => Color.LimeGreen,
                StatusType.Slow or StatusType.Freeze => Color.LightSkyBlue,
                StatusType.Stun => Color.Yellow,
                _ => Color.White
            };
            _floatingTexts.Add(new FloatingText(e.Position - new Vector2(0, 32), label, color, 1.3f));
        });

        _eventManager.Subscribe<OnElementalReactionEvent>(e =>
        {
            _floatingTexts.Add(new FloatingText(e.Position - new Vector2(0, 48), $"{e.ReactionName}!", Color.Gold, 1.5f, 1.1f));
        });

        _eventManager.Subscribe<OnPlayerDiedEvent>(e =>
        {
            Console.WriteLine($"[EVENT] ☠ TỬ VONG: {e.PlayerName} — {e.CauseOfDeath}");
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 60), "TỬ VONG!", Color.DarkRed, 4.0f, 1.4f));
            TriggerShake(0.8f, 10.0f);
            SpawnExplosion(_player.Position, Color.DarkRed, 40);
        });
    }

    // ====================================================================
    // HIỆU ỨNG HẠT, RUNG MÀN HÌNH VÀ THANH MÁU
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
        if (cult.CurrentState == CultivationState.Meditating && _random.NextDouble() < 0.12)
        {
            // Linh khí bay lên quanh cơ thể
            Vector2 pos = new Vector2(_player.PositionX + _random.Next(-12, 12), _player.PositionY + _random.Next(-4, 12));
            Vector2 vel = new Vector2(0, -25f);
            Color auraColor = _random.Next(2) == 0 ? Color.Gold * 0.8f : Color.SkyBlue * 0.8f;
            _particles.Add(new Particle(pos, vel, auraColor, (float)(_random.NextDouble() * 2.5 + 1.5), (float)(_random.NextDouble() * 0.8 + 0.6), ParticleType.Aura));
        }
    }

    private void UpdateBreakthroughVFX()
    {
        var cult = _player.Cultivation;
        if (cult.CurrentState != CultivationState.Breakthrough || _random.NextDouble() >= 0.25) return;

        // Linh khí thiên địa cuộn xoáy về phía người chơi
        Vector2 center = _player.Position;
        double angle = _random.NextDouble() * Math.PI * 2;
        Vector2 spawnPos = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 60f;

        Vector2 dir = center - spawnPos;
        float dist = dir.Length();
        dir.Normalize();
        Vector2 vel = dir * (dist * 1.5f + 50f);

        Color color = cult.HP < cult.MaxHP * 0.4f ? Color.Red * 0.9f
                    : cult.IsHeavenlyTribulation ? Color.DeepSkyBlue * 0.9f
                    : Color.Purple * 0.9f;
        if (_random.Next(3) == 0) color = Color.Gold * 0.9f;

        _particles.Add(new Particle(spawnPos, vel, color, (float)(_random.NextDouble() * 2 + 1.5), 0.7f, ParticleType.Spark));
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
        DrawRect(x, y, width, height, bgColor);

        int fillWidth = (int)(width * Math.Clamp(ratio, 0f, 1f));
        if (fillWidth > 0)
        {
            DrawRect(x, y, fillWidth, height, barColor);

            // Hiệu ứng thủy tinh 3D - nửa trên sáng bóng
            DrawRect(x, y, fillWidth, Math.Max(1, height / 3), Color.White * 0.22f);

            // Nửa dưới bóng mờ
            DrawRect(x, y + height - Math.Max(1, height / 3), fillWidth, Math.Max(1, height / 3), Color.Black * 0.18f);
        }

        DrawRect(x, y, width, height, borderColor, true);

        if (!string.IsNullOrEmpty(label))
        {
            _spriteBatch.DrawString(_font, label, new Vector2(x + width + 10, y - 2), textColor, 0f, Vector2.Zero, 0.78f, SpriteEffects.None, 0f);
        }
    }

    private void HandleMonsterKilled(Monster monster)
    {
        TriggerShake(0.35f, 6.0f);
        SpawnElementalBurst(monster.Position, monster.Element, 25);

        // Yêu Đan phẩm chất theo phẩm giai Yêu Thú
        int grade = monster.Grade;
        string coreId = grade <= 3 ? "yeu_dan_ha_pham" : grade <= 6 ? "yeu_dan_trung_pham" : "yeu_dan_thuong_pham";
        DropItem(coreId, monster.Position);
        _floatingTexts.Add(new FloatingText(monster.Position, $"Rơi {GetItemName(coreId)}!", Color.Yellow, 2.5f, 1.1f));

        // 40% rơi nguyên liệu theo hệ Yêu Thú
        if (_random.NextDouble() < 0.4)
        {
            string materialId = monster.Element switch
            {
                Element.Wood => "linh_moc_tam",
                Element.Fire => "hoa_tinh_thach",
                _ => "han_thiet"
            };
            DropItem(materialId, monster.Position + new Vector2(_random.Next(-15, 15), _random.Next(-15, 15)));
        }

        _monsters.Remove(monster);
    }

    // ====================================================================
    // LÒ LUYỆN (LUYỆN ĐAN / LUYỆN KHÍ)
    // ====================================================================

    private bool CanCraftRecipe(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= _recipes.Count) return false;
        var recipe = _recipes[recipeIndex];

        return (int)_player.Cultivation.CurrentRealm >= recipe.TierRequired &&
               recipe.Ingredients.All(ing => _player.Inventory.GetItemCount(ing.ItemId) >= ing.Quantity);
    }

    private void CraftRecipe(int recipeIndex)
    {
        if (!CanCraftRecipe(recipeIndex))
        {
            _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 30), "Không đủ nguyên liệu hoặc cảnh giới!", Color.Red));
            return;
        }

        var recipe = _recipes[recipeIndex];
        foreach (var ingredient in recipe.Ingredients)
        {
            _player.Inventory.RemoveItem(ingredient.ItemId, ingredient.Quantity);
        }

        AddItemById(recipe.OutputId, 1);
        _floatingTexts.Add(new FloatingText(_player.Position - new Vector2(0, 35), $"Luyện thành: {recipe.OutputName}!", Color.Lime, 2.0f, 1.1f));
        Console.WriteLine($"[Lò Luyện] Luyện thành công: {recipe.OutputName}!");

        TriggerShake(0.2f, 4f);
        for (int i = 0; i < 15; i++)
        {
            Vector2 sparkVel = new Vector2((float)(_random.NextDouble() * 100 - 50), (float)(_random.NextDouble() * -80 - 20));
            _particles.Add(new Particle(ForgePosition, sparkVel, Color.OrangeRed, 3f, 0.8f, ParticleType.Spark));
        }
    }

    private void HandleCraftingClick(int mouseX, int mouseY)
    {
        int slotLeft = CRAFT_PANEL_X + 15;
        int slotRight = CRAFT_PANEL_X + CRAFT_PANEL_W - 15;
        if (mouseX < slotLeft || mouseX > slotRight) return;

        for (int i = 0; i < Math.Min(MAX_RECIPES_SHOWN, _recipes.Count); i++)
        {
            int slotY = CRAFT_SLOT_START_Y + i * (CRAFT_SLOT_H + CRAFT_SLOT_GAP);
            if (mouseY >= slotY && mouseY <= slotY + CRAFT_SLOT_H)
            {
                _selectedRecipeIndex = i;
                return;
            }
        }

        if (mouseY >= CRAFT_BTN_Y && mouseY <= CRAFT_BTN_Y + CRAFT_BTN_H)
        {
            CraftRecipe(_selectedRecipeIndex);
        }
    }

    private void DrawCraftingUI()
    {
        int startX = CRAFT_PANEL_X;
        int startY = CRAFT_PANEL_Y;
        int width = CRAFT_PANEL_W;

        DrawRect(startX, startY, width, CRAFT_PANEL_H, new Color(24, 20, 38, 235));
        DrawRect(startX, startY, width, CRAFT_PANEL_H, new Color(110, 85, 140), true);
        DrawRect(startX + 1, startY + 1, width - 2, 1, new Color(160, 120, 190, 150));

        _spriteBatch.DrawString(_font, "LÒ LUYỆN (ĐAN / KHÍ)", new Vector2(startX + 18, startY + 10), Color.Gold, 0f, Vector2.Zero, 0.88f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, "[F1-F4] Chọn | [Click] Luyện", new Vector2(startX + 18, startY + 29), Color.Gray, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        DrawRect(startX + 15, startY + 46, width - 30, 2, new Color(110, 85, 140));

        int slotX = startX + 15;
        int slotW = width - 30;

        for (int i = 0; i < Math.Min(MAX_RECIPES_SHOWN, _recipes.Count); i++)
        {
            var recipe = _recipes[i];
            int sY = CRAFT_SLOT_START_Y + i * (CRAFT_SLOT_H + CRAFT_SLOT_GAP);
            bool isSelected = _selectedRecipeIndex == i;

            if (isSelected)
            {
                DrawRect(slotX, sY, slotW, CRAFT_SLOT_H, Color.Purple * 0.2f);
                DrawRect(slotX, sY, slotW, CRAFT_SLOT_H, Color.Gold, true);
            }
            else
            {
                DrawRect(slotX, sY, slotW, CRAFT_SLOT_H, new Color(20, 16, 28));
                DrawRect(slotX, sY, slotW, CRAFT_SLOT_H, new Color(80, 65, 100), true);
            }

            bool realmOk = (int)_player.Cultivation.CurrentRealm >= recipe.TierRequired;
            string realmReq = $"(Yêu cầu: {CultivationComponent.GetRealmDisplayName((CultivationRealm)recipe.TierRequired)})";

            _spriteBatch.DrawString(_font, $"{i + 1}. {recipe.OutputName}", new Vector2(slotX + 10, sY + 6), isSelected ? Color.Gold : Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, realmReq, new Vector2(slotX + 10, sY + 22), realmOk ? Color.DarkGray : Color.Tomato, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);

            for (int k = 0; k < recipe.Ingredients.Count && k < 3; k++)
            {
                DrawIngredientStatus(recipe.Ingredients[k].ItemId, recipe.Ingredients[k].Quantity, slotX + 15, sY + 40 + k * 15);
            }
        }

        bool canCraftSelected = CanCraftRecipe(_selectedRecipeIndex);
        Color btnColor = canCraftSelected ? new Color(50, 160, 50) : new Color(80, 80, 80);
        Color borderBtnColor = canCraftSelected ? Color.Lime : Color.DarkGray;

        DrawRect(slotX, CRAFT_BTN_Y, slotW, CRAFT_BTN_H, btnColor);
        DrawRect(slotX, CRAFT_BTN_Y, slotW, CRAFT_BTN_H, borderBtnColor, true);

        if (canCraftSelected)
        {
            DrawRect(slotX, CRAFT_BTN_Y, slotW, Math.Max(1, CRAFT_BTN_H / 3), Color.White * 0.15f);
        }

        string btnText = "BẮT ĐẦU LUYỆN";
        Vector2 textSize = _font.MeasureString(btnText) * 0.9f;
        _spriteBatch.DrawString(_font, btnText, new Vector2(slotX + slotW / 2f - textSize.X / 2f, CRAFT_BTN_Y + CRAFT_BTN_H / 2f - textSize.Y / 2f),
                               canCraftSelected ? Color.White : Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
    }

    private void DrawIngredientStatus(string itemId, int required, int x, int y)
    {
        int owned = _player.Inventory.GetItemCount(itemId);
        Color textColor = owned >= required ? Color.LimeGreen : Color.Tomato;
        _spriteBatch.DrawString(_font, $"- {GetItemName(itemId)}: {owned}/{required}", new Vector2(x, y), textColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    /// <summary>Một công thức Lò Luyện (gom từ crafting_recipe của Pháp Khí và Đan Dược).</summary>
    private sealed record CraftingRecipe(string OutputId, string OutputName, int TierRequired, List<CraftingIngredient> Ingredients);

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

    private struct SavedFormationData
    {
        public int type { get; set; }
        public int ammo { get; set; }
        public int maxAmmo { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }
}
