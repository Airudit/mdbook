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
  locations.
- `--Single-File <file>` — in addition to the per-file output, combine every
  rendered page into one HTML file at `<file>`, prefixed with a table of contents
  linking to each page.
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

- For every input `X.md`, a file `X.md.html` next to the source.
- Local `.md` links inside the content are rewritten to `.md.html` so the
  rendered book stays navigable.
- Links to external URLs (`http`, `https`, `ftp`) get a `class="external"` so a
  template can style them.
- With `--Export`, the above are copied into the export tree; with
  `--Single-File`, they are also merged into one document.
