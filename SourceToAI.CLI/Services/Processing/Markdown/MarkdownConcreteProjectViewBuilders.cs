using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.Markdown;

public sealed class CompleteMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileTypeService,
        viewGenerators,
        viewKey: "complete",
        includeNonCSharpFiles: true,
        passOriginalSourceTextForCSharp: true);

public sealed class SignaturesOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileTypeService,
        viewGenerators,
        viewKey: "signatures-only",
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);

public sealed class PublicOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileTypeService,
        viewGenerators,
        viewKey: "public-only",
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);

public sealed class DtoOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileTypeService,
        viewGenerators,
        viewKey: "dto-only",
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);
