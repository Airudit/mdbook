
[Airudit.MdBook](https://github.com/Airudit/mdbook)
=========

turns a collection of markdown/commonmark files into a (digital) book

- nuget `Airudit.MdBook` is the dotnet tool at [nuget.org](https://www.nuget.org/packages/Airudit.MdBook)
- nuget `Airudit.MdBook.Core` is the code library at [nuget.org](https://www.nuget.org/packages/Airudit.MdBook.Core)

Why single-file?
------------------------------------

Most documentation tools build a *website* — a tree of HTML, CSS and assets that needs a
server, or at least needs to stay intact, to be read. mdbook builds the opposite: **one
self-contained HTML file** that opens anywhere and bends to however you want to read it —
on screen, in print, in a reader, or down a shell. It's text you can grep, diff and even
edit, and everything it needs lives inside it.

Inspired by [SingleFile](https://github.com/gildas-lormeau/SingleFile). The longer version
— why I built it this way — is in
[Why mdbook exists](https://github.com/Airudit/mdbook/blob/main/help/about.en.md).

Features
------------------------------------

- Render Markdown / CommonMark to standalone, self-contained HTML — inline CSS, no
  external CDN, both screen- and print-friendly.
- **Single-file books** — combine a whole folder into one HTML file with a nested
  table of contents and working in-file links. See
  [Single-file books](https://github.com/Airudit/mdbook/blob/main/help/single-file.en.md).
- **`--ByLang`** — one combined book per language for multilingual sources.
- Output in place, into a directory (`--Export`), or as a single file — or several at
  once; runs quiet by default, with `--Verbose` for a per-page trace.
- Composable pages with `{{include: …}}`, automatic link rewriting, and a code library
  for embedding the renderer in your own tools.

Documentation
------------------------------------

Full user guide in [`help/`](https://github.com/Airudit/mdbook/blob/main/help/README.en.md): getting started, command-line
usage, single-file books, templates and placeholders, file includes, and using the
code library.

Usage
------------------------------------

```bash
mdbook {file path or directory}+ [options]
```

Render loose files in place, export a folder to a directory, or combine a folder into
one self-contained book:

```bash
mdbook my.md dir/*.md other-dir/         # render each in place
mdbook .      --Export ~/docs/           # export a mirrored tree
mdbook docs/  --Single-File docs.html    # one combined book
```

Run `mdbook --help` for every option, or read the full
[Command-line usage](https://github.com/Airudit/mdbook/blob/main/help/cli-usage.en.md) guide (modes, `--ByLang`, templates,
environment variables). You can also drive the renderer from C# — see
[Use as a C# library](https://github.com/Airudit/mdbook/blob/main/help/library.en.md).


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

Pin the tool per-project so its version is tracked in source control:

```console
dotnet new tool-manifest            # once, committed to the repo
dotnet tool install Airudit.MdBook
dotnet tool restore                 # on every build machine
dotnet mdbook help/ README.md       # then run it
```

See [Command-line usage](https://github.com/Airudit/mdbook/blob/main/help/cli-usage.en.md) for the full CI walkthrough.


More information
------------------------------------

This project uses [Markdig](https://github.com/xoofx/markdig) as MD parser and HTML renderer.

We use `mdbook` at [Airudit](https://www.airudit.com/) to bundle documentation files.

If you need a different template, feel free to create one based on [the built-in ones](https://github.com/Airudit/mdbook/tree/main/src/Airudit.MdBook.Core/res).

To embed the renderer in your own tool, see [Use as a C# library](https://github.com/Airudit/mdbook/blob/main/help/library.en.md) —
with a runnable [example](https://github.com/Airudit/mdbook/blob/main/src/Airudit.MdBook.UnitTests/UseAsCodeLibrary.cs).

**Maintainers:** see [RELEASING.md](https://github.com/Airudit/mdbook/blob/main/RELEASING.md) for how releases are published.

