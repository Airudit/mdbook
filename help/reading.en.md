Reading a book
================================

A book is one self-contained HTML file, so you can read it however suits you. None of
these ways need the network, and none need mdbook itself — you only need the file.

In a browser (the simplest)
----------------------------------------------------------------

Double-click the file, or open it from your browser's *File* menu. It opens straight from
disk — no server. Use the browser's find (Ctrl/Cmd-F) to search the whole book, and the
table of contents to jump between pages.

On paper
----------------------------------------------------------------

Print from the browser (Ctrl/Cmd-P). The template is print-styled: a Markdown `---`
becomes a page break, headings scale sensibly, and the text stays legible with "Background
graphics" turned off. Want a PDF after all? *Save as PDF* from the same print dialog —
your choice, not the format's.

In reader mode or on an e-reader
----------------------------------------------------------------

Most browsers have a reader mode that strips the page down to just the text. For an
e-reader, convert the file to EPUB with a local tool — for example
`pandoc book.html -o book.epub`, or Calibre — and side-load it.

In a terminal
----------------------------------------------------------------

No graphical browser needed — it's just HTML:

- **Terminal browsers** render it to formatted text: `w3m book.html`, `lynx book.html`,
  `elinks book.html` (or `links`).
- **Convert to text and page it:** `pandoc book.html -t plain | less`, or
  `html2text book.html | less`.
- **Or read the source directly** — `less book.html`, or `grep` for a word to jump
  straight to it.

These are all local tools; install them from your package manager (`apt install w3m`,
`brew install lynx`, …). Nothing here touches the network.

In a word processor — to review and comment
----------------------------------------------------------------

Word, LibreOffice Writer and OnlyOffice all open HTML directly. Hand the file to a
reviewer who doesn't read Markdown: they can read it laid out as intended, add comments
and tracked changes, and save — no Markdown, and no special tooling on their side.

As a file you can edit
----------------------------------------------------------------

It's your file. Open it in any text editor to fix a typo or tweak wording, or in a browser
to read — there's no licence, account, or export step standing in the way.
