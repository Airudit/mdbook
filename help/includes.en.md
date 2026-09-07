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

Where the directive appears, `mdbook` splices in the contents of the referenced
file and renders the combined document as one page. Rules:

- Put the directive on its **own line**. It pulls in block-level content
  (headings, lists, paragraphs), so it is not meant to sit inside a sentence.
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

Relative links inside an included file
----------------------------------------------------------------

Relative links **inside** an included file are automatically rebased to the
including page. A link that was correct in the part stays correct once the part is
pulled in from another folder.

For example, with `page.md` doing `{{include: help/intro.md}}`, and `intro.md`
linking to `page1.md`, the rendered page links to `help/page1.md.html` — the folder
the part came from — so the link resolves. External URLs and absolute paths are
left untouched.

Nested includes
----------------------------------------------------------------

Includes may nest: an included file can itself contain `{{include: …}}`
directives, each resolved relative to the file that holds it, so a part that pulls
in another part works to any depth. An include cycle — a file that, directly or
indirectly, includes itself — is detected and stopped with a comment marker rather
than looping.
