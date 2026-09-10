Releasing
=========

For maintainers. Publishing is handled by the `publish` GitHub Actions workflow
(`.github/workflows/publish.yml`), triggered when a GitHub Release is published.

**What gets published:**

- `Airudit.MdBook` NuGet package (dotnet global tool) → nuget.org
- `Airudit.MdBook.Core` NuGet package (code library) → nuget.org
- `mdbook-{version}-linux-x64.tar.gz` → attached to the GitHub Release
- `mdbook-{version}-win-x64.zip` → attached to the GitHub Release
- `mdbook-help-{version}.{lang}.{light,dark}.html` → this project's help guide rendered
  as single-file books, attached to the GitHub Release

**Steps to release:**

1. Push all changes to `main`.
2. Create and push a version tag: `git tag -a v1.2.3 -m "…" && git push origin v1.2.3`.
3. On GitHub, create a Release from that tag — this triggers the workflow.
4. The workflow builds, tests, and publishes everything automatically.

The version is derived from the git tag via [MinVer](https://github.com/adamralph/minver).
The tag must start with `v` (e.g. `v1.2.3`); a pre-release suffix (e.g. `v1.2.3-beta.1`)
publishes a prerelease and a prerelease NuGet package.

The binary release assets target `net8.0` and are framework-dependent (require .NET 8 on
the target machine). **Stable releases must carry the binaries.**

Required secret: `NUGETAIRUDIT` (NuGet API key with push rights).
