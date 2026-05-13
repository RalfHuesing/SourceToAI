using System.Globalization;
using System.Text;

namespace SourceToAI.CLI.Services.Export;

public sealed class MultiViewReadmeMarkdownGenerator : IMultiViewReadmeMarkdownGenerator
{
    public string GenerateGlobalExportOverview(DateTimeOffset generatedAtUtc)
    {
        var stamp = generatedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.AppendLine("# SourceToAI — Export-Verzeichnis (KI-Orientierung)");
        sb.AppendLine();
        sb.AppendLine(
            "Dieses Verzeichnis ist ein **offline erzeugter KI-Feed** der CLI **SourceToAI**: strukturierte Markdown-Dateien mit YAML-Frontmatter, **MANIFEST** und **CONTENT** aus .NET-Quellcode (oder aus per Decompiler aufbereiteten Assemblies). Es ist **kein** Git-Repository, sondern ein **Snapshot** zum Einspeisen in LLM-Prompts oder zum Durchsuchen mit Werkzeugen.");
        sb.AppendLine();
        sb.AppendLine("## Meta");
        sb.AppendLine();
        sb.AppendLine($"- **Generiert (UTC):** `{stamp}`");
        sb.AppendLine();
        sb.AppendLine("## Ordner auf oberster Ebene");
        sb.AppendLine();
        sb.AppendLine(
            $"- **`{MultiViewExportPaths.IsolatedFolderName}/<Solution>/`** — alles **pro erkanntem Solution-Namen** gruppiert: View-Unterordner (`complete/`, …), `dependency-graph.md` und die **lösungsspezifische** `readme.md` mit Details zu MANIFEST/CONTENT und Dateinamensschema.");
        sb.AppendLine(
            $"- **`{MultiViewExportPaths.MergedFolderName}/<view>/`** — **alle** exportierten Projekte **aller** Quellen dieses Laufs nach View sortiert (ein flacher Baum pro Sicht), ideal wenn du **workspace-weit** arbeiten willst.");
        sb.AppendLine(
            $"- **`{MultiViewExportPaths.SafetyMarkerFileName}`** — Sicherheitsmarker der CLI; nicht löschen, wenn du den Exportordner wiederverwenden willst.");
        sb.AppendLine();
        sb.AppendLine("## Views (`<view>` in Pfaden und Dateinamen)");
        sb.AppendLine();
        sb.AppendLine(
            "Jede View ist ein anderer „Zoom“ auf dieselben geparsten C#-Dateien (pro Lauf ein Parse, mehrere Sichten):");
        sb.AppendLine();
        sb.AppendLine(
            $"- **`{MultiViewExportPaths.CompleteFolderName}`** — vollständiger Text pro Projekt inkl. Nicht-`.cs`-Dateien. Virtuelle Solution-Doku (Root-`README`, `.cursor/rules`, …) erscheint als `<Solution>..Docs-complete.md` (Projektname `.Docs`).");
        sb.AppendLine(
            $"- **`{MultiViewExportPaths.SignaturesOnlyFolderName}`** — nur Signaturen, wenig Tokens, gut für Architektur und Schnittstellen.");
        sb.AppendLine(
            $"- **`{MultiViewExportPaths.PublicOnlyFolderName}`** — öffentliche API inkl. Rümpfe, wo sinnvoll.");
        sb.AppendLine(
            $"- **`{MultiViewExportPaths.DtoOnlyFolderName}`** — DTOs, Records, Enums — Datenverträge ohne Service-Logik.");
        sb.AppendLine();
        sb.AppendLine("## Wo stehen die Details?");
        sb.AppendLine();
        sb.AppendLine(
            "Die **tiefe** Erklärung (Tabellen zu MANIFEST/CONTENT, Dual-Write `Isolated` vs. `Merged`, Hinweise pro Lösung) steht jeweils unter **`Isolated/<Solution>/readme.md`** — dort einsteigen, wenn du nur **eine** Solution im Blick hast.");
        sb.AppendLine();
        sb.AppendLine("## Navigation bei großen Exporten (`rg` / `grep`)");
        sb.AppendLine();
        sb.AppendLine(
            "Der Baum kann **sehr viele** und **große** `.md`-Dateien enthalten. Lade nicht alles blind in einen Chat — nutze **ripgrep** (`rg`) oder **grep**, um gezielt Stellen zu finden:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Symbol oder Text in allen Feeds suchen (Beispiel)");
        sb.AppendLine("rg -n \"MyTypeName\" Merged/complete");
        sb.AppendLine();
        sb.AppendLine("# Nach einem bestimmten Projekt-Stamm im Dateinamen filtern");
        sb.AppendLine("rg --files -g \"*MyProject*\" Merged/complete");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine(
            "Auf Windows steht `rg` ggf. nicht im PATH — dann `grep` oder die Suche der IDE im Ordner `Merged/` bzw. `Isolated/<Solution>/` verwenden.");
        sb.AppendLine();
        return sb.ToString();
    }

