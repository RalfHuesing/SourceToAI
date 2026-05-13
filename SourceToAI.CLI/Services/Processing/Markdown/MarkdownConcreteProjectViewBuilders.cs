using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.Markdown;

public sealed class CompleteMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    [FromKeyedServices(MarkdownViewKeys.Complete)] IViewGenerator viewGenerator)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        viewGenerator,
        includeNonCSharpFiles: true,
        passOriginalSourceTextForCSharp: true);

public sealed class SignaturesOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    [FromKeyedServices(MarkdownViewKeys.SignaturesOnly)] IViewGenerator viewGenerator)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        viewGenerator,
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);

public sealed class PublicOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    [FromKeyedServices(MarkdownViewKeys.PublicOnly)] IViewGenerator viewGenerator)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        viewGenerator,
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);

public sealed class DtoOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    [FromKeyedServices(MarkdownViewKeys.DtoOnly)] IViewGenerator viewGenerator)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        viewGenerator,
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);
