using System;
using System.Collections.ObjectModel;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     Represents a menu item in the menu bar
/// </summary>
public class MenuItemComponent
{
    /// <summary>
    ///     Initializes a new MenuItem
    /// </summary>
    /// <param name="text">The text to display</param>
    public MenuItemComponent(string text)
    {
        Text = text;
        SubItems = new ObservableCollection<MenuItemComponent>();
    }

    /// <summary>
    ///     Gets or sets the text
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    ///     Gets or sets whether this item is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether this is a separator
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    ///     Gets or sets the shortcut text (e.g., "Ctrl+O")
    /// </summary>
    public string? ShortcutText { get; set; }

    /// <summary>
    ///     Gets the sub-items for this menu item
    /// </summary>
    public ObservableCollection<MenuItemComponent> SubItems { get; }

    /// <summary>
    ///     Event fired when this menu item is clicked
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    ///     Performs a click action on this menu item
    /// </summary>
    public void PerformClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Adds a sub-item to this menu item
    /// </summary>
    /// <param name="text">Sub-item text</param>
    /// <returns>The created MenuItemComponent</returns>
    public MenuItemComponent AddSubItem(string text)
    {
        var item = new MenuItemComponent(text);
        SubItems.Add(item);
        return item;
    }

    /// <summary>
    ///     Adds a separator to the sub-items
    /// </summary>
    public void AddSeparator()
    {
        SubItems.Add(new MenuItemComponent("") { IsSeparator = true });
    }
}