Single-file books
================================

Combining a whole folder into one self-contained HTML document is `mdbook`'s headline
capability. Instead of a directory of pages and assets, you get a **single file** you can
email, print, drop onto a share, or ship next to an application — it carries its own table
of contents, navigates itself, and — with its images inlined — needs nothing alongside it.

Building one
----------------------------------------------------------------

```bash
mdbook docs/ --Single-File book.html
```

Every `.md` file under `docs/` is rendered and concatenated into `book.html`, in the order
`mdbook` would otherwise render them (see *Ordering* in
[Command-line usage](cli-usage.en.md)). The template's inline CSS travels with the file, so
the result is standalone and print-friendly.

On its own, `--Single-File` writes only the combined file — the in-place `X.md.html` files
are suppressed. Add `--Side` to keep those too (see *Operation modes* in
[Command-line usage](cli-usage.en.md)).

The book's title and cover
----------------------------------------------------------------

The combined document has one `<title>`, and it can carry a title and an introduction of its
own — separate from any single page. The `<title>` is resolved from the first of these that
is set:

1. `--Title <str>` on the command line;
2. the **`.mdbook` file** title (below);
3. the first page's front-matter `title:`;
4. the first page's first heading;
5. the output file name.

### The `.mdbook.<lang>.md` file

Name a file `.mdbook.md` — or `.mdbook.<lang>.md` per language — among the inputs to give the
book its own metadata, cover, and a small **manifest** that controls ordering, exclusion and
the table of contents:

```markdown
---
title: MySystem Handbook            # book <title>
lang: en                         # book language (localizes the TOC headings)
priority: [readme, install]      # featured pages -> the "Start here" band, in this order
exclude: [internal/, beta.md]    # pages to drop entirely (see "Excluding pages")
exclusive: no                    # yes = ship ONLY the priority pages
toc: yes                         # no = suppress the generated TOC (bring your own index)
priority-title: Read these first # optional band heading (default localized)
toc-title: In this book          # optional tree heading (default localized)
---

Welcome. Start with [installation](install.en.md), then [licensing](licensing.en.md).
```

- **`title` / `lang`** set the book `<title>` (step 2 above) and language, which also localizes
  the table-of-contents headings.
- **Body text** is rendered as the book's **introduction**, placed above the table of contents
  — a natural home for a curated index, with links that resolve to in-file anchors like any
  other cross-page link.
- **`priority`, `exclusive`, `toc`, `priority-title`, `toc-title`** shape page order and the
  table of contents — see *Page order* and *Table of contents* below.
- **`exclude`** removes pages — see *Excluding pages* below. Unlike every other key, it acts in
  **all** output modes, not just the combined one.
- Every key is optional; an absent `.mdbook` file is the same as an all-default one, and unknown
  keys are ignored so new keys stay forward-compatible.

**Path entries** (`priority`, `exclude`) match pages **by path, relative to the `.mdbook`
file's own directory — not your working directory.** The trailing `.md` and any language
segment are optional (`guide/intro` matches `guide/intro.en-US.md`); a trailing `/` matches a
directory and its whole subtree; a `*` globs within one path segment (`advanced/*.md`).
Resolving against the `.mdbook` file's directory means the same manifest works whether you build
from the project root or from inside the docs folder.

The `.mdbook` file is **not a page**: it is never rendered in place, exported, or listed in the
table of contents, and a `.mdbook.*.md` found by scanning a folder is ignored (name it
explicitly on the command line to use it).

Under `--ByLang`, each book uses the `.mdbook` file whose language matches, falling back to a
language-neutral `.mdbook.md`. Give one per language for a localized cover:

```bash
mdbook --Single-File book.html --ByLang .mdbook.*.md .
```

writes `book.en.html` and `book.fr.html`, each with its own title and introduction.

Page order
----------------------------------------------------------------

By default, pages are listed and concatenated in input order:

- Inputs are taken in the order given on the command line, files and folders alike.
  `mdbook README.md guide/ appendix.md` places `README.md` first, then the contents of
  `guide/`, then `appendix.md`.
- Inside a folder, `README` comes first, then `Index`, then the remaining files by name
  (case-insensitive); sub-folders follow, also by name.
- A file named both explicitly and inside a listed folder appears once, at its first
  position. So `mdbook guide/intro.md guide/` puts `intro.md` first and the rest of `guide/`
  after it, with no duplicate.

The `.mdbook` manifest and per-page front matter override this for the combined book:

- **Featured pages first.** `priority: [a, b]` in the `.mdbook` file lifts those pages to the
  front, in the order listed (and into the "Start here" band — see below).
- **Then by per-page `order`.** A page may carry an `order:` number in its own front matter;
  the remaining pages are sorted by it, ascending. A page with no `order` keeps its input
  position — the sort is stable — after all numbered pages. A convention that scales: give the
  pages that matter low numbers and blocks of shared docs higher ranges (1000s, 2000s), so each
  block stays together and sorts after your own pages.
- **`exclusive: yes`** ships only the `priority` pages and drops the rest.

Ordering (the band and per-page `order`) shapes the combined `--Single-File` book only; the
in-place and `--Export` modes render each page on its own, with no sequence.

Excluding pages
----------------------------------------------------------------

