using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.UI;
using SquidCraft.Client.Components.UI.Controls;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Scenes;

/// <summary>
///     UI Test Scene for showcasing and testing all UI components
/// </summary>
public class UITestScene : SceneBase
{
    private string _statusText = "UI Components Test Scene - Press F2 to return to 3D world, ESC to exit game";
    private SpriteFontBase? _font;
    private bool _controlsInitialized;

    // Sample UI controls
    private CheckBoxComponent? _sampleCheckBox;
    private RadioButtonComponent? _sampleRadio1;
    private RadioButtonComponent? _sampleRadio2;
    private SliderComponent? _sampleSlider;
    private NumericUpDownComponent? _sampleNumeric;
    private ButtonComponent? _sampleButton;
    private ScrollBarComponent? _sampleScrollBar;
    private GroupBoxComponent? _sampleGroupBox;
    private ListBoxComponent? _sampleListBox;
    private MenuBarComponent? _sampleMenuBar;

    /// <summary>
    ///     Initializes the UI test scene
    /// </summary>
    public UITestScene() : base("UI Test Scene")
    {
    }

    /// <summary>
    ///     Called when the scene is loaded
    /// </summary>
    protected override void OnLoad()
    {
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf("DefaultFont", 12);
    }

    /// <summary>
    ///     Called when the scene is unloaded
    /// </summary>
    protected override void OnUnload()
    {
        // Cleanup if needed
    }

    /// <summary>
    ///     Called every frame to update scene logic
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    protected override void OnUpdate(GameTime gameTime)
    {
        // Initialize controls after assets are loaded
        if (!_controlsInitialized && _font != null)
        {
            InitializeControls();
            _controlsInitialized = true;
        }

        // Update controls
        _sampleCheckBox?.Update(gameTime);
        _sampleRadio1?.Update(gameTime);
        _sampleRadio2?.Update(gameTime);
        _sampleSlider?.Update(gameTime);
        _sampleNumeric?.Update(gameTime);
        _sampleButton?.Update(gameTime);
        _sampleScrollBar?.Update(gameTime);
        _sampleGroupBox?.Update(gameTime);
        _sampleListBox?.Update(gameTime);
        _sampleMenuBar?.Update(gameTime);
    }

