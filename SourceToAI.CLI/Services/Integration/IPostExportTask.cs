using System.Threading.Tasks;

namespace SourceToAI.CLI.Services.Integration;

public interface IPostExportTask
{
    Task ExecuteAsync(string solutionName, string outputDirectory);
}
