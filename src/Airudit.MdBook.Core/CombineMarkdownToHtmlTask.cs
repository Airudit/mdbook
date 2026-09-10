
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

        var errors = 0;

        var layer = context.RequireSingleLayer<SimpleMarkdownToHtmlLayer>();
        if (layer?.SingleFile == null)
        {
            return;
        }

        // Assign each page a stable, path-based anchor slug (e.g. "guide/intro.md" ->
        // "guide-intro"), stored on the item so the table of contents, images and per-language
        // splitting can reuse it. Cross-page links are then resolved to these in-file anchors
        // instead of the non-existent .md.html targets (issue #17). Slugs are path-based (not
        // positional) so adding or reordering a page does not shift another page's anchor.
        var slugBySource = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usedSlugs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in layer.Items)
        {
            if (!page.IsMarkdown)
            {
                continue;
            }

            page.Anchor = MakeUniqueSlug(page, usedSlugs);
            slugBySource[page.SourceFile.FullName] = page.Anchor;
        }

        var langs = new Dictionary<string, int>();
        using var list = new StringWriter();
        list.WriteLine("<article id=list>");
        list.WriteLine("<ul>");
        using var contents = new StringWriter();

        foreach (var page in layer.Items)
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

            list.Write("<li><a href=\"#" + elementId + "\">");
            list.Write(HttpUtility.HtmlEncode(page.SourceFile.Name));
            list.WriteLine("</a></li>");
           
            contents.WriteLine();
            contents.WriteLine("<article id=\"" + elementId + "\"" + langAttribute + ">");
            contents.WriteLine();
            contents.WriteLine(RewriteLocalLinksToAnchors(page.HtmlContents?.ToString(), page.SourceFile.DirectoryName, slugBySource));
            contents.WriteLine();
            contents.WriteLine("</article>");
            contents.WriteLine();
        }
        
        list.WriteLine("</ul>");
        list.WriteLine("</article>");
        list.WriteLine();
        list.WriteLine();

        // substitute HTML template variables
        // don't forget to HTML-escape strings!
        // known variables are: 
        // - {{{PageTitle}}}  the title for the page
        // - {{{Contents}}}   the markdown-converted HTML part
        // - {{{Lang}}}       the page's lang
        // - {{{Info}}}       a information string
        var langName = langs.Count > 0 ? langs.OrderByDescending(x => x.Value).First().Key : "en-US";
        var lang = new CultureInfo(langName);
        var title = Path.GetFileNameWithoutExtension(layer.SingleFile);
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

        var path = layer.SingleFile;
        using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        {
            file.SetLength(0L);
            using (var writer = new StreamWriter(file, Encoding.UTF8))
            {
                writer.WriteLine(pageContents);
            }
        }
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
}
