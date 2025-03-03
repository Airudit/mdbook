
namespace EA4T.SteadyBear.Packager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Markdig;
    using System.Globalization;

    public sealed class SimpleMarkdownToHtmlLayer
    {
        public SimpleMarkdownToHtmlLayer()
        {
        }

        /// <summary>
        /// The task's key.
        /// </summary>
        public string Key { get; set; }

        public MarkdownPipeline Pipeline { get; internal set; }

        public string Template { get; internal set; }

        /// <summary>
        /// Files processed.
        /// </summary>
        public List<SimpleMarkdownToHtmlLayerItem> Items { get; } = new List<SimpleMarkdownToHtmlLayerItem>();

        /// <summary>
        /// Export orders.
        /// </summary>
        public List<SimpleMarkdownToHtmlLayerExport> Exports { get; } = new List<SimpleMarkdownToHtmlLayerExport>();

        public string SingleFile { get; set; }
        
        public string TemplateFilePath { get; set; }

        public SimpleMarkdownToHtmlLayerItem AddFile(FileInfo sourceFile, bool isMarkdown)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));

            var item = new SimpleMarkdownToHtmlLayerItem();
            item.SourceFile = sourceFile;
            item.IsMarkdown = isMarkdown;
            if (isMarkdown)
            {
                item.TargetFile = new FileInfo(sourceFile.FullName + ".html");
            }
            else
            {
                item.TargetFile = new FileInfo(sourceFile.FullName);
            }

            this.Items.Add(item);
            return item;
        }

    }

    public sealed class SimpleMarkdownToHtmlLayerItem
    {
        public FileInfo SourceFile { get; internal set; }

        public FileInfo TargetFile { get; internal set; }
        public string[] RelativePath { get; set; }
        public bool IsMarkdown { get; set; }
        public string HtmlContents { get; set; }
        public CultureInfo Lang { get; set; }
    }

    public sealed class SimpleMarkdownToHtmlLayerExport
    {
        public DirectoryInfo Directory { get; set; }
    }
}