    /// <summary>
    ///     Initializes the sample UI controls
    /// </summary>
    private void InitializeControls()
    {
        // Checkbox
        _sampleCheckBox = new CheckBoxComponent("Enable Feature")
        {
            Position = new Vector2(600, 100),
            IsChecked = true
        };
        _sampleCheckBox.CheckedChanged += (sender, args) =>
        {
            _statusText = $"Checkbox: {(args.IsChecked ? "Checked" : "Unchecked")}";
        };

        // Radio buttons
        _sampleRadio1 = new RadioButtonComponent("Option A", "SampleGroup")
        {
            Position = new Vector2(600, 140),
            IsChecked = true
        };
        _sampleRadio1.CheckedChanged += (sender, args) =>
        {
            if (args.IsChecked) _statusText = "Radio: Option A selected";
        };

        _sampleRadio2 = new RadioButtonComponent("Option B", "SampleGroup")
        {
            Position = new Vector2(600, 160)
        };
        _sampleRadio2.CheckedChanged += (sender, args) =>
        {
            if (args.IsChecked) _statusText = "Radio: Option B selected";
        };

        // Slider
        _sampleSlider = new SliderComponent(0f, 100f, 50f, 200f)
        {
            Position = new Vector2(600, 200)
        };
        _sampleSlider.ValueChanged += (sender, args) =>
        {
            _statusText = $"Slider: {args.NewValue:F1}";
        };

        // Numeric up/down
        _sampleNumeric = new NumericUpDownComponent(0f, 100f, 42f, 120f)
        {
            Position = new Vector2(600, 230)
        };
        _sampleNumeric.ValueChanged += (sender, value) =>
        {
            _statusText = $"Numeric: {value}";
        };

        // Button
        _sampleButton = new ButtonComponent
        {
            Position = new Vector2(600, 270),
            Size = new Vector2(100, 30),
            Text = "Click Me"
        };
        _sampleButton.Clicked += (sender, args) =>
        {
            _statusText = "Button clicked!";
        };

        // ScrollBar
        _sampleScrollBar = new ScrollBarComponent(ScrollBarOrientation.Vertical)
        {
            Position = new Vector2(750, 100),
            Size = new Vector2(20, 200),
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };
        _sampleScrollBar.ValueChanged += (sender, value) =>
        {
            _statusText = $"ScrollBar: {value}";
        };
        _sampleNumeric.ValueChanged += (sender, value) =>
        {
            _statusText = $"Numeric: {value}";
        };

        // Button
        _sampleButton = new ButtonComponent
        {
            Position = new Vector2(550, 260),
            Size = new Vector2(100, 30),
            Text = "Click Me"
        };
        _sampleButton.Clicked += (sender, args) =>
        {
            _statusText = "Button clicked!";
        };





        // ScrollBar
        _sampleScrollBar = new ScrollBarComponent(ScrollBarOrientation.Vertical)
        {
            Position = new Vector2(750, 100),
            Size = new Vector2(20, 200),
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };
        _sampleScrollBar.ValueChanged += (sender, value) =>
        {
            _statusText = $"ScrollBar: {value}";
        };

        // GroupBox
        _sampleGroupBox = new GroupBoxComponent("Settings", 150f, 100f)
        {
            Position = new Vector2(600, 320)
        };

        // ListBox
        _sampleListBox = new ListBoxComponent(150f, 120f)
        {
            Position = new Vector2(600, 430)
        };
        _sampleListBox.AddItem("Item 1");
        _sampleListBox.AddItem("Item 2");
        _sampleListBox.AddItem("Item 3");
        _sampleListBox.AddItem("Item 4");
        _sampleListBox.AddItem("Item 5");
        _sampleListBox.SelectedIndexChanged += (sender, index) =>
        {
            _statusText = $"ListBox: Selected {index}";
        };

        // MenuBar
        _sampleMenuBar = new MenuBarComponent
        {
            Position = new Vector2(600, 560),
            Size = new Vector2(200, 24)
        };
        _sampleMenuBar.AddMenuItem("File");
        _sampleMenuBar.AddMenuItem("Edit");
        _sampleMenuBar.AddMenuItem("View");

        // Initialize all controls
        _sampleCheckBox.Initialize();
        _sampleRadio1.Initialize();
        _sampleRadio2.Initialize();
        _sampleSlider.Initialize();
        _sampleNumeric.Initialize();
        _sampleButton.Initialize();
        _sampleScrollBar.Initialize();
        _sampleGroupBox.Initialize();
        _sampleListBox.Initialize();
        _sampleMenuBar.Initialize();
    }

