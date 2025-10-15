using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Scenes;

/// <summary>
/// Example scene demonstrating 3D component usage
/// </summary>
public class Example3dScene : SceneBase
{
    private SpriteFontBase? _font;
    private Example3dComponent? _cube;

    public Example3dScene() : base("3D Example Scene")
    {
    }

    protected override void OnLoad()
    {
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf("DefaultFont", 12);

        // Create and add 3D component
        _cube = new Example3dComponent
        {
            Position = new Vector3(0, 0, 0),
            Scale = new Vector3(0.5f, 0.5f, 0.5f),
            Name = "Rotating Cube"
        };

        base.Components3d.Add(_cube);
    }

    protected override void OnUnload()
    {
        // Components are automatically disposed by SceneBase
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Handle input for the 3D component
        var keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.W))
            _cube!.Position += new Vector3(0, 0.1f, 0);
        if (keyboardState.IsKeyDown(Keys.S))
            _cube!.Position += new Vector3(0, -0.1f, 0);
        if (keyboardState.IsKeyDown(Keys.A))
            _cube!.Position += new Vector3(-0.1f, 0, 0);
        if (keyboardState.IsKeyDown(Keys.D))
            _cube!.Position += new Vector3(0.1f, 0, 0);
        if (keyboardState.IsKeyDown(Keys.Q))
            _cube!.Position += new Vector3(0, 0, 0.1f);
        if (keyboardState.IsKeyDown(Keys.E))
            _cube!.Position += new Vector3(0, 0, -0.1f);

        if (keyboardState.IsKeyDown(Keys.R))
            _cube!.Rotation += new Vector3(0.05f, 0, 0);
        if (keyboardState.IsKeyDown(Keys.T))
            _cube!.Rotation += new Vector3(0, 0.05f, 0);
        if (keyboardState.IsKeyDown(Keys.Y))
            _cube!.Rotation += new Vector3(0, 0, 0.05f);
    }

    protected override void OnDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Clear with a dark background for 3D
        SquidCraftClientContext.GraphicsDevice.Clear(Color.DarkSlateGray);

        if (_font != null)
        {
            spriteBatch.DrawString(_font, "3D Component Example", new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(_font, "Controls:", new Vector2(10, 30), Color.Yellow);
            spriteBatch.DrawString(_font, "WASD - Move cube", new Vector2(10, 50), Color.LightGray);
            spriteBatch.DrawString(_font, "QE - Move forward/back", new Vector2(10, 70), Color.LightGray);
            spriteBatch.DrawString(_font, "RTY - Rotate cube", new Vector2(10, 90), Color.LightGray);
            spriteBatch.DrawString(_font, "F1 - Back to UI demo", new Vector2(10, 110), Color.Red);

            if (_cube != null)
            {
                spriteBatch.DrawString(_font,
                    $"Position: {_cube.Position.X:F1}, {_cube.Position.Y:F1}, {_cube.Position.Z:F1}",
                    new Vector2(10, 140), Color.Cyan);
                spriteBatch.DrawString(_font,
                    $"Rotation: {_cube.Rotation.X:F1}, {_cube.Rotation.Y:F1}, {_cube.Rotation.Z:F1}",
                    new Vector2(10, 160), Color.Cyan);
            }
        }
    }

    protected override void OnHandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (keyboardState.IsKeyDown(Keys.F1))
        {
            // Switch back to UI demo scene
            var sceneManager = SquidCraftClientContext.SceneManager;
            sceneManager.SwitchToScene("UI Test Scene");
        }
    }
}
