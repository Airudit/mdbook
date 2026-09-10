
namespace Airudit.MdBook.Core
{
    using Airudit.MdBook.Core.Internals;
    using Markdig;
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

        public void Visit(PackageContext context)
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

            // prepare pipeline
            this.layer.Pipeline = new MarkdownPipelineBuilder()
                .UseAutoIdentifiers()
                .UseAutoLinks()
                .UsePipeTables()
                .UseEmphasisExtras()
                .UseTaskLists()
                .Build();

            // prepare template
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
        }

        public void Verify(PackageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        public void Run(PackageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            this.RemoveIncludedPartials();

            foreach (var item in this.layer.Items.ToArray()) // we need to change the collection while enumerating it
            {
                this.ProcessFileMarkdown(context, item);
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

        private void ProcessFileMarkdown(PackageContext context, SimpleMarkdownToHtmlLayerItem item)
        {
            var interactor = context.GetSingleLayer<CommandLineLayer>();
            interactor?.Out?.WriteLine("Processing markdown file \"" + item.SourceFile + "\". ");

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

            // Remember the page's first level-1 heading; the single-file table of contents
            // uses it as the page label instead of the bare file name.
            item.Title = ExtractFirstHeadingTitle(dom);

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
                    return WebUtility.HtmlEncode(title);
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
                else
                {
                    return string.Empty;
                }
            });

            // keep the full page in memory so it can be exported even when not written in place
            item.RenderedPage = page;

            // write the in-place side-by-side HTML file, unless suppressed (issue #9)
            if (this.layer.SideBySide)
            {
                using (var targetStream = new FileStream(item.TargetFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(targetStream, Encoding.UTF8))
                    {
                        writer.Write(page);
                        writer.Flush();
                    }
                }
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
