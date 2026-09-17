
namespace Airudit.MdBook.Core
{
    using Airudit.MdBook.Core.Internals;
    using Markdig;
    using Markdig.Extensions.Yaml;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Converts some markdown files to HTML.
    /// </summary>
    public sealed class SimpleMarkdownToHtmlTask : ITask
    {
        private static readonly JsonHelper json = new JsonHelper(string.Empty);
        private static readonly Regex replacer = new Regex(@"\{\{\{([^}]+)\}\}\}", RegexOptions.Compiled);
        private static readonly Regex linksRegex = new Regex(@"<a href=""([^""]+)"">", RegexOptions.Compiled);
        private static readonly Regex includeLineRegex = new Regex(@"(?m)^[ \t]*\{\{include: *([/a-zA-Z0-9 ()+='"",.?_-]+)\}\}[ \t]*$", RegexOptions.Compiled);

        // A line that opens an ordered-list item ("1. " or "1) ") carrying text. When such a
        // line is immediately underlined by a setext bar, CommonMark reads it as a list item +
        // thematic break, not a heading (issue #15); escaping the marker turns it into a heading.
        private static readonly Regex numberedItemLineRegex = new Regex(@"^([ \t]{0,3})(\d{1,9})([.)])([ \t]+\S.*)$", RegexOptions.Compiled);
        private static readonly Regex setextUnderlineRegex = new Regex(@"^[ \t]{0,3}(-+|=+)[ \t]*\r?$", RegexOptions.Compiled);
        private static readonly Regex codeFenceLineRegex = new Regex(@"^[ \t]{0,3}(`{3,}|~{3,})", RegexOptions.Compiled);

        // Opt-out for the numbered-setext-heading fix; on by default.
        private const string NumberedSetextFixEnvVar = "MDBOOK_NUMBERED_SETEXT_FIX";

        private static readonly char[] directorySeparators = new char[] { '/', '\\', };
        private string? profile;
        private SimpleMarkdownToHtmlLayer? layer;

        // Visit and Verify are synchronous; only Run does async work (the diagram pre-render pass).
        public Task VisitAsync(PackageContext context, CancellationToken cancellationToken = default)
        {
            this.Visit(context);
            return Task.CompletedTask;
        }

        public Task VerifyAsync(PackageContext context, CancellationToken cancellationToken = default)
        {
            this.Verify(context);
            return Task.CompletedTask;
        }

        private void Visit(PackageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (this.layer != null)
            {
                throw new InvalidOperationException("Cannot run this task twice. ");
            }

            this.layer = context.RequireSingleLayer<SimpleMarkdownToHtmlLayer>();
            if (this.layer == null)
            {
                this.layer = new SimpleMarkdownToHtmlLayer();
                context.AddLayer(layer);
            }

            // prepare template first: the diagram renderer needs the template's declared colour
            // scheme (below) before the pipeline is built, to pick a matching diagram theme (issue #5).
            const string builtinPrefix = "builtin:";
            var myAssembly = typeof(SimpleMarkdownToHtmlTask).Assembly;
            if (this.layer.TemplateFilePath != null && this.layer.TemplateFilePath.StartsWith(builtinPrefix, StringComparison.InvariantCulture))
            {
                var path = "Airudit.MdBook.Core.res." + this.layer.TemplateFilePath.Substring(builtinPrefix.Length);
                this.layer.Template = this.ReadTemplateFromAssembly(myAssembly, path);
            }
            else if (this.layer.TemplateFilePath != null)
            {
                using (var templateStream = new FileStream(this.layer.TemplateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var templateReader = new StreamReader(templateStream!, Encoding.UTF8))
                {
                    this.layer.Template = templateReader.ReadToEnd();
                }
            }
            else
            {
                var path = "Airudit.MdBook.Core.res.default.light.html";
                this.layer.Template = this.ReadTemplateFromAssembly(myAssembly, path);
            }

            // The template may declare its colour scheme (light/dark); it drives the diagram theme.
            this.layer.ColorScheme = TemplateColorScheme.Resolve(this.layer.Template);

            // prepare pipeline
            var pipelineBuilder = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAutoIdentifiers()
                .UseAutoLinks()
                .UsePipeTables()
                .UseEmphasisExtras()
                .UseTaskLists();

            // Build-time syntax highlighting: a rendering-only extension, added only when enabled
            // so it stays a discrete stage (issue #25).
            if (this.layer.Highlight)
            {
                // Cap ColorCode's regexes before it ever compiles them, so a malformed code block
                // aborts and falls back to plain instead of backtracking for minutes (issue #25).
                SyntaxHighlightingExtension.InstallDefaultRegexTimeout();
                pipelineBuilder.Extensions.Add(new SyntaxHighlightingExtension());
            }

            // Build-time diagram rendering (issue #5). Added after the highlighter so it decorates
            // (and runs before) it: diagram fences become inline SVG, every other block still
            // highlights. Markdig's renderer is synchronous, so the diagrams are pre-rendered
            // asynchronously in Run; this renderer only looks the results up in the shared cache.
            pipelineBuilder.Extensions.Add(new DiagramRenderingExtension(this.layer.DiagramSvgs));

            this.layer.Pipeline = pipelineBuilder.Build();
        }

        private void Verify(PackageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        public async Task RunAsync(PackageContext context, CancellationToken cancellationToken = default)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            this.RemoveIncludedPartials();

            // The diagram provider and its --verbose logger live for the whole run; the pre-render
            // pass (below, per page) fills the shared SVG cache the Markdig renderer reads.
            var interactor = context.GetSingleLayer<CommandLineLayer>();
            Action<string>? diagramLog = this.layer.Verbose ? (message => interactor?.Out?.WriteLine(message)) : null;
            var diagramProvider = DiagramProviderFactory.Create(this.layer.Diagrams, this.layer.KrokiUrl, diagramLog);

            // --Title names one page's <title>; on a multi-page side/export run it stamps the same
            // title on every page. Unusual, so warn once. The --Single-File book <title> is a
            // separate, intended use of --Title and never warns.
            if (this.layer.Title != null && (this.layer.SideBySide || this.layer.Exports.Count > 0))
            {
                var pageCount = this.layer.Items.Count(candidate => candidate.IsMarkdown);
                if (pageCount > 1)
                {
                    interactor?.ErrorOut?.WriteLine("--Title \"" + this.layer.Title + "\" is applied to all " + pageCount + " pages; it is meant mainly for single-page or --Single-File output. ");
                }
            }

            foreach (var item in this.layer.Items.ToArray()) // we need to change the collection while enumerating it
            {
                await this.ProcessFileMarkdownAsync(context, item, diagramProvider, diagramLog, cancellationToken).ConfigureAwait(false);
            }

            // Book-metadata sidecars feed only the combined output, so process them for their
            // front-matter and intro body only when a single file is being built.
            if (this.layer.SingleFile != null)
            {
                foreach (var sidecar in this.layer.Sidecars)
                {
                    await this.ProcessFileMarkdownAsync(context, sidecar, diagramProvider, diagramLog, cancellationToken).ConfigureAwait(false);
                }
            }

            this.EmitDiagramWarnings(context);
        }

        // Reports diagram fences left unrendered during the run, once (issue #5): a setup hint when no
        // provider is configured, or a failure note when the configured renderer could not produce them.
        private void EmitDiagramWarnings(PackageContext context)
        {
            if (!this.layer.DiagramWarnings.Any)
            {
                return;
            }

            var interactor = context.GetSingleLayer<CommandLineLayer>();
            var errorOut = interactor?.ErrorOut;
            if (errorOut == null)
            {
                return;
            }

            var tags = string.Join(", ", this.layer.DiagramWarnings.SkippedTags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
            if (this.layer.Diagrams == DiagramProviderKind.None)
            {
                errorOut.WriteLine("Diagrams not rendered (" + tags + "). Configure a renderer:");
                errorOut.WriteLine("  - local Docker (offline, private): --Diagrams docker");
                errorOut.WriteLine("  - public Kroki (uploads your diagram source): --Diagrams kroki --KrokiUrl https://kroki.io/");
                errorOut.WriteLine("  - self-hosted Kroki and details: see the diagrams help page (help/diagrams).");
            }
            else
            {
                var provider = this.layer.Diagrams == DiagramProviderKind.Docker ? "docker" : "kroki";
                errorOut.WriteLine("Diagrams not rendered by the " + provider + " renderer (" + tags + "): the tool is not installed, the server is unreachable, or rendering failed. Left as plain blocks.");
            }
        }

        // A file pulled into another page with {{include}} is a partial, not a page: drop it
        // from the page list so it is not rendered (and duplicated) as a standalone page — in
        // any output mode. A partial named explicitly on the command line is kept (issue #23).
        private void RemoveIncludedPartials()
        {
            var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in this.layer.Items)
            {
                if (item.IsMarkdown)
                {
                    this.CollectIncludedFiles(item.SourceFile, included, visiting);
                }
            }

            if (included.Count == 0)
            {
                return;
            }

            this.layer.Items.RemoveAll(item =>
                item.IsMarkdown
                && !item.ExplicitlyListed
                && included.Contains(item.SourceFile.FullName));
        }

        // Records every file reachable through {{include}} directives from <paramref name="file"/>,
        // resolving paths exactly as AppendInclude does; recursive and cycle-guarded.
        private void CollectIncludedFiles(FileInfo file, HashSet<string> included, HashSet<string> visiting)
        {
            if (!file.Exists || !visiting.Add(file.FullName))
            {
                return;
            }

            var raw = File.ReadAllText(file.FullName, Encoding.UTF8);
            foreach (Match match in includeLineRegex.Matches(raw))
            {
                var path = match.Groups[1].Value;
                if (path.Length >= 1 && path[0] == '/')
                {
                    path = path.Substring(1);
                }

                if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var includedFile = new FileInfo(Path.Combine(file.DirectoryName!, path));

                // A file that includes itself (directly or through a cycle back to itself) is
                // still a page, not a partial — only demote a file included by a *different* one.
                if (!string.Equals(includedFile.FullName, file.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    included.Add(includedFile.FullName);
                }

                this.CollectIncludedFiles(includedFile, included, visiting);
            }

            visiting.Remove(file.FullName);
        }

        private async Task ProcessFileMarkdownAsync(PackageContext context, SimpleMarkdownToHtmlLayerItem item, IDiagramProvider diagramProvider, Action<string>? diagramLog, CancellationToken cancellationToken)
        {
            var interactor = context.GetSingleLayer<CommandLineLayer>();
            if (this.layer.Verbose)
            {
                interactor?.Out?.WriteLine("Processing markdown file \"" + item.SourceFile + "\". ");
            }

            // prepare
            var title = Path.GetFileNameWithoutExtension(item.SourceFile.Name);
            var fileName = Path.GetFileName(item.SourceFile.Name);

            // detect lang in file name
            CultureInfo lang = null;
            var dot = new char[] { '.', };
            var titleParts = title.Split(dot);
            if (titleParts.Length > 1 && titleParts[^1].Length >= 2)
            {
                try
                {
                    lang = new CultureInfo(titleParts[^1]);
                    item.Lang = lang;
                    var newTitleParts = new string[titleParts.Length - 1];
                    Array.Copy(titleParts, newTitleParts, newTitleParts.Length);
                    title = string.Join(dot[0].ToString(), newTitleParts);
                }
                catch (CultureNotFoundException)
                {
                }
            }

            // Build the document tree: parse the source file and splice in the contents of
            // any {{include: ...}} directive (recursively), rebasing each included file's
            // relative links to the folder it was pulled from. Then rewrite every local
            // .md link to its generated .html and register linked assets for export.
            var dom = new MarkdownDocument();
            this.AppendFile(item, dom, item.SourceFile, string.Empty, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new IncludeCounter());
            this.RewriteLocalLinks(item, dom);

            // Inline local images as data: URIs so the page stays self-contained once moved
            // away from its source folder (issues #13, #21). Resolves against the page's own
            // directory — included files' image paths were already rebased to it.
            if (this.layer.Embed)
            {
                SelfContainedImageEmbedder.EmbedImages(
                    dom,
                    item.SourceFile.DirectoryName!,
                    this.layer.Verbose ? (message => interactor?.Out?.WriteLine(message)) : null);
            }

            // Remember the page's first level-1 heading; the single-file table of contents
            // uses it as the page label instead of the bare file name.
            item.Title = ExtractFirstHeadingTitle(dom);

            // A top-of-file YAML front-matter `title:` names the page explicitly, overriding both
            // the file-name-derived page <title> and the first-heading TOC label. Kept on its own
            // field (not folded into Title) so the book-title fallback chain can still tell a
            // front-matter title apart from a first heading.
            item.FrontMatterTitle = ExtractFrontMatterTitle(dom);

            // A sidecar's front-matter `lang:` sets the book language authoritatively; otherwise its
            // language stays the one inferred from the file name's ".xx" segment above.
            if (item.IsSidecar)
            {
                var frontMatterLang = FrontMatterScalar(dom, "lang");
                if (frontMatterLang != null)
                {
                    try
                    {
                        item.Lang = new CultureInfo(frontMatterLang);
                    }
                    catch (CultureNotFoundException)
                    {
                    }
                }
            }

            // Pre-render every diagram fence to SVG (async, in parallel) before Markdig's synchronous
            // renderer runs; the renderer then just inlines the cached SVGs (issue #5).
            await DiagramPrerenderer.RenderAsync(dom, diagramProvider, this.layer.ColorScheme, this.layer.DiagramSvgs, this.layer.DiagramWarnings, diagramLog, cancellationToken).ConfigureAwait(false);

            // generate HTML
            string htmlContents = Markdown.ToHtml(dom, this.layer.Pipeline);

            // add a class="external" to external links <a href="http://...">
            htmlContents = linksRegex.Replace(htmlContents, new MatchEvaluator(match =>
            {
                var contents = match.Value;
                var url = match.Groups[1].Value;
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                {
                    contents = "<a class=\"external\" href=\"" + url + "\">";
                }

                return contents;
            }));
            item.HtmlContents = htmlContents;

            // substitute HTML template variables
            // don't forget to HTML-escape strings!
            // known variables are:
            // - {{{PageTitle}}}  the title for the page
            // - {{{Contents}}}   the markdown-converted HTML part
            // - {{{Lang}}}       the page's lang
            // - {{{Info}}}       a information string
            // - {{{Copyright}}}  the --Copyright notice
            var page = replacer.Replace(this.layer.Template, match =>
            {
                var key = match.Groups[1].Value;

                if ("PageTitle".Equals(key, StringComparison.Ordinal))
                {
                    return WebUtility.HtmlEncode(this.layer.Title ?? item.FrontMatterTitle ?? title);
                }
                else if ("Contents".Equals(key, StringComparison.Ordinal))
                {
                    return "<article>\n" + htmlContents + "</article>\n"; // not escaped
                }
                else if ("Lang".Equals(key, StringComparison.Ordinal))
                {
                    return lang != null ? lang.Name : "en";
                }
                else if ("Info".Equals(key, StringComparison.Ordinal))
                {
                    return WebUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture, "This document was generated automatically from file \"{0}\" on {1:o} using the Airudit.MdBook tool. Manual modifications will be lost next time this file is generated again. ", fileName, DateTime.UtcNow));
                }
                else if ("Copyright".Equals(key, StringComparison.Ordinal))
                {
                    return WebUtility.HtmlEncode(this.layer.Copyright ?? string.Empty);
                }
                else if (HighlightStylesheet.IsPlaceholder(key))
                {
                    return HighlightStylesheet.Render(key, this.layer.Highlight);
                }
                else
                {
                    return string.Empty;
                }
            });

            // keep the full page in memory so it can be exported even when not written in place
            item.RenderedPage = page;

            // write the in-place side-by-side HTML file, unless suppressed (issue #9). A sidecar is
            // never written in place — it is book metadata, not a page.
            if (this.layer.SideBySide && !item.IsSidecar)
            {
                using (var targetStream = new FileStream(item.TargetFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(targetStream, Encoding.UTF8))
                    {
                        writer.Write(page);
                        writer.Flush();
                    }
                }

                // In-place files are what --side (or the default no-destination run) explicitly
                // asks for, so confirm each write even when not running verbose.
                interactor?.Out?.WriteLine("Wrote " + item.TargetFile.FullName);
            }
        }

