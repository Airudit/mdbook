
namespace Airudit.MdBook.Core.Internals
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// An object using SON object backend. 
    /// </summary>
    public interface IJsonObject
    {
        /// <summary>
        /// The JSON node representing the current object.
        /// </summary>
        JObject Node { get; }
    }
}
