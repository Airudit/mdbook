Why mdbook exists
================================

I got tired of shipping documentation as a PDF.

A PDF arrives as a wall. You can't grep it, you can't diff it in a review, you can't skim
it down a terminal. To open one you reach for a reader that has quietly grown teeth — AI
sidebars, editing tools, "sign in", ads. And the alternative, the man page, is the far
end: fast and honest, but locked to the shell and forbidding to anyone who didn't grow up
there.

I wanted the middle that used to exist — a document you're *handed*, and then use your own
way.

So mdbook turns your Markdown into a single HTML file. One file: styling inline, no CDN,
no server, nothing trailing behind it. What you do with it from there is yours — see
[Reading a book](reading.en.md) for how:

- **Read it on screen**, where it reflows to your window and searches in the browser.
- **Print it** cleanly — plenty of readers still want paper, and they count.
- **Open it in a reader** — reader mode, an e-reader, anything that renders HTML.
- **Read it in a shell** if that's your world: it's just text, so grep it, page it, view
  the source.
- **Hand it to a reviewer** who doesn't read Markdown — Word, LibreOffice and OnlyOffice
  open HTML directly, comments and tracked changes included.
- **Edit it.** It's your file now — open it, change it, save it. No licence, no account,
  no export dance.

That is the freedom I was after: not chained to a questionable PDF reader, not standing in
awe before a man page — just handed something open that bends to how *you* want to use it.

And it lasts. HTML is one of the most open and stable standards we have. The file mdbook
writes today would render in a browser from twenty years ago — Netscape and Internet
Explorer 3 would open it fine — and I'd bet on twenty years from now just the same. Try
that with this season's PDF feature set.

The idea owes a debt to [SingleFile](https://github.com/gildas-lormeau/SingleFile), the
browser extension that saves a whole page as one self-contained HTML file. I wanted that,
but generated from your sources instead of scraped from a rendered page.

> **Where this stands today.** mdbook already renders self-contained HTML, builds
> single-file books, prints, and handles `--ByLang` and includes. Two honest caveats: the
> "everything lives inside it" promise holds for text and styling but *not yet* for images
> — those are still referenced by path, and inlining them is on the roadmap
> ([#21](https://github.com/Airudit/mdbook/issues/21)) — and the in-file search and richer
> navigation described below are direction, not shipped.

What mdbook will — and won't — become
----------------------------------------------------------------

I may well add richer navigation, and one day in-file search. But only the kind that lives
*inside* the file: a little local JavaScript, no network, no backend, nothing to install
on the reader's side. The line I won't cross is the one that turns a document back into a
website.

If a hosted documentation site is what you want — server-backed search, a persistent
sidebar, versioned docs — [mdBook](https://rust-lang.github.io/mdBook/) and
[MkDocs](https://www.mkdocs.org/) are excellent, and you should reach for them. mdbook is
the other promise: everything the reader needs is already in the single file you were
handed, it bends to however you want to read it, and it keeps working when the network —
and this season's fashionable file format — doesn't.
