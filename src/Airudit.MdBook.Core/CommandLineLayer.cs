
namespace Airudit.MdBook.Core;

using System;

public sealed class CommandLineLayer
{
    public CommandLineLayer(TextWriter @out, TextWriter errorOut, TextReader @in, string[] args)
    {
        this.Out = @out;
        this.ErrorOut = errorOut;
        this.In = @in;
        this.Arguments = args;
    }

    public TextWriter Out { get; }
    public TextWriter ErrorOut { get; }
    public TextReader In { get; }
    public string[] Arguments { get; init; }
    
}
