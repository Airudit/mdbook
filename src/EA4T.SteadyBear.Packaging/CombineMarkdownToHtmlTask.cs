
namespace EA4T.SteadyBear.Packaging;

using EA4T.SteadyBear.PackageInstall;
using EA4T.SteadyBear.PackageInstall.Internals;
using EA4T.SteadyBear.Packager;
using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

/// <summary>
/// Combines the files handled by the <see cref="SimpleMarkdownToHtmlTask"/>.
/// </summary>
public class CombineMarkdownToHtmlTask : IPackageTask
{
    private static readonly Regex replacer = new Regex(@"\{\{\{([^}]+)\}\}\}", RegexOptions.Compiled);
    private readonly string parentTaskKey;

    public CombineMarkdownToHtmlTask(string key, string parentTaskKey)
    {
        this.parentTaskKey = parentTaskKey;
        this.Key = key;
    }

    public string Name { get => nameof(CombineMarkdownToHtmlTask); }
    
    public string Key { get; }

    public void Visit(PackageContext context)
    {
    }

    public void Verify(PackageContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.Verified(this, true);
    }

    public void Run(PackageContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var interactor = context.RequireSingleLayer<IInteractor>();
        var errors = 0;

        var layer = context.Layers.OfType<SimpleMarkdownToHtmlLayer>().Single(x => this.parentTaskKey.Equals(x.Key, StringComparison.Ordinal));
        if (layer?.SingleFile == null)
        {
            return;
        }

        var langs = new Dictionary<string, int>();
        using var list = new StringWriter();
        list.WriteLine("<article id=list>");
        list.WriteLine("<ul>");
        using var contents = new StringWriter();

        for (var p = 0; p < layer.Items.Count; p++)
        {
            var page = layer.Items[p];
            if (!page.IsMarkdown)
            {
                continue;
            }

            var elementId = "page-" + p.ToInvariantString();
            if (page.Lang != null)
            {
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
            contents.WriteLine("<article id=\"" + elementId + "\">");
            contents.WriteLine();
            contents.WriteLine(page.HtmlContents?.ToString());
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
                return WebUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture, "This document was generated automatically from many files on {0:o} using the MarkdownToHtmlTask tool. Manual modifications will be lost next time this file is generated again. ", DateTime.UtcNow));
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
}
