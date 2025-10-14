using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Collections;
using SquidCraft.Client.Components.Interfaces;

namespace SquidCraft.Client.Extensions;

/// <summary>
/// Extension methods for SCDrawableCollection to provide batch operations
/// </summary>
public static class SCDrawableCollectionExtensions
{
    /// <summary>
    /// Updates all enabled components in the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="gameTime">Game timing information</param>
    public static void UpdateAll<T>(this SCDrawableCollection<T> collection, GameTime gameTime)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.IsEnabled)
            {
                component.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Draws all visible components in the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public static void DrawAll<T>(
        this SCDrawableCollection<T> collection,
        GameTime gameTime,
        SpriteBatch spriteBatch
    )
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.IsVisible)
            {
                component.Draw(gameTime, spriteBatch);
            }
        }
    }

    /// <summary>
    /// Updates and draws all components that are enabled and visible
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public static void UpdateAndDrawAll<T>(
        this SCDrawableCollection<T> collection,
        GameTime gameTime,
        SpriteBatch spriteBatch
    )
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];

            if (component.IsEnabled)
            {
                component.Update(gameTime);
            }

            if (component.IsVisible)
            {
                component.Draw(gameTime, spriteBatch);
            }
        }
    }

    /// <summary>
    /// Updates components within a specific ZIndex range
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="minZIndex">Minimum ZIndex (inclusive)</param>
    /// <param name="maxZIndex">Maximum ZIndex (inclusive)</param>
    public static void UpdateRange<T>(
        this SCDrawableCollection<T> collection,
        GameTime gameTime,
        int minZIndex,
        int maxZIndex
    )
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.IsEnabled && component.ZIndex >= minZIndex && component.ZIndex <= maxZIndex)
            {
                component.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Draws components within a specific ZIndex range
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    /// <param name="minZIndex">Minimum ZIndex (inclusive)</param>
    /// <param name="maxZIndex">Maximum ZIndex (inclusive)</param>
    public static void DrawRange<T>(
        this SCDrawableCollection<T> collection,
        GameTime gameTime,
        SpriteBatch spriteBatch,
        int minZIndex,
        int maxZIndex
    )
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.IsVisible && component.ZIndex >= minZIndex && component.ZIndex <= maxZIndex)
            {
                component.Draw(gameTime, spriteBatch);
            }
        }
    }

    /// <summary>
    /// Gets all enabled components from the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Enumerable of enabled components</returns>
    public static IEnumerable<T> GetEnabled<T>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();
        return collection.GetEnabledComponents();
    }

    /// <summary>
    /// Gets all disabled components from the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Enumerable of disabled components</returns>
    public static IEnumerable<T> GetDisabled<T>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (!component.IsEnabled)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets all visible components from the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Enumerable of visible components</returns>
    public static IEnumerable<T> GetVisible<T>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();
        return collection.GetVisibleComponents();
    }

    /// <summary>
    /// Gets all invisible components from the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Enumerable of invisible components</returns>
    public static IEnumerable<T> GetInvisible<T>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (!component.IsVisible)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets all components that are both enabled and visible
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Enumerable of active components</returns>
    public static IEnumerable<T> GetActive<T>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();
        return collection.GetActiveComponents();
    }

    /// <summary>
    /// Gets components that are visible and enabled ordered by descending ZIndex.
    /// Useful for hit-testing scenarios where top-most components should be evaluated first.
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="components">Enumerable of components to evaluate</param>
    /// <returns>Enumerable of visible and enabled components ordered by ZIndex (highest first)</returns>
    public static IEnumerable<T> GetVisibleEnabledDescendingByZIndex<T>(this IEnumerable<T> components)
        where T : class, ISCDrawableComponent
    {
        var filtered = new List<T>();

        foreach (var component in components)
        {
            if (component.IsVisible && component.IsEnabled)
            {
                filtered.Add(component);
            }
        }

        if (filtered.Count == 0)
        {
            yield break;
        }

        filtered.Sort(static (left, right) => right.ZIndex.CompareTo(left.ZIndex));

        for (var i = 0; i < filtered.Count; i++)
        {
            yield return filtered[i];
        }
    }

    /// <summary>
    /// Gets all focused components from the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Enumerable of focused components</returns>
    public static IEnumerable<T> GetFocused<T>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.IsFocused)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Finds the first component with the specified name (case-insensitive)
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="name">Name to search for</param>
    /// <returns>First component with matching name, or null if not found</returns>
    public static T? FindByName<T>(this SCDrawableCollection<T> collection, string name)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (string.Equals(component.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return component;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the first component with the specified ID
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="id">ID to search for</param>
    /// <returns>First component with matching ID, or null if not found</returns>
    public static T? FindById<T>(this SCDrawableCollection<T> collection, string id)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (string.Equals(component.Id, id, StringComparison.Ordinal))
            {
                return component;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds all components with the specified name (case-insensitive)
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="name">Name to search for</param>
    /// <returns>Enumerable of components with matching name</returns>
    public static IEnumerable<T> FindAllByName<T>(this SCDrawableCollection<T> collection, string name)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (string.Equals(component.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets components of a specific type
    /// </summary>
    /// <typeparam name="T">Base type implementing ISCDrawableComponent</typeparam>
    /// <typeparam name="TSpecific">Specific type to filter for</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Enumerable of components of the specified type</returns>
    public static IEnumerable<TSpecific> OfType<T, TSpecific>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
        where TSpecific : class, T
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is TSpecific specificComponent)
            {
                yield return specificComponent;
            }
        }
    }

    /// <summary>
    /// Gets all children of a specific parent component recursively
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="parent">Parent component to find children for</param>
    /// <returns>Enumerable of child components</returns>
    public static IEnumerable<T> GetChildren<T>(this SCDrawableCollection<T> collection, T parent)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.Parent == parent)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets all children of a specific parent component recursively
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="parent">Parent component to find children for</param>
    /// <returns>Enumerable of all descendant components</returns>
    public static IEnumerable<T> GetDescendants<T>(this SCDrawableCollection<T> collection, T parent)
        where T : class, ISCDrawableComponent
    {
        var directChildren = collection.GetChildren(parent).ToList();

        foreach (var child in directChildren)
        {
            yield return child;

            foreach (var descendant in collection.GetDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Updates all enabled components in a specific ZIndex layer
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="zIndex">Exact ZIndex to update</param>
    public static void UpdateLayer<T>(this SCDrawableCollection<T> collection, GameTime gameTime, int zIndex)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.IsEnabled && component.ZIndex == zIndex)
            {
                component.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Draws all visible components in a specific ZIndex layer
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    /// <param name="zIndex">Exact ZIndex to draw</param>
    public static void DrawLayer<T>(
        this SCDrawableCollection<T> collection,
        GameTime gameTime,
        SpriteBatch spriteBatch,
        int zIndex
    )
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.IsVisible && component.ZIndex == zIndex)
            {
                component.Draw(gameTime, spriteBatch);
            }
        }
    }

    /// <summary>
    /// Counts components matching a predicate
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="predicate">Predicate to match components</param>
    /// <returns>Number of matching components</returns>
    public static int CountWhere<T>(this SCDrawableCollection<T> collection, Func<T, bool> predicate)
        where T : class, ISCDrawableComponent
    {
        var count = 0;
        for (var i = 0; i < collection.Count; i++)
        {
            if (predicate(collection[i]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Checks if any component matches a predicate
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="predicate">Predicate to match components</param>
    /// <returns>True if any component matches</returns>
    public static bool Any<T>(this SCDrawableCollection<T> collection, Func<T, bool> predicate)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (predicate(collection[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if all components match a predicate
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="predicate">Predicate to match components</param>
    /// <returns>True if all components match</returns>
    public static bool All<T>(this SCDrawableCollection<T> collection, Func<T, bool> predicate)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (!predicate(collection[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Moves all components by the specified offset
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="offset">Offset to move by</param>
    public static void MoveAll<T>(this SCDrawableCollection<T> collection, Vector2 offset)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            component.Position += offset;
        }
    }

    /// <summary>
    /// Sets the position of all components
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="position">New position</param>
    public static void SetPositionAll<T>(this SCDrawableCollection<T> collection, Vector2 position)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].Position = position;
        }
    }

    /// <summary>
    /// Sets the scale of all components
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="scale">New scale</param>
    public static void SetScaleAll<T>(this SCDrawableCollection<T> collection, Vector2 scale)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].Scale = scale;
        }
    }

    /// <summary>
    /// Scales all components by the specified factor
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="factor">Scale factor</param>
    public static void ScaleAll<T>(this SCDrawableCollection<T> collection, float factor)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            component.Scale *= factor;
        }
    }

    /// <summary>
    /// Scales all components by the specified factor (separate X and Y)
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="factor">Scale factor for X and Y</param>
    public static void ScaleAll<T>(this SCDrawableCollection<T> collection, Vector2 factor)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            component.Scale *= factor;
        }
    }

    /// <summary>
    /// Gets components within a rectangular area
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="area">Rectangular area to check</param>
    /// <returns>Enumerable of components within the area</returns>
    public static IEnumerable<T> GetComponentsInArea<T>(this SCDrawableCollection<T> collection, Rectangle area)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (area.Contains(component.Position))
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets components at a specific position (with optional tolerance)
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="position">Position to check</param>
    /// <param name="tolerance">Distance tolerance (default: 0)</param>
    /// <returns>Enumerable of components at the position</returns>
    public static IEnumerable<T> GetComponentsAtPosition<T>(
        this SCDrawableCollection<T> collection,
        Vector2 position,
        float tolerance = 0f
    )
        where T : class, ISCDrawableComponent
    {
        var toleranceSquared = tolerance * tolerance;

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            var distanceSquared = Vector2.DistanceSquared(component.Position, position);

            if (distanceSquared <= toleranceSquared)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets the closest component to a position
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="position">Position to check from</param>
    /// <returns>Closest component, or null if collection is empty</returns>
    public static T? GetClosestComponent<T>(this SCDrawableCollection<T> collection, Vector2 position)
        where T : class, ISCDrawableComponent
    {
        if (collection.Count == 0)
        {
            return null;
        }

        T? closest = null;
        var minDistanceSquared = float.MaxValue;

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            var distanceSquared = Vector2.DistanceSquared(component.Position, position);

            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                closest = component;
            }
        }

        return closest;
    }

    /// <summary>
    /// Gets components sorted by distance from a position (closest first)
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="position">Position to measure from</param>
    /// <returns>Enumerable of components sorted by distance</returns>
    public static IEnumerable<T> GetComponentsByDistance<T>(this SCDrawableCollection<T> collection, Vector2 position)
        where T : class, ISCDrawableComponent
    {
        var componentsWithDistance = new List<(T Component, float DistanceSquared)>(collection.Count);

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            var distanceSquared = Vector2.DistanceSquared(component.Position, position);
            componentsWithDistance.Add((component, distanceSquared));
        }

        componentsWithDistance.Sort((a, b) => a.DistanceSquared.CompareTo(b.DistanceSquared));

        foreach (var (component, _) in componentsWithDistance)
        {
            yield return component;
        }
    }

    /// <summary>
    /// Calculates the bounding rectangle that contains all components
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Bounding rectangle, or Rectangle.Empty if collection is empty</returns>
    public static Rectangle GetBoundingRectangle<T>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
    {
        if (collection.Count == 0)
        {
            return Rectangle.Empty;
        }

        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        for (var i = 0; i < collection.Count; i++)
        {
            var pos = collection[i].Position;

            if (pos.X < minX)
                minX = pos.X;
            if (pos.Y < minY)
                minY = pos.Y;
            if (pos.X > maxX)
                maxX = pos.X;
            if (pos.Y > maxY)
                maxY = pos.Y;
        }

        return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
    }

    /// <summary>
    /// Calculates the center point of all components
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <returns>Center point, or Vector2.Zero if collection is empty</returns>
    public static Vector2 GetCenterPoint<T>(this SCDrawableCollection<T> collection)
        where T : class, ISCDrawableComponent
    {
        if (collection.Count == 0)
        {
            return Vector2.Zero;
        }

        var sum = Vector2.Zero;

        for (var i = 0; i < collection.Count; i++)
        {
            sum += collection[i].Position;
        }

        return sum / collection.Count;
    }

    /// <summary>
    /// Sets the opacity for all components in the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="opacity">Opacity value to set (0.0f to 1.0f)</param>
    public static void SetOpacity<T>(this SCDrawableCollection<T> collection, float opacity)
        where T : class, ISCDrawableComponent
    {
        opacity = MathHelper.Clamp(opacity, 0.0f, 1.0f);

        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].Opacity = opacity;
        }
    }

    /// <summary>
    /// Sets the rotation for all components in the collection
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="rotation">Rotation angle in radians</param>
    public static void SetRotation<T>(this SCDrawableCollection<T> collection, float rotation)
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].Rotation = rotation;
        }
    }

    /// <summary>
    /// Fades all components to a specific opacity over time
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="targetOpacity">Target opacity value (0.0f to 1.0f)</param>
    /// <param name="duration">Duration of the fade in seconds</param>
    /// <param name="gameTime">Current game time for interpolation</param>
    public static void FadeTo<T>(
        this SCDrawableCollection<T> collection,
        float targetOpacity,
        float duration,
        GameTime gameTime
    )
        where T : class, ISCDrawableComponent
    {
        targetOpacity = MathHelper.Clamp(targetOpacity, 0.0f, 1.0f);

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            var currentOpacity = component.Opacity;
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (duration > 0)
            {
                var opacityChange = (targetOpacity - currentOpacity) * (deltaTime / duration);
                component.Opacity = MathHelper.Clamp(currentOpacity + opacityChange, 0.0f, 1.0f);
            }
            else
            {
                component.Opacity = targetOpacity;
            }
        }
    }

    /// <summary>
    /// Rotates all components to a specific angle over time
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="targetRotation">Target rotation angle in radians</param>
    /// <param name="duration">Duration of the rotation in seconds</param>
    /// <param name="gameTime">Current game time for interpolation</param>
    public static void RotateTo<T>(
        this SCDrawableCollection<T> collection,
        float targetRotation,
        float duration,
        GameTime gameTime
    )
        where T : class, ISCDrawableComponent
    {
        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            var currentRotation = component.Rotation;
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (duration > 0)
            {
                var rotationChange = (targetRotation - currentRotation) * (deltaTime / duration);
                component.Rotation = currentRotation + rotationChange;
            }
            else
            {
                component.Rotation = targetRotation;
            }
        }
    }

    /// <summary>
    /// Gets all components with opacity above a threshold
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="minOpacity">Minimum opacity threshold (0.0f to 1.0f)</param>
    /// <returns>Enumerable of components with opacity above threshold</returns>
    public static IEnumerable<T> GetOpaque<T>(this SCDrawableCollection<T> collection, float minOpacity = 0.1f)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.Opacity >= minOpacity)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets all components with opacity below a threshold
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="maxOpacity">Maximum opacity threshold (0.0f to 1.0f)</param>
    /// <returns>Enumerable of components with opacity below threshold</returns>
    public static IEnumerable<T> GetTransparent<T>(this SCDrawableCollection<T> collection, float maxOpacity = 0.9f)
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.Opacity <= maxOpacity)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets all components with rotation within a range
    /// </summary>
    /// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
    /// <param name="collection">Collection of components</param>
    /// <param name="minRotation">Minimum rotation angle in radians</param>
    /// <param name="maxRotation">Maximum rotation angle in radians</param>
    /// <returns>Enumerable of components with rotation within range</returns>
    public static IEnumerable<T> GetRotated<T>(
        this SCDrawableCollection<T> collection,
        float minRotation,
        float maxRotation
    )
        where T : class, ISCDrawableComponent
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var component = collection[i];
            if (component.Rotation >= minRotation && component.Rotation <= maxRotation)
            {
                yield return component;
            }
        }
    }
}
