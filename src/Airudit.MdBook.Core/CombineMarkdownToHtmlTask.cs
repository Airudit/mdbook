
namespace Airudit.MdBook.Core;

using Airudit.MdBook.Core.Internals;
using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

/// <summary>
/// Combines the files handled by the <see cref="SimpleMarkdownToHtmlTask"/>.
/// </summary>
public class CombineMarkdownToHtmlTask : ITask
{
    private static readonly Regex replacer = new Regex(@"\{\{\{([^}]+)\}\}\}", RegexOptions.Compiled);

    // Local (non-external) anchors emitted by SimpleMarkdownToHtmlTask: "<a href="...">".
    // External links carry a class attribute before href, so this only matches local ones.
    private static readonly Regex localLinkRegex = new Regex(@"<a href=""([^""]+)"">", RegexOptions.Compiled);

    public CombineMarkdownToHtmlTask()
    {
    }

    public void Visit(PackageContext context)
    {
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

        var layer = context.RequireSingleLayer<SimpleMarkdownToHtmlLayer>();
        if (layer?.SingleFile == null)
        {
            // --ByLang only makes sense alongside --Single-File (issue #22).
            if (layer?.ByLang == true)
            {
                context.GetSingleLayer<CommandLineLayer>()?.ErrorOut?.WriteLine("--ByLang requires --Single-File. ");
            }

            return;
        }

        if (!layer.ByLang)
        {
            this.CombineInto(context, layer, layer.Items, layer.SingleFile);
            return;
        }

        // --ByLang: emit one combined book per detected language (issue #22). Regional variants
        // are unified by their two-letter ISO code, so en, en-US and en-GB share one "en" book
        // (each article still keeps its exact lang attribute). Language-neutral pages (no ".xx"
        // suffix) are included in every language's book, so shared front-matter appears in each.
        var languages = new List<string>();
        foreach (var item in layer.Items)
        {
            if (item.IsMarkdown && item.Lang != null && !languages.Contains(item.Lang.TwoLetterISOLanguageName, StringComparer.OrdinalIgnoreCase))
            {
                languages.Add(item.Lang.TwoLetterISOLanguageName);
            }
        }

        if (languages.Count == 0)
        {
            context.GetSingleLayer<CommandLineLayer>()?.ErrorOut?.WriteLine("--ByLang found no page with a detected language (a trailing \".xx\" in the file name). Nothing was written. ");
            return;
        }

        foreach (var language in languages)
        {
            var subset = new List<SimpleMarkdownToHtmlLayerItem>();
            foreach (var item in layer.Items)
            {
                if (!item.IsMarkdown || item.Lang == null || string.Equals(item.Lang.TwoLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase))
                {
                    subset.Add(item);
                }
            }

            // The book represents the language, not a region, so its document-level {{{Lang}}}
            // is the neutral two-letter code even when its pages carry regional tags.
            this.CombineInto(context, layer, subset, SubstituteLangPlaceholder(layer.SingleFile, language), language);
        }
    }

    // Builds one combined single-file document from the given pages and writes it to
    // outputPath. Shared by the whole-book path and the per-language --ByLang path (issue #22).
    private void CombineInto(PackageContext context, SimpleMarkdownToHtmlLayer layer, IReadOnlyList<SimpleMarkdownToHtmlLayerItem> items, string outputPath, string documentLang = null)
    {
        // Assign each page a stable, path-based anchor slug (e.g. "guide/intro.md" ->
        // "guide-intro"), stored on the item so the table of contents, images and per-language
        // splitting can reuse it. Cross-page links are then resolved to these in-file anchors
        // instead of the non-existent .md.html targets (issue #17). Slugs are path-based (not
        // positional) so adding or reordering a page does not shift another page's anchor.
        var slugBySource = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usedSlugs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in items)
        {
            if (!page.IsMarkdown)
            {
                continue;
            }

            page.Anchor = MakeUniqueSlug(page, usedSlugs);
            slugBySource[page.SourceFile.FullName] = page.Anchor;
        }

        using var list = new StringWriter();
        list.WriteLine("<article id=list>");
        WriteTableOfContents(list, items);
        list.WriteLine("</article>");
        list.WriteLine();
        list.WriteLine();

        var langs = new Dictionary<string, int>();
        using var contents = new StringWriter();

        foreach (var page in items)
        {
            if (!page.IsMarkdown)
            {
                continue;
            }

            var elementId = page.Anchor;

            // Tag the section with its own language so screen readers, hyphenation and
            // spellcheck switch per section in a mixed-language book (the document-level
            // {{{Lang}}} only carries the majority language).
            var langAttribute = string.Empty;
            if (page.Lang != null)
            {
                langAttribute = " lang=\"" + HttpUtility.HtmlEncode(page.Lang.Name) + "\"";

                int count = 0;
                if (langs.TryGetValue(page.Lang.Name, out count))
                {
                    langs[page.Lang.Name] = count + 1;
                }
                else
                {
                    langs[page.Lang.Name] = count = 0;
                }
            }
           
            contents.WriteLine();
            contents.WriteLine("<article id=\"" + elementId + "\"" + langAttribute + ">");
            contents.WriteLine();
            contents.WriteLine(RewriteLocalLinksToAnchors(page.HtmlContents?.ToString(), page.SourceFile.DirectoryName, slugBySource));
            contents.WriteLine();
            contents.WriteLine("</article>");
            contents.WriteLine();
        }
        
