using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     Represents a single tab page in a TabControl
/// </summary>
public class TabPage
{
    private string _text;

    /// <summary>
    ///     Initializes a new TabPage
    /// </summary>
    /// <param name="text">Tab header text</param>
    /// <param name="tag">Optional tag for identifying the tab</param>
    public TabPage(string text, object? tag = null)
    {
        _text = text;
        Tag = tag;
        Components = new Collection<IUIComponent>();
    }

    /// <summary>
    ///     Gets or sets the tab header text
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    /// <summary>
    ///     Gets or sets whether this tab page is visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether this tab page is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets an optional tag for identifying this tab
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    ///     Gets the collection of components in this tab page
    /// </summary>
    public Collection<IUIComponent> Components { get; }

    /// <summary>
    ///     Gets or sets the background color for this tab page
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets whether this tab can be closed (if TabControl allows closing)
    /// </summary>
    public bool CanClose { get; set; } = true;

    /// <summary>
    ///     Adds a component to this tab page
    /// </summary>
    /// <param name="component">Component to add</param>
    public void AddComponent(IUIComponent component)
    {
        Components.Add(component);
    }

    /// <summary>
    ///     Removes a component from this tab page
    /// </summary>
    /// <param name="component">Component to remove</param>
    public void RemoveComponent(IUIComponent component)
    {
        Components.Remove(component);
    }

    /// <summary>
    ///     Clears all components from this tab page
    /// </summary>
    public void ClearComponents()
    {
        Components.Clear();
    }
}

/// <summary>
///     Interface for UI components
/// </summary>
public interface IUIComponent
{
    Vector2 Position { get; set; }
    Vector2 Size { get; set; }
    bool IsEnabled { get; set; }
    bool Visible { get; set; }
}