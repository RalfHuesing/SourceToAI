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
        sb.AppendLine("## Best Practice für KIs: Definitionen zuerst, nicht in Treffern ertrinken");
        sb.AppendLine();
        sb.AppendLine(
            "In **`Merged/complete/`** (und `Isolated/.../complete/`) tauchen Namen von Methoden/Typen sehr oft als **Aufrufe** oder **Verweise** auf — eine Suche nach einem Methodennamen liefert dort leicht **hunderte irrelevante Treffer** statt der **Definition**.");
        sb.AppendLine();
        sb.AppendLine("Empfohlene Reihenfolge:");
        sb.AppendLine();
        sb.AppendLine(
            $"1. **Signaturen / API-Oberfläche:** Zuerst in **`Merged/{MultiViewExportPaths.SignaturesOnlyFolderName}/`** suchen (bei genau einer Solution alternativ `Isolated/<Solution>/{MultiViewExportPaths.SignaturesOnlyFolderName}/`). Dort stehen kompakte Signaturen — ideal, um die **richtige Projekt-Datei** (`<Solution>.<Projekt>-signatures-only.md`) und Stelle zu finden.");
        sb.AppendLine(
            $"2. **Implementierung lesen:** Dieselbe logische Datei unter **`Merged/{MultiViewExportPaths.CompleteFolderName}/`** bzw. **`Isolated/<Solution>/{MultiViewExportPaths.CompleteFolderName}/`** öffnen (gleiches Namensschema, Endung `-complete.md`) und den **CONTENT**-Block zur passenden Manifest-ID lesen.");
        sb.AppendLine(
            $"3. **Nur öffentliche Oberfläche:** Wenn es um `public`/`protected` geht, kann **`Merged/{MultiViewExportPaths.PublicOnlyFolderName}/`** helfen; für reine Datenverträge **`Merged/{MultiViewExportPaths.DtoOnlyFolderName}/`**.");
        sb.AppendLine(
            "4. **Verwendungsstellen (Who-calls):** Erst in `complete/` breit suchen, wenn du wirklich wissen willst, **wo** etwas überall aufgerufen wird — nicht, um die Definition zu erraten.");
        sb.AppendLine();
        sb.AppendLine(
            "Struktur **ohne** Quelltext: pro Solution `Isolated/<Solution>/dependency-graph.md` (Projekt- und NuGet-Abhängigkeiten) — nützlich, um z. B. zu klären, in welchem **Projekt** eine Assembly steckt, bevor du zielgerichtet Dateien filterst.");
        sb.AppendLine();
        sb.AppendLine("## Navigation bei großen Exporten (`rg` / `grep`)");
        sb.AppendLine();
        sb.AppendLine(
            "Der Baum kann **sehr viele** und **große** `.md`-Dateien enthalten. Lade nicht alles blind in einen Chat — nutze **ripgrep** (`rg`) oder **grep**, um gezielt Stellen zu finden. Unter Windows oft mit **PowerShell** im Export-Wurzelverzeichnis:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# 1) Definition/Signatur finden (bevorzugt)");
        sb.AppendLine($"rg -n \"GetOrCreateSession\" Merged/{MultiViewExportPaths.SignaturesOnlyFolderName}");
        sb.AppendLine();
        sb.AppendLine("# 2) Treffer auf wenige Projekt-Feeds eingrenzen (Dateiname = <Solution>.<Projekt>-<view>.md)");
        sb.AppendLine($"rg --files -g \"*Examples.Library*-{MultiViewExportPaths.SignaturesOnlyFolderName}.md\" Merged/{MultiViewExportPaths.SignaturesOnlyFolderName}");
        sb.AppendLine();
        sb.AppendLine("# 3) Nur Dateiliste (schnell), dann gezielt eine .md öffnen");
        sb.AppendLine($"rg -l \"record TelemetryEnvelope\" Merged/{MultiViewExportPaths.SignaturesOnlyFolderName}");
        sb.AppendLine();
        sb.AppendLine("# Anti-Pattern: Symbol nur in complete workspace-weit — oft zu viele Call-Sites");
        sb.AppendLine("# rg -n \"GetOrCreateSession\" Merged/complete   # nur mit Zusatzfiltern oder nach Schritt 1");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine(
            "Steht `rg` nicht im PATH, IDE-Suche in `Merged/signatures-only` und `Merged/complete` mit denselben Einschränkungen nutzen.");
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
        sb.AppendLine("## Gezielte Suche (KI) — dieselbe Logik wie in der globalen `readme.md`");
        sb.AppendLine();
        sb.AppendLine(
            "Für **Definitionen** zuerst **`./signatures-only/`** durchsuchen, dann die passende Datei unter **`./complete/`** mit gleichem `<Solution>.<Projekt>-…`-Stamm öffnen. **`./complete/`** allein nach einem Methodennamen durchsuchen liefert oft massenhaft **Aufrufe** statt der Definition.");
        sb.AppendLine();
        sb.AppendLine("Beispiele (PowerShell, Arbeitsverzeichnis = dieser Ordner):");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine($"rg -n \"GetOrCreateSession\" ./{MultiViewExportPaths.SignaturesOnlyFolderName}");
        sb.AppendLine($"rg --files -g \"*Examples.Library*-{MultiViewExportPaths.SignaturesOnlyFolderName}.md\" ./{MultiViewExportPaths.SignaturesOnlyFolderName}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine(
            $"Workspace-weit (alle Solutions im Export): dieselben Muster unter **`../{MultiViewExportPaths.MergedFolderName}/{MultiViewExportPaths.SignaturesOnlyFolderName}/`**.");
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
