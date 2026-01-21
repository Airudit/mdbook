
namespace Airudit.MdBook.Core;

public sealed class PackageContext
{
    private List<object> layers { get; } = new();
    
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
}
