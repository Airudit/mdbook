
[Airudit.MdBook](https://github.com/Airudit/mdbook)
=========

turns a collection of markdown/commonmark files into a (digital) book

- nuget `Airudit.MdBook` is the dotnet tool at [nuget.org](https://www.nuget.org/packages/Airudit.MdBook)
- nuget `Airudit.MdBook.Core` is the code library at [nuget.org](https://www.nuget.org/packages/Airudit.MdBook.Core)

Features
------------------------------------

- Render Markdown / CommonMark to standalone, self-contained HTML — inline CSS, no
  external CDN, both screen- and print-friendly.
- **Single-file books** — combine a whole folder into one HTML file with a nested
  table of contents and working in-file links. See
  [Single-file books](help/single-file.en.md).
- **`--ByLang`** — one combined book per language for multilingual sources.
- Output in place, into a directory (`--Export`), or as a single file — or several at
  once; runs quiet by default, with `--Verbose` for a per-page trace.
- Composable pages with `{{include: …}}`, automatic link rewriting, and a code library
  for embedding the renderer in your own tools.

Documentation
------------------------------------

Full user guide in [`help/`](help/README.en.md): getting started, command-line
usage, single-file books, templates and placeholders, file includes, and using the
code library.

Usage
------------------------------------

```
mdbook --help
```

```
Airudit.MdBook – Usage

This command will generate HTML files for each specified markdown file.

command usage:
    mdbook {file path}+ [options]

Output modes (default: write X.md.html next to each source):
    --Export <dir>        Copy the generated pages into <dir> (mirrored paths)
    --Single-File <file>  Combine every page into one self-contained HTML file
    --Side                Also write the in-place files. They are on by default but
                          suppressed once --Export or --Single-File is given; pass
                          --Side to keep writing them as well.

Single-file options:
    --ByLang              With --Single-File, write one combined file per detected
                          language. Put "{lang}" in the file path (e.g. book.{lang}.html);
                          otherwise ".{lang}" is inserted before the extension.

Rendering:
    --Template <file>     HTML template file path, or a builtin: name (see below)
    --Copyright <str>     Copyright notice, exposed to the template as {{{Copyright}}}

Output & info:
    --Verbose, -v         Print a per-page trace while rendering (quiet by default)
    --Version             Print the tool version and exit

Built-in templates:
    --Template builtin:default.light.html
    --Template builtin:default.dark.html

Environment variables:
    MDBOOK_TEMPLATE       Default template used when --Template is not given
    MDBOOK_COPYRIGHT      Default copyright notice used when --Copyright is not given
    MDBOOK_NUMBERED_SETEXT_FIX
                          Set to 0/false/off/no to disable the numbered setext fix
```

Make HTML files from MD files now:

```
mdbook    my.md dir/*.md other-dir/
```

Make HTML files from local MD files in a dedicated directory:

```
mdbook    .         --export ~/docs/
```

Make a single HTML file from all MD in directory docs:

```
mdbook     docs/    --single-file docs.html
```

You can also do all this using C# by adding a PackageReference to the code library.


Install options
------------------------------------

### Install from binary release

For non-developer use. The script installs .NET if needed.

**Linux** — one-liner:

```bash
curl -fsSL https://raw.githubusercontent.com/Airudit/mdbook/refs/heads/main/packages/install.sh | bash
```

**Windows** — open PowerShell:

```powershell
irm https://raw.githubusercontent.com/Airudit/mdbook/refs/heads/main/packages/install.ps1 | iex
```

Both install `mdbook` and write an `mdbook-update` command for future updates.

To pass arguments (e.g. see all options), use the scriptblock form instead of `irm | iex`:

```bash
# bash
curl -fsSL https://raw.githubusercontent.com/Airudit/mdbook/refs/heads/main/packages/install.sh | bash -s -- --help
```

```powershell
# PowerShell
$f="$env:TEMP\mdbook-install.ps1"; irm https://raw.githubusercontent.com/Airudit/mdbook/refs/heads/main/packages/install.ps1 -OutFile $f; & $f -Help; del $f
```

Verify the install:

```
mdbook --help
```


### Machine install (global)

For developers with the .NET SDK installed:

```
dotnet tool install -g Airudit.MdBook
```

> You can invoke the tool using the following command: mdbook  
Tool 'airudit.mdbook' (version '0.1.2') was successfully installed.

See also: [how to manage and use .NET tools](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools), [dotnet tool install troubleshooting](https://learn.microsoft.com/en-us/dotnet/core/tools/troubleshoot-usage-issues)


### Use during CI

In your repository: make a project local install with:

```console
# create a tool manifest file for your project
dotnet new tool-manifest

# verify
cat .config/dotnet-tools.json

# install
dotnet tool install Airudit.MdBook

# verify
cat .config/dotnet-tools.json

# verify command
dotnet mdbook --help
```

During your CI, restore the tools:

```console
dotnet tool restore
```

Now you can use the command in your build process

```console
dotnet mdbook help/ README.md
```


Code
------------------------------------

To run, use: 

```bash
dotnet run -v q --framework net8.0 --project src/Airudit.MdBook -- --help
```


Releasing
------------------------------------

Publishing is handled by the `publish` GitHub Actions workflow (`.github/workflows/publish.yml`), triggered when a GitHub Release is published.

**What gets published:**

- `Airudit.MdBook` NuGet package (dotnet global tool) → nuget.org
- `Airudit.MdBook.Core` NuGet package (code library) → nuget.org
- `mdbook-{version}-linux-x64.tar.gz` → attached to the GitHub Release
- `mdbook-{version}-win-x64.zip` → attached to the GitHub Release

**Steps to release:**

1. Push all changes to `main`
2. Create and push a version tag: `git tag v1.2.3 && git push origin v1.2.3`
3. On GitHub, create a Release from that tag — this triggers the workflow
4. The workflow builds, tests, and publishes everything automatically

The version is derived from the git tag via [MinVer](https://github.com/adamralph/minver). The tag must start with `v` (e.g. `v1.2.3`).

The binary release assets target `net8.0` and are framework-dependent (require .NET 8 on the target machine).

Required secret: `NUGETAIRUDIT` (NuGet API key with push rights).


More information
------------------------------------

This project uses [Markdig](https://github.com/xoofx/markdig) as MD parser and HTML renderer.

We use this at [Airudit](https://www.airudit.com/) to bundle documentation files. 

If you need a different template, feel free to create one based on [the built-in ones](https://github.com/Airudit/mdbook/tree/main/src/Airudit.MdBook.Core/res).

To use a code library, you can use this basic code (see [unit test](src/Airudit.MdBook.UnitTests/UseAsCodeLibrary.cs)):

```csharp
using Airudit.MdBook.Core;

/// <summary>
/// Create an HTML from Markdown path file
/// </summary>
public static void SimpleMdToHtml(string inputFilePath, string? templateFilePath)
{
    // configure
    var sourceFile = new FileInfo(inputFilePath);
    var layer = new SimpleMarkdownToHtmlLayer();
    layer.AddFile(sourceFile, true);
    layer.TemplateFilePath = templateFilePath;

    // prepare stack
    var context = new PackageContext();
    context.AddLayer(layer);
    var simpleConverterTask = new SimpleMarkdownToHtmlTask();
    simpleConverterTask.Visit(context);

    // generate the file
    simpleConverterTask.Run(context);
}
```

