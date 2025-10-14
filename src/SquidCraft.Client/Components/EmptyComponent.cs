using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Components.Interfaces;
using SquidCraft.Client.Extensions;

namespace SquidCraft.Client.Components;

/// <summary>
/// Root component that serves as the top-level container for all UI components.
/// Handles input dispatching to children with advanced features like mouse-based focus.
/// </summary>
public class EmptyComponent : BaseComponent
{
    private ISCDrawableComponent? _hoveredComponent;
    private ISCDrawableComponent? _focusedComponent;

    public EmptyComponent()
    {
        Name = "RootComponent";
        HasFocus = true; // Root always has focus
    }

    /// <summary>
    /// Handles keyboard input and dispatches to focused component
    /// </summary>
    public override void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus)
        {
            return;
        }

        // If there's a focused component, give it priority for keyboard input
        if (_focusedComponent != null && _focusedComponent.IsEnabled && _focusedComponent.HasFocus)
        {
            _focusedComponent.HandleKeyboard(keyboardState, gameTime);
            return;
        }

        // Otherwise, propagate to all children that have focus (ordered by ZIndex, top to bottom)
        foreach (var child in Children.Where(c => c.IsEnabled && c.HasFocus).OrderByDescending(c => c.ZIndex))
        {
            child.HandleKeyboard(keyboardState, gameTime);
        }
    }

    /// <summary>
    /// Handles mouse input and dispatches to children based on mouse position
    /// </summary>
    public override void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus)
        {
            return;
        }

        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        ISCDrawableComponent? newHoveredComponent = null;

        // Find the topmost component under the mouse (highest ZIndex first)
        foreach (var child in Children.GetVisibleEnabledDescendingByZIndex())
        {
            // Check if mouse is within component bounds
            if (IsMouseOverComponent(child, mousePosition))
            {
                newHoveredComponent = child;
                break; // Found the topmost component
            }
        }

        // Update hovered component
        if (newHoveredComponent != _hoveredComponent)
        {
            _hoveredComponent = newHoveredComponent;
        }

        // Handle mouse click to set focus
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            if (newHoveredComponent != null)
            {
                SetFocusedComponent(newHoveredComponent);
            }
        }

        // Dispatch mouse input to hovered component
        if (_hoveredComponent != null && _hoveredComponent.HasFocus)
        {
            _hoveredComponent.HandleMouse(mouseState, gameTime);
        }

        // Also dispatch to focused component if it's different from hovered
        if (_focusedComponent != null && _focusedComponent != _hoveredComponent && _focusedComponent.HasFocus)
        {
            _focusedComponent.HandleMouse(mouseState, gameTime);
        }

        // Dispatch to all children that explicitly have focus (for components that need global mouse tracking)
        foreach (var child in Children.Where(c => c.IsEnabled && c.HasFocus && c != _hoveredComponent && c != _focusedComponent))
        {
            child.HandleMouse(mouseState, gameTime);
        }
    }

    /// <summary>
    /// Sets the focused component and updates focus states
    /// </summary>
    /// <param name="component">The component to focus</param>
    public void SetFocusedComponent(ISCDrawableComponent? component)
    {
        if (_focusedComponent == component)
        {
            return;
        }

        // Remove focus from previous component
        if (_focusedComponent != null)
        {
            _focusedComponent.IsFocused = false;
        }

        _focusedComponent = component;

        // Set focus to new component
        if (_focusedComponent != null)
        {
            _focusedComponent.IsFocused = true;
        }
    }

    /// <summary>
    /// Gets the currently focused component
    /// </summary>
    public ISCDrawableComponent? FocusedComponent => _focusedComponent;

    /// <summary>
    /// Gets the currently hovered component
    /// </summary>
    public ISCDrawableComponent? HoveredComponent => _hoveredComponent;

    /// <summary>
    /// Checks if the mouse is over a component
    /// </summary>
    private static bool IsMouseOverComponent(ISCDrawableComponent component, Vector2 mousePosition)
    {
        return component.Contains(mousePosition);
    }
}
