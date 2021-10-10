
namespace EA4T.SteadyBear.Packager
{
    using EA4T.SteadyBear.PackageInstall;
    using EA4T.SteadyBear.Packager.Internals;
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
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Converts some markdown files to HTML.
    /// </summary>
    public sealed class SimpleMarkdownToHtmlTask : IPackageTask
    {
        private static readonly JsonHelper json = new JsonHelper(string.Empty);
        private static readonly Regex replacer = new Regex(@"\{\{\{([^}]+)\}\}\}", RegexOptions.Compiled);
        private static readonly char[] directorySeparators = new char[] { '/', '\\', };
        private readonly string key;
        private bool defaultMainSource;
        private bool defaultCustomerSource;
        private string profile;
        private SimpleMarkdownToHtmlLayer layer;

        public SimpleMarkdownToHtmlTask(string key)
        {
            this.key = key;
        }

        public string Name => nameof(SimpleMarkdownToHtmlTask);

        public void Visit(PackageContext context)
        {
            if (this.layer != null)
            {
                throw new InvalidOperationException("Cannot run this task twice. ");
            }

            var interactor = context.RequireSingleLayer<Interactor>();

            this.layer = context.GetSingleLayer<SimpleMarkdownToHtmlLayer>();
            if (this.layer == null)
            {
                this.layer = new SimpleMarkdownToHtmlLayer();
                this.layer.Key = this.key;
                context.AddLayer(layer);
            }

            // prepare pipeline
            this.layer.Pipeline = new MarkdownPipelineBuilder()
                .UseAutoIdentifiers()
                .UseAutoLinks()
                .UsePipeTables()
                .Build();

            // prepare template
            using (var templateStream = typeof(SimpleMarkdownToHtmlTask).Assembly.GetManifestResourceStream("EA4T.SteadyBear.Packager.Resources.MarkdownToHtml.html"))
            using (var templateReader = new StreamReader(templateStream, Encoding.UTF8))
            {
                this.layer.Template = templateReader.ReadToEnd();
            }

            context.Visited(this);
        }

        public void Verify(PackageContext context)
        {
            var errors = 0;
            var interactor = context.RequireSingleLayer<Interactor>();

            context.Verified(this, errors == 0);
        }

        public void Run(PackageContext context)
        {
            var errors = 0;
            var interactor = context.RequireSingleLayer<Interactor>();

            foreach (var item in this.layer.Items.ToArray()) // we need to change the collection while enumerating it
            {
                this.ProcessFileMarkdown(context, item);
            }

            context.Ran(this, errors == 0);
        }

        private void ProcessFileMarkdown(PackageContext context, SimpleMarkdownToHtmlLayerItem item)
        {
            var interactor = context.RequireSingleLayer<Interactor>();
            interactor.WriteTaskInfo(this, "Processing markdown file \"" + item.SourceFile + "\". ");

            // prepare
            var title = Path.GetFileNameWithoutExtension(item.SourceFile.Name);
            var fileName = Path.GetFileName(item.SourceFile.Name);

            // detect lang in file name
            CultureInfo lang = null;
            var dot = new char[] { '.', };
            var titleParts = title.Split(dot);
            if (titleParts.Length > 1 && titleParts[titleParts.Length - 1].Length >= 2)
            {
                try
                {
                    lang = new CultureInfo(titleParts[titleParts.Length - 1]);
                    var newTitleParts = new string[titleParts.Length - 1];
                    Array.Copy(titleParts, newTitleParts, newTitleParts.Length);
                    title = string.Join(dot[0].ToString(), newTitleParts);
                }
                catch (CultureNotFoundException)
                {
                }
            }

            // read markdown file
            string text;
            using (var soureceStream = new FileStream(item.SourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(soureceStream, Encoding.UTF8))
            {
                text = reader.ReadToEnd();
            }

            // include markdown parts
            var includeRegex = new Regex(@"\{\{(include: *)([/a-zA-Z0-9 ()+='"",.?_-]+)\}\}", RegexOptions.None);
            text = includeRegex.Replace(text, new MatchEvaluator(m =>
            {
                var origValue = m.Value;
                var value = origValue;
                var contents = new StringBuilder();

                var command = m.Groups[1].Value;
                if (command.StartsWith("include:", StringComparison.Ordinal))
                {
                    var path = m.Groups[2].Value;
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.Length >= 1 && path[0] == '/')
                        {
                            path = path.Substring(1);
                        }

                        var fullPath = Path.Combine(item.SourceFile.DirectoryName, path);
                        var doc = new FileInfo(fullPath);
                        if (!doc.Exists)
                        {
                            contents.Append(@"<!-- " + origValue + ": NO SUCH FILE -->");
                        }
                        else if (path.EndsWith(".md"))
                        {
                            try
                            {
                                contents.Append(@"\n<!-- " + origValue + ": INCLUDE BEGINS -->\n");
                                contents.Append(System.IO.File.ReadAllText(doc.FullName, Encoding.UTF8));
                                contents.Append(@"\n<!-- " + origValue + ": INCLUDE ENDS   -->\n");
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                contents.Append(@"<!-- " + origValue + ": " + ex.Message + " -->");
                            }
                        }
                        else
                        {
                            contents.Append(value = @"<!-- " + origValue + ": INVALID FILE EXTENSION -->");
                        }
                    }
                }
                else
                {
                    contents.Append(value);
                }

                return contents.ToString();
            }));

            // change markdown hyperlinks from md to md.html (X.md => x.md.html)
            // TODO: to consider: only change local .md links if these are to be rendered?
            var dom = Markdown.Parse(text, this.layer.Pipeline);
            this.EnhanceDom(item, dom);

            // generate HTML
            string htmlContents;
            {
                var writer = new StringWriter();
                var renderer = new HtmlRenderer(writer);
                renderer.Render(dom);
                htmlContents = writer.ToString();
            }

            // substitute HTML template variables
            // don't forget to HTML-escape strings!
            // known variables are: 
            // - {{{PageTitle}}}  the title for the page
            // - {{{Contents}}}   the markdown-converted HTML part
            // - {{{Lang}}}       the page's lang
            // - {{{Info}}}       a information string
            var page = replacer.Replace(this.layer.Template, new MatchEvaluator(match =>
            {
                var key = match.Groups[1].Value;

                if ("PageTitle".Equals(key, StringComparison.Ordinal))
                {
                    return WebUtility.HtmlEncode(title);
                }
                else if ("Contents".Equals(key, StringComparison.Ordinal))
                {
                    return htmlContents; // not escaped
                }
                else if ("Lang".Equals(key, StringComparison.Ordinal))
                {
                    return lang != null ? lang.Name : "en";
                }
                else if ("Info".Equals(key, StringComparison.Ordinal))
                {
                    return WebUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture, "This document was generated automatically from file \"{0}\" on {1:o} using the MarkdownToHtmlTask tool. Manual modifications will be lost next time this file is generated again. ", fileName, DateTime.UtcNow));
                }
                else
                {
                    return string.Empty;
                }
            }));

            // write HTMl file
            using (var targetStream = new FileStream(item.TargetFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(targetStream, Encoding.UTF8))
                {
                    writer.Write(page);
                    writer.Flush();
                }
            }
        }

        internal SimpleMarkdownToHtmlTask SetProfile(string profile)
        {
            this.profile = profile;
            return this;
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

        private void EnhanceDom(SimpleMarkdownToHtmlLayerItem context, ContainerBlock root)
        {
            foreach (var item in root)
            {
                if (item is ContainerBlock containerBlock)
                {
                    this.EnhanceDom(context, containerBlock);
                }
                else if (item is ParagraphBlock paragraph)
                {
                    this.EnhanceDom(context, paragraph);
                }
                else if (item is Block block)
                {
                    this.EnhanceDom(context, block);
                }
            }
        }

        private void EnhanceDom(SimpleMarkdownToHtmlLayerItem context, Block block)
        {
        }

        private void EnhanceDom(SimpleMarkdownToHtmlLayerItem context, ParagraphBlock paragraph)
        {
            foreach (var item in paragraph.Inline)
            {
                if (item is LinkInline link)
                {
                    if (link.Url != null && Uri.TryCreate(link.Url, UriKind.RelativeOrAbsolute, out Uri uri))
                    {
                        if (uri.IsAbsoluteUri)
                        {
                        }
                        else
                        {
                            var path = link.Url;
                            if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                            {
                                // local link to a markdown document: fix link url
                                path = path + ".html";
                            }
                            else
                            {
                                // local link to a non-markdown file
                                // add it for export
                                var linkFilePath = Path.Combine(context.SourceFile.DirectoryName, link.Url);
                                var resource = this.layer.AddFile(new FileInfo(linkFilePath), false);
                                resource.RelativePath = GetRelativePath(context.RelativePath.Take(context.RelativePath.Length - 1).ToArray(), link.Url);
                            }

                            link.Url = path;
                        }
                    }
                }
            }
        }

        public static string[] GetRelativePath(string[] left, string right)
        {
            var newPath = GetRelativePath(left, SplitPath(right));
            return newPath;
        }

        public static string[] GetRelativePath(string[] left, string[] right)
        {
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
    }
}
