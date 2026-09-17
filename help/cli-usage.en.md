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

### Introduction

`mdbook` emits HTML in one of three ways, described below. They can be combined,
and giving `--Export` or `--Single-File` **suppresses** the in-place `X.md.html`
files — the destination is assumed to be what you want; pass `--Side` to keep
writing them as well.

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

For the reader's side of each mode — who opens the output and how — see
[Reading a book](reading.en.md).

### In place (default)

For every input `X.md`, a file `X.md.html` is written next to the source; a folder is
rendered in place, preserving its structure. This is what you get when no destination
option is given. Reach for it to **read and review your local Markdown properly while
you work on it** — keep the folder open in a browser and refresh as you edit.

### Export

`--Export <dir>` copies the generated pages into `<dir>`, preserving each file's
relative path, and copies the non-Markdown files you link to (images, downloads)
alongside. It may be given more than once to export to several places. This is the mode
to **publish a navigable copy to a static-file web server** — a mirrored tree, ready to
drop into a site root.

### Single file

`--Single-File <file>` combines every page into one self-contained HTML document — its
own table of contents, in-file navigation, and (by default) its images inlined. This is
`mdbook`'s headline output: **one portable, offline document to hand to a reader**, with
no source folder, no server, and no `mdbook` needed to open it. See
[Single-file books](single-file.en.md).

Options
----------------------------------------------------------------

Option names are case-insensitive (`--export` and `--Export` are equal).

- `--Export <dir>` — copy the generated HTML into `<dir>`, preserving each file's
  relative path. Files you linked to that are not Markdown (images, downloads)
  are copied along too, unless `--Embed` inlines the images instead. May be given
  more than once to export to several
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
- `--Title <str>` — an explicit title, overriding the derived one in **every** mode. With
  `--Single-File` it names the book (its `{{{PageTitle}}}`); in the in-place and export
  modes it overrides each page's `{{{PageTitle}}}`. Applied to a **multi-page** in-place or
  export run it stamps the same title on every page (a warning is printed), so it is meant
  mainly for single(-file) output. No environment-variable fallback — the flag is the only
  source. See *Titles and front matter* below.
- `--Embed` — inline the local images referenced by `![alt](path)` into the HTML as
  `data:` URIs, so the output stays self-contained once moved away from its source
  folder. Off by default, but **on automatically with `--Single-File`** (an external
  image would defeat the point of one self-contained file). Each image's type is
  detected from its content; remote, missing and already-inlined images are left as
  references, and a non-image file you link to is still copied/exported as before.
- `--No-Embed` — opt out of image inlining, keeping every image as an external
  reference even under `--Single-File`.
- `--Highlight` / `--No-Highlight` — syntax-highlight fenced code blocks whose
  language is recognized, coloring them with classes from the template (see
  `{{{HighlightStyles}}}` in [Templates](templates.en.md)). **On by default**;
  pass `--No-Highlight` to turn it off. A block with no language, or an
  unrecognized one, is left as plain code either way. No JavaScript, no network —
  the colors are plain CSS, so they print and read in a terminal browser.
- `--Diagrams <none|docker|kroki>` — render diagram fences (`mermaid`, `plantuml`, …) to
  inline SVG at build time. **Off by default** (`none`): the block stays plain and a hint
  is shown. `docker` renders locally with one-shot images (offline, private); `kroki` uses
  a Kroki server (needs `--KrokiUrl`) and covers every Kroki diagram type. See
  [Diagrams](diagrams.en.md).
- `--KrokiUrl <url>` — the Kroki base URL for `--Diagrams kroki`, e.g. `https://kroki.io/`
  (with the trailing slash). A **public** server receives your diagram source — prefer a
  local renderer or a self-hosted Kroki for confidential content.
- `--Verbose`, `-v` — print a per-page trace (`Processing markdown file "…"`) while
  rendering. A run is quiet by default; the in-place writes performed by `--Side`
  (or a default no-destination run) are reported either way. For diagrams, `--Verbose`
  also explains why any fence was left unrendered (a server error, Docker not installed, …).
