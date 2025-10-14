using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Single-line editable text input control.
/// </summary>
public class TextBoxComponent : BaseComponent
{
    private SpriteFontBase? _font;
    private string _text = string.Empty;
    private string _placeholderText = string.Empty;
    private int _fontSize;
    private KeyboardState _previousKeyboardState;
    private float _caretTimer;
    private bool _isCaretVisible = true;
    private int _caretIndex;
    private float _preferredWidth;
    private bool _isMouseDownInside;

    private readonly Dictionary<Keys, string> _shiftedNumberMap = new()
    {
        { Keys.D1, "!" }, { Keys.D2, "@" }, { Keys.D3, "#" }, { Keys.D4, "$" }, { Keys.D5, "%" },
        { Keys.D6, "^" }, { Keys.D7, "&" }, { Keys.D8, "*" }, { Keys.D9, "(" }, { Keys.D0, ")" }
    };

    /// <summary>
    /// Occurs when the text inside the textbox changes.
    /// </summary>
    public event EventHandler<string>? TextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBoxComponent"/>.
    /// </summary>
    public TextBoxComponent(
        string text = "",
        string fontName = "DefaultFont",
        int fontSize = 16,
        Vector2? position = null)
    {
        FontName = fontName;
        _fontSize = Math.Max(8, fontSize);
        Position = position ?? Vector2.Zero;

        BackgroundColor = new Color(33, 37, 41);
        BorderColor = new Color(73, 80, 87);
        FocusedBorderColor = new Color(116, 198, 255);
        ForegroundColor = Color.White;
        PlaceholderColor = new Color(255, 255, 255, 128);
        CaretColor = Color.White;
        Padding = new Vector2(10f, 8f);

        MaxLength = 256;
        CaretBlinkInterval = 0.5f;
        _preferredWidth = 240f;

        HasFocus = true;
        LoadFont();
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// Gets the font name used by this textbox.
    /// </summary>
    public string FontName { get; }

    /// <summary>
    /// Gets or sets the font size for text rendering.
    /// </summary>
    public int FontSize
    {
        get => _fontSize;
        set
        {
            value = Math.Max(8, value);
            if (_fontSize != value)
            {
                _fontSize = value;
                LoadFont();
                UpdateLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets the text content of the textbox.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            var newValue = value ?? string.Empty;
            if (newValue.Length > MaxLength)
            {
                newValue = newValue[..MaxLength];
            }

            if (_text != newValue)
            {
                _text = newValue;
                _caretIndex = Math.Clamp(_caretIndex, 0, _text.Length);
                ResetCaret();
                UpdateLayout();
                TextChanged?.Invoke(this, _text);
            }
        }
    }

    /// <summary>
    /// Gets or sets the placeholder text displayed when the textbox is empty.
    /// </summary>
    public string PlaceholderText
    {
        get => _placeholderText;
        set
        {
            _placeholderText = value ?? string.Empty;
            UpdateLayout();
        }
    }

    /// <summary>
    /// Gets or sets the character used to mask the text for password input.
    /// Set to null to disable masking.
    /// </summary>
    public char? PasswordMask { get; set; }

    /// <summary>
    /// Gets or sets the background color of the textbox.
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Border color used when the textbox is not focused.
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Border color used when the textbox has focus.
    /// </summary>
    public Color FocusedBorderColor { get; set; }

    /// <summary>
    /// Gets or sets the color of the text.
    /// </summary>
    public Color ForegroundColor { get; set; }

    /// <summary>
    /// Gets or sets the color applied to the placeholder message.
    /// </summary>
    public Color PlaceholderColor { get; set; }

    /// <summary>
    /// Gets or sets the caret color.
    /// </summary>
    public Color CaretColor { get; set; }

    /// <summary>
    /// Horizontal and vertical padding applied inside the textbox.
    /// </summary>
    public Vector2 Padding { get; set; }

    /// <summary>
    /// Width the textbox tries to maintain when computing the layout.
    /// </summary>
    public float PreferredWidth
    {
        get => _preferredWidth;
        set
        {
            if (Math.Abs(_preferredWidth - value) > float.Epsilon)
            {
                _preferredWidth = Math.Max(32f, value);
                UpdateLayout();
            }
        }
    }

    /// <summary>
    /// Maximum number of characters allowed in the textbox.
    /// </summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the caret blinking interval in seconds.
    /// </summary>
    public float CaretBlinkInterval { get; set; }

    /// <summary>
    /// Gets or sets the current caret position.
    /// </summary>
    public int CaretIndex
    {
        get => _caretIndex;
        set => _caretIndex = Math.Clamp(value, 0, _text.Length);
    }

    /// <inheritdoc />
    public override Vector2 Size
    {
        get => base.Size;
        set
        {
            base.Size = value;
            _preferredWidth = Math.Max(32f, value.X);
        }
    }

    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        if (IsFocused)
        {
            _caretTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_caretTimer >= CaretBlinkInterval)
            {
                _caretTimer -= CaretBlinkInterval;
                _isCaretVisible = !_isCaretVisible;
            }
        }
        else
        {
            _isCaretVisible = false;
            _caretTimer = 0f;
        }

        base.Update(gameTime);
    }

