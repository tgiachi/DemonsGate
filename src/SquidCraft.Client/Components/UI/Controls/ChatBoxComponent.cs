using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;

namespace SquidCraft.Client.Components.UI.Controls;

public class ChatBoxComponent : BaseComponent
{
    private readonly ScrollingTextBoxComponent _messagesBox;
    private readonly TextBoxComponent _inputBox;
    private readonly List<ChatMessage> _messages = new();
    private bool _isVisible = true;
    private float _currentFadeTime = 5f;
    private Keys _previousKey = Keys.None;

    public ChatBoxComponent(
        Vector2? position = null,
        Vector2? size = null)
    {
        Position = position ?? new Vector2(10, 10);
        Size = size ?? new Vector2(400, 300);

        var messagesHeight = Size.Y - 40;
        
        _messagesBox = new ScrollingTextBoxComponent(
            fontName: "DefaultFont",
            fontSize: 14,
            position: Position,
            size: new Vector2(Size.X, messagesHeight))
        {
            BackgroundColor = new Color(0, 0, 0, 180),
            BorderColor = new Color(255, 255, 255, 100),
            TextColor = Color.White,
            Padding = new Vector2(8, 6),
            LineSpacing = 2f
        };

        _inputBox = new TextBoxComponent(
            text: "",
            fontName: "DefaultFont",
            fontSize: 14,
            position: Position + new Vector2(0, messagesHeight + 4))
        {
            BackgroundColor = new Color(0, 0, 0, 200),
            BorderColor = new Color(255, 255, 255, 150),
            ForegroundColor = Color.White,
            PlaceholderColor = new Color(200, 200, 200, 128),
            MaxLength = 256,
            HasFocus = false
        };
        
        _inputBox.PlaceholderText = "Press T to chat...";
        
        UpdateInputBoxSize();
    }

    public bool IsInputActive { get; private set; }
    
    public float FadeDelay { get; set; } = 5f;
    
    public bool AlwaysVisible { get; set; } = false;
    
    public int MaxMessages { get; set; } = 100;

    public event EventHandler<string>? MessageSent;
    
    public event EventHandler<string>? CommandExecuted;

    public void AddMessage(string message, ChatMessageType type = ChatMessageType.Normal)
    {
        var chatMessage = new ChatMessage
        {
            Text = message,
            Type = type,
            Timestamp = DateTime.Now
        };
        
        _messages.Add(chatMessage);
        
        while (_messages.Count > MaxMessages)
        {
            _messages.RemoveAt(0);
        }

        var coloredMessage = FormatMessage(chatMessage);
        _messagesBox.AppendLine(coloredMessage);
        
        _currentFadeTime = FadeDelay;
        _isVisible = true;
    }

    public void AddSystemMessage(string message)
    {
        AddMessage($"[SYSTEM] {message}", ChatMessageType.System);
    }

    public void AddErrorMessage(string message)
    {
        AddMessage($"[ERROR] {message}", ChatMessageType.Error);
    }

    public void Clear()
    {
        _messages.Clear();
        _messagesBox.Clear();
    }

    public override void Initialize()
    {
        base.Initialize();
        _messagesBox.Initialize();
        _inputBox.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsVisible) return;

        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboard = Keyboard.GetState();

        if (keyboard.IsKeyDown(Keys.T) && _previousKey != Keys.T && !IsInputActive)
        {
            OpenInput();
        }
        else if (keyboard.IsKeyDown(Keys.Escape) && IsInputActive)
        {
            CloseInput();
        }
        else if (keyboard.IsKeyDown(Keys.Enter) && IsInputActive && _previousKey != Keys.Enter)
        {
            SubmitMessage();
        }

        _previousKey = keyboard.GetPressedKeys().Length > 0 ? keyboard.GetPressedKeys()[0] : Keys.None;

        if (!AlwaysVisible && !IsInputActive)
        {
            _currentFadeTime -= deltaTime;
            if (_currentFadeTime <= 0)
            {
                _isVisible = false;
            }
        }
        else
        {
            _isVisible = true;
        }

        if (IsInputActive)
        {
            _inputBox.Update(gameTime);
        }
        
        _messagesBox.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition = default)
    {
        if (!IsVisible) return;

        var alpha = _isVisible ? 1f : Math.Max(0f, _currentFadeTime / 1f);
        
        if (alpha <= 0.01f) return;

        var originalMessagesAlpha = _messagesBox.BackgroundColor.A;
        var originalInputAlpha = _inputBox.BackgroundColor.A;

        if (alpha < 1f)
        {
            _messagesBox.BackgroundColor = _messagesBox.BackgroundColor * alpha;
            _messagesBox.BorderColor = _messagesBox.BorderColor * alpha;
            _messagesBox.TextColor = _messagesBox.TextColor * alpha;
        }

        _messagesBox.Draw(spriteBatch, gameTime);

        if (IsInputActive)
        {
            _inputBox.Draw(spriteBatch, gameTime);
        }

        if (alpha < 1f)
        {
            _messagesBox.BackgroundColor = new Color(
                _messagesBox.BackgroundColor.R,
                _messagesBox.BackgroundColor.G,
                _messagesBox.BackgroundColor.B,
                originalMessagesAlpha);
            _messagesBox.BorderColor = new Color(
                _messagesBox.BorderColor.R,
                _messagesBox.BorderColor.G,
                _messagesBox.BorderColor.B,
                originalMessagesAlpha);
            _messagesBox.TextColor = Color.White;
        }
    }

    private void OpenInput()
    {
        IsInputActive = true;
        _inputBox.HasFocus = true;
        _inputBox.Text = "";
        _isVisible = true;
        _currentFadeTime = FadeDelay;
    }

    private void CloseInput()
    {
        IsInputActive = false;
        _inputBox.HasFocus = false;
        _inputBox.Text = "";
    }

    private void SubmitMessage()
    {
        var message = _inputBox.Text?.Trim();
        
        if (string.IsNullOrEmpty(message))
        {
            CloseInput();
            return;
        }

        if (message.StartsWith('/'))
        {
            CommandExecuted?.Invoke(this, message);
        }
        else
        {
            MessageSent?.Invoke(this, message);
            AddMessage($"<Tu> {message}", ChatMessageType.Player);
        }

        CloseInput();
    }

    private string FormatMessage(ChatMessage message)
    {
        var timestamp = message.Timestamp.ToString("HH:mm:ss");
        return message.Type switch
        {
            ChatMessageType.System => $"[{timestamp}] {message.Text}",
            ChatMessageType.Error => $"[{timestamp}] {message.Text}",
            ChatMessageType.Player => $"[{timestamp}] {message.Text}",
            ChatMessageType.Server => $"[{timestamp}] <Server> {message.Text}",
            _ => $"[{timestamp}] {message.Text}"
        };
    }

    private void UpdateInputBoxSize()
    {
        var inputWidth = Size.X;
        var inputHeight = 32f;
        
        _inputBox.Size = new Vector2(inputWidth, inputHeight);
    }


}

public class ChatMessage
{
    public string Text { get; set; } = string.Empty;
    public ChatMessageType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum ChatMessageType
{
    Normal,
    System,
    Error,
    Player,
    Server,
    Info
}
