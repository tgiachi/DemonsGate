using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Components;
using SquidCraft.Client.Context;
using SquidCraft.Client.Data;
using SquidCraft.Client.Services;
using SquidCraft.Client.Components.UI.Controls;
using SquidCraft.Client.Scenes;
using SquidCraft.Core.Json;
using SquidCraft.Game.Data.Types;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Assets;
using SquidCraft.Game.Data.Context;

namespace SquidCraft.Client;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private readonly ILogger _logger;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ImGUIDebuggerService _imGUIDebuggerService;
    private WorldComponent? _worldComponent;
    private CameraComponent? _cameraComponent;
    private BlockOutlineComponent? _blockOutlineComponent;
    private ProgressBarComponent? _progressBarComponent;
    private float _progressTimer;
    private ScrollingTextBoxComponent? _logTextBox;
    private ChatBoxComponent? _chatBox;
    private static readonly RasterizerState ScissorRasterizerState = new() { ScissorTestEnable = true };
    private UITestScene? _uiTestScene;
    private bool _isUITestMode;

    public Game1()
    {
        JsonUtils.RegisterJsonContext(SquidCraftClientJsonContext.Default);
        JsonUtils.RegisterJsonContext(SquidCraftGameJsonContext.Default);
        JsonUtils.RegisterJsonContext(SquidCraftClientJsonContext.Default);

        Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
            .WriteTo.Console(formatProvider: Thread.CurrentThread.CurrentCulture)
            .CreateLogger();

        _logger = Log.ForContext<Game1>();

        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    protected override void Initialize()
    {
        SquidCraftClientContext.SceneManager = new SceneManager();
        SquidCraftClientContext.RootComponent.AddChild(SquidCraftClientContext.SceneManager);

        _imGUIDebuggerService = new ImGUIDebuggerService(this);

        IsMouseVisible = false;

        base.Initialize();
    }


    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        SquidCraftClientContext.GraphicsDevice = GraphicsDevice;
        SquidCraftClientContext.AssetManagerService = new AssetManagerService(
            Path.Combine(Directory.GetCurrentDirectory()),
            GraphicsDevice
        );

        SquidCraftClientContext.BlockManagerService = new BlockManagerService();
        SquidCraftClientContext.AssetManagerService.LoadFontTtf("Fonts/DefaultFont.ttf", "DefaultFont");
        SquidCraftClientContext.AssetManagerService.LoadTexture("Textures/default_blocks.png", "DefaultBlocks");


        SquidCraftClientContext.AssetManagerService.LoadAtlas(
            "DefaultBlocks",
            new AtlasDefinition()
            {
                TileWidth = 16,
                TileHeight = 16
            },
            "DefaultBlocksAtlas"
        );

        var blocks = JsonUtils.DeserializeFromFile<BlockDefinitionData[]>(
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Blocks", "blocks.json")
        );

        foreach (var block in blocks)
        {
            SquidCraftClientContext.BlockManagerService.AddBlockDefinition("DefaultBlocksAtlas", block);
        }




        _cameraComponent = new CameraComponent(GraphicsDevice)
        {
            Position = new Vector3(9f, ChunkEntity.Height + 10f, 9f),
            Pitch = -45f,
            MoveSpeed = 25f,
            MouseSensitivity = 0.1f,
            EnableInput = true,
            EnablePhysics = true,
            FlyMode = false,
            Gravity = 32f,
            JumpForce = 10f,
            IsMouseCaptured = true
        };

        var watchTextComponent = new WatchTextComponent(
            new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2),
            TimeSpan.FromSeconds(1),
            () => $"X: {_cameraComponent.Position.X:F1} " +
                  $"Y: {_cameraComponent.Position.Y:F1} " +
                  $"Z: {_cameraComponent.Position.Z:F1} | " +
                  $"Yaw: {_cameraComponent.Yaw:F1} " +
                  $"Pitch: {_cameraComponent.Pitch:F1}"
        );

        // // Add day/night cycle display
        // var timeDisplayComponent = new WatchTextComponent(
        //     new Vector2(16, 32),
        //     TimeSpan.FromSeconds(0.5f),
        //     () => {
        //         if (_worldComponent != null)
        //         {
        //             var timeOfDay = _worldComponent.DayNightCycle.TimeOfDay;
        //             var hour = (int)(timeOfDay * 24);
        //             var minute = (int)((timeOfDay * 24 - hour) * 60);
        //             var sunIntensity = _worldComponent.DayNightCycle.GetSunIntensity();
        //             return $"Time: {hour:D2}:{minute:D2} | Sun: {(sunIntensity * 100):F0}%";
        //         }
        //         return "Time: --:--";
        //     }
        // );

        SquidCraftClientContext.RootComponent.AddChild(watchTextComponent);
        // SquidCraftClientContext.RootComponent.AddChild(timeDisplayComponent);

            _worldComponent = new WorldComponent(GraphicsDevice, _cameraComponent)
            {
                ViewRange = 150f,
                EnableFrustumCulling = true,
                MaxRaycastDistance = 10f,
                ChunkLoadDistance = 2,
                MaxChunkBuildsPerFrame = 5,
                GenerationDistance = 3,
                ChunkGenerator = CreateFlatChunk
            };

        _cameraComponent.CheckCollision = (pos, size) => _worldComponent.IsBlockSolid(pos);

        // // Test DayNightCycle
        // _logger.Information("DayNightCycle created, initial time: {_time}", _worldComponent.DayNightCycle.TimeOfDay);

        _blockOutlineComponent = new BlockOutlineComponent(GraphicsDevice)
        {
            OutlineColor = Color.White * 0.8f
        };


        var viewport = GraphicsDevice.Viewport;
        SquidCraftClientContext.RootComponent.Size = new Vector2(viewport.Width, viewport.Height);
        SquidCraftClientContext.SceneManager.Size = SquidCraftClientContext.RootComponent.Size;

        var textComponent = new TextComponent()
        {
            FontSize = 24
        };
        textComponent.Text = "SquidCraft";

        SquidCraftClientContext.RootComponent.AddChild(textComponent);

        var fpsComponent = new FpsComponent(position: new Vector2(16, 16), fontSize: 16, color: Color.White);
        fpsComponent.ZIndex = 100;
        SquidCraftClientContext.RootComponent.AddChild(fpsComponent);

        var inspectorWindow = new WindowComponent("Inspector")
        {
            Position = new Vector2(32, 72),
            Size = new Vector2(320, 280)
        };
        inspectorWindow.ContentPanel.Spacing = 10f;
        inspectorWindow.ContentPanel.Padding = Vector2.Zero;
        inspectorWindow.ContentPanel.AutoSize = false;
        inspectorWindow.ContentPanel.HasFocus = true;

        SquidCraftClientContext.RootComponent.AddChild(inspectorWindow);

        var labelComponent = new LabelComponent("Enter block label:", fontSize: 16);
        inspectorWindow.AddContent(labelComponent);

        var textBoxComponent = new TextBoxComponent()
        {
            PreferredWidth = 260f,
            PlaceholderText = "Type here..."
        };
        inspectorWindow.AddContent(textBoxComponent);

        var buttonComponent = new ButtonComponent("Apply Label");
        buttonComponent.Clicked += (_, _) =>
        {
            _logger.Information("Apply button clicked with input: {Input}", textBoxComponent.Text);
            _logTextBox?.AppendLine($"[{DateTime.Now:HH:mm:ss}] Apply clicked with '{textBoxComponent.Text}'");
        };
        inspectorWindow.AddContent(buttonComponent);

        var comboBoxComponent = new ComboBoxComponent(
            new[] { "Grass", "Dirt", "Stone", "Snow", "Water" }
        );
        comboBoxComponent.Width = 260f;
        comboBoxComponent.SelectedIndexChanged += (_, index) =>
        {
            var item = comboBoxComponent.SelectedItem ?? "<none>";
            _logger.Information("ComboBox selection changed to {Index}:{Value}", index, item);
            _logTextBox?.AppendLine($"[{DateTime.Now:HH:mm:ss}] ComboBox -> {item}");
        };
        inspectorWindow.AddContent(comboBoxComponent);

        _progressBarComponent = new ProgressBarComponent(size: new Vector2(260, 24))
        {
            Minimum = 0f,
            Maximum = 1f,
            ShowLabel = true,
            LabelFormat = "{0:P0}"
        };
        inspectorWindow.AddContent(_progressBarComponent);

        _logTextBox = new ScrollingTextBoxComponent(position: new Vector2(340, 80), size: new Vector2(320, 220))
        {
            AutoScroll = true,
            MaxLines = 200
        };
        _logTextBox.AppendLine("[Log] UI initialized.");
        SquidCraftClientContext.RootComponent.AddChild(_logTextBox);

        var toolTip = new ToolTipComponent();
        toolTip.Show(new Vector2(340, 320), "WASD: move | Space/Shift: up/down | Mouse: look");
        SquidCraftClientContext.RootComponent.AddChild(toolTip);

        _chatBox = new ChatBoxComponent(
            position: new Vector2(10, GraphicsDevice.Viewport.Height - 310),
            size: new Vector2(500, 300))
        {
            FadeDelay = 5f,
            AlwaysVisible = false,
            MaxMessages = 100
        };
        
        _chatBox.MessageSent += OnChatMessageSent;
        _chatBox.CommandExecuted += OnChatCommandExecuted;
        
        _chatBox.Initialize();
        SquidCraftClientContext.RootComponent.AddChild(_chatBox);
        
        _chatBox.AddSystemMessage("Welcome to SquidCraft!");
        _chatBox.AddSystemMessage("Press T to open chat");
        _chatBox.AddMessage("Use /help to see available commands", ChatMessageType.Info);

        _uiTestScene = new UITestScene();
        _uiTestScene.Load();
        _isUITestMode = true;
        if (_cameraComponent != null)
        {
            _cameraComponent.EnableInput = false;
            _cameraComponent.IsMouseCaptured = false;
        }
        IsMouseVisible = true;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Toggle UI test mode with F1
        if (Keyboard.GetState().IsKeyDown(Keys.F1) && !_isUITestMode)
        {
            _isUITestMode = true;
            _uiTestScene?.Load();
            if (_cameraComponent != null)
            {
                _cameraComponent.EnableInput = false;
                _cameraComponent.IsMouseCaptured = false;
            }
            IsMouseVisible = true;
        }
        else if (Keyboard.GetState().IsKeyDown(Keys.F2) && _isUITestMode)
        {
            _isUITestMode = false;
            _uiTestScene?.Unload();
            if (_cameraComponent != null)
            {
                _cameraComponent.EnableInput = true;
                _cameraComponent.IsMouseCaptured = true;
            }
            IsMouseVisible = false;
        }

        if (_isUITestMode)
        {
            _uiTestScene?.Update(gameTime);
        }
        else
        {
            var isChatActive = _chatBox?.IsInputActive ?? false;
            
            if (_cameraComponent != null)
            {
                _cameraComponent.EnableInput = !isChatActive;
                _cameraComponent.IsMouseCaptured = !isChatActive;
            }
            
            IsMouseVisible = !(_cameraComponent?.IsMouseCaptured ?? false);
        }

        _progressTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_progressBarComponent != null)
        {
            var progress = (MathF.Sin(_progressTimer) + 1f) * 0.5f;
            _progressBarComponent.Value = progress;
            if (_logTextBox != null)
            {
                var logLine = $"[{gameTime.TotalGameTime:c}] Progress: {progress:P0}";
                if ((int)(_progressTimer * 2f) % 20 == 0)
                {
                    _logTextBox.AppendLine(logLine);
                }
            }
        }

        // Handle block breaking with left mouse click
        if (!_isUITestMode && Mouse.GetState().LeftButton == ButtonState.Pressed)
        {
            if (_worldComponent?.SelectedBlock is var selected && selected.HasValue)
            {
                var (chunk, x, y, z) = selected.Value;
                var blockWorldPos = chunk.Position + new Vector3(x, y, z);

                // Remove the block (set to air)
                chunk.Chunk?.SetBlock(x, y, z, new BlockEntity(0, BlockType.Air));

                // Invalidate chunk mesh and lighting for this chunk and all adjacent chunks
                _worldComponent?.InvalidateBlockAndAdjacentChunks(chunk, x, y, z);

                // Spawn particles at block position
                _worldComponent.SpawnParticles(blockWorldPos + new Vector3(0.5f, 0.5f, 0.5f), 20, spread: 0.5f, speed: 3f, lifeTime: 1.5f, Color.Orange);

                _logger.Information("Block broken at {Position}", blockWorldPos);
            }
        }

        // Handle block placing with right mouse click
        if (!_isUITestMode && Mouse.GetState().RightButton == ButtonState.Pressed)
        {
            // For placing, we need to find an adjacent air block to the selected block
            if (_worldComponent?.SelectedBlock is var selected && selected.HasValue)
            {
                var (chunk, x, y, z) = selected.Value;

                // Try to place on top of the selected block first
                var placeX = x;
                var placeY = y + 1;
                var placeZ = z;

                // Check if the position is valid and empty
                if (placeY < ChunkEntity.Height)
                {
                    var existingBlock = chunk.Chunk?.GetBlock(placeX, placeY, placeZ);
                    if (existingBlock == null || existingBlock.BlockType == BlockType.Air)
                    {
                        var blockWorldPos = chunk.Position + new Vector3(placeX, placeY, placeZ);

                        // Place a dirt block
                        chunk.Chunk?.SetBlock(placeX, placeY, placeZ, new BlockEntity(1, BlockType.Dirt));

                        // Invalidate chunk mesh and lighting for this chunk and all adjacent chunks
                        _worldComponent?.InvalidateBlockAndAdjacentChunks(chunk, placeX, placeY, placeZ);

                        // Spawn particles at block position
                        _worldComponent.SpawnParticles(blockWorldPos + new Vector3(0.5f, 0.5f, 0.5f), 15, spread: 0.3f, speed: 2f, lifeTime: 1f, Color.Brown);

                        _logger.Information("Block placed at {Position}", blockWorldPos);
                    }
                }
            }
        }

        _worldComponent?.Update(gameTime);
        //_blockPreviewComponent?.Update(gameTime);
        SquidCraftClientContext.RootComponent.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_isUITestMode)
        {
            _spriteBatch.Begin();
            // Draw UI test scene
            _uiTestScene?.Draw(gameTime, _spriteBatch);

            _spriteBatch.End();
        }
        else
        {
            // Dynamic sky color based on time of day
            // var skyColor = Color.CornflowerBlue; // Default fallback
            // if (_worldComponent != null)
            // {
            //     var sunColor = _worldComponent.DayNightCycle.GetSunColor();
            //     var sunIntensity = _worldComponent.DayNightCycle.GetSunIntensity();

            //     // Create sky color based on sun color and intensity
            //     var timeOfDay = _worldComponent.DayNightCycle.TimeOfDay;

            //     if (timeOfDay < 0.2f || timeOfDay > 0.8f)
            //     {
            //         // Night sky: dark blue with slight sun color tint
            //         skyColor = new Color(0.1f, 0.1f, 0.3f) * 0.3f;
            //     }
            //     else if (timeOfDay >= 0.2f && timeOfDay <= 0.3f)
            //     {
            //         // Sunrise: blend from night to day
            //         var t = (timeOfDay - 0.2f) / 0.1f;
            //         var nightSky = new Color(0.1f, 0.1f, 0.3f) * 0.3f;
            //         var daySky = new Color(0.4f, 0.6f, 1.0f);
            //         skyColor = Color.Lerp(nightSky, daySky, t);
            //     }
            //     else if (timeOfDay >= 0.7f && timeOfDay <= 0.8f)
            //     {
            //         // Sunset: blend from day to night
            //         var t = (timeOfDay - 0.7f) / 0.1f;
            //         var daySky = new Color(0.4f, 0.6f, 1.0f);
            //         var nightSky = new Color(0.1f, 0.1f, 0.3f) * 0.3f;
            //         skyColor = Color.Lerp(daySky, nightSky, t);
            //     }
            //     else
            //     {
            //         // Day sky: light blue
            //         skyColor = new Color(0.4f, 0.6f, 1.0f);
            //     }
            // }

            GraphicsDevice.Clear(Color.CornflowerBlue);

            var viewportBounds = GraphicsDevice.Viewport.Bounds;
            GraphicsDevice.ScissorRectangle = viewportBounds;

            _worldComponent?.Draw(gameTime);
            //_blockPreviewComponent?.Draw3D(gameTime);

            if (_worldComponent?.SelectedBlock is var selected && selected.HasValue)
            {
                var (chunk, x, y, z) = selected.Value;
                var blockWorldPos = chunk.Position + new Vector3(x, y, z);
                _blockOutlineComponent?.Draw(blockWorldPos, _cameraComponent!.View, _cameraComponent.Projection);
            }

            _spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                ScissorRasterizerState
            );
            GraphicsDevice.ScissorRectangle = viewportBounds;

            //_blockPreviewComponent?.DrawLabels(_spriteBatch);
            SquidCraftClientContext.RootComponent.Draw(_spriteBatch, gameTime);

            _spriteBatch.End();

            _imGUIDebuggerService.Draw(gameTime, _spriteBatch);
        }

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _worldComponent?.Dispose();
        _blockOutlineComponent?.Dispose();
        _uiTestScene?.Unload();
        base.UnloadContent();
    }

    private void OnChatMessageSent(object? sender, string message)
    {
        _logger.Information("Chat message sent: {Message}", message);
        _chatBox?.AddSystemMessage($"Message sent: {message}");
    }

    private void OnChatCommandExecuted(object? sender, string command)
    {
        _logger.Information("Chat command executed: {Command}", command);
        
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLower();

        switch (cmd)
        {
            case "/help":
                _chatBox?.AddSystemMessage("Available commands:");
                _chatBox?.AddMessage("/help - Show this message", ChatMessageType.Info);
                _chatBox?.AddMessage("/clear - Clear chat", ChatMessageType.Info);
                _chatBox?.AddMessage("/time - Show current time", ChatMessageType.Info);
                _chatBox?.AddMessage("/pos - Show player position", ChatMessageType.Info);
                _chatBox?.AddMessage("/fly - Toggle fly mode", ChatMessageType.Info);
                _chatBox?.AddMessage("/water - Place water block in front of you", ChatMessageType.Info);
                break;

            case "/clear":
                _chatBox?.Clear();
                _chatBox?.AddSystemMessage("Chat cleared");
                break;

            case "/time":
                var now = DateTime.Now;
                _chatBox?.AddSystemMessage($"Current time: {now:HH:mm:ss}");
                break;

            case "/pos":
                if (_cameraComponent != null)
                {
                    var pos = _cameraComponent.Position;
                    _chatBox?.AddSystemMessage($"Position: X={pos.X:F2}, Y={pos.Y:F2}, Z={pos.Z:F2}");
                }
                break;

            case "/fly":
                if (_cameraComponent != null)
                {
                    _cameraComponent.FlyMode = !_cameraComponent.FlyMode;
                    var mode = _cameraComponent.FlyMode ? "enabled" : "disabled";
                    _chatBox?.AddSystemMessage($"Fly mode {mode}");
                }
                break;

            case "/water":
                if (_worldComponent != null && _cameraComponent != null)
                {
                    var ray = _cameraComponent.GetPickRay();
                    var raycastResult = _worldComponent.RaycastBlock(ray);
                    
                    if (raycastResult.HasValue)
                    {
                        var (chunk, x, y, z) = raycastResult.Value;
                        
                        var placeY = y + 1;
                        if (placeY < ChunkEntity.Height)
                        {
                            var existingBlock = chunk.Chunk?.GetBlock(x, placeY, z);
                            if (existingBlock == null || existingBlock.BlockType == BlockType.Air)
                            {
                                var waterBlock = new BlockEntity(DateTime.Now.Ticks, BlockType.Water)
                                {
                                    WaterLevel = 7
                                };
                                
                                chunk.Chunk?.SetBlock(x, placeY, z, waterBlock);
                                _worldComponent.InvalidateBlockAndAdjacentChunks(chunk, x, placeY, z);
                                
                                _chatBox?.AddSystemMessage($"Water block placed at Y={placeY}");
                            }
                            else
                            {
                                _chatBox?.AddErrorMessage("Position occupied!");
                            }
                        }
                        else
                        {
                            _chatBox?.AddErrorMessage("Height too high!");
                        }
                    }
                    else
                    {
                        _chatBox?.AddErrorMessage("No block selected!");
                    }
                }
                break;

            default:
                _chatBox?.AddErrorMessage($"Unknown command: {cmd}");
                _chatBox?.AddMessage("Use /help to see available commands", ChatMessageType.Info);
                break;
        }
    }

    private static ChunkEntity CreateFlatChunk(int chunkX, int chunkZ)
    {
        var chunkOrigin = new System.Numerics.Vector3(
            chunkX * ChunkEntity.Size,
            0f,
            chunkZ * ChunkEntity.Size
        );

        var chunk = new ChunkEntity(chunkOrigin);
        long id = (chunkX * 1000000L) + (chunkZ * 1000L) + 1;

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                var isWater = (x > 5 && x < 10 && z > 5 && z < 10);
                var isTower = (x == 3 || x == 12) && (z == 3 || z == 12);

                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    BlockType blockType;

                    if (isTower && y > 58 && y < 63)
                    {
                        blockType = BlockType.Stone;
                    }
                    else if (y == 0)
                    {
                        blockType = BlockType.Bedrock;
                    }
                    else if (y < ChunkEntity.Height - 2)
                    {
                        blockType = BlockType.Dirt;
                    }
                    else if (y < ChunkEntity.Height - 1)
                    {
                        blockType = isWater ? BlockType.Dirt : BlockType.Dirt;
                    }
                    else
                    {
                        blockType = isWater ? BlockType.Water : BlockType.Grass;
                    }

                    if (blockType != BlockType.Air)
                    {
                        chunk.SetBlock(x, y, z, new BlockEntity(id++, blockType));
                    }
                }
            }
        }

        var lightSystem = new Systems.ChunkLightSystem();
        lightSystem.CalculateInitialSunlight(chunk);

        return chunk;
    }
}
