using System.Globalization;
using System.Text;

namespace SourceToAI.CLI.Services.Export;

public sealed class MultiViewReadmeMarkdownGenerator : IMultiViewReadmeMarkdownGenerator
{
    public string Generate(string repositoryRootFolderName, DateTimeOffset generatedAtUtc)
    {
        var stamp = generatedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.AppendLine($"# SourceToAI — Globaler Export: {repositoryRootFolderName}");
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
        sb.AppendLine("| `readme.md` | Diese Übersicht; globale Orientierung im Export-Baum. |");
        sb.AppendLine("| `Isolated/<Solution>/dependency-graph.md` | **Solution-Ebene** (isoliert): Architektur-Überblick mit csproj-Abhängigkeiten und NuGet-Paketen **ohne** eingebetteten Quellcode. |");
        sb.AppendLine("| `Merged/<view>/<Solution>.<Projekt>-<view>.md` | Alle Projekte aller Solutions nach View sortiert — ideal für KI-Prompts über den gesamten Workspace. |");
        sb.AppendLine("| `Isolated/<Solution>/<view>/<Solution>.<Projekt>-<view>.md` | Klassische Gruppierung nach Solution — exakt identischer Inhalt wie im `Merged/`-Baum, jedoch für lösungsspezifische Prompts. |");
        sb.AppendLine();
        sb.AppendLine("Dabei entspricht `<view>` einer der folgenden Sichten:");
        sb.AppendLine("- `complete`: **Vollständiger** Stand pro Projekt inkl. Nicht-`.cs`-Dateien (1:1-Texte). **Solution-Dokumentation** (Root, `.cursor`, …) liegt als `<Solution>..Docs-complete.md` (Projektname `.Docs`) in diesem Ordner.");
        sb.AppendLine("- `signatures-only`: Nur Signaturen — wenig Tokens, Schnittstellen & Typen für Architekturfragen.");
        sb.AppendLine("- `public-only`: Öffentliche API inkl. Bodies — wenn Implementierungsdetails **nur** für `public`/`protected` relevant sein sollen.");
        sb.AppendLine("- `dto-only`: Datenmodelle (DTOs, Records, Enums) — Domain- und API-Verträge ohne Service-Logik.");
        sb.AppendLine();
        sb.AppendLine("## Hinweise");
        sb.AppendLine();
        sb.AppendLine("- Pro **Projekt** (und virtuellem `.Docs`) existiert **höchstens eine** Markdown-Datei pro View-Ordner; leere Kombinationen werden nicht als Stub-Datei geschrieben. Orchestrierung: alle Views nacheinander aus derselben geparsten Quelle.");
        sb.AppendLine("- C#-Dateien werden pro Lauf nur einmal eingelesen und für alle Sichten aus demselben AST abgeleitet (Performance).");
        sb.AppendLine("- Die Markdown-Datei zu einem Projekt ist in `Isolated/` und `Merged/` inhaltlich exakt identisch (Dual Write), um je nach Anforderung den perfekten Prompt-Kontext zu bieten.");
        return sb.ToString();
    }
}
