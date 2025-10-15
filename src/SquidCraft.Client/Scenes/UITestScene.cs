using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Components;
using SquidCraft.Client.Components.UI;
using SquidCraft.Client.Components.UI.Controls;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Scenes;

public class UITestScene : SceneBase
{
    private string _statusText = "UI Components Test Scene - Press F2 to return to 3D world, ESC to exit game";
    private SpriteFontBase? _font;
    private bool _controlsInitialized;

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
    private TabControlComponent? _sampleTabControl;
    private ToolTipComponent? _sampleToolTip;
    private ButtonComponent? _tooltipTriggerButton;

    public UITestScene() : base("UI Test Scene")
    {
    }

    private void InitializeControls()
    {
        _sampleCheckBox = new CheckBoxComponent("Enable Feature")
        {
            Position = new Vector2(600, 100),
            IsChecked = true
        };
        _sampleCheckBox.CheckedChanged += (sender, args) =>
        {
            _statusText = $"Checkbox: {(args.IsChecked ? "Checked" : "Unchecked")}";
        };

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

        _sampleSlider = new SliderComponent(0f, 100f, 50f, 200f)
        {
            Position = new Vector2(600, 200)
        };
        _sampleSlider.ValueChanged += (sender, args) =>
        {
            _statusText = $"Slider: {args.NewValue:F1}";
        };

        _sampleNumeric = new NumericUpDownComponent(0f, 100f, 42f, 120f)
        {
            Position = new Vector2(600, 230)
        };
        _sampleNumeric.ValueChanged += (sender, value) =>
        {
            _statusText = $"Numeric: {value}";
        };

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

        _sampleGroupBox = new GroupBoxComponent("Settings", 150f, 100f)
        {
            Position = new Vector2(600, 320)
        };

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

        _sampleMenuBar = new MenuBarComponent
        {
            Position = new Vector2(600, 560),
            Size = new Vector2(200, 24)
        };
        _sampleMenuBar.AddMenuItem("File");
        _sampleMenuBar.AddMenuItem("Edit");
        _sampleMenuBar.AddMenuItem("View");

        _sampleTabControl = new TabControlComponent
        {
            Position = new Vector2(800, 100),
            Size = new Vector2(300, 200)
        };
        var tab1 = _sampleTabControl.AddTab("Tab 1");
        tab1.Content = "Content of Tab 1";
        var tab2 = _sampleTabControl.AddTab("Tab 2");
        tab2.Content = "Content of Tab 2";
        var tab3 = _sampleTabControl.AddTab("Tab 3");
        tab3.Content = "Content of Tab 3";

        _tooltipTriggerButton = new ButtonComponent
        {
            Position = new Vector2(800, 320),
            Size = new Vector2(150, 30),
            Text = "Hover for Tooltip"
        };

        _sampleToolTip = new ToolTipComponent
        {
            IsVisible = false
        };

        Components.Add(_sampleCheckBox);
        Components.Add(_sampleRadio1);
        Components.Add(_sampleRadio2);
        Components.Add(_sampleSlider);
        Components.Add(_sampleNumeric);
        Components.Add(_sampleButton);
        Components.Add(_sampleScrollBar);
        Components.Add(_sampleGroupBox);
        Components.Add(_sampleListBox);
        Components.Add(_sampleMenuBar);
        Components.Add(_sampleTabControl);
        Components.Add(_tooltipTriggerButton);
        Components.Add(_sampleToolTip);
    }

    protected override void OnLoad()
    {
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf("DefaultFont", 12);
        InitializeControls();

        _controlsInitialized = true;
    }

    protected override void OnUnload()
    {
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (_tooltipTriggerButton != null && _sampleToolTip != null)
        {
            var mouseState = Mouse.GetState();
            var mousePos = new Vector2(mouseState.X, mouseState.Y);
            var buttonBounds = new Rectangle(
                (int)_tooltipTriggerButton.Position.X,
                (int)_tooltipTriggerButton.Position.Y,
                (int)_tooltipTriggerButton.Size.X,
                (int)_tooltipTriggerButton.Size.Y
            );

            if (buttonBounds.Contains(mousePos))
            {
                _sampleToolTip.Show(mousePos + new Vector2(10, 10), "This is a tooltip!\nIt can have multiple lines.\nHover over me!");
            }
            else
            {
                _sampleToolTip.Hide();
            }
        }
    }

    protected override void OnHandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
    }

    protected override void OnHandleMouse(MouseState mouseState, GameTime gameTime)
    {
    }

    protected override void OnDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        SquidCraftClientContext.GraphicsDevice.Clear(new Color(64, 64, 96));

        if (_font != null)
        {
            spriteBatch.DrawString(_font, "UI Components Showcase", new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(_font, _statusText, new Vector2(10, 30), Color.Yellow);

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
            spriteBatch.DrawString(_font, "✓ ToolTipComponent - Contextual tooltips", new Vector2(10, y), Color.LightGreen);
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
            spriteBatch.DrawString(_font, "• Hover the tooltip button", new Vector2(10, y), Color.LightGray);
            y += 15;

            y += 20;
            spriteBatch.DrawString(_font, "Press F2 to switch to 3D world, ESC to exit game", new Vector2(10, y), Color.Red);
        }
    }
}