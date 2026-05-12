using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.Export;

public class DependencyGraphMarkdownGeneratorTests
{
    private readonly CsprojDependencyGraphMarkdownGenerator _sut = new();

    [Fact]
    public void Generate_sdk_style_csproj_lists_packages_and_project_reference_paths()
    {
        using var ws = new TempWorkspace();
        var libCsproj = ws.WriteFile(
            "Lib/Lib.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
            </Project>
            """);

        var appCsproj = ws.WriteFile(
            "App/App.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="OptionalVersionPkg" />
                <ProjectReference Include="..\\Lib\\Lib.csproj" />
              </ItemGroup>
            </Project>
            """);

        var projects = new[]
        {
            new ProjectDefinition("App", appCsproj),
            new ProjectDefinition("Lib", libCsproj),
        };

        var result = _sut.Generate(ws.Root, projects);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;
        Assert.Contains("Newtonsoft.Json", md, StringComparison.Ordinal);
        Assert.Contains("13.0.3", md, StringComparison.Ordinal);
        Assert.Contains("OptionalVersionPkg", md, StringComparison.Ordinal);
        Assert.Contains("Lib/Lib.csproj", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_legacy_msbuild_namespace_reads_package_and_project_references()
    {
        using var ws = new TempWorkspace();
        var other = ws.WriteFile(
            "Other/Other.csproj",
            """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemGroup>
                <PackageReference Include="LegacyPkg" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        var main = ws.WriteFile(
            "Main/Main.csproj",
            """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemGroup>
                <ProjectReference Include="..\Other\Other.csproj" />
              </ItemGroup>
            </Project>
            """);

        var projects = new[]
        {
            new ProjectDefinition("Main", main),
            new ProjectDefinition("Other", other),
        };

        var result = _sut.Generate(ws.Root, projects);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;
        Assert.Contains("LegacyPkg", md, StringComparison.Ordinal);
        Assert.Contains("2.0.0", md, StringComparison.Ordinal);
        Assert.Contains("Other/Other.csproj", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_reads_version_from_child_element_when_attribute_absent()
    {
        using var ws = new TempWorkspace();
        var path = ws.WriteFile(
            "Pkg/Pkg.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="ChildVersioned">
                  <Version>3.1.4</Version>
                </PackageReference>
              </ItemGroup>
            </Project>
            """);

        var result = _sut.Generate(ws.Root, [new ProjectDefinition("Pkg", path)]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Contains("ChildVersioned", result.Value!, StringComparison.Ordinal);
        Assert.Contains("3.1.4", result.Value!, StringComparison.Ordinal);
    }
}
