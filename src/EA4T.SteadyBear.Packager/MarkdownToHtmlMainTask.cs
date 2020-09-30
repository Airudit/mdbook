
namespace EA4T.SteadyBear.Packager
{
    using EA4T.SteadyBear.PackageInstall;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Command to convert some markdown files to HTML (no packaging involved).
    /// </summary>
    public sealed class MarkdownToHtmlMainTask : OrdererTask
    {
        public MarkdownToHtmlMainTask()
            : base(nameof(MarkdownToHtmlMainTask))
        {
        }

        public bool DoRun { get; private set; } = true;

        public override void Visit(PackageContext context)
        {
            var interactor = context.RequireSingleLayer<Interactor>();

            // parse console arguments
            var isHelp = false;
            var files = new List<FileInfo>();
            var directories = new List<DirectoryInfo>();
            interactor.ConsumeArguments(a =>
            {
                if ("--help".Equals(a.Current, StringComparison.OrdinalIgnoreCase))
                {
                    isHelp = true;
                    a.ConsumeOne();
                }
                else
                {
                    // extra values???
                    if (Directory.Exists(a.Current))
                    {
                        directories.Add(new DirectoryInfo(a.Current));
                        a.ConsumeOne();
                    }
                    else if (File.Exists(a.Current))
                    {
                        files.Add(new FileInfo(a.Current));
                        a.ConsumeOne();
                    }
                    else
                    {
                        interactor.WriteTaskError(this, "Unknown argumeent \"" + a.Current + "\". ");
                    }
                }
            });

            if (isHelp)
            {
                interactor.WriteTaskError(this, "Displaying help. ");
                interactor.Out.WriteLine("");
                interactor.Out.WriteLine("The `MarkdownToHtml` command will generate HTML files for each specified markdown file. ");
                interactor.Out.WriteLine("");
                interactor.Out.WriteLine("MarkdownToHtml command usage: ");
                interactor.Out.WriteLine("    {file path}+ [options]");
                interactor.Out.WriteLine("");
                interactor.Out.WriteLine("Options: ");
                interactor.Out.WriteLine("");
                Environment.Exit(1);
            }
            else
            {
            }

            // fill layer with files
            var layer = new SimpleMarkdownToHtmlLayer();
            context.AddLayer(layer);
            foreach (var item in files)
            {
                layer.AddFile(item);
            }

            foreach (var dir in directories)
            {
                foreach (var file in dir.GetFiles("*.md", SearchOption.TopDirectoryOnly))
                {
                    layer.AddFile(file);
                }
            }

            base.Visit(context);
        }

        protected override void Populate(PackageContext context)
        {
            var interactor = context.RequireSingleLayer<Interactor>();

            this.layer.Tasks.Add(new SimpleMarkdownToHtmlTask());
        }

        public override void Verify(PackageContext context)
        {
            base.Verify(context);
        }

        public override void Run(PackageContext context)
        {
            if (this.DoRun)
            {
                base.Run(context);
            }
            else
            {
                var interactor = context.RequireSingleLayer<Interactor>();
                interactor.Out.WriteLine(string.Empty);
                interactor.WriteTaskError(this, "Not running. ");
                interactor.Out.WriteLine(string.Empty);
            }
        }
    }
}