        private string MakePathUniform(string path, char dsc)
        {
            if (dsc != '\\')
            {
                path = path.Replace('\\', dsc);
            }

            if (dsc != '/')
            {
                path = path.Replace('/', dsc);
            }

            return path;
        }

        // Parses <paramref name="file"/>, splices any {{include}} directive into
        // <paramref name="target"/> (recursively), and rebases each included file's relative
        // links by <paramref name="prefix"/> — the folder path from the host file's directory
        // to this file's directory. <paramref name="stack"/> guards against include cycles.
        private void AppendFile(SimpleMarkdownToHtmlLayerItem item, MarkdownDocument target, FileInfo file, string prefix, HashSet<string> stack, IncludeCounter counter)
        {
            if (!stack.Add(file.FullName))
            {
                // include cycle: leave a marker and stop descending
                target.Add(this.MakeCommentBlock("{{include}} cycle at " + file.Name));
                return;
            }

            var raw = File.ReadAllText(file.FullName, Encoding.UTF8);

            // Neutralise numbered setext headings before parsing (issue #15) so "1. Title" over a
            // dashed bar renders as a heading, not an ordered-list item + a stray thematic break.
            if (IsNumberedSetextFixEnabled())
            {
                raw = FixNumberedSetextHeadings(raw);
            }

            // Replace each standalone {{include: ...}} line with a markdown-inert token so the
            // parser cannot mangle the directive; remember the path behind each token.
            var directives = new Dictionary<string, IncludeDirective>(StringComparer.Ordinal);
            var text = includeLineRegex.Replace(raw, match =>
            {
                var token = "mdbookinclude" + counter.Next().ToString(CultureInfo.InvariantCulture) + "token";
                directives[token] = new IncludeDirective(match.Value.Trim(), match.Groups[1].Value);
                return token;
            });

            var dom = Markdown.Parse(text, this.layer.Pipeline);

            // Rebase this file's own relative links by its folder before its blocks are moved
            // into the target. Done on the whole document (a container) because Markdig only
            // descends into a leaf block's inlines when traversing from a container.
            RebaseLinks(dom, prefix);

            foreach (var block in dom.ToArray())
            {
                if (block is ParagraphBlock paragraph
                    && directives.TryGetValue(GetInlineText(paragraph.Inline).Trim(), out var directive))
                {
                    this.AppendInclude(item, target, file, prefix, directive, stack, counter);
                }
                else
                {
                    dom.Remove(block);
                    target.Add(block);
                }
            }

            stack.Remove(file.FullName);
        }

        // The numbered-setext fix is on unless the opt-out variable holds a falsey token.
        private static bool IsNumberedSetextFixEnabled()
        {
            var value = Environment.GetEnvironmentVariable(NumberedSetextFixEnvVar);
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "0":
                case "false":
                case "off":
                case "no":
                    return false;
                default:
                    return true;
            }
        }

        // Escapes the list marker of any "1. Title"/"1) Title" line that is immediately followed by
        // a setext underline, so Markdig reads a paragraph + underline (a heading) instead of an
        // ordered-list item + thematic break (issue #15). Content inside fenced code blocks is left
        // untouched; original line endings (including CRLF) are preserved.
        private static string FixNumberedSetextHeadings(string text)
        {
            // Split on '\n' only: any trailing '\r' stays attached to each line and is carried
            // through unchanged, so CRLF sources round-trip exactly.
            var lines = text.Split('\n');
            var inFence = false;
            for (var i = 0; i < lines.Length; i++)
            {
                if (codeFenceLineRegex.IsMatch(lines[i]))
                {
                    inFence = !inFence;
                    continue;
                }

                if (inFence || i + 1 >= lines.Length)
                {
                    continue;
                }

                var match = numberedItemLineRegex.Match(lines[i]);
                if (match.Success && setextUnderlineRegex.IsMatch(lines[i + 1]))
                {
                    // Insert a backslash after the digits: "1. Title" -> "1\. Title". The backslash
                    // escapes the marker (so it is no longer a list item) and is not rendered.
                    var digits = match.Groups[2];
                    var cut = digits.Index + digits.Length;
                    lines[i] = lines[i].Substring(0, cut) + "\\" + lines[i].Substring(cut);
                }
            }

            return string.Join("\n", lines);
        }

