using System.Threading.Tasks;

namespace SourceToAI.CLI.Services.Integration;

public interface IGoogleDriveClient
{
    Task ReplaceSolutionFolderAsync(string targetRootFolder, string solutionName, string localDirectory);
}