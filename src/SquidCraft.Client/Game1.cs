using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Components;
using SquidCraft.Client.Context;
using SquidCraft.Client.Services;

namespace SquidCraft.Client;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ImGUIDebuggerService _imGUIDebuggerService;

    public Game1()
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: Thread.CurrentThread.CurrentCulture)
            .CreateLogger();

        _graphics = new GraphicsDeviceManager(this);
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
        SquidCraftClientContext.AssetManagerService.LoadFontTtf("Fonts/DefaultFont.ttf", "DefaultFont");

        var textComponent = new TextComponent()
        {
            FontSize = 24
        };
        textComponent.Text = "SquidCraft";

        SquidCraftClientContext.RootComponent.AddChild(textComponent);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        SquidCraftClientContext.RootComponent.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        SquidCraftClientContext.RootComponent.Draw(_spriteBatch, gameTime);

        _spriteBatch.End();

        _imGUIDebuggerService.Draw(gameTime, _spriteBatch);

        base.Draw(gameTime);
    }
}
