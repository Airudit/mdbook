
namespace EA4T.SteadyBear.Packager
{
    using EA4T.SteadyBear.PackageInstall;
    using EA4T.SteadyBear.Packaging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Main command. Command to convert some markdown files to HTML (no packaging involved).
    /// </summary>
    public sealed class MarkdownToHtmlMainTask : OrdererTask
    {
        private const string MarkdownLayerKey = "MarkdownToHtml";

        public MarkdownToHtmlMainTask()
            : base(nameof(MarkdownToHtmlMainTask))
        {
        }

        public bool DoRun { get; private set; } = true;

        public override void Visit(PackageContext context)
        {
            var interactor = context.RequireSingleLayer<IInteractor>();

            var layer = new SimpleMarkdownToHtmlLayer();
            layer.Key = MarkdownLayerKey;
            context.AddLayer(layer);

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
                else if ("--export".Equals(a.Current, StringComparison.OrdinalIgnoreCase))
                {
                    a.ConsumeOne();
                    if (!string.IsNullOrEmpty(a.Next))
                    {
                        var export = new SimpleMarkdownToHtmlLayerExport();
                        export.Directory = new DirectoryInfo(a.Next);
                        layer.Exports.Add(export);
                        a.ConsumeOne();
                    }
                    else
                    {
                        interactor.WriteTaskError(this, "Argument --Export must be followed by a directory path. ");
                    }
                }
                else if ("--single-file".Equals(a.Current, StringComparison.OrdinalIgnoreCase))
                {
                    a.ConsumeOne();
                    if (!string.IsNullOrEmpty(a.Next))
                    {
                        layer.SingleFile = a.Next;
                        a.ConsumeOne();
                    }
                    else
                    {
                        interactor.WriteTaskError(this, "Argument --Single-File must be followed by a file path. ");
                    }
                }
                else if ("--template".Equals(a.Current, StringComparison.OrdinalIgnoreCase))
                {
                    a.ConsumeOne();
                    if (!string.IsNullOrEmpty(a.Next))
                    {
                        layer.TemplateFilePath = a.Next;
                        a.ConsumeOne();
                    }
                    else
                    {
                        interactor.WriteTaskError(this, "Argument --template must be followed by a file path. ");
                    }
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
                interactor.Out.WriteLine("    --Export <dir>        Exports the generated documentation to this directory");
                interactor.Out.WriteLine("    --Single-File <file>  Exports the generated documentation to a single file");
                interactor.Out.WriteLine("    --Template <file>     Specifies the HTML template file");
                interactor.Out.WriteLine("");
                Environment.Exit(1);
            }
            else
            {
            }

            // verify exports
            for (int e = 0; e < layer.Exports.Count; e++)
            {
                var export = layer.Exports[e];
                bool directoryMayExist = false;
                var dir = export.Directory;
                while (dir != null && !dir.Equals(dir.Parent))
                {
                    if (dir.Exists)
                    {
                        directoryMayExist = true;
                        break;
                    }

                    dir = dir.Parent;
                }

                if (!directoryMayExist)
                {
                    interactor.WriteTaskError(this, "Cannot find part of the export directory \"" + export.Directory + "\". ");
                }
            }

            // fill layer with files
            // recursive inventory of files from given folders
            foreach (var dir in directories)
            {
                var root1 = new string[] { dir.Name, };
                this.ExpandDirectoryToFiles(dir, layer, root1);
            }

            // extra CLI file paths
            foreach (var file in files)
            {
                var item = layer.AddFile(file, true);
                item.RelativePath = new string[] { file.Name, };
            }

            base.Visit(context);
        }

        protected override void Populate(PackageContext context)
        {
            var interactor = context.RequireSingleLayer<IInteractor>();

            this.OrdererTaskLayer.Tasks.Add(new SimpleMarkdownToHtmlTask(MarkdownLayerKey));
            this.OrdererTaskLayer.Tasks.Add(new ExportMarkdownToHtmlTask("ExportMarkdownToHtml", MarkdownLayerKey));
            this.OrdererTaskLayer.Tasks.Add(new CombineMarkdownToHtmlTask("CombineMarkdownToHtmlTask", MarkdownLayerKey));
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
                var interactor = context.RequireSingleLayer<IInteractor>();
                interactor.Out.WriteLine(string.Empty);
                interactor.WriteTaskError(this, "Not running. ");
                interactor.Out.WriteLine(string.Empty);
            }
        }

        private void ExpandDirectoryToFiles(DirectoryInfo directory, SimpleMarkdownToHtmlLayer layer, string[] path)
        {
            // files
            foreach (var file in directory.GetFiles("*.md", SearchOption.TopDirectoryOnly))
            {
                var item = layer.AddFile(file, true);
                item.IsMarkdown = true;
                item.RelativePath = SimpleMarkdownToHtmlTask.GetRelativePath(path, file.Name);
            }

            // child directories
            foreach (var dir in directory.GetDirectories())
            {
                this.ExpandDirectoryToFiles(dir, layer, SimpleMarkdownToHtmlTask.GetRelativePath(path, dir.Name));
            }
        }
    }
}
