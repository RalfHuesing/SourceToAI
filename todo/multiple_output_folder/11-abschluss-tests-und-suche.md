# Step 11 — Abschluss: Vollständiger Testlauf & Suche nach Rest-Referenzen

## Ziel

Sicherstellen, dass keine Assertions, Kommentare oder Hilfetexte die **alte** Struktur (`{Export}/{Solution}/readme.md`, `Solution.Projekt.md` ohne View-Suffix) mehr voraussetzen.

## Voraussetzungen

- [Step 06](06-tests-multi-view-export-paths.md) bis [Step 10](10-tests-console-orchestrator.md) abgearbeitet.

## Befehle

```bash
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj
dotnet build -c Release
```

Optional mit Warnungen als Fehler (falls im Repo üblich):

```bash
dotnet build /warnaserror
```

## Repository-Suche (manuell im Agent-Chat ausführen)

Sinnvolle Muster:

```text
rg "GetSolutionExportRoot" SourceToAI.Tests SourceToAI.CLI
rg "\.Proj[0-9]\.md|FixtureSol\.|GranSol\.|MySol\." SourceToAI.Tests
rg "dependency-graph\.md" SourceToAI.Tests SourceToAI.CLI
```

Treffer in **Kommentaren** oder **README** (siehe Step 12) ebenfalls bereinigen.

## Bekannte Nicht-Ziele

- `_e2e-smoke-export`-Spiegel unter `_e2e-smoke-export\...` — nur anfassen, wenn ihr diesen Smoke-Ordner aktiv pflegt; nicht Teil der Kern-Test-suite.

## Referenzen

- [`konzept.md`](konzept.md) Abschnitt 5 (Liste der Testdateien).
