Diagrams
================================

mdbook can turn fenced code blocks tagged with a diagram language — `mermaid`, `plantuml`,
and more — into diagrams in the output. They are rendered **once, at build time**, and
inlined as SVG, so the finished page needs no JavaScript, no network, and no plugins: it
still prints and reads offline like every other mdbook page. The build carries the weight
so the document doesn't.

Rendering is off by default, because it needs a renderer you choose. When a page contains a
diagram and no renderer is configured, mdbook leaves the block as plain text and prints a
one-line hint telling you how to turn one on. If a renderer *is* configured but a diagram
still fails, run with `--Verbose` to see why (a server error, Docker not installed, …).

Two ways to render
----------------------------------------------------------------

### Local Docker (offline, private)

`--Diagrams docker` renders each diagram with a one-shot `docker run`, using small official
images (mermaid via `minlag/mermaid-cli`, plantuml via `plantuml/plantuml`). Nothing leaves
your machine, so this is the right choice for confidential content. You need Docker
installed; the first render of each kind pulls its image.

```
mdbook book/ --Export out --Diagrams docker
```

### Kroki (a diagram server)

`--Diagrams kroki --KrokiUrl <url>` sends each diagram's source to a Kroki server, which
returns the SVG. One server renders mermaid and the whole Kroki set (plantuml, graphviz,
d2, ditaa, …). Point it at your own server (below) or, if you accept the confidentiality
trade-off, at the public one:

```
mdbook book/ --Export out --Diagrams kroki --KrokiUrl https://kroki.io/
```

`--KrokiUrl` is a full base URL and needs the trailing slash: `https://kroki.io/`.

Run your own Kroki, offline
----------------------------------------------------------------

Mermaid is rendered by a Kroki companion container, so a mermaid-capable Kroki is two
containers. A minimal `docker-compose.yml`:

```yaml
services:
  kroki:
    image: yuzutech/kroki
    depends_on: [mermaid]
    environment: [KROKI_MERMAID_HOST=mermaid]
    ports: ["8000:8000"]
    tmpfs: ["/tmp:exec"]
  mermaid:
    image: yuzutech/kroki-mermaid
    expose: ["8002"]
```

`docker compose up -d`, then use `--KrokiUrl http://localhost:8000/`. Everything stays on
your own network.

Confidentiality
----------------------------------------------------------------

A **public** Kroki server (such as `https://kroki.io/`) receives the full source of every
diagram you render. Do not use one for confidential diagrams. The local Docker renderer and
a self-hosted Kroki both keep your source on your own machine — prefer them when in doubt.
mdbook never contacts a server unless you set its URL.

Dark templates
----------------------------------------------------------------

A rendered diagram carries its own colours, so it must match the page. mdbook reads the
template's colour scheme from the standard `color-scheme` meta tag and picks a matching
diagram theme. The built-in templates already declare theirs; a custom template opts in with
one line in its `<head>`:

```
<meta name="color-scheme" content="dark">
```

Use `dark` for a dark template, `light` (or nothing) for a light one. This is the standard
HTML tag, so it also lets the browser theme its own form controls and scrollbars to match.

Environment variables
----------------------------------------------------------------

`--Diagrams` and `--KrokiUrl` default from `MDBOOK_DIAGRAMS` and `MDBOOK_KROKI_URL` when the
flags are absent. `MDBOOK_DIAGRAM_TIMEOUT_MS` caps each render (default 120000).
