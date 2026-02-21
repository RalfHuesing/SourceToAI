using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceToAI.CLI.Configuration;

public class AppSettings
{
    public string OutputRootDirectory { get; set; } = string.Empty;
    public string[] ExcludedDirectories { get; set; } = [];
    public string[] IncludedExtensions { get; set; } = [];
}