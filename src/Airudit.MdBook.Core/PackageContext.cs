
namespace Airudit.MdBook.Core;

using EA4T.SteadyBear.Packager;
using System;

public class PackageContext
{
    private List<object> layers { get; } = new();
    
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
