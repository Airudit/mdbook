
namespace EA4T.SteadyBear.Packager
{
    using Airudit.MdBook.Core;
    using Airudit.MdBook.Core.Internals;
    using Somewhere;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Main command. Command to convert some markdown files to HTML (no packaging involved).
    /// </summary>
    public sealed class MarkdownToHtmlMainTask : ITask
    {
        private const string MarkdownLayerKey = "MarkdownToHtml";

        public MarkdownToHtmlMainTask()
        {
        }

        public void Visit(PackageContext context)
        {
            var interactor = context.RequireSingleLayer<CommandLineLayer>();

            var layer = new SimpleMarkdownToHtmlLayer();
            layer.Key = MarkdownLayerKey;
            context.AddLayer(layer);

            // parse console arguments
            var isHelp = false;
            var errors = new List<string>();
            var files = new List<FileInfo>();
            var directories = new List<DirectoryInfo>();
            var args = new ParseArgs(interactor.Arguments);
            while (args.MoveNext())
            {
                string arg;
                if (args.Is(arg = "--help"))
                {
                    isHelp = true;
                }
                else if (args.Is(arg = "--export"))
                {
                    if (args.Has(1))
                    {
                        args.MoveNext();
                        var export = new SimpleMarkdownToHtmlLayerExport();
                        export.Directory = new DirectoryInfo(args.Current);
                        layer.Exports.Add(export);
                    }
                    else
                    {
                        errors.Add("Argument " + arg + " must be followed by a directory path. ");
                    }
                }
                else if (args.Is(arg = "--single-file"))
                {
                    if (args.Has(1))
                    {
                        args.MoveNext();
                        layer.SingleFile = args.Current;
                    }
                    else
                    {
                        errors.Add("Argument " + arg + " must be followed by a file path. ");
                    }
                }
                else if (args.Is(arg = "--template"))
                {
                    if (args.Has(1))
                    {
                        args.MoveNext();
                        layer.TemplateFilePath = args.Current;
                    }
                    else
                    {
                        errors.Add("Argument " + arg + " must be followed by a file path. ");
                    }
                }
                else
                {
                    // extra values???
                    if (Directory.Exists(args.Current))
                    {
                        directories.Add(new DirectoryInfo(args.Current));
                    }
                    else if (File.Exists(args.Current))
                    {
                        files.Add(new FileInfo(args.Current));
                    }
                    else
                    {
                        errors.Add("Unknown argumeent \"" + args.Current + "\". ");
                    }
                }
            }

            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    interactor.ErrorOut.WriteLine(error);
                }
            }

            if (isHelp)
            {
                interactor.Out.WriteLine("Displaying help. ");
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
                Environment.Exit(0);
            }
            else
            {
            }

            if (errors.Any())
            {
                Environment.Exit(1);
                return;
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
                    interactor.ErrorOut.WriteLine("Cannot find part of the export directory \"" + export.Directory + "\". ");
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
        }

        public void Verify(PackageContext context)
        {
        }

        public void Run(PackageContext context)
        {
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
