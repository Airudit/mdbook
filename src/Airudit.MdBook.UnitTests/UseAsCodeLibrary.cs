namespace Airudit.MdBook.UnitTests;

using Airudit.MdBook.Core;
using System.Text;

public class UseAsCodeLibrary
{
    [Fact]
    public void Demo()
    {
        var inputPath = GetLocalFilePath("sample1.md");
        var outputPath = GetLocalFilePath("sample1.md.html");

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        // demo code from the README file
        SimpleMdToHtml(inputPath, null);

        // verify
        Assert.True(File.Exists(outputPath));
        var contents = File.ReadAllText(outputPath, Encoding.UTF8);
        Assert.Contains("<html", contents);
        Assert.Contains("<h1 id=\"title\">Title</h1>", contents);

        File.Delete(outputPath);
    }

    private static string GetLocalFilePath(string path)
    {
        return Path.Combine(Environment.CurrentDirectory, "../../..", path);
    }

    /// <summary>
    /// Create an HTML from Markdown path file
    /// </summary>
    public static void SimpleMdToHtml(string inputFilePath, string? templateFilePath)
    {
        // configure
        var sourceFile = new FileInfo(inputFilePath);
        var layer = new SimpleMarkdownToHtmlLayer();
        layer.AddFile(sourceFile, true);
        layer.TemplateFilePath = templateFilePath;

        // prepare stack
        var context = new PackageContext();
        context.AddLayer(layer);
        var simpleConverterTask = new SimpleMarkdownToHtmlTask();
        simpleConverterTask.Visit(context);

        // this generates the file
        simpleConverterTask.Run(context);
    }

}
