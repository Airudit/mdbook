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

Operation modes
----------------------------------------------------------------

`mdbook` emits HTML in one of three ways. They can be combined, and `--Side` brings
back the in-place files whenever a destination would otherwise replace them.

- **In place (default)** — for every input `X.md`, a file `X.md.html` is written next
  to the source; a folder is rendered in place, preserving its structure. This is what
  you get when no destination option is given.
- **Export** — `--Export <dir>` copies the generated pages into `<dir>`, preserving
  each file's relative path, and copies the non-Markdown files you link to (images,
  downloads) alongside. It may be given more than once to export to several places.
- **Single file** — `--Single-File <file>` combines every page into one self-contained
  HTML document with its own table of contents and in-file navigation. This is
  `mdbook`'s headline output; see [Single-file books](single-file.en.md).

Giving `--Export` or `--Single-File` **suppresses** the in-place `X.md.html` files — the
destination is assumed to be what you want. Pass `--Side` to keep writing them as well.

| Mode | Example | What is written | In-place `X.md.html` |
|---|---|---|---|
| In place (default) | `mdbook docs/` | `X.md.html` next to each source | written |
| Export | `mdbook docs/ --Export out/` | mirrored tree in `out/`, linked assets copied | suppressed (add `--Side`) |
| Single file | `mdbook docs/ --Single-File book.html` | one combined `book.html` | suppressed (add `--Side`) |

Inside the content, local `.md` links are rewritten to `.md.html` so a rendered book
stays navigable; with `--Single-File` they instead resolve to the target page's in-file
`#anchor`. Links to external URLs (`http`, `https`, `ftp`) get a `class="external"` so a
template can style them. Files pulled in with `{{include: …}}` are partials and are not
written as pages of their own (see [Including files](includes.en.md)).

Options
----------------------------------------------------------------

Option names are case-insensitive (`--export` and `--Export` are equal).

- `--Export <dir>` — copy the generated HTML into `<dir>`, preserving each file's
  relative path. Files you linked to that are not Markdown (images, downloads)
  are copied along too. May be given more than once to export to several
  locations. On its own this writes only into `<dir>` — the in-place files next
  to the sources are suppressed (see `--Side`).
- `--Single-File <file>` — combine every rendered page into one self-contained HTML
  file at `<file>`, with a table of contents and in-file navigation. This is the
  headline mode; see [Single-file books](single-file.en.md) for the full behaviour.
- `--ByLang` — with `--Single-File`, write one combined file **per language** instead
  of one merged document. See [Single-file books](single-file.en.md).
- `--Side` — also write each page's HTML in place, next to its source file. The
  in-place files are written by default, but are suppressed once `--Export` or
  `--Single-File` is given; pass `--Side` to keep writing them as well.
- `--Template <file>` — use `<file>` as the HTML template instead of the built-in
  one. Accepts a path, or a `builtin:` name (see below). Details in
  [Templates and placeholders](templates.en.md).
- `--Copyright <str>` — a copyright notice made available to the template as
  `{{{Copyright}}}`.
- `--Verbose`, `-v` — print a per-page trace (`Processing markdown file "…"`) while
  rendering. A run is quiet by default; the in-place writes performed by `--Side`
  (or a default no-destination run) are reported either way.
- `--Version` — print the tool version (the full semantic version, e.g.
  `0.4.0+<commit>`) and exit.

Built-in templates:

```
--Template builtin:default.light.html
--Template builtin:default.dark.html
```

Environment variables
----------------------------------------------------------------

- `MDBOOK_TEMPLATE` — sets the default template (a file path or a `builtin:` name)
  used when `--Template` is not given on the command line. `--Template` always takes
  precedence, and an empty value is ignored (the built-in default is used). Handy for
  a shell session or CI where the same template is used for every call.
- `MDBOOK_COPYRIGHT` — sets the default copyright notice (exposed to the template as
  `{{{Copyright}}}`) used when `--Copyright` is not given on the command line. `--Copyright`
  always takes precedence, and an empty value is ignored.
- `MDBOOK_NUMBERED_SETEXT_FIX` — controls the fix that keeps a numbered setext heading
  (`1. Title` over an `----` underline) parsing as a heading instead of an ordered-list
  item followed by a thematic break. The fix is **on by default**; set the variable to
  `0`, `false`, `off`, or `no` to opt out and get plain CommonMark parsing. Any other
  value (or leaving it unset) keeps the fix on.

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

Build one combined file per language from a multilingual folder:

```bash
mdbook docs/ --Single-File docs.{lang}.html --ByLang
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

Ordering
----------------------------------------------------------------

The order in which inputs are taken, folders are walked, and duplicates are dropped is only
observable in the combined output, so it is documented with the mode it affects — see
*Page order* in [Single-file books](single-file.en.md).
