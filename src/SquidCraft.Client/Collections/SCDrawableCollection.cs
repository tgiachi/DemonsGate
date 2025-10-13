using System.Collections;
using SquidCraft.Client.Components.Interfaces;

namespace SquidCraft.Client.Collections;

/// <summary>
/// High-performance sorted collection for ISCDrawableComponent objects, automatically sorted by ZIndex
/// </summary>
/// <typeparam name="T">Type implementing ISCDrawableComponent</typeparam>
public class SCDrawableCollection<T> : IEnumerable<T>
    where T : class, ISCDrawableComponent
{
    private readonly List<T> _components;
    private readonly Dictionary<Type, List<T>> _componentsByType;
    private readonly Dictionary<T, int> _componentToIndex;
    private readonly Dictionary<T, int> _lastKnownZIndex;
    private bool _isDirty;
    private bool _isTypeCacheDirty;

    public SCDrawableCollection()
    {
        _components = [];
        _componentToIndex = new Dictionary<T, int>();
        _lastKnownZIndex = new Dictionary<T, int>();
        _componentsByType = new Dictionary<Type, List<T>>();
        _isDirty = false;
        _isTypeCacheDirty = false;
    }

    public SCDrawableCollection(int capacity)
    {
        _components = new List<T>(capacity);
        _componentToIndex = new Dictionary<T, int>(capacity);
        _lastKnownZIndex = new Dictionary<T, int>(capacity);
        _componentsByType = new Dictionary<Type, List<T>>();
        _isDirty = false;
        _isTypeCacheDirty = false;
    }

    /// <summary>
    /// Number of components in the collection
    /// </summary>
    public int Count => _components.Count;

    /// <summary>
    /// Gets component at specified index (after sorting)
    /// </summary>
    /// <param name="index">Index of the component</param>
    /// <returns>Component at the specified index</returns>
    public T this[int index]
    {
        get
        {
            EnsureSorted();
            return _components[index];
        }
    }

    /// <summary>
    /// Gets enumerator for the collection (sorted by ZIndex)
    /// </summary>
    /// <returns>Enumerator for the collection</returns>
    public IEnumerator<T> GetEnumerator()
    {
        EnsureSorted();
        return _components.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Adds a component to the collection
    /// </summary>
    /// <param name="component">Component to add</param>
    /// <exception cref="ArgumentNullException">Thrown when component is null</exception>
    /// <exception cref="ArgumentException">Thrown when component already exists in collection</exception>
    public void Add(T component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_componentToIndex.ContainsKey(component))
        {
            throw new ArgumentException("Component already exists in collection", nameof(component));
        }

        _components.Add(component);
        _componentToIndex[component] = _components.Count - 1;
        _lastKnownZIndex[component] = component.ZIndex;
        _isDirty = true;
        _isTypeCacheDirty = true;
    }

    /// <summary>
    /// Removes a component from the collection
    /// </summary>
    /// <param name="component">Component to remove</param>
    /// <returns>True if component was removed, false if not found</returns>
    public bool Remove(T component)
    {
        if (component == null || !_componentToIndex.ContainsKey(component))
        {
            return false;
        }

        _components.Remove(component);
        _componentToIndex.Remove(component);
        _lastKnownZIndex.Remove(component);
        _isDirty = true;
        _isTypeCacheDirty = true;
        RebuildIndexMap();

        return true;
    }

    /// <summary>
    /// Removes component at specified index
    /// </summary>
    /// <param name="index">Index of component to remove</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range</exception>
    public void RemoveAt(int index)
    {
        EnsureSorted();

        if (index < 0 || index >= _components.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var component = _components[index];

        _components.RemoveAt(index);
        _componentToIndex.Remove(component);
        _lastKnownZIndex.Remove(component);
        _isTypeCacheDirty = true;
        RebuildIndexMap();
    }

    /// <summary>
    /// Checks if collection contains the specified component
    /// </summary>
    /// <param name="component">Component to check</param>
    /// <returns>True if component exists in collection</returns>
    public bool Contains(T component)
    {
        return component != null && _componentToIndex.ContainsKey(component);
    }

    /// <summary>
    /// Checks if collection contains any component of the specified type (with caching for performance)
    /// </summary>
    /// <typeparam name="TComponent">Type of component to check for</typeparam>
    /// <returns>True if any component of the specified type exists in collection</returns>
    public bool Contains<TComponent>()
        where TComponent : class, T
    {
        EnsureTypeCacheUpdated();
        return _componentsByType.ContainsKey(typeof(TComponent))
               && _componentsByType[typeof(TComponent)].Count > 0;
    }

    /// <summary>
    /// Clears all components from the collection
    /// </summary>
    public void Clear()
    {
        _components.Clear();
        _componentToIndex.Clear();
        _lastKnownZIndex.Clear();
        _componentsByType.Clear();
        _isDirty = false;
        _isTypeCacheDirty = false;
    }

    /// <summary>
    /// Gets components within a specific ZIndex range
    /// </summary>
    /// <param name="minZIndex">Minimum ZIndex (inclusive)</param>
    /// <param name="maxZIndex">Maximum ZIndex (inclusive)</param>
    /// <returns>Enumerable of components within the specified range</returns>
    public IEnumerable<T> GetComponentsInZRange(int minZIndex, int maxZIndex)
    {
        EnsureSorted();

        foreach (var component in _components)
        {
            if (component.ZIndex >= minZIndex && component.ZIndex <= maxZIndex)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets all enabled components
    /// </summary>
    /// <returns>Enumerable of enabled components</returns>
    public IEnumerable<T> GetEnabledComponents()
    {
        EnsureSorted();

        foreach (var component in _components)
        {
            if (component.IsEnabled)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets all visible components
    /// </summary>
    /// <returns>Enumerable of visible components</returns>
    public IEnumerable<T> GetVisibleComponents()
    {
        EnsureSorted();

        foreach (var component in _components)
        {
            if (component.IsVisible)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets all components that are both enabled and visible
    /// </summary>
    /// <returns>Enumerable of enabled and visible components</returns>
    public IEnumerable<T> GetActiveComponents()
    {
        EnsureSorted();

        foreach (var component in _components)
        {
            if (component.IsEnabled && component.IsVisible)
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Gets components of a specific type (with caching for performance)
    /// </summary>
    /// <typeparam name="TComponent">Type of component to retrieve</typeparam>
    /// <returns>Enumerable of components of the specified type</returns>
    public IEnumerable<TComponent> GetComponentsOfType<TComponent>()
        where TComponent : class, T
    {
        EnsureTypeCacheUpdated();

        if (_componentsByType.TryGetValue(typeof(TComponent), out var components))
        {
            foreach (var component in components)
            {
                if (component is TComponent typedComponent)
                {
                    yield return typedComponent;
                }
            }
        }
    }

    /// <summary>
    /// Gets the first component of a specific type, or null if not found
    /// </summary>
    /// <typeparam name="TComponent">Type of component to retrieve</typeparam>
    /// <returns>First component of the specified type, or null</returns>
    public TComponent? GetFirstComponentOfType<TComponent>()
        where TComponent : class, T
    {
        EnsureTypeCacheUpdated();

        if (_componentsByType.TryGetValue(typeof(TComponent), out var components) && components.Count > 0)
        {
            return components[0] as TComponent;
        }

        return null;
    }

    /// <summary>
    /// Forces a resort of the collection
    /// </summary>
    public void ForceSort()
    {
        _isDirty = true;
        EnsureSorted();
    }

    /// <summary>
    /// Converts collection to array (sorted by ZIndex)
    /// </summary>
    /// <returns>Array of components sorted by ZIndex</returns>
    public T[] ToArray()
    {
        EnsureSorted();
        return _components.ToArray();
    }

    /// <summary>
    /// Checks if any component's ZIndex has changed and marks collection as dirty if needed
    /// </summary>
    public void CheckForZIndexChanges()
    {
        foreach (var kvp in _lastKnownZIndex)
        {
            var component = kvp.Key;
            var lastKnownZIndex = kvp.Value;

            if (component.ZIndex != lastKnownZIndex)
            {
                _lastKnownZIndex[component] = component.ZIndex;
                _isDirty = true;
            }
        }
    }

    private void EnsureTypeCacheUpdated()
    {
        if (!_isTypeCacheDirty)
        {
            return;
        }

        _componentsByType.Clear();

        foreach (var component in _components)
        {
            var componentType = component.GetType();

            // Add the exact type
            if (!_componentsByType.ContainsKey(componentType))
            {
                _componentsByType[componentType] = new List<T>();
            }

            _componentsByType[componentType].Add(component);

            // Add all base types and interfaces that are assignable from T
            var currentType = componentType.BaseType;
            while (currentType != null && typeof(T).IsAssignableFrom(currentType))
            {
                if (!_componentsByType.TryGetValue(currentType, out List<T>? value))
                {
                    value = new List<T>();
                    _componentsByType[currentType] = value;
                }

                value.Add(component);
                currentType = currentType.BaseType;
            }

            // Add all interfaces that are assignable from T
            foreach (var interfaceType in componentType.GetInterfaces())
            {
                if (typeof(T).IsAssignableFrom(interfaceType))
                {
                    if (!_componentsByType.TryGetValue(interfaceType, out List<T>? value))
                    {
                        value = new List<T>();
                        _componentsByType[interfaceType] = value;
                    }

                    value.Add(component);
                }
            }
        }

        _isTypeCacheDirty = false;
    }

    private void EnsureSorted()
    {
        if (!_isDirty)
        {
            return;
        }

        _components.Sort((x, y) => x.ZIndex.CompareTo(y.ZIndex));
        RebuildIndexMap();
        _isDirty = false;
    }

    private void RebuildIndexMap()
    {
        _componentToIndex.Clear();
        for (var i = 0; i < _components.Count; i++)
        {
            _componentToIndex[_components[i]] = i;
        }
    }
}