The `.mdbook` file's `exclude:` list removes pages from the build entirely — a shared doc that
does not apply to this project, a draft, a whole feature's folder:

```yaml
exclude: [ internal/, drafts/*.md, beta.md ]
```

Unlike ordering and the table of contents, **`exclude` acts in every output mode** — in place,
`--Export` and `--Single-File` alike: an excluded page is rendered nowhere. Entries match the
same way as `priority` — relative to the `.mdbook` file's directory, a trailing `/` for a
directory subtree, `*` for a glob, the language segment and `.md` optional. A one-line summary
reports how many pages were dropped.

Table of contents
----------------------------------------------------------------

The combined file opens with a table of contents in two parts:

- an optional **"Start here" band** — the `priority` pages as a flat, curated shortlist at the
  top (a priority page also appears in the tree below, like a quick-links box); then
- the full **nested tree** that mirrors the source folder structure, with the common leading
  folder stripped and pages sorted by their per-page `order` within each level.

Each page is labelled by its **title** — its front-matter `title:` if it has one, otherwise its
first level-1 heading (`# …`), falling back to the file name without `.md`. Folders appear as
plain labels.

- The two headings are localized from the book's language — the band defaults to `Start here` /
  `Pour commencer` / …, the tree to `Contents` / `Sommaire` / … — and each can be overridden
  with `priority-title:` / `toc-title:` in the `.mdbook` file.
- **`toc: no`** suppresses the whole generated table of contents (band and tree); write your own
  index in the `.mdbook` file's introduction instead.
- The table of contents is `<article id="toc" class="toc">` and the band is
  `<nav class="toc-band">`, so a template can style or float them purely in CSS.

In-file links
----------------------------------------------------------------

Because every page now lives in one document, a link from one page to another can no longer
point at a separate `.md.html` file. `mdbook` rewrites each cross-page `.md` link to the
target page's **in-file anchor** (`#slug`), so navigation works inside the single file.

- Anchors are **path-based slugs** — `guide/intro.md` becomes `guide-intro` — so they stay
  stable when you add or reorder pages.
- A link to a page that is **not** part of the bundle, an external URL, or a bare
  `#fragment` is left untouched.
- A heading-level fragment on a cross-page link is dropped: auto-generated heading ids are
  not unique across the merged document, so only the page-level anchor resolves reliably.

Self-contained images
----------------------------------------------------------------

For the single file to be truly self-contained, its images have to travel inside it. So
under `--Single-File`, `mdbook` **inlines local images by default**: each image referenced
by `![alt](path)` is embedded into the HTML as a `data:` URI, and the file keeps displaying
it wherever it is moved.

- The image's type is detected from its **content** (not its extension), so a mislabelled
  file still renders.
- **Remote** image URLs, **missing** files, and images already written as `data:` URIs are
  left as references.
- Pass `--No-Embed` to turn inlining off and keep every image as an external reference.
  Outside `--Single-File`, inlining is off unless you ask for it with `--Embed`.

See [`--Embed` in the command-line usage](cli-usage.en.md) for the full behaviour.

One file per language
----------------------------------------------------------------

A multilingual folder — pages named `README.en.md`, `README.fr.md`, … — can be split into
one book per language with `--ByLang`:

```bash
mdbook docs/ --Single-File book.{lang}.html --ByLang
```

produces `book.en.html`, `book.fr.html`, … each containing only that language's pages.

- A page's language comes from a trailing segment in its file name (`guide.en.md` → `en`,
  `guide.en-GB.md` → `en-GB`).
- **Regional variants are unified** by their two-letter code: `en`, `en-US` and `en-GB`
  share one `en` book. Each section keeps its exact `lang` attribute, while the book's
  document language is the neutral code.
- The output path's `{lang}` placeholder is substituted per language. With no placeholder,
  `.{lang}` is inserted before the extension (`book.html` → `book.en.html`).
- **Language-neutral pages** (no `.xx` suffix) are included in every language's book, so
  shared front-matter and appendices appear in each.
- `--ByLang` requires `--Single-File`; if no page carries a detected language, nothing is
  written and an error is reported.

Included partials are not pages
----------------------------------------------------------------

A file pulled into another with `{{include: …}}` is a **partial**: it is spliced into its
host page and is not rendered as a page of its own, so it gets no table-of-contents entry and
its content is never duplicated. Name it explicitly on the command line to keep it as a page
as well. See [Including files](includes.en.md).

What you see when it runs
----------------------------------------------------------------

On completion, `--Single-File` prints a one-line summary — `Combined 12 pages into
/abs/path/book.html` (one line per language under `--ByLang`). The per-page trace is quiet by
default; add `--Verbose` / `-v` to watch each page as it is processed.

Known limitations
----------------------------------------------------------------

- **No cross-language fallback.** Under `--ByLang`, a document that exists in only one
  language is absent from the other languages' books; there is no automatic fallback to
  another language yet. Tracked in
  [issue #24](https://github.com/Airudit/mdbook/issues/24).
- **Most of the `.mdbook` manifest affects the combined output only.** The book title,
  introduction, `priority`, per-page `order` and the table-of-contents keys apply to
  `--Single-File`; in the in-place and `--Export` modes they are ignored (use `--Title` and
  per-page front-matter for titles there). The one exception is **`exclude`, which acts in every
  mode.**
