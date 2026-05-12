using SourceToAI.CLI.Services.IO;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.Markdown;

public sealed class CompleteMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileReader fileReader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileReader,
        fileTypeService,
        viewGenerators,
        viewKey: "complete",
        includeNonCSharpFiles: true,
        passOriginalSourceTextForCSharp: true);

public sealed class SignaturesOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileReader fileReader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileReader,
        fileTypeService,
        viewGenerators,
        viewKey: "signatures-only",
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);

public sealed class PublicOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileReader fileReader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileReader,
        fileTypeService,
        viewGenerators,
        viewKey: "public-only",
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);

public sealed class DtoOnlyMarkdownProjectViewBuilder(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileReader fileReader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators)
    : MarkdownProjectViewBuilderBase(
        csharpDocumentLoader,
        fileReader,
        fileTypeService,
        viewGenerators,
        viewKey: "dto-only",
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);
