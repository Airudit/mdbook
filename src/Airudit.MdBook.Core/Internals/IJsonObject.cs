
namespace Airudit.Promethai.Domain.Core.Internals
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

    public interface IJsonConfigurationObject : IJsonObject
    {
        /// <summary>
        /// The path of the file that loaded this object.
        /// </summary>
        string ConfigurationFilePath { get; set; }

        /// <summary>
        /// When configuration is loaded from a file, this contains the work directory. 
        /// </summary>
        string WorkDirectory { get; set; }

        /// <summary>
        /// When configuration is loaded from a file, this contains the file encoding. 
        /// </summary>
        string ConfigurationFileEncoding { get; set; }

        /// <summary>
        /// Indicates how this configuration has been created. 
        /// <para>When "LoadedFromFile", indicates the configuration is loaded from a file</para>
        /// <para>When "ZeroNoFile", indicates the configuration is empty and not loaded from a file</para>
        /// </summary>
        string ConfigurationFlag { get; set; }

        /// <summary>
        /// The configuration name you desire.
        /// </summary>
        string ConfigurationName { get; set; }
    }
}
