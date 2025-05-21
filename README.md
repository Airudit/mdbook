
mdbook
=========

turns a collection a markdown files into a (digital) book

Machine install (global)
------------------------------------

Install command:

```
dotnet tool install -g Airudit.MdBook
```

> You can invoke the tool using the following command: mdbook
Tool 'airudit.mdbook' (version '0.1.2') was successfully installed.

Use command:

```
mdbook --help
```

> Displaying help.  
>
> The `MarkdownToHtml` command will generate HTML files for each specified markdown file.
>
> MarkdownToHtml command usage:  
> &gt; {file path}+ [options]
>
> Options:  
> --Export &lt;dir&gt;        Exports the generated documentation to this directory  
> --Single-File &lt;file&gt;  Exports the generated documentation to a single file  
> --Template &lt;file&gt;     Specifies the HTML template file  
> --Copyright &lt;str&gt;     Specifies a copyright notice  


Use during CI
------------------------------------

In your repository: make a project local install with:

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

During your CI, restore the tools:

```console
dotnet tool restore
```

Now you can use the command in your build process

```console
dotnet mdbook help/ README.md
```



