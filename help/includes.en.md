Including files
================================

A Markdown file can pull in the contents of another Markdown file before
rendering, using an `include` directive. This lets you assemble a page from
reusable parts — a shared header, a common notice, a section maintained
elsewhere.

Syntax
----------------------------------------------------------------

```
{{include: path/to/part.md}}
```

Where the directive appears, `mdbook` splices in the raw text of the referenced
file, then renders the combined document as one. Rules:

- The path is resolved **relative to the file that contains the directive**.
- A leading slash is allowed and simply ignored: `{{include: /part.md}}` and
  `{{include: part.md}}` mean the same thing.
- Only `.md` files may be included.

Example
----------------------------------------------------------------

`index.md`:

```markdown
# Handbook

{{include: sections/intro.md}}

{{include: sections/setup.md}}
```

Rendering `index.md` produces a single page containing the introduction and setup
sections inline.

When something goes wrong
----------------------------------------------------------------

Include problems do not stop the build; they leave an HTML comment in the output
so you can spot them when viewing source:

- The file does not exist → `<!-- {{include: …}}: NO SUCH FILE -->`
- The target is not a `.md` file → `<!-- {{include: …}}: INVALID FILE EXTENSION -->`
- The file cannot be read → the error message is emitted as a comment.

Known limitation — links inside an included file
----------------------------------------------------------------

Relative links **inside** an included file are not rebased to the including file's
location. A link that was correct in the part becomes wrong once the part is
inlined somewhere else.

For example, with `page.md` doing `{{include: help/intro.md}}`, and `intro.md`
containing a link to `page1.md` (meaning `help/page1.md`), the rendered page links
to `page1.md` relative to `page.md` — that is, the wrong folder — and the link
breaks.

Until this is fixed, prefer links that do not depend on the including file's
location: link with paths relative to the *final* page, or use absolute URLs. This
is tracked as issue #7 in the project tracker.
