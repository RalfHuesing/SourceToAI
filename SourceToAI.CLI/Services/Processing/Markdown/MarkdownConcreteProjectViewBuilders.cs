using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.Markdown;

public sealed class CompleteMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    [FromKeyedServices(MarkdownViewKeys.Complete)] IViewGenerator viewGenerator)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileTypeService,
        viewGenerator,
        includeNonCSharpFiles: true,
        passOriginalSourceTextForCSharp: true);

public sealed class SignaturesOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    [FromKeyedServices(MarkdownViewKeys.SignaturesOnly)] IViewGenerator viewGenerator)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileTypeService,
        viewGenerator,
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);

public sealed class PublicOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    [FromKeyedServices(MarkdownViewKeys.PublicOnly)] IViewGenerator viewGenerator)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileTypeService,
        viewGenerator,
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);

public sealed class DtoOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    [FromKeyedServices(MarkdownViewKeys.DtoOnly)] IViewGenerator viewGenerator)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileTypeService,
        viewGenerator,
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);
