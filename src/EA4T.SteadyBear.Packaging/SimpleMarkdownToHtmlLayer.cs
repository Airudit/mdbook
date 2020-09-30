
namespace EA4T.SteadyBear.Packager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Markdig;

    public sealed class SimpleMarkdownToHtmlLayer
    {
        public SimpleMarkdownToHtmlLayer()
        {
        }

        public MarkdownPipeline Pipeline { get; internal set; }

        public string Template { get; internal set; }

        public List<SimpleMarkdownToHtmlLayerItem> Items { get; } = new List<SimpleMarkdownToHtmlLayerItem>();

        public void AddFile(FileInfo sourceFile)
        {
            if (sourceFile == null)
                throw new ArgumentNullException("sourceFile");

            var item = new SimpleMarkdownToHtmlLayerItem();
            item.SourceFile = sourceFile;
            item.TargetFile = new FileInfo(sourceFile.FullName + ".html");
            this.Items.Add(item);
        }
    }

    public sealed class SimpleMarkdownToHtmlLayerItem
    {
        public FileInfo SourceFile { get; internal set; }

        public FileInfo TargetFile { get; internal set; }
    }
}