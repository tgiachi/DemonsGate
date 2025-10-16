using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Components;
using SquidCraft.Client.Context;
using SquidCraft.Client.Data;
using SquidCraft.Client.Services;
using SquidCraft.Client.Components.UI.Controls;

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
    private float _progressTimer;
    private ChatBoxComponent? _chatBox;
    private static readonly RasterizerState ScissorRasterizerState = new() { ScissorTestEnable = true };


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

        _worldComponent = new WorldComponent(_cameraComponent)
        {
            ViewRange = 150f,
            EnableFrustumCulling = true,
            MaxRaycastDistance = 10f,
            ChunkLoadDistance = 2,
            MaxChunkBuildsPerFrame = 5,
            GenerationDistance = 3,
            ChunkGenerator = CreateFlatChunkAsync
        };

        _cameraComponent.CheckCollision = (pos, size) => _worldComponent.IsBlockSolid(pos);


        _blockOutlineComponent = new BlockOutlineComponent(GraphicsDevice)
        {
            OutlineColor = Color.White * 0.8f
        };


        var viewport = GraphicsDevice.Viewport;
        SquidCraftClientContext.RootComponent.Size = new Vector2(viewport.Width, viewport.Height);
        SquidCraftClientContext.SceneManager.Size = SquidCraftClientContext.RootComponent.Size;


        var fpsComponent = new FpsComponent(position: new Vector2(16, 16), fontSize: 16, color: Color.White);
        fpsComponent.ZIndex = 100;
        SquidCraftClientContext.RootComponent.AddChild(fpsComponent);



        _chatBox = new ChatBoxComponent(
            position: new Vector2(10, GraphicsDevice.Viewport.Height - 310),
            size: new Vector2(500, 300)
        )
        {
            FadeDelay = 5f,
            AlwaysVisible = false,
            MaxMessages = 100
        };

        _chatBox.MessageSent += OnChatMessageSent;

        _chatBox.Initialize();
        SquidCraftClientContext.RootComponent.AddChild(_chatBox);

        _chatBox.AddSystemMessage("Welcome to SquidCraft!");
        _chatBox.AddSystemMessage("Press T to open chat");
        _chatBox.AddMessage("Use /help to see available commands", ChatMessageType.Info);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var isChatActive = _chatBox?.IsInputActive ?? false;

        if (_cameraComponent != null)
        {
            _cameraComponent.EnableInput = !isChatActive;
            _cameraComponent.IsMouseCaptured = !isChatActive;
        }

        IsMouseVisible = !(_cameraComponent?.IsMouseCaptured ?? false);



        // Handle block breaking with left mouse click
        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
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
                _worldComponent.SpawnParticles(
                    blockWorldPos + new Vector3(0.5f, 0.5f, 0.5f),
                    20,
                    spread: 0.5f,
                    speed: 3f,
                    lifeTime: 1.5f,
                    Color.Orange
                );

                _logger.Information("Block broken at {Position}", blockWorldPos);
            }
        }

        // Handle block placing with right mouse click
        if (Mouse.GetState().RightButton == ButtonState.Pressed)
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
                        _worldComponent.SpawnParticles(
                            blockWorldPos + new Vector3(0.5f, 0.5f, 0.5f),
                            15,
                            spread: 0.3f,
                            speed: 2f,
                            lifeTime: 1f,
                            Color.Brown
                        );

                        _logger.Information("Block placed at {Position}", blockWorldPos);
                    }
                }
            }
        }

        _worldComponent?.Update(gameTime);
        SquidCraftClientContext.RootComponent.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
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

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _worldComponent?.Dispose();
        _blockOutlineComponent?.Dispose();
        base.UnloadContent();
    }

    private void OnChatMessageSent(object? sender, string message)
    {
        _logger.Information("Chat message sent: {Message}", message);
        _chatBox?.AddSystemMessage($"Message sent: {message}");
    }


    private Task<ChunkEntity> CreateFlatChunkAsync(int chunkX, int chunkZ)
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


        _worldComponent.CalculateInitialLighting(chunk);


        return Task.FromResult(chunk);
    }
}
