
namespace Airudit.MdBook.Core
{
    using Airudit.MdBook.Core.Internals;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Main command. Command to convert some markdown files to HTML (no packaging involved).
    /// </summary>
    public sealed class CommandLineMarkdownToHtmlPrepareTask : ITask
    {
        private const string MarkdownLayerKey = "MarkdownToHtml";

        public CommandLineMarkdownToHtmlPrepareTask()
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
            var sideExplicit = false;
            var errors = new List<string>();
            var inputs = new List<FileSystemInfo>();
            using var args = new ParseArgs(interactor.Arguments);
            while (args.MoveNext())
            {
                string arg;
                if (args.Is(arg = "--help"))
                {
                    isHelp = true;
                }
                else if (args.Is(arg = "--side"))
                {
                    sideExplicit = true;
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
                else if (args.Is(arg = "--copyright"))
                {
                    if (args.Has(1))
                    {
                        args.MoveNext();
                        layer.Copyright = args.Current;
                    }
                    else
                    {
                        errors.Add("Argument " + arg + " must be followed by a file path. ");
                    }
                }
                else
                {
                    // extra values: input files and directories, kept in command-line order
                    if (Directory.Exists(args.Current))
                    {
                        inputs.Add(new DirectoryInfo(args.Current));
                    }
                    else if (File.Exists(args.Current))
                    {
                        inputs.Add(new FileInfo(args.Current));
                    }
                    else
                    {
                        errors.Add("Unknown argument \"" + args.Current + "\". ");
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
                interactor.Out.WriteLine("Airudit.MdBook – Usage");
                interactor.Out.WriteLine("");
                interactor.Out.WriteLine("This command will generate HTML files for each specified markdown file. ");
                interactor.Out.WriteLine("");
                interactor.Out.WriteLine("command usage: ");
                interactor.Out.WriteLine("    mdbook {file path}+ [options]");
                interactor.Out.WriteLine("");
                interactor.Out.WriteLine("Options: ");
                interactor.Out.WriteLine("    --Export <dir>        Exports the generated documentation to this directory");
                interactor.Out.WriteLine("    --Single-File <file>  Exports the generated documentation to a single file");
                interactor.Out.WriteLine("    --Side                Also writes each page's HTML next to its source file.");
                interactor.Out.WriteLine("                          These in-place files are written by default, but are");
                interactor.Out.WriteLine("                          suppressed once --Single-File or --Export is given; pass");
                interactor.Out.WriteLine("                          --Side to keep writing them as well.");
                interactor.Out.WriteLine("    --Template <file>     Specifies the HTML template file path");
                interactor.Out.WriteLine("    --Copyright <str>     Specifies a copyright notice");
                interactor.Out.WriteLine("");
                interactor.Out.WriteLine("Built-in templates:");
                interactor.Out.WriteLine("    --Template builtin:default.light.html");
                interactor.Out.WriteLine("    --Template builtin:default.dark.html");
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

            // Side-by-side in-place files are the default output only when no other destination
            // is given. Any explicit destination (--single-file or --export) suppresses them,
            // unless --side is passed to keep writing them as well.
            layer.SideBySide = sideExplicit || (layer.SingleFile == null && layer.Exports.Count == 0);

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

            // Fill the layer with files in the order given on the command line, de-duplicated by
            // full path (first occurrence wins) so a file named both explicitly and inside a
            // listed folder is emitted once, at its first position (issue #4).
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var input in inputs)
            {
                if (input is DirectoryInfo dir)
                {
                    this.ExpandDirectoryToFiles(dir, layer, new string[] { dir.Name, }, seen);
                }
                else if (input is FileInfo file && seen.Add(file.FullName))
                {
                    var item = layer.AddFile(file, true);
                    item.RelativePath = new string[] { file.Name, };
                }
            }
        }

        public void Verify(PackageContext context)
        {
        }

        public void Run(PackageContext context)
        {
        }

        private void ExpandDirectoryToFiles(DirectoryInfo directory, SimpleMarkdownToHtmlLayer layer, string[] path, HashSet<string> seen)
        {
            // files: README first, then Index, then the rest alphabetically (deterministic order)
            var mdFiles = directory.GetFiles("*.md", SearchOption.TopDirectoryOnly);
            Array.Sort(mdFiles, CompareByHoistThenName);
            foreach (var file in mdFiles)
            {
                if (seen.Add(file.FullName))
                {
                    var item = layer.AddFile(file, true);
                    item.IsMarkdown = true;
                    item.RelativePath = SimpleMarkdownToHtmlTask.GetRelativePath(path, file.Name);
                }
            }

            // child directories, alphabetically
            var subDirectories = directory.GetDirectories();
            Array.Sort(subDirectories, (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            foreach (var dir in subDirectories)
            {
                this.ExpandDirectoryToFiles(dir, layer, SimpleMarkdownToHtmlTask.GetRelativePath(path, dir.Name), seen);
            }
        }

        // Orders directory files: README before Index before everything else, then by name.
        private static int CompareByHoistThenName(FileInfo a, FileInfo b)
        {
            var rankA = HoistRank(a.Name);
            var rankB = HoistRank(b.Name);
            if (rankA != rankB)
            {
                return rankA - rankB;
            }

            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        // README-family files sort first (rank 0), Index-family next (rank 1), the rest last (2).
        // Matches on the leading name segment so localized variants (README.en.md) hoist too.
        private static int HoistRank(string fileName)
        {
            var head = fileName.Split('.')[0];
            if (string.Equals(head, "readme", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(head, "index", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return 2;
        }
    }
}
