
using Airudit.MdBook.Core;

var context = new PackageContext();
context.AddLayer(new CommandLineLayer(Console.Out, Console.Error, Console.In, args));

var tasks = new List<ITask>();
tasks.Add(new CommandLineMarkdownToHtmlPrepareTask()); // parse CLI args
tasks.Add(new SimpleMarkdownToHtmlTask()); // parse and convert docs (in-memory)
tasks.Add(new ExportMarkdownToHtmlTask()); // write files
tasks.Add(new CombineMarkdownToHtmlTask()); // merge and write single file
tasks.ForEach(t => t.Visit(context));
tasks.ForEach(t => t.Verify(context));
tasks.ForEach(t => t.Run(context));
