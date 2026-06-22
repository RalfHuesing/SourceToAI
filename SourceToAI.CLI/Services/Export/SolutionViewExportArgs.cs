using System;
using System.Collections.Generic;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Argument-Bündel für <see cref="IMultiViewExportService.WriteMergedSolutionViews"/>.
/// </summary>
public sealed record SolutionViewExportArgs(
    string OutputRoot,
    string SolutionDisplayName,
    string SolutionRootPath,
    Guid SessionId,
    DateTimeOffset Generated,
    IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> ProjectsWithFiles,
    IReadOnlyList<string>? SolutionDocumentationAbsolutePaths,
    IReadOnlyList<(string DirectoryName, IReadOnlyList<string> AbsoluteFilePaths)> UnmappedDirectories);
