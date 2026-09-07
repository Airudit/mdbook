Command-line usage
================================

Synopsis
----------------------------------------------------------------

```
mdbook {file path or directory}+ [options]
```

You give `mdbook` one or more inputs, and it generates HTML. Inputs may be mixed
freely:

- A **file** (`hello.md`) is rendered to `hello.md.html` next to the source.
- A **directory** (`help/`) is scanned recursively for `*.md` files; each is
  rendered in place, preserving the folder structure.

Anything that is neither an existing file nor directory is reported as an unknown
argument and the run stops.

Options
----------------------------------------------------------------

Option names are case-insensitive (`--export` and `--Export` are equal).

- `--Export <dir>` — copy the generated HTML into `<dir>`, preserving each file's
  relative path. Files you linked to that are not Markdown (images, downloads)
  are copied along too. May be given more than once to export to several
  locations. On its own this writes only into `<dir>` — the in-place files next
  to the sources are suppressed (see `--Side`).
- `--Single-File <file>` — combine every rendered page into one HTML file at
  `<file>`, prefixed with a table of contents linking to each page. On its own
  this writes only the single file — the in-place files are suppressed (see
  `--Side`).
- `--Side` — also write each page's HTML in place, next to its source file. The
  in-place files are written by default, but are suppressed once `--Export` or
  `--Single-File` is given; pass `--Side` to keep writing them as well.
- `--Template <file>` — use `<file>` as the HTML template instead of the built-in
  one. Accepts a path, or a `builtin:` name (see below). Details in
  [Templates and placeholders](templates.en.md).
- `--Copyright <str>` — a copyright notice made available to the template as
  `{{{Copyright}}}`.

Built-in templates:

```
--Template builtin:default.light.html
--Template builtin:default.dark.html
```

Examples
----------------------------------------------------------------

Render loose files and a folder in one call:

```bash
mdbook my.md dir/*.md other-dir/
```

Render a folder into a dedicated output directory:

```bash
mdbook . --Export ~/docs/
```

Build one combined HTML file from every page in `docs/`:

```bash
mdbook docs/ --Single-File docs.html
```

Build the combined file but keep the per-page files next to the sources too:

```bash
mdbook docs/ --Single-File docs.html --Side
```

Render with the dark built-in template:

```bash
mdbook help/ --Template builtin:default.dark.html --Export ./site
```

Use during CI
----------------------------------------------------------------

Install `mdbook` as a project-local tool so the version is pinned in source
control:

```bash
# once, committed to the repo
dotnet new tool-manifest
dotnet tool install Airudit.MdBook

# on every build machine
dotnet tool restore

# then run it
dotnet mdbook help/ README.md
```

What gets written
----------------------------------------------------------------

- By default, for every input `X.md`, a file `X.md.html` is written next to the
  source.
- Giving an output destination changes where the pages go: `--Export <dir>`
  writes them into `<dir>` (preserving relative paths), and `--Single-File <file>`
  merges them into one document. Either one suppresses the in-place `X.md.html`
  files; add `--Side` to write those as well.
- Local `.md` links inside the content are rewritten to `.md.html` so the
  rendered book stays navigable.
- Links to external URLs (`http`, `https`, `ftp`) get a `class="external"` so a
  template can style them.