    /// <inheritdoc />
    public override void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus || !IsFocused)
        {
            _previousKeyboardState = keyboardState;
            return;
        }

        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (!_previousKeyboardState.IsKeyDown(key))
            {
                ProcessKeyPress(key, keyboardState);
            }
        }

        _previousKeyboardState = keyboardState;
        base.HandleKeyboard(keyboardState, gameTime);
    }

    /// <inheritdoc />
    public override void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        var isInside = Contains(mousePosition);

        if (mouseState.LeftButton == ButtonState.Pressed && isInside)
        {
            if (!_isMouseDownInside)
            {
                _isMouseDownInside = true;
                HasFocus = true;
                SetCaretFromMouse(mousePosition);
            }
        }
        else if (mouseState.LeftButton == ButtonState.Released)
        {
            _isMouseDownInside = false;
        }

        base.HandleMouse(mouseState, gameTime);
    }

    /// <inheritdoc />
    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        var font = _font;
        if (font == null)
        {
            return;
        }

        var absolutePosition = Position + parentPosition;
        var resolvedSize = ResolveSize();

        var bounds = new Rectangle(
            (int)absolutePosition.X,
            (int)absolutePosition.Y,
            (int)resolvedSize.X,
            (int)resolvedSize.Y);

        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();

        spriteBatch.Draw(pixel, bounds, BackgroundColor * Opacity);

        var borderColor = IsFocused ? FocusedBorderColor : BorderColor;
        DrawBorder(spriteBatch, pixel, bounds, 1, borderColor * Opacity);

        var textPosition = absolutePosition + Padding;
        var displayText = PasswordMask.HasValue && !string.IsNullOrEmpty(_text) ? new string(PasswordMask.Value, _text.Length) : _text;
        var message = string.IsNullOrEmpty(_text) ? _placeholderText : displayText;
        var textColor = string.IsNullOrEmpty(_text) ? PlaceholderColor : ForegroundColor;

        spriteBatch.DrawString(font, message, textPosition, textColor * Opacity);

        if (IsFocused && _isCaretVisible)
        {
            DrawCaret(spriteBatch, pixel, textPosition);
        }
    }

    private void LoadFont()
    {
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf(FontName, _fontSize);
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (_font == null)
        {
            return;
        }

        var textSize = _font.MeasureString(string.IsNullOrEmpty(_text) ? _placeholderText : _text);
        var height = Math.Max(textSize.Y, _font.LineHeight);
        var width = Math.Max(_preferredWidth, textSize.X + Padding.X * 2f);

        base.Size = new Vector2(width, height + Padding.Y * 2f);
    }

    private void ResetCaret()
    {
        _caretTimer = 0f;
        _isCaretVisible = true;
    }

    private void ProcessKeyPress(Keys key, KeyboardState state)
    {
        var shift = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
        var ctrl = state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl);

        switch (key)
        {
            case Keys.Back:
                if (_caretIndex > 0 && _text.Length > 0)
                {
                    _text = _text.Remove(_caretIndex - 1, 1);
                    _caretIndex--;
                    NotifyTextChanged();
                }
                break;
            case Keys.Delete:
                if (_caretIndex < _text.Length)
                {
                    _text = _text.Remove(_caretIndex, 1);
                    NotifyTextChanged();
                }
                break;
            case Keys.Left:
                if (_caretIndex > 0)
                {
                    _caretIndex--;
                    ResetCaret();
                }
                break;
            case Keys.Right:
                if (_caretIndex < _text.Length)
                {
                    _caretIndex++;
                    ResetCaret();
                }
                break;
            case Keys.Home:
                _caretIndex = 0;
                ResetCaret();
                break;
            case Keys.End:
                _caretIndex = _text.Length;
                ResetCaret();
                break;
            default:
                if (!ctrl && TryConvertKeyToText(key, shift, out var text))
                {
                    InsertText(text);
                }
                break;
        }
    }

    private void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (_text.Length + text.Length > MaxLength)
        {
            var allowedLength = MaxLength - _text.Length;
            text = text[..Math.Max(0, allowedLength)];
        }

        if (text.Length == 0)
        {
            return;
        }

        _text = _text.Insert(_caretIndex, text);
        _caretIndex += text.Length;
        NotifyTextChanged();
    }

    private void NotifyTextChanged()
    {
        ResetCaret();
        UpdateLayout();
        TextChanged?.Invoke(this, _text);
    }

    private bool TryConvertKeyToText(Keys key, bool shift, out string text)
    {
        text = string.Empty;

        if (key >= Keys.A && key <= Keys.Z)
        {
            var offset = key - Keys.A;
            var character = (char)('a' + offset);
            text = shift ? character.ToString().ToUpperInvariant() : character.ToString();
            return true;
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            if (shift && _shiftedNumberMap.TryGetValue(key, out var shifted))
            {
                text = shifted;
            }
            else
            {
                var offset = key - Keys.D0;
                text = offset.ToString();
            }

            return true;
        }

        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            text = ((int)(key - Keys.NumPad0)).ToString();
            return true;
        }

        switch (key)
        {
            case Keys.Space:
                text = " ";
                return true;
            case Keys.OemComma:
                text = shift ? "<" : ",";
                return true;
            case Keys.OemPeriod:
                text = shift ? ">" : ".";
                return true;
            case Keys.OemMinus:
                text = shift ? "_" : "-";
                return true;
            case Keys.OemPlus:
                text = shift ? "+" : "=";
                return true;
            case Keys.OemQuestion:
                text = shift ? "?" : "/";
                return true;
            case Keys.OemSemicolon:
                text = shift ? ":" : ";";
                return true;
            case Keys.OemQuotes:
                text = shift ? "\"" : "'";
                return true;
            case Keys.OemOpenBrackets:
                text = shift ? "{" : "[";
                return true;
            case Keys.OemCloseBrackets:
                text = shift ? "}" : "]";
                return true;
            case Keys.OemBackslash:
                text = shift ? "|" : "\\";
                return true;
            case Keys.Tab:
                text = "\t";
                return true;
        }

        return false;
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, int thickness, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), color);
    }

    private void DrawCaret(SpriteBatch spriteBatch, Texture2D pixel, Vector2 textPosition)
    {
        if (_font == null)
        {
            return;
        }

        var prefix = _caretIndex > 0 ? _text[.._caretIndex] : string.Empty;
        var prefixWidth = _font.MeasureString(prefix).X;

        var caretX = textPosition.X + prefixWidth;
        var caretHeight = _font.LineHeight > 0 ? _font.LineHeight : _font.MeasureString("M").Y;

        var caretRect = new Rectangle(
            (int)caretX,
            (int)textPosition.Y,
            2,
            (int)caretHeight
        );

        spriteBatch.Draw(pixel, caretRect, CaretColor * Opacity);
    }

    private void SetCaretFromMouse(Vector2 mousePosition)
    {
        if (_font == null)
        {
            return;
        }

        var bounds = Bounds;
        var localX = mousePosition.X - bounds.X - Padding.X;
        localX = Math.Max(0, localX);

        var accumulator = 0f;
        var index = 0;
        while (index < _text.Length)
        {
            var characterWidth = _font.MeasureString(_text[index].ToString()).X;
            if (localX < accumulator + characterWidth / 2f)
            {
                break;
            }

            accumulator += characterWidth;
            index++;
        }

        _caretIndex = index;
        ResetCaret();
    }
}
