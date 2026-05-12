using System.Globalization;
using System.Text;

namespace SourceToAI.CLI.Services.Export;

public sealed class MultiViewReadmeMarkdownGenerator : IMultiViewReadmeMarkdownGenerator
{
    public string Generate(string repositoryRootFolderName, DateTimeOffset generatedAtUtc)
    {
        var stamp = generatedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.AppendLine($"# SourceToAI — Export: {repositoryRootFolderName}");
        sb.AppendLine();
        sb.AppendLine("## Meta");
        sb.AppendLine();
        sb.AppendLine($"- **Repository-Ordnername (laut Konzept):** `{repositoryRootFolderName}`");
        sb.AppendLine($"- **Generiert (UTC):** `{stamp}`");
        sb.AppendLine();
        sb.AppendLine("## Aufbau einer Datei pro Projekt (KI-Kontext)");
        sb.AppendLine();
        sb.AppendLine("Jede `.md` direkt unter einem **View-Ordner** (`complete/`, `signatures-only/`, `public-only/`, `dto-only/`) beschreibt **genau ein** exportiertes Projekt — inklusive YAML-Frontmatter, **MANIFEST**-Tabelle (IDs, Typ, Hash, Größe, Pfade relativ zum jeweiligen Projektroot) und **CONTENT**-Abschnitten mit denselben IDs. Prompts können Manifest-Zeilen und eingebettete Dateien konsistent referenzieren.");
        sb.AppendLine();
        sb.AppendLine("## Dateien und Ordner (Prompt-Use-Cases)");
        sb.AppendLine();
        sb.AppendLine("| Pfad | Wann nutzen? |");
        sb.AppendLine("|:---|:---|");
        sb.AppendLine("| `readme.md` | Diese Übersicht; Orientierung im Export-Baum. |");
        sb.AppendLine("| `dependency-graph.md` | **Solution-Ebene** (gleiche Verzeichnisebene wie diese `readme.md`): Architektur-Überblick mit csproj-Abhängigkeiten und NuGet-Paketen **ohne** eingebetteten Quellcode — nicht pro View dupliziert. |");
        sb.AppendLine("| `complete/<Solution>.<Projekt>.md` | **Vollständiger** Stand pro Projekt inkl. Nicht-`.cs`-Dateien (1:1-Texte). **Solution-Dokumentation** (Root, `.cursor`, …) liegt als `<Solution>..Docs.md` (Projektname `.Docs`) in diesem Ordner. Weitere Dateinamen: Solution- und Projektname, sanitisiert — siehe Export auf der Platte. |");
        sb.AppendLine("| `signatures-only/<Solution>.<Projekt>.md` | Nur Signaturen — wenig Tokens, Schnittstellen & Typen für Architekturfragen. |");
        sb.AppendLine("| `public-only/<Solution>.<Projekt>.md` | Öffentliche API inkl. Bodies — wenn Implementierungsdetails **nur** für `public`/`protected` relevant sein sollen. |");
        sb.AppendLine("| `dto-only/<Solution>.<Projekt>.md` | Datenmodelle (DTOs, Records, Enums) — Domain- und API-Verträge ohne Service-Logik. |");
        sb.AppendLine();
        sb.AppendLine("## Hinweise");
        sb.AppendLine();
        sb.AppendLine("- Pro **Projekt** (und virtuellem `.Docs`) existiert **höchstens eine** Markdown-Datei pro View-Ordner; leere Kombinationen werden nicht als Stub-Datei geschrieben. Orchestrierung: alle Views nacheinander aus derselben geparsten Quelle.");
        sb.AppendLine("- C#-Dateien werden pro Lauf nur einmal eingelesen und für alle Sichten aus demselben AST abgeleitet (Performance).");
        return sb.ToString();
    }
}
