
namespace EA4T.SteadyBear.Packager
{
    using EA4T.SteadyBear.PackageInstall;
    using EA4T.SteadyBear.Packager.Internals;
    using Markdig;
    using System;
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
        private bool defaultMainSource;
        private bool defaultCustomerSource;
        private string profile;
        private SimpleMarkdownToHtmlLayer layer;

        public SimpleMarkdownToHtmlTask()
        {
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
                context.AddLayer(layer);
            }

            // prepare pipeline
            this.layer.Pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
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

            foreach (var item in this.layer.Items)
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

            // TODO: change markdown hyperlinks from md to md.html (X.md => x.md.html)
            ;

            // generate HTML
            var contents = Markdown.ToHtml(text, this.layer.Pipeline);

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
                    return contents; // not escaped
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
    }
}