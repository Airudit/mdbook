Getting started
================================

This page installs the tool and renders a first Markdown file.

Install
----------------------------------------------------------------

`mdbook` runs on the .NET runtime — not the full .NET SDK. The runtime must be
present on the machine: either it is already installed, or the binary installer
below installs it for you.

**Linux** — one line:

```bash
curl -fsSL https://raw.githubusercontent.com/Airudit/mdbook/refs/heads/main/packages/install.sh | bash
```

**Windows** — in PowerShell:

```powershell
irm https://raw.githubusercontent.com/Airudit/mdbook/refs/heads/main/packages/install.ps1 | iex
```

Both install the `mdbook` command and an `mdbook-update` command for later
updates. Verify the install with:

```bash
mdbook --help
```

Developers who already have the .NET SDK can instead install the global tool from
NuGet:

```bash
dotnet tool install -g Airudit.MdBook
```

Render your first file
----------------------------------------------------------------

Create a Markdown file and render it:

```bash
echo "# Hello\n\nThis is **mdbook**." > hello.md
mdbook hello.md
```

This writes `hello.md.html` next to the source. Open it in a browser. The page is
self-contained: the styling is inlined, so you can move or e-mail the single file
and it still renders correctly.

Render a whole folder
----------------------------------------------------------------

Point `mdbook` at a directory to render every `.md` file it contains, including
sub-folders:

```bash
mdbook help/
```

To gather the rendered pages into a separate output tree, add `--Export`:

```bash
mdbook help/ --Export ./site
```

See [Command-line usage](cli-usage.en.md) for every input form and option.

Which Markdown is supported
----------------------------------------------------------------

`mdbook` uses [Markdig](https://github.com/xoofx/markdig) with these extensions
enabled:

- **Pipe tables** — `| a | b |` grid tables.
- **Task lists** — `- [ ]` / `- [x]` checkboxes.
- **Auto identifiers** — headings get an `id`, so `#section` anchors work.
- **Auto links** — bare URLs become links.
- **Emphasis extras** — `~~strikethrough~~`, `++inserted++`, and similar.

On top of that, `mdbook` adds two features of its own: file
[includes](includes.en.md) and [template placeholders](templates.en.md).
