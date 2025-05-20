
namespace Airudit.MdBook.Core
{
    using Airudit.MdBook.Core.Internals;
    using EA4T.SteadyBear.Packager;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Exports the files handled by the <see cref="SimpleMarkdownToHtmlTask"/>.
    /// </summary>
    public sealed class ExportMarkdownToHtmlTask : ITask
    {
        private readonly string parentTaskKey;
        private SimpleMarkdownToHtmlLayer layer;

        public ExportMarkdownToHtmlTask()
        {
        }

        public string Name => nameof(ExportMarkdownToHtmlTask);

        public string Key { get; }

        public void Visit(PackageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            this.layer = context.RequireSingleLayer<SimpleMarkdownToHtmlLayer>();
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

            var interactor = context.RequireSingleLayer<CommandLineLayer>();
            var errors = 0;

            // for each export order
            for (int e = 0; e < this.layer.Exports.Count; e++)
            {
                var export = this.layer.Exports[e];
                interactor.Out.WriteLine("Exporting to: " + export.Directory.FullName);

                // create directory location: create missing folders in path
                CreateFilesystemFolderPath(export.Directory);

                // copy output files
                for (int f = 0; f < this.layer.Items.Count; f++)
                {
                    var item = this.layer.Items[f];
                    if (item != null && item.TargetFile != null && item.TargetFile.Exists && item.RelativePath != null)
                    {
                        // for current file, define export directory by (export dir + file relative dir)
                        var path = new string[item.RelativePath.Length];
                        path[0] = export.Directory.FullName;
                        Array.Copy(item.RelativePath, 0, path, 1, item.RelativePath.Length - 1);
                        var fileExportDirectoryPath = Path.Combine(path);
                        var directory = new DirectoryInfo(fileExportDirectoryPath);

                        // create directory and copy file
                        CreateFilesystemFolderPath(directory);
                        var fileExportPath = Path.Combine(directory.FullName, item.TargetFile.Name);
                        File.Copy(item.TargetFile.FullName, fileExportPath, true);
                    }
                    else
                    {
                        // missing data: non-exportable
                    }
                }
            }
        }

        internal static void CreateFilesystemFolderPath(DirectoryInfo folder)
        {
            // create directory location: determine each folder in path
            var dir = folder;
            var directoriesFromRoot = new List<DirectoryInfo>();
            while (dir != null && !dir.Equals(dir.Parent))
            {
                directoriesFromRoot.Insert(0, dir);
                dir = dir.Parent;
            }

            // create directory location: create missing folders in path
            for (int d = 1; d < directoriesFromRoot.Count; d++)
            {
                if (!directoriesFromRoot[d].Exists)
                {
                    directoriesFromRoot[d].Create();
                }
            }
        }
    }
}