    /// <summary>
    ///     Called when keyboard input is received. Override to implement scene-specific keyboard handling logic.
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="gameTime">Game timing information</param>
    protected override void OnHandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        // Handle keyboard input for sample controls
        _sampleNumeric?.HandleKeyboard(keyboardState, gameTime);
    }

    /// <summary>
    ///     Called when mouse input is received. Override to implement scene-specific mouse handling logic.
    /// </summary>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="gameTime">Game timing information</param>
    protected override void OnHandleMouse(MouseState mouseState, GameTime gameTime)
    {
        // Handle mouse input for sample controls
        _sampleCheckBox?.HandleMouse(mouseState, gameTime);
        _sampleRadio1?.HandleMouse(mouseState, gameTime);
        _sampleRadio2?.HandleMouse(mouseState, gameTime);
        _sampleSlider?.HandleMouse(mouseState, gameTime);
        _sampleNumeric?.HandleMouse(mouseState, gameTime);
        _sampleButton?.HandleMouse(mouseState, gameTime);
        _sampleScrollBar?.HandleMouse(mouseState, gameTime);
        _sampleListBox?.HandleMouse(mouseState, gameTime);
        _sampleMenuBar?.HandleMouse(mouseState, gameTime);
    }

    /// <summary>
    ///     Called every frame to draw the scene
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    protected override void OnDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Clear with a nice background color
        SquidCraftClientContext.GraphicsDevice.Clear(new Color(64, 64, 96));

        spriteBatch.Begin();

        // Draw title and status
        if (_font != null)
        {
            spriteBatch.DrawString(_font, "UI Components Showcase", new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(_font, _statusText, new Vector2(10, 30), Color.Yellow);

            // Draw component list
            var y = 60;
            spriteBatch.DrawString(_font, "Available UI Components:", new Vector2(10, y), Color.Cyan);
            y += 20;

            spriteBatch.DrawString(_font, "✓ CheckBoxComponent - Boolean values", new Vector2(10, y), Color.LightGreen);
            y += 15;
            spriteBatch.DrawString(_font, "✓ RadioButtonComponent - Exclusive selection", new Vector2(10, y), Color.LightGreen);
            y += 15;
            spriteBatch.DrawString(_font, "✓ GroupBoxComponent - Control grouping", new Vector2(10, y), Color.LightGreen);
            y += 15;
            spriteBatch.DrawString(_font, "✓ ScrollBarComponent - Scrolling values", new Vector2(10, y), Color.LightGreen);
            y += 15;
            spriteBatch.DrawString(_font, "✓ MenuBarComponent - Dropdown menus", new Vector2(10, y), Color.LightGreen);
            y += 15;
            spriteBatch.DrawString(_font, "✓ ListBoxComponent - Item selection", new Vector2(10, y), Color.LightGreen);
            y += 15;
            spriteBatch.DrawString(_font, "✓ SliderComponent - Range values", new Vector2(10, y), Color.LightGreen);
            y += 15;
            spriteBatch.DrawString(_font, "✓ NumericUpDownComponent - Numeric input", new Vector2(10, y), Color.LightGreen);
            y += 15;
            spriteBatch.DrawString(_font, "✓ TabControl/TabPage - Tabbed interface", new Vector2(10, y), Color.LightGreen);
            y += 15;

            y += 10;
            spriteBatch.DrawString(_font, "Existing Components (from SquidCraft):", new Vector2(10, y), Color.Cyan);
            y += 20;
            spriteBatch.DrawString(_font, "• ButtonComponent", new Vector2(10, y), Color.White);
            y += 15;
            spriteBatch.DrawString(_font, "• TextBoxComponent", new Vector2(10, y), Color.White);
            y += 15;
            spriteBatch.DrawString(_font, "• ComboBoxComponent", new Vector2(10, y), Color.White);
            y += 15;
            spriteBatch.DrawString(_font, "• ProgressBarComponent", new Vector2(10, y), Color.White);
            y += 15;
            spriteBatch.DrawString(_font, "• LabelComponent", new Vector2(10, y), Color.White);
            y += 15;

            y += 20;
            spriteBatch.DrawString(_font, "Interactive Samples (right side):", new Vector2(10, y), Color.Yellow);
            y += 20;
            spriteBatch.DrawString(_font, "• Try clicking the checkbox", new Vector2(10, y), Color.LightGray);
            y += 15;
            spriteBatch.DrawString(_font, "• Select different radio options", new Vector2(10, y), Color.LightGray);
            y += 15;
            spriteBatch.DrawString(_font, "• Drag the slider", new Vector2(10, y), Color.LightGray);
            y += 15;
            spriteBatch.DrawString(_font, "• Use arrows on numeric control", new Vector2(10, y), Color.LightGray);
            y += 15;
            spriteBatch.DrawString(_font, "• Click the button", new Vector2(10, y), Color.LightGray);
            y += 15;

            y += 20;
            spriteBatch.DrawString(_font, "Press F2 to switch to 3D world, ESC to exit game", new Vector2(10, y), Color.Red);
        }

        // Draw sample controls
        if (_controlsInitialized)
        {
            _sampleCheckBox?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleRadio1?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleRadio2?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleSlider?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleNumeric?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleButton?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleScrollBar?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleGroupBox?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleListBox?.Draw(spriteBatch, gameTime, Vector2.Zero);
            _sampleMenuBar?.Draw(spriteBatch, gameTime, Vector2.Zero);
        }

        spriteBatch.End();
    }
}