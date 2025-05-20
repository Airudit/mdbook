
using Airudit.MdBook.Core;
using Airudit.MdBook.Core.Internals;
using EA4T.SteadyBear.Packager;

var context = new PackageContext();

context.AddLayer(new CommandLineLayer(Console.Out, Console.Error, Console.In, args));

var tasks = new List<ITask>();
tasks.Add(new MarkdownToHtmlMainTask());
tasks.Add(new SimpleMarkdownToHtmlTask());
tasks.Add(new ExportMarkdownToHtmlTask());
tasks.Add(new CombineMarkdownToHtmlTask());
tasks.ForEach(t => t.Visit(context));
tasks.ForEach(t => t.Verify(context));
tasks.ForEach(t => t.Run(context));
