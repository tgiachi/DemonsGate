using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Components;
using SquidCraft.Client.Components.Networks;
using SquidCraft.Client.Context;
using SquidCraft.Client.Data;
using SquidCraft.Client.Services;
using SquidCraft.Client.Components.UI.Controls;

using SquidCraft.Core.Json;
using SquidCraft.Game.Data.Types;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Assets;
using SquidCraft.Game.Data.Context;
using SquidCraft.Services.Data.Config.Sections;

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
    private ChatBoxComponent? _chatBox;
    private static readonly RasterizerState ScissorRasterizerState = new() { ScissorTestEnable = true };

    private NetworkClientComponent _networkClientComponent;
    private MouseState _previousMouseState;




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
            FlyMode = false,
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

        _cameraComponent.IsBlockSolid = _worldComponent.IsBlockSolid;

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

        _networkClientComponent = new NetworkClientComponent(new GameNetworkConfig());

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

        _networkClientComponent.Update(gameTime);



        // Handle block breaking with left mouse click
        var currentMouseState = Mouse.GetState();
        if (currentMouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released)
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
                var blockType = chunk.Chunk?.GetBlock(x, y, z)?.BlockType ?? BlockType.Dirt;
                var blockColor = blockType switch
                {
                    BlockType.Grass => Color.Green,
                    BlockType.Dirt => new Color(139, 69, 19),
                    BlockType.Stone => Color.Gray,
                    BlockType.Wood => new Color(139, 90, 43),
                    BlockType.Leaves => Color.DarkGreen,
                    BlockType.Water => Color.Blue,
                    BlockType.Snow => Color.White,
                    BlockType.TallGrass => Color.YellowGreen,
                    BlockType.Flower => Color.Yellow,
                    _ => Color.White
                };

                _worldComponent.SpawnParticles(
                    blockWorldPos + new Vector3(0.5f, 0.5f, 0.5f),
                    12,
                    spread: 2f,
                    speed: 4f,
                    lifeTime: 1.0f,
                    blockColor
                );

                _logger.Information("Block broken at {Position}", blockWorldPos);
            }
        }
        
        _previousMouseState = currentMouseState;

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
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _networkClientComponent.Draw(gameTime, _spriteBatch);

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


    private static Task<ChunkEntity> CreateFlatChunkAsync(int chunkX, int chunkZ)
    {
        var chunkOrigin = new System.Numerics.Vector3(
            chunkX * ChunkEntity.Size,
            0f,
            chunkZ * ChunkEntity.Size
        );

        var chunk = new ChunkEntity(chunkOrigin);
        long id = (chunkX * 1000000L) + (chunkZ * 1000L) + 1;

        var random = new Random((chunkX * 73856093) ^ (chunkZ * 19349663));

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    BlockType blockType = BlockType.Air;

                    if (y == 0)
                    {
                        blockType = BlockType.Bedrock;
                    }
                    else if (y < 60)
                    {
                        blockType = BlockType.Dirt;
                    }
                    else if (y == 60)
                    {
                        blockType = BlockType.Grass;
                    }
                    else if (y == 61)
                    {
                        var rand = random.NextDouble();
                        if (rand < 0.15)
                        {
                            blockType = BlockType.TallGrass;
                        }
                        else if (rand < 0.20)
                        {
                            blockType = BlockType.Flower;
                        }
                    }

                    if (blockType != BlockType.Air)
                    {
                        chunk.SetBlock(x, y, z, new BlockEntity(id++, blockType));
                    }
                }
            }
        }

        return Task.FromResult(chunk);
    }
}
