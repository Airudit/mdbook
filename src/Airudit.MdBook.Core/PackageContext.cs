
namespace Airudit.MdBook.Core;

/// <summary>
/// A common context for a pipeline processing. Layers allows storing various data.
/// </summary>
public sealed class PackageContext
{
    private readonly List<object> layers = new();
    
    /// <summary>
    /// The data layers.
    /// </summary>
    public IReadOnlyList<object> Layers => layers;

    public void AddLayer(object layer)
    {
        this.layers.Add(layer);
    }

    public T RequireSingleLayer<T>()
    {
        return this.layers.OfType<T>().Single();
    }

    public T? GetSingleLayer<T>()
    {
        return this.layers.OfType<T>().SingleOrDefault();
    }
}
