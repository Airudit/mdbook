Airudit.MdBook — user guide
================================

`mdbook` turns a folder of Markdown / CommonMark files into a set of standalone,
self-contained HTML pages — a small "book" you can open in any browser, print,
or ship alongside an application.

This guide is itself built with `mdbook`: the Markdown sources live in `help/`,
and rendering that folder produces the HTML pages you may be reading now.

What it does
----------------------------------------------------------------

- Renders each `.md` file to a matching `.md.html` file, next to the source.
- Wraps the result in a self-contained HTML template — inline CSS, no external
  CDN, both screen- and print-friendly.
- Rewrites local `.md` links to `.md.html`, so a rendered book stays navigable.
- Copies non-Markdown files you link to (images, downloads) into the export.
- Optionally combines every page into a single HTML file with a table of
  contents.

Contents
----------------------------------------------------------------

- [Getting started](getting-started.en.md) — install the tool and render your
  first file.
- [Command-line usage](cli-usage.en.md) — the command and every option.
- [Templates and placeholders](templates.en.md) — the built-in templates and the
  `{{{…}}}` variables they fill.
- [Including files](includes.en.md) — composing a page from several Markdown
  parts with `{{include: …}}`.
- [Use as a C# library](library.en.md) — drive the renderer from your own code.

About file names and language
----------------------------------------------------------------

A trailing two-letter segment in a file name is read as a language code: a file
named `guide.en.md` has the title "guide" and the language `en`, and renders to
`guide.en.md.html`. This is why every page in this guide ends in `.en.md`. See
[Templates and placeholders](templates.en.md) for how the language reaches the
output.