        // Resolves one {{include}} directive found in <paramref name="includingFile"/> and
        // appends the included file's blocks to <paramref name="target"/>.
        private void AppendInclude(SimpleMarkdownToHtmlLayerItem item, MarkdownDocument target, FileInfo includingFile, string prefix, IncludeDirective directive, HashSet<string> stack, IncludeCounter counter)
        {
            var path = directive.Path;
            if (path.Length >= 1 && path[0] == '/')
            {
                path = path.Substring(1);
            }

            var fullPath = Path.Combine(includingFile.DirectoryName, path);
            var included = new FileInfo(fullPath);
            if (!included.Exists)
            {
                target.Add(this.MakeCommentBlock(directive.Text + ": NO SUCH FILE"));
            }
            else if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                target.Add(this.MakeCommentBlock(directive.Text + ": INVALID FILE EXTENSION"));
            }
            else
            {
                try
                {
                    var childPrefix = CombinePrefix(prefix, GetDirectoryPart(path));
                    this.AppendFile(item, target, included, childPrefix, stack, counter);
                }
                catch (UnauthorizedAccessException ex)
                {
                    target.Add(this.MakeCommentBlock(directive.Text + ": " + ex.Message));
                }
            }
        }

        // Rewrites every local .md link in the assembled document to its .html output and
        // registers any linked non-markdown file for export. Unlike a paragraph-only walk,
        // this reaches links inside headings, lists and tables.
        private void RewriteLocalLinks(SimpleMarkdownToHtmlLayerItem item, MarkdownDocument dom)
        {
            foreach (var link in dom.Descendants().OfType<LinkInline>())
            {
                if (link.Url == null || link.Url.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Uri.IsWellFormedUriString(link.Url, UriKind.Absolute))
                {
                    // fully qualified URL: leave unchanged
                    continue;
                }

                if (!Uri.TryCreate(link.Url, UriKind.Relative, out _) || MyExtensions.IsInvalidFileRelativePath(link.Url))
                {
                    // invalid relative path (see unit tests for IsInvalidFileRelativePath)
                    continue;
                }

                if (link.Url.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    // local link to a markdown document: point at its generated .html
                    link.Url += ".html";
                }
                else if (this.layer.Embed && link.IsImage)
                {
                    // will be inlined as a data: URI by SelfContainedImageEmbedder — there is no
                    // separate asset to copy alongside the output.
                }
                else
                {
                    // local link to a non-markdown file: register it for export
                    var linkFilePath = Path.Combine(item.SourceFile.DirectoryName, link.Url);
                    var resource = this.layer.AddFile(new FileInfo(linkFilePath), false);
                    resource.RelativePath = GetRelativePath(item.RelativePath.Take(item.RelativePath.Length - 1).ToArray(), link.Url);
                }
            }
        }

        // Prefixes every local, non-anchor relative link in <paramref name="block"/> with
        // <paramref name="prefix"/> and normalises the result. No-op at the host level (empty
        // prefix).
        private static void RebaseLinks(MarkdownObject block, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return;
            }

            foreach (var link in block.Descendants().OfType<LinkInline>())
            {
                if (link.Url == null || link.Url.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Uri.IsWellFormedUriString(link.Url, UriKind.Absolute) || MyExtensions.IsInvalidFileRelativePath(link.Url))
                {
                    continue;
                }

                link.Url = NormalizeRelativePath(prefix, link.Url);
            }
        }

        // Rebuilds "<!-- text -->" as a passthrough HTML block.
        private Block MakeCommentBlock(string text)
        {
            var document = Markdown.Parse("<!-- " + text + " -->\n", this.layer.Pipeline);
            var block = document[0];
            document.RemoveAt(0);
            return block;
        }

        // The `title:` value of a top-of-file YAML front-matter block, or null when the document
        // has no front-matter or no such key. Front-matter is only ever the document's first block
        // (Markdig accepts it only at the very start), so an included partial's own front-matter is
        // never read here. v1 reads a single key; unknown keys are ignored (forward-compatible).
        private static string? ExtractFrontMatterTitle(MarkdownDocument dom)
        {
            return FrontMatterScalar(dom, "title");
        }

        // The value of a top-level scalar key in the document's YAML front-matter block, or null when
        // there is no front-matter or no such key. A minimal single-line reader (v1 needs only
        // "title" and "lang"), so the tool keeps no YAML-parser dependency; indented (nested) and
        // unknown keys are ignored.
        private static string? FrontMatterScalar(MarkdownDocument dom, string key)
        {
            if (dom.Count == 0 || dom[0] is not YamlFrontMatterBlock yaml)
            {
                return null;
            }

            var lines = yaml.Lines.Lines;
            for (var i = 0; i < yaml.Lines.Count; i++)
            {
                var line = lines[i].Slice.ToString();
                if (line.Length == 0 || char.IsWhiteSpace(line[0]))
                {
                    continue;
                }

                var colon = line.IndexOf(':');
                if (colon <= 0 || !string.Equals(line.Substring(0, colon), key, StringComparison.Ordinal))
                {
                    continue;
                }

                var value = UnquoteYamlScalar(line.Substring(colon + 1).Trim());
                return value.Length > 0 ? value : null;
            }

            return null;
        }

        // Strips one layer of matching single or double quotes from a YAML scalar, leaving a bare
        // value untouched. Enough for the `title:` line v1 reads; not a general YAML unescaper.
        private static string UnquoteYamlScalar(string value)
        {
            if (value.Length >= 2
                && (value[0] == '"' || value[0] == '\'')
                && value[^1] == value[0])
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }

        // The text of the document's first level-1 heading, or null when there is none.
        private static string? ExtractFirstHeadingTitle(MarkdownDocument dom)
        {
            foreach (var heading in dom.Descendants().OfType<HeadingBlock>())
            {
                if (heading.Level == 1)
                {
                    var text = GetInlineText(heading.Inline).Trim();
                    return text.Length > 0 ? text : null;
                }
            }

            return null;
        }

        // Concatenates the literal text of an inline container, used to read a directive
        // token back from its parsed paragraph.
        private static string GetInlineText(ContainerInline? inline)
        {
            if (inline == null)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            foreach (var node in inline.Descendants())
            {
                if (node is LiteralInline literal)
                {
                    text.Append(literal.Content.ToString());
                }
            }

            return text.ToString();
        }

        // Joins a base folder and a relative link, collapsing "." and ".." segments and
        // preserving any trailing #fragment. Always returns forward-slash separators.
        private static string NormalizeRelativePath(string prefix, string url)
        {
            var fragment = string.Empty;
            var hash = url.IndexOf('#');
            if (hash >= 0)
            {
                fragment = url.Substring(hash);
                url = url.Substring(0, hash);
            }

            var segments = new List<string>();
            foreach (var part in (prefix + "/" + url).Split(directorySeparators))
            {
                if (part.Length == 0 || ".".Equals(part, StringComparison.Ordinal))
                {
                    // skip empty and current-directory segments
                }
                else if ("..".Equals(part, StringComparison.Ordinal) && segments.Count > 0 && !"..".Equals(segments[^1], StringComparison.Ordinal))
                {
                    segments.RemoveAt(segments.Count - 1);
                }
                else
                {
                    segments.Add(part);
                }
            }

            return string.Join("/", segments) + fragment;
        }

        // Combines the running prefix with an include's own directory part.
        private static string CombinePrefix(string prefix, string directoryPart)
        {
            if (string.IsNullOrEmpty(directoryPart))
            {
                return prefix;
            }

            return NormalizeRelativePath(prefix, directoryPart);
        }

        // Directory portion of an include path ("help/readme.md" => "help"; "part.md" => "").
        private static string GetDirectoryPart(string path)
        {
            var index = path.Replace('\\', '/').LastIndexOf('/');
            return index < 0 ? string.Empty : path.Substring(0, index);
        }

        public static string[] GetRelativePath(string[] left, string right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            var newPath = GetRelativePath(left, SplitPath(right));
            return newPath;
        }

        public static string[] GetRelativePath(string[] left, string[] right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            var newPath = new string[left.Length + right.Length];
            Array.Copy(left, newPath, left.Length);
            Array.Copy(right, 0, newPath, left.Length, right.Length);
            return newPath;
        }

        private static string[] SplitPath(string right)
        {
            var parts = right.Split(directorySeparators);
            var result = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part) || ".".Equals(part, StringComparison.Ordinal))
                {
                }
                else
                {
                    result.Add(part);
                }
            }

            return result.ToArray();
        }

        private string ReadTemplateFromAssembly(Assembly assembly, string path)
        {
            using (var templateStream = assembly.GetManifestResourceStream(path))
            {
                if (templateStream == null)
                {
                    throw new InvalidOperationException("No such built-in template " + path);
                }

                using (var templateReader = new StreamReader(templateStream, Encoding.UTF8))
                {
                    return templateReader.ReadToEnd();
                }
            }
        }

        // Hands out unique, stable token numbers while a single page (and its nested
        // includes) is assembled.
        private sealed class IncludeCounter
        {
            private int value;

            public int Next()
            {
                return this.value++;
            }
        }

        // A parsed {{include}} directive: its original text (for error markers) and the raw
        // path it points at.
        private readonly struct IncludeDirective
        {
            public IncludeDirective(string text, string path)
            {
                this.Text = text;
                this.Path = path;
            }

            public string Text { get; }

            public string Path { get; }
        }
    }
}
