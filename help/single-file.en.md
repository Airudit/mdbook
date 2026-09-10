Single-file books
================================

Combining a whole folder into one self-contained HTML document is `mdbook`'s headline
capability. Instead of a directory of pages and assets, you get a **single file** you can
email, print, drop onto a share, or ship next to an application — it carries its own table
of contents, navigates itself, and needs nothing alongside it (with one caveat on images,
below).

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

Page order
----------------------------------------------------------------

Pages are listed in the table of contents and concatenated in this order:

- Inputs are taken in the order given on the command line, files and folders alike.
  `mdbook README.md guide/ appendix.md` places `README.md` first, then the contents of
  `guide/`, then `appendix.md`.
- Inside a folder, `README` comes first, then `Index`, then the remaining files by name
  (case-insensitive); sub-folders follow, also by name.
- A file named both explicitly and inside a listed folder appears once, at its first
  position. So `mdbook guide/intro.md guide/` puts `intro.md` first and the rest of `guide/`
  after it, with no duplicate.

Table of contents
----------------------------------------------------------------

The combined file opens with a table of contents that mirrors the source folder structure:
pages are nested under their folders, with the common leading folder stripped. Each page is
labelled by its **title** — its first level-1 heading (`# …`), falling back to the file name
without `.md` when a page has no heading. Folders appear as plain labels.

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

- **Images are not embedded yet.** A local image referenced by a page still points at its
  original relative path, so it does not travel inside the single file — move or ship the
  file and its images break. Embedding them (as `data:` URIs) is tracked in
  [issue #21](https://github.com/Airudit/mdbook/issues/21) (depends on #13). External image
  URLs are unaffected.
- **No cross-language fallback.** Under `--ByLang`, a document that exists in only one
  language is absent from the other languages' books; there is no automatic fallback to
  another language yet. Tracked in
  [issue #24](https://github.com/Airudit/mdbook/issues/24).
