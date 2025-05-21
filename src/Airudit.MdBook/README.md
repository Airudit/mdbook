
dotnet mdbook
========================

This dotnet tool will turn a collection a markdown files into a book.

## Install

Make a project local install with:

```console
# create a tool manifest file for your project
dotnet new tool-manifest

# verify
cat .config/dotnet-tools.json

# install
dotnet tool install Airudit.MdBook

# verify
cat .config/dotnet-tools.json

# verify command
dotnet mdbook --help
```

## Restore

If the tool il already installed in your project, you need to restore the tools using

```console
dotnet tool restore
```

## Use

Now you can use the command in your build process

```console
dotnet mdbook help/ README.md
```

Usage:

```
MarkdownToHtml command usage: 
    {file path}+ [options]

Options: 
    --Export <dir>        Exports the generated documentation to this directory
    --Single-File <file>  Exports the generated documentation to a single file
    --Template <file>     Specifies the HTML template file
```

## More info

[Airudit.MdBook](https://github.com/Airudit/mdbook) project

[Tutorial: Install and use a .NET local tool using the .NET CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use)
