Use as a C# library
================================

The rendering engine ships as a separate NuGet package, `Airudit.MdBook.Core`, so
you can generate HTML from your own .NET code instead of shelling out to the
command-line tool.

Install
----------------------------------------------------------------

```bash
dotnet add package Airudit.MdBook.Core
```

Render a file
----------------------------------------------------------------

The renderer is organised as a *context* holding one or more *layers*, processed
by *tasks*. To convert a single file you configure a
`SimpleMarkdownToHtmlLayer`, add it to a `PackageContext`, then run a
`SimpleMarkdownToHtmlTask`:

```csharp
using Airudit.MdBook.Core;

// Create an HTML file from a Markdown file.
public static void SimpleMdToHtml(string inputFilePath, string? templateFilePath)
{
    // configure the input
    var sourceFile = new FileInfo(inputFilePath);
    var layer = new SimpleMarkdownToHtmlLayer();
    layer.AddFile(sourceFile, true);
    layer.TemplateFilePath = templateFilePath; // null → built-in light template

    // prepare the processing stack
    var context = new PackageContext();
    context.AddLayer(layer);
    var task = new SimpleMarkdownToHtmlTask();
    task.Visit(context);

    // generate the file
    task.Run(context);
}
```

This writes `<input>.md.html` next to the source file. Passing `null` for the
template uses the built-in light template; pass a file path, or a `builtin:` name
such as `"builtin:default.dark.html"`, to choose another. Template placeholders
work exactly as in [Templates and placeholders](templates.en.md).

How the pieces fit
----------------------------------------------------------------

- `PackageContext` — the shared state a run passes through.
- `SimpleMarkdownToHtmlLayer` — the inputs and settings: the files to render
  (`AddFile`), `TemplateFilePath`, and `Copyright`.
- `SimpleMarkdownToHtmlTask` — parses and converts the Markdown in memory.
  `Visit` prepares the pipeline and loads the template; `Run` writes the output.

The command-line tool is a thin wrapper over the same building blocks: it adds a
task to parse arguments, one to export the results to a directory, and one to
combine them into a single file. If you need those behaviours from code, see the
`ExportMarkdownToHtmlTask` and `CombineMarkdownToHtmlTask` types in the same
package.

The working example above is kept in sync with the project's unit tests
(`UseAsCodeLibrary`), so it stays buildable.
