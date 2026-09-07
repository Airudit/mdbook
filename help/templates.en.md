Templates and placeholders
================================

Every page `mdbook` produces is your rendered Markdown poured into an HTML
*template*. The template is a single, self-contained HTML file with an inline
`<style>` block — no external CSS or CDN — so the output works offline and in
isolated networks.

Choosing a template
----------------------------------------------------------------

Two templates ship inside the tool and are selected by a `builtin:` name:

```bash
mdbook help/ --Template builtin:default.light.html
mdbook help/ --Template builtin:default.dark.html
```

With no `--Template`, the light built-in is used. To use your own, pass a file
path:

```bash
mdbook help/ --Template ./my-template.html
```

The easiest way to start a custom template is to copy a built-in one and edit it.
The built-ins live in the sources at `src/Airudit.MdBook.Core/res/`.

The `{{{…}}}` placeholders
----------------------------------------------------------------

Inside a template, `mdbook` replaces triple-brace placeholders with page data
before writing the file. Despite the look, this is **not** a full Mustache or
Handlebars engine: there are no loops, conditionals or partials — just a fixed
set of known variables, replaced by simple text substitution. The triple brace
is borrowed from Mustache's "unescaped" `{{{ }}}` form, which fits because
`{{{Contents}}}` is raw HTML that must not be escaped.

These placeholders belong in the **template**, not in your Markdown. (For
composing content from several Markdown files, see
[Including files](includes.en.md), which uses a different `{{ }}` syntax.)

Known variables:

- `{{{PageTitle}}}` — the page title, taken from the file name with any language
  suffix removed (`guide.en.md` → "guide"). HTML-escaped.
- `{{{Contents}}}` — the rendered Markdown, as raw HTML wrapped in an
  `<article>`. Not escaped.
- `{{{Lang}}}` — the page language code, suitable for `<html lang="…">`. Taken
  from the file-name suffix (`guide.en.md` → `en`); defaults to `en` when there
  is no suffix.
- `{{{Info}}}` — an automatically generated notice stating the file was produced
  by the tool and that manual edits will be lost on the next run.
- `{{{Copyright}}}` — the string passed with `--Copyright`. HTML-escaped.

Any unknown `{{{…}}}` placeholder is replaced with an empty string.

A minimal template
----------------------------------------------------------------

```html
<!doctype html>
<html lang="{{{Lang}}}">
<head>
  <meta charset="utf-8">
  <title>{{{PageTitle}}}</title>
  <style>/* your inline styles here */</style>
</head>
<body>
  {{{Contents}}}
  <footer>{{{Copyright}}} <small>{{{Info}}}</small></footer>
</body>
</html>
```

Printing
----------------------------------------------------------------

The built-in templates are designed to print cleanly. Two conventions matter if
you write your own:

- A Markdown thematic break (`---`) is treated as a **page break** in print. Use
  an invisible, zero-height `hr` carrying `break-after: page`; a `display: none`
  rule would remove the element entirely and could not force a break.
- Drive print sizing from a single `html { font-size }` knob inside
  `@media print`, and size headings in `rem` so they scale from it. Do not depend
  on background fills — the page must stay legible with the browser's "Background
  graphics" option turned off.