- `--Version` — print the tool version (the full semantic version, e.g.
  `0.4.0+<commit>`) and exit.

Built-in templates:

```
--Template builtin:default.light.html
--Template builtin:default.dark.html
```

Titles and front matter
----------------------------------------------------------------

By default a page's title is its file name (minus any `.xx` language segment), and a combined
book's title is the output file name. Three things override that.

### A page's own title

Begin a Markdown file with a YAML **front-matter** block to name it explicitly:

```markdown
---
title: Installation guide
---

# ...
```

The block is recognized only at the very top of the file — so a `---` later in the document
is still a page break, not front matter — and it is removed from the rendered body. Its
`title:` sets both the page `{{{PageTitle}}}` and, in a combined book, the page's
table-of-contents label. Only `title` is read for now; unknown keys are ignored.

### `--Title`

`--Title <str>` overrides the title from the command line, in every mode (see *Options*).

### A book's metadata and cover: `.mdbook.<lang>.md`

A file named `.mdbook.md` — or `.mdbook.<lang>.md` per language (`.mdbook.en.md`,
`.mdbook.fr.md`) — is a **book-metadata sidecar**, not a page. Name it among the inputs and
`mdbook` reads its front-matter as the book's metadata (`title`, `lang`) and, if it has body
text, renders that body as the book's **introduction**, placed above the table of contents.
The sidecar is never rendered, exported, or listed as a page, and one found by scanning a
folder is ignored (name it explicitly to use it). It applies to the combined `--Single-File`
output only; for the full behaviour and the book-title fallback chain, see
[Single-file books](single-file.en.md).

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
- `MDBOOK_HIGHLIGHT_TIMEOUT_MS` — the per-block cap for syntax highlighting, in
  milliseconds (default `1000`). See *Troubleshooting* below; you rarely need to touch it.
- `MDBOOK_DIAGRAMS` / `MDBOOK_KROKI_URL` — defaults for `--Diagrams` and `--KrokiUrl` when
  the flags are not given; the flags always take precedence. See [Diagrams](diagrams.en.md).
- `MDBOOK_DIAGRAM_TIMEOUT_MS` — the per-diagram render cap, in milliseconds (default
  `120000`); a first `docker` render may pull its image, so it is generous.

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

Export self-contained pages, with their images inlined instead of copied alongside:

```bash
mdbook . --Export ~/docs/ --Embed
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

Build one book per language, each with its own cover — title and introduction — from a
`.mdbook.<lang>.md` sidecar. The shell expands `.mdbook.*.md` to the per-language sidecars and
`.` supplies the pages, so a generic build script never has to list the files:

```bash
mdbook --Single-File book.html --ByLang .mdbook.*.md .
```

Each sidecar (`.mdbook.en.md`, `.mdbook.fr.md`) sets its book's title and intro; `--ByLang`
writes `book.en.html` and `book.fr.html`. See [Single-file books](single-file.en.md).

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

Troubleshooting
----------------------------------------------------------------

You will not normally need this section — it covers one rough edge of syntax highlighting.

A run that is unexpectedly slow (seconds where it is usually instant) is almost always a
**malformed code block** feeding the highlighter. The syntax highlighter tokenizes with
regular expressions, and certain invalid input — most often JSON fragments with comments,
truncated structures, or a string that is never closed — can send those expressions into
pathological backtracking. This is a known limitation of the underlying highlighter, not of
your document.

`mdbook` guards against it: each block is capped (1 second by default), and a block that hits
the cap is simply rendered **uncolored** instead of stalling the run. So the worst case is a
few uncolored blocks and a second or two of delay — never a hang. If it still bothers you:

- Lower the cap with `MDBOOK_HIGHLIGHT_TIMEOUT_MS` (e.g. `300`) so a bad block falls back sooner.
- Or turn highlighting off entirely for that run with `--No-Highlight`.
