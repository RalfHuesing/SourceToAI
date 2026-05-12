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
        relativeOutputFile: "complete/full-source.md",
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
        relativeOutputFile: "signatures-only/signatures.md",
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
        relativeOutputFile: "public-only/public-api.md",
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
        relativeOutputFile: "dto-only/models.md",
        includeNonCSharpFiles: false,
        passOriginalSourceTextForCSharp: false);
