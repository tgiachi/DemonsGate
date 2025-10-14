using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Components;
using SquidCraft.Client.Context;
using SquidCraft.Client.Data;
using SquidCraft.Client.Services;
using SquidCraft.Client.Components.UI.Controls;
using SquidCraft.Client.Components.UI.Layout;
using SquidCraft.Client.Types.Layout;
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
    private Block3DComponent? _blockPreviewComponent;
    private ChunkComponent? _chunkComponent;
    private WorldComponent? _worldComponent;
    private CameraComponent? _cameraComponent;
    private BlockOutlineComponent? _blockOutlineComponent;
    private ProgressBarComponent? _progressBarComponent;
    private float _progressTimer;
    private ScrollingTextBoxComponent? _logTextBox;
    private static readonly RasterizerState ScissorRasterizerState = new() { ScissorTestEnable = true };

    public Game1()
    {
        JsonUtils.RegisterJsonContext(SquidCraftClientJsonContext.Default);
        JsonUtils.RegisterJsonContext(SquidCraftGameJsonContext.Default);
        JsonUtils.RegisterJsonContext(SquidCraftClientJsonContext.Default);

        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: Thread.CurrentThread.CurrentCulture)
            .CreateLogger();

        _logger = Log.ForContext<Game1>();

        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        SquidCraftClientContext.SceneManager = new SceneManager();
        SquidCraftClientContext.RootComponent.AddChild(SquidCraftClientContext.SceneManager);

        _imGUIDebuggerService = new ImGUIDebuggerService(this);

        base.Initialize();
    }


    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        SquidCraftClientContext.GraphicsDevice = GraphicsDevice;
        SquidCraftClientContext.AssetManagerService = new AssetManagerService(
            Directory.GetCurrentDirectory(),
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

        // _blockPreviewComponent = new Block3DComponent(GraphicsDevice, SquidCraftClientContext.BlockManagerService)
        // {
        //     BlockType = BlockType.Grass,
        //     Size = 1f,
        //     Position = Vector3.Zero,
        //     ShowSideLabels = true,
        //     AutoRotate = true
        // };

        _cameraComponent = new CameraComponent(GraphicsDevice)
        {
            Position = new Vector3(55f, 65f, 55f),
            Target = new Vector3(8f, 18f, 8f),
            MoveSpeed = 25f,
            MouseSensitivity = 0.003f,
            EnableInput = true
        };

        _worldComponent = new WorldComponent(GraphicsDevice, _cameraComponent)
        {
            ViewRange = 150f,
            EnableFrustumCulling = true,
            MaxRaycastDistance = 10f
        };

        _blockOutlineComponent = new BlockOutlineComponent(GraphicsDevice)
        {
            OutlineColor = Color.White * 0.8f
        };




        _ = _worldComponent.AddChunkAsync(CreateDemoChunk());
        _ = _worldComponent.AddChunkAsync(CreateDemoChunk(new System.Numerics.Vector3(16f, -20f, -8f)));


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
            new[] { "Grass", "Dirt", "Stone", "Snow", "Water" });
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
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

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

        _chunkComponent?.Update(gameTime);
        _worldComponent?.Update(gameTime);
        //_blockPreviewComponent?.Update(gameTime);
        SquidCraftClientContext.RootComponent.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        var viewportBounds = GraphicsDevice.Viewport.Bounds;
        GraphicsDevice.ScissorRectangle = viewportBounds;

        _worldComponent?.Draw(gameTime);
        _chunkComponent?.Draw(gameTime);
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
        _chunkComponent?.Dispose();
        _blockPreviewComponent?.Dispose();
        _blockOutlineComponent?.Dispose();
        base.UnloadContent();
    }

    private static ChunkEntity CreateDemoChunk(System.Numerics.Vector3? position = null)
    {
        var chunkOrigin = position ?? new System.Numerics.Vector3(-ChunkEntity.Size / 2f, -20f, -ChunkEntity.Size / 2f);
        var chunk = new ChunkEntity(chunkOrigin);
        long id = 1;

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                var height = 18 + (int)(MathF.Sin(x * 0.35f) * 3f + MathF.Cos(z * 0.35f) * 3f);
                height = Math.Clamp(height, 4, ChunkEntity.Height - 2);

                for (int y = 0; y <= height; y++)
                {
                    var blockType = BlockType.Stone;

                    if (y == 0)
                    {
                        blockType = BlockType.Bedrock;
                    }
                    else if (y < height - 3)
                    {
                        blockType = BlockType.Stone;
                    }
                    else if (y < height)
                    {
                        blockType = BlockType.Dirt;
                    }
                    else
                    {
                        blockType = BlockType.Grass;
                    }

                    chunk.SetBlock(x, y, z, new BlockEntity(id++, blockType));
                }
            }
        }

        return chunk;
    }
}
