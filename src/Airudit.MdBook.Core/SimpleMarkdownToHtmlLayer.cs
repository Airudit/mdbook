
namespace Airudit.MdBook.Core
{
    using Markdig;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Globalization;

    /// <summary>
    /// MD to HTML main layer.
    /// </summary>
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

        /// <summary>
        /// The contents of the HTML template file.
        /// </summary>
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

        /// <summary>
        /// With <see cref="SingleFile"/>, emit one combined file per detected language instead of
        /// one merged file. The output path's <c>{lang}</c> placeholder (or an inserted
        /// <c>.{lang}</c>) is substituted per language. Set by --bylang. See issue #22.
        /// </summary>
        public bool ByLang { get; set; }

        /// <summary>
        /// Whether to inline local images referenced by <c>![alt](path)</c> as <c>data:</c> URIs,
        /// making the output self-contained (issues #13, #21). Off by default; turned on by
        /// --Embed and, unless --No-Embed is given, automatically alongside --Single-File.
        /// </summary>
        public bool Embed { get; set; }

        /// <summary>
        /// Whether to syntax-highlight fenced code blocks whose language ColorCode recognises,
        /// emitting classed token spans coloured by the template's <c>&lt;style&gt;</c> (issue #25).
        /// On by default; turned off by --No-Highlight. Unknown/absent languages are unaffected.
        /// </summary>
        public bool Highlight { get; set; } = true;

        /// <summary>
        /// Which provider renders diagram fences (mermaid, plantuml, …) to inline SVG. None by default
        /// (diagram fences stay plain and a one-shot warning is shown); set by --Diagrams. See issue #5.
        /// </summary>
        public DiagramProviderKind Diagrams { get; set; } = DiagramProviderKind.None;

        /// <summary>
        /// Base URL of the Kroki server used when <see cref="Diagrams"/> is
        /// <see cref="DiagramProviderKind.Kroki"/>; set by --KrokiUrl. Ignored otherwise.
        /// </summary>
        public string? KrokiUrl { get; set; }

        /// <summary>
        /// The colour scheme the active template declares (via a <c>&lt;!-- mdbook:color-scheme … --&gt;</c>
        /// marker), used to pick a matching diagram theme. Light unless the template says otherwise.
        /// </summary>
        public DiagramColorScheme ColorScheme { get; set; } = DiagramColorScheme.Light;

        /// <summary>
        /// Collects diagram fences left unrendered during a run, so a single setup/failure warning can
        /// be emitted at the end. Populated by the diagram renderer; read by the render task.
        /// </summary>
        public DiagramWarningSink DiagramWarnings { get; } = new DiagramWarningSink();

        /// <summary>
        /// Whether to write each page's HTML in place, next to its source file. On by default;
        /// turned off when an output destination (--single-file or --export) is given without
        /// an explicit --side request. See <see cref="CommandLineMarkdownToHtmlPrepareTask"/>.
        /// </summary>
        public bool SideBySide { get; set; } = true;

        /// <summary>
        /// Whether to print the per-page "Processing markdown file" trace. Off by default so a
        /// run stays quiet apart from high-level summaries; turned on by --verbose / -v.
        /// </summary>
        public bool Verbose { get; set; }

        public string TemplateFilePath { get; set; }
        
        public string? Copyright { get; set; }

        public SimpleMarkdownToHtmlLayerItem AddFile(FileInfo sourceFile, bool isMarkdown)
        {
            if (sourceFile == null)
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

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
        public FileInfo SourceFile { get; internal set; } = null!;

        public FileInfo TargetFile { get; internal set; } = null!;
        public bool IsMarkdown { get; set; }

        public string[]? RelativePath { get; set; }
        public string? HtmlContents { get; set; }

        /// <summary>
        /// True when the file was named directly on the command line (not discovered by a
        /// directory scan). Such a page is always rendered, even if another page also pulls it
        /// in with <c>{{include}}</c> (which otherwise demotes a file to a non-rendered partial).
        /// </summary>
        public bool ExplicitlyListed { get; set; }

        /// <summary>
        /// The page's first level-1 heading text, if any. Used as its table-of-contents label
        /// in a <c>--single-file</c> bundle, falling back to the file name when absent.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The page's stable, path-based anchor slug within a <c>--single-file</c> bundle
        /// (e.g. "guide/intro.md" -> "guide-intro"). Assigned by
        /// <see cref="CombineMarkdownToHtmlTask"/>; used as the page's <c>&lt;article id&gt;</c>,
        /// its table-of-contents target, and the destination of cross-page links. Null when
        /// not combining into a single file.
        /// </summary>
        public string? Anchor { get; set; }

        /// <summary>
        /// The full templated HTML page for this file, kept in memory so it can be exported
        /// even when the in-place side-by-side file is not written.
        /// </summary>
        public string? RenderedPage { get; set; }
        public CultureInfo? Lang { get; set; }
    }

    public sealed class SimpleMarkdownToHtmlLayerExport
    {
        public DirectoryInfo Directory { get; set; } = null!;
    }
}
