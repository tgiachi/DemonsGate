using System.Collections;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Collections;

public class SC3dDrawableCollection<T> : IEnumerable<T>
    where T : class, ISC3dDrawableComponent
{
    private readonly List<T> _components = new();

    public int Count => _components.Count;

    public T this[int index] => _components[index];

    public void Add(T component)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (_components.Contains(component))
        {
            throw new ArgumentException("Component already exists in collection", nameof(component));
        }
        _components.Add(component);
    }

    public bool Remove(T component)
    {
        return _components.Remove(component);
    }

    public void Clear()
    {
        _components.Clear();
    }

    public IEnumerable<T> GetEnabledComponents()
    {
        return _components.Where(c => c.IsEnabled);
    }

    public IEnumerable<T> GetVisibleComponents()
    {
        return _components.Where(c => c.IsVisible);
    }

    public IEnumerable<T> GetActiveComponents()
    {
        return _components.Where(c => c.IsEnabled && c.IsVisible);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _components.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
