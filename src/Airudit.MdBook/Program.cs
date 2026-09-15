
using Airudit.MdBook.Core;

var context = new PackageContext();
context.AddLayer(new CommandLineLayer(Console.Out, Console.Error, Console.In, args));

var tasks = new List<ITask>();
tasks.Add(new CommandLineMarkdownToHtmlPrepareTask()); // parse CLI args
tasks.Add(new SimpleMarkdownToHtmlTask()); // parse and convert docs (in-memory)
tasks.Add(new ExportMarkdownToHtmlTask()); // write files
tasks.Add(new CombineMarkdownToHtmlTask()); // merge and write single file

foreach (var task in tasks)
{
    await task.VisitAsync(context);
}

foreach (var task in tasks)
{
    await task.VerifyAsync(context);
}

foreach (var task in tasks)
{
    await task.RunAsync(context);
}