        // substitute HTML template variables
        // don't forget to HTML-escape strings!
        // known variables are: 
        // - {{{PageTitle}}}  the title for the page
        // - {{{Contents}}}   the markdown-converted HTML part
        // - {{{Lang}}}       the page's lang
        // - {{{Info}}}       a information string
        // --ByLang passes the book's neutral language; otherwise use the pages' majority language.
        var langName = documentLang ?? (langs.Count > 0 ? langs.OrderByDescending(x => x.Value).First().Key : "en-US");
        var lang = new CultureInfo(langName);
        var title = Path.GetFileNameWithoutExtension(outputPath);
        var pageContents = replacer.Replace(layer.Template, new MatchEvaluator(match =>
        {
            var key = match.Groups[1].Value;

            if ("PageTitle".Equals(key, StringComparison.Ordinal))
            {
                return WebUtility.HtmlEncode(title);
            }
            else if ("Contents".Equals(key, StringComparison.Ordinal))
            {
                return list.ToString() + contents.ToString(); // not escaped
            }
            else if ("Lang".Equals(key, StringComparison.Ordinal))
            {
                return lang != null ? lang.Name : "en";
            }
            else if ("Info".Equals(key, StringComparison.Ordinal))
            {
                return WebUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture, "This document was generated automatically from many files on {0:o} using the dotnet mdbook tool. Manual modifications will be lost next time this file is generated again. ", DateTime.UtcNow));
            }
            else if ("Copyright".Equals(key, StringComparison.Ordinal))
            {
                return WebUtility.HtmlEncode(layer.Copyright ?? string.Empty);
            }
            else
            {
                return string.Empty;
            }
        }));

        var path = outputPath;
        using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        {
            file.SetLength(0L);
            using (var writer = new StreamWriter(file, Encoding.UTF8))
            {
                writer.WriteLine(pageContents);
            }
        }

        // Confirm the combined file was written, mirroring the "Exporting to:" line --Export
        // prints. This is a normal (non-verbose) summary; the per-page trace stays behind -v.
        var pageCount = items.Count(item => item.IsMarkdown);
        var interactor = context.GetSingleLayer<CommandLineLayer>();
        interactor?.Out?.WriteLine("Combined " + pageCount + (pageCount == 1 ? " page into " : " pages into ") + Path.GetFullPath(path));
    }

    // Builds the per-language output path for --ByLang (issue #22). A "{lang}" placeholder in the
    // path is replaced with the language code; if the path has no placeholder, ".{lang}" is
    // inserted before the extension, so "book.html" becomes "book.en.html" (decision 1a).
    private static string SubstituteLangPlaceholder(string path, string language)
    {
        const string placeholder = "{lang}";
        if (path.Contains(placeholder, StringComparison.Ordinal))
        {
            return path.Replace(placeholder, language, StringComparison.Ordinal);
        }

        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path); // includes the leading dot, or empty
        var newName = fileName + "." + language + extension;
        return string.IsNullOrEmpty(directory) ? newName : Path.Combine(directory, newName);
    }

    // Rewrites every local link in <paramref name="html"/> that points at another bundled
    // page (a ".md.html" target next to it) to that page's in-file "#slug" anchor. Links to
    // pages outside the bundle, anchors, and absolute URLs are left untouched. Any heading
    // fragment on the link is dropped: auto-generated heading IDs are not unique across the
    // merged document, so only the page-level anchor can be resolved reliably (issue #17).
    private static string RewriteLocalLinksToAnchors(string html, string baseDirectory, IDictionary<string, string> slugBySource)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        return localLinkRegex.Replace(html, match =>
        {
            var url = match.Groups[1].Value;
            if (url.Length == 0 || url[0] == '#' || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return match.Value;
            }

            var hash = url.IndexOf('#');
            var basePart = hash >= 0 ? url.Substring(0, hash) : url;
            if (!basePart.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            // "X.md.html" was produced from a local "X.md" link; strip .html to get the .md path.
            var candidate = basePart.Substring(0, basePart.Length - ".html".Length);
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(baseDirectory, Uri.UnescapeDataString(candidate)));
            }
            catch (ArgumentException)
            {
                return match.Value;
            }

            return slugBySource.TryGetValue(fullPath, out var slug)
                ? "<a href=\"#" + slug + "\">"
                : match.Value;
        });
    }

    // Writes a nested table of contents that mirrors the source directory tree: pages are
    // grouped under their folders, folders shown as plain-text labels and pages as anchor
    // links (issue #18). The longest folder prefix shared by every page is dropped, so a
    // single "mdbook book/" run does not wrap everything in a redundant "book" node.
    private static void WriteTableOfContents(TextWriter list, IEnumerable<SimpleMarkdownToHtmlLayerItem> items)
    {
        var pages = items.Where(item => item.IsMarkdown).ToList();
        var commonPrefix = CommonFolderPrefixLength(pages);
        var root = new TocNode();
        foreach (var page in pages)
        {
            var relativePath = page.RelativePath;
            var node = root;
            if (relativePath != null)
            {
                // walk the folder segments (everything but the file name), skipping the shared prefix
                for (var i = commonPrefix; i < relativePath.Length - 1; i++)
                {
                    node = node.GetOrAddFolder(relativePath[i]);
                }
            }

            node.Children.Add(new TocNode { Name = TableOfContentsLabel(page), Anchor = page.Anchor });
        }

        RenderTableOfContents(list, root.Children);
    }

    // Emits a <ul> for a level of the TOC tree: file nodes as "<li><a href="#slug">name</a>",
    // folder nodes as "<li>name<ul>...</ul></li>".
    private static void RenderTableOfContents(TextWriter list, List<TocNode> nodes)
    {
        list.WriteLine("<ul>");
        foreach (var node in nodes)
        {
            if (node.Anchor != null)
            {
                list.Write("<li><a href=\"#" + node.Anchor + "\">");
                list.Write(HttpUtility.HtmlEncode(node.Name));
                list.WriteLine("</a></li>");
            }
            else
            {
                list.Write("<li>");
                list.Write(HttpUtility.HtmlEncode(node.Name));
                RenderTableOfContents(list, node.Children);
                list.WriteLine("</li>");
            }
        }

        list.WriteLine("</ul>");
    }

    // A page's table-of-contents label: its first level-1 heading, or the file name with a
    // trailing ".md" removed when the page has no heading.
    private static string TableOfContentsLabel(SimpleMarkdownToHtmlLayerItem page)
    {
        if (!string.IsNullOrWhiteSpace(page.Title))
        {
            return page.Title;
        }

        var name = page.SourceFile.Name;
        return name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? name.Substring(0, name.Length - ".md".Length)
            : name;
    }

    // The number of leading folder segments shared by every page (the file name excluded),
    // so that common ancestry is not repeated in the table of contents.
    private static int CommonFolderPrefixLength(List<SimpleMarkdownToHtmlLayerItem> pages)
    {
        if (pages.Count == 0)
        {
            return 0;
        }

        var shortestFolderDepth = int.MaxValue;
        foreach (var page in pages)
        {
            var folderDepth = Math.Max(0, (page.RelativePath?.Length ?? 0) - 1);
            shortestFolderDepth = Math.Min(shortestFolderDepth, folderDepth);
        }

        var prefix = 0;
        for (; prefix < shortestFolderDepth; prefix++)
        {
            var segment = pages[0].RelativePath![prefix];
            foreach (var page in pages)
            {
                if (!string.Equals(page.RelativePath![prefix], segment, StringComparison.Ordinal))
                {
                    return prefix;
                }
            }
        }

        return prefix;
    }

    // A page's anchor slug, made unique within the document by appending -2, -3, ... on
    // collision (deterministic by document order).
    private static string MakeUniqueSlug(SimpleMarkdownToHtmlLayerItem item, HashSet<string> used)
    {
        var baseSlug = SlugFromItem(item);
        var slug = baseSlug;
        for (var n = 2; !used.Add(slug); n++)
        {
            slug = baseSlug + "-" + n.ToString(CultureInfo.InvariantCulture);
        }

        return slug;
    }

    // Builds a readable, path-based slug from a page's relative path ("guide/intro.md" ->
    // "guide-intro"). Each path segment and dotted name part is normalised separately with
    // Markdig's slugifier (the same one it uses for heading IDs) and joined with '-'; the
    // language suffix is kept ("readme.en.md" -> "readme-en") so multi-language books stay
    // unambiguous.
    private static string SlugFromItem(SimpleMarkdownToHtmlLayerItem item)
    {
        var tokens = new List<string>();
        var parts = item.RelativePath;
        if (parts != null)
        {
            for (var i = 0; i < parts.Length; i++)
            {
                var segment = parts[i];
                if (i == parts.Length - 1 && segment.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    segment = segment.Substring(0, segment.Length - ".md".Length);
                }

                foreach (var piece in segment.Split('.'))
                {
                    var token = Markdig.Helpers.LinkHelper.Urilize(piece, false, true);
                    if (!string.IsNullOrEmpty(token))
                    {
                        tokens.Add(token);
                    }
                }
            }
        }

        if (tokens.Count == 0)
        {
            var name = Markdig.Helpers.LinkHelper.Urilize(Path.GetFileNameWithoutExtension(item.SourceFile.Name), false, true);
            if (!string.IsNullOrEmpty(name))
            {
                tokens.Add(name);
            }
        }

        return tokens.Count > 0 ? string.Join("-", tokens) : "page";
    }

    // A node in the table-of-contents tree: a file leaf (<see cref="Anchor"/> set) or a
    // folder (children only). Folders keep an index so the same directory is reused while
    // children preserve first-seen (document) order.
    private sealed class TocNode
    {
        private Dictionary<string, TocNode>? folders;

        public string Name { get; set; } = string.Empty;

        public string? Anchor { get; set; }

        public List<TocNode> Children { get; } = new List<TocNode>();

        public TocNode GetOrAddFolder(string name)
        {
            this.folders ??= new Dictionary<string, TocNode>(StringComparer.Ordinal);
            if (!this.folders.TryGetValue(name, out var child))
            {
                child = new TocNode { Name = name };
                this.folders[name] = child;
                this.Children.Add(child);
            }

            return child;
        }
    }
}
