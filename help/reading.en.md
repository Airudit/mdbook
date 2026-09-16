Reading a book
================================

What you read depends on how the book was produced. `mdbook` has three output
shapes, each with its own reader and purpose. This page takes them in turn,
starting with the single file — the one you're most likely to pass around.

A single self-contained file — to hand to a reader
----------------------------------------------------------------

Produced with `--Single-File` (see [Single-file books](single-file.en.md) for how
to build it). The whole book is one HTML file, so the reader needs nothing else:
no source folder, no server, no `mdbook`, no network — just the file. This is the
shape to **ship portable, offline, printable documentation to an end-user**: a
customer, a reviewer, a colleague who won't clone the repo. Here are the ways they
can read it.

### In a browser (the simplest)

Double-click the file, or open it from your browser's *File* menu. It opens straight from
disk — no server. Use the browser's find (Ctrl/Cmd-F) to search the whole book, and the
table of contents to jump between pages.

### On paper

Print from the browser (Ctrl/Cmd-P). The template is print-styled: a Markdown `---`
becomes a page break, headings scale sensibly, and the text stays legible with "Background
graphics" turned off. Want a PDF after all? *Save as PDF* from the same print dialog —
your choice, not the format's.

A few dialog settings trade paper and ink for reading comfort, or the other way round:

- **Background graphics — off** saves ink, and the template is designed to read fine without it.
- **Scale** sizes the text: raise it for larger, more comfortable type, or lower it to fit
  more on each page.
- **Pages per sheet — 2** halves the paper; with the short paragraphs typical of a book like
  this it often reads *better*, not worse, at a comfortable scale.
- **Double-sided (duplex)** halves the paper again, and reads like a real book.

### In reader mode or on an e-reader

Most browsers have a reader mode that strips the page down to just the text.

On an e-reader, try the file **as-is** first — often no conversion is needed. Many
devices (Kobo, PocketBook, and most Android-based readers) list HTML among their
formats and open it directly: copy the file over USB and read. Because the book is one
self-contained file with its styles inline, there is nothing else to transfer.

Two things push you towards converting anyway. First, some devices — Kindle most
notably — won't open a plain HTML file at all; there you send it through *Send to
Kindle* (which converts for you) or convert it yourself. Second, even where HTML
opens, you give up the reader's native pagination and font-size controls. For proper
reflow, chapter navigation and the device's own font sizing, convert to EPUB with a
local tool — `pandoc book.html -o book.epub`, or Calibre — and side-load that instead.

### In a terminal

No graphical browser needed — it's just HTML:

- **Terminal browsers** render it to formatted text: `w3m book.html`, `lynx book.html`,
  `elinks book.html` (or `links`).
- **Convert to text and page it:** `pandoc book.html -t plain | less`, or
  `html2text book.html | less`.
- **Or read the source directly** — `less book.html`, or `grep` for a word to jump
  straight to it.

These are all local tools; install them from your package manager (`apt install w3m`,
`brew install lynx`, …). Nothing here touches the network.

### In a word processor — to review and comment

Word, LibreOffice Writer and OnlyOffice all open HTML directly. Hand the file to a
reviewer who doesn't read Markdown: they can read it laid out as intended, add comments
and tracked changes, and save — no Markdown, and no special tooling on their side.

### As a file you can edit

It's your file. Open it in any text editor to fix a typo or tweak wording, or in a browser
to read — there's no licence, account, or export step standing in the way.

Pages rendered in place — to read while you write
----------------------------------------------------------------

The default (`--Side`): every `X.md` gets an `X.md.html` next to it, with links
between pages rewritten to match. The reader here is **you, the author**, on your
own machine — reading each page properly laid out while you edit its source, and
refreshing after a re-render. The "just read my local Markdown correctly" mode.

An exported site — to publish on the web
----------------------------------------------------------------

Produced with `--Export <dir>`: a mirrored tree of pages plus their linked
assets, ready to serve. The reader is **anyone visiting the site** over the
network, addressed by a URL — many readers, not one recipient. Drop the exported
directory into a static-file web server's root and it's browsable like any site.