    public string GenerateIsolatedSolutionReadme(
        string solutionDisplayName,
        string repositoryRootFolderName,
        DateTimeOffset generatedAtUtc)
    {
        var stamp = generatedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.AppendLine($"# SourceToAI — Isolierter Export: {solutionDisplayName}");
        sb.AppendLine();
        sb.AppendLine("## Meta");
        sb.AppendLine();
        sb.AppendLine($"- **Solution (Anzeigename):** `{solutionDisplayName}`");
        sb.AppendLine($"- **Repository-Ordnername (laut Konzept):** `{repositoryRootFolderName}`");
        sb.AppendLine($"- **Generiert (UTC):** `{stamp}`");
        sb.AppendLine();
        sb.AppendLine("## Aufbau einer Datei pro Projekt (KI-Kontext)");
        sb.AppendLine();
        sb.AppendLine(
            "Jede `.md` direkt unter einem **View-Ordner** (`./complete/`, `./signatures-only/`, `./public-only/`, `./dto-only/`) beschreibt **genau ein** exportiertes Projekt — inklusive YAML-Frontmatter, **MANIFEST**-Tabelle (IDs, Typ, Hash, Größe, Pfade relativ zum jeweiligen Projektroot) und **CONTENT**-Abschnitten mit denselben IDs. Prompts können Manifest-Zeilen und eingebettete Dateien konsistent referenzieren.");
        sb.AppendLine();
        sb.AppendLine("## Dateien und Ordner (Prompt-Use-Cases)");
        sb.AppendLine();
        sb.AppendLine("Alle Pfade relativ zu **diesem** Ordner (`Isolated/<Solution>/`), sofern nicht anders angegeben:");
        sb.AppendLine();
        sb.AppendLine("| Pfad | Wann nutzen? |");
        sb.AppendLine("|:---|:---|");
        sb.AppendLine("| `./readme.md` | Diese Übersicht — Kontext und Konventionen für **diese** Lösung. |");
        sb.AppendLine("| `./dependency-graph.md` | **Solution-Ebene:** Architektur-Überblick mit csproj-Abhängigkeiten und NuGet-Paketen **ohne** eingebetteten Quellcode. |");
        sb.AppendLine("| `./<view>/<Solution>.<Projekt>-<view>.md` | Projekt-Feeds gruppiert unter dieser Lösung. |");
        sb.AppendLine("| `../Merged/<view>/<Solution>.<Projekt>-<view>.md` | Derselbe Inhalt **workspace-weit** unter `Merged/` — ideal, wenn mehrere Quellen im selben Export liegen. |");
        sb.AppendLine();
        sb.AppendLine("Dabei entspricht `<view>` einer der folgenden Sichten:");
        sb.AppendLine(
            $"- `{MultiViewExportPaths.CompleteFolderName}`: **Vollständiger** Stand pro Projekt inkl. Nicht-`.cs`-Dateien (1:1-Texte). **Solution-Dokumentation** (Root, `.cursor`, …) liegt als `<Solution>..Docs-complete.md` (Projektname `.Docs`) in diesem Ordner.");
        sb.AppendLine(
            $"- `{MultiViewExportPaths.SignaturesOnlyFolderName}`: Nur Signaturen — wenig Tokens, Schnittstellen & Typen für Architekturfragen.");
        sb.AppendLine(
            $"- `{MultiViewExportPaths.PublicOnlyFolderName}`: Öffentliche API inkl. Bodies — wenn Implementierungsdetails **nur** für `public`/`protected` relevant sein sollen.");
        sb.AppendLine(
            $"- `{MultiViewExportPaths.DtoOnlyFolderName}`: Datenmodelle (DTOs, Records, Enums) — Domain- und API-Verträge ohne Service-Logik.");
        sb.AppendLine();
        sb.AppendLine("## Hinweise");
        sb.AppendLine();
        sb.AppendLine(
            "- Pro **Projekt** (und virtuellem `.Docs`) existiert **höchstens eine** Markdown-Datei pro View-Ordner; leere Kombinationen werden nicht als Stub-Datei geschrieben. Orchestrierung: alle Views nacheinander aus derselben geparsten Quelle.");
        sb.AppendLine(
            "- C#-Dateien werden pro Lauf nur einmal eingelesen und für alle Sichten aus demselben AST abgeleitet (Performance).");
        sb.AppendLine(
            $"- Die Markdown-Datei zu einem Projekt ist unter `{MultiViewExportPaths.IsolatedFolderName}/` und `{MultiViewExportPaths.MergedFolderName}/` inhaltlich exakt identisch (Dual Write), um je nach Anforderung den passenden Prompt-Kontext zu wählen.");
        return sb.ToString();
    }
}
