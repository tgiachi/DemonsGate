using Microsoft.Xna.Framework;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Components.Interfaces;
using SquidCraft.Client.Types.Layout;

namespace SquidCraft.Client.Components.UI.Layout;

/// <summary>
/// Lightweight layout container that positions children sequentially in a single direction.
/// </summary>
public class StackPanelComponent : BaseComponent
{
    private bool _layoutInvalidated = true;
    private int _lastLayoutSignature;
    private StackOrientation _orientation = StackOrientation.Vertical;
    private float _spacing = 4f;
    private Vector2 _padding = Vector2.Zero;
    private Alignment _alignment = Alignment.Start;
    private bool _autoSize = true;

    /// <summary>
    /// Gets or sets the layout orientation.
    /// </summary>
    public StackOrientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                RequestLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets spacing between child elements.
    /// </summary>
    public float Spacing
    {
        get => _spacing;
        set
        {
            if (Math.Abs(_spacing - value) > float.Epsilon)
            {
                _spacing = Math.Max(0f, value);
                RequestLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets padding applied inside the panel bounds.
    /// </summary>
    public Vector2 Padding
    {
        get => _padding;
        set
        {
            if (_padding != value)
            {
                _padding = value;
                RequestLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets the alignment perpendicular to the stack direction.
    /// </summary>
    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                RequestLayout();
            }
        }
    }

    /// <summary>
    /// When true the panel grows to fit its visible children.
    /// </summary>
    public bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (_autoSize != value)
            {
                _autoSize = value;
                RequestLayout();
            }
        }
    }

    /// <summary>
    /// Triggers a layout pass on the next update.
    /// </summary>
    public void RequestLayout() => _layoutInvalidated = true;

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        RequestLayout();
    }

    public override void Update(GameTime gameTime)
    {
        var visibleChildren = Children.Where(c => c.IsVisible).ToList();
        var signature = ComputeSignature(visibleChildren);

        if (_layoutInvalidated || signature != _lastLayoutSignature)
        {
            PerformLayout(visibleChildren);
            _lastLayoutSignature = signature;
            _layoutInvalidated = false;
        }

        base.Update(gameTime);
    }

    private void PerformLayout(IReadOnlyList<ISCDrawableComponent> visibleChildren)
    {
        if (visibleChildren.Count == 0)
        {
            if (AutoSize)
            {
                base.Size = Vector2.Zero;
            }
            return;
        }

        LayoutChildren(visibleChildren, ResolveSize());

        if (AutoSize)
        {
            var total = CalculateAutoSize(visibleChildren);
            var previous = Size;

            if (previous != total)
            {
                base.Size = total;
                LayoutChildren(visibleChildren, total);
            }
        }
    }

    private void LayoutChildren(IReadOnlyList<ISCDrawableComponent> children, Vector2 panelSize)
    {
        var cursor = new Vector2(Padding.X, Padding.Y);
        var availableCross = Orientation == StackOrientation.Vertical
            ? Math.Max(0, panelSize.X - Padding.X * 2f)
            : Math.Max(0, panelSize.Y - Padding.Y * 2f);

        foreach (var child in children)
        {
            switch (Orientation)
            {
                case StackOrientation.Vertical:
                    child.Position = new Vector2(Position.X + Align(child.Size.X, availableCross, Axis.Horizontal), Position.Y + cursor.Y);
                    cursor.Y += child.Size.Y + Spacing;
                    break;

                case StackOrientation.Horizontal:
                    child.Position = new Vector2(Position.X + cursor.X, Position.Y + Align(child.Size.Y, availableCross, Axis.Vertical));
                    cursor.X += child.Size.X + Spacing;
                    break;
            }
        }
    }

    private Vector2 CalculateAutoSize(IReadOnlyList<ISCDrawableComponent> children)
    {
        float extentPrimary = 0f;
        float extentCross = 0f;

        foreach (var child in children)
        {
            switch (Orientation)
            {
                case StackOrientation.Vertical:
                    extentPrimary += child.Size.Y + Spacing;
                    extentCross = MathHelper.Max(extentCross, child.Size.X);
                    break;
                case StackOrientation.Horizontal:
                    extentPrimary += child.Size.X + Spacing;
                    extentCross = MathHelper.Max(extentCross, child.Size.Y);
                    break;
            }
        }

        if (children.Count > 0)
        {
            extentPrimary -= Spacing;
        }

        return Orientation == StackOrientation.Vertical
            ? new Vector2(extentCross + Padding.X * 2f, Math.Max(0, extentPrimary) + Padding.Y * 2f)
            : new Vector2(Math.Max(0, extentPrimary) + Padding.X * 2f, extentCross + Padding.Y * 2f);
    }

    private float Align(float childExtent, float availableExtent, Axis axis)
    {
        var padding = axis == Axis.Horizontal ? Padding.X : Padding.Y;

        return Alignment switch
        {
            Alignment.Start => padding,
            Alignment.Center => padding + MathHelper.Max(0, (availableExtent - childExtent) / 2f),
            Alignment.End => padding + MathHelper.Max(0, availableExtent - childExtent),
            _ => padding
        };
    }

    private int ComputeSignature(IEnumerable<ISCDrawableComponent> visibleChildren)
    {
        var hash = new HashCode();
        hash.Add(Orientation);
        hash.Add(Spacing);
        hash.Add(Padding);
        hash.Add(Alignment);
        hash.Add(AutoSize);

        foreach (var child in visibleChildren)
        {
            hash.Add(child.Id);
            hash.Add(child.Size);
        }

        return hash.ToHashCode();
    }

    private enum Axis
    {
        Horizontal,
        Vertical
    }
}
