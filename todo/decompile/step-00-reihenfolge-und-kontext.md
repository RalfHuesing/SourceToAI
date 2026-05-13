# Step 00 — Reihenfolge, Kontext, Regeln

Diese Datei ist der **Einstieg** für leere Chats. Die eigentliche Fachidee steht in [konzept.md](./konzept.md); hier nur Navigation und globale Vorgaben.

## Ausführungsreihenfolge (strikt nacheinander)

| Nr. | Datei | Kurzinhalt |
|-----|--------|------------|
| 01 | [step-01-nuget-icsharpcode-decompiler.md](./step-01-nuget-icsharpcode-decompiler.md) | NuGet `ICSharpCode.Decompiler` (aktuellste stabile Version) |
| 02 | [step-02-assembly-decompiler-service-und-di.md](./step-02-assembly-decompiler-service-und-di.md) | `IAssemblyDecompilerService` + Implementierung + DI |
| 03 | [step-03-console-orchestrator-assembly-pfad.md](./step-03-console-orchestrator-assembly-pfad.md) | Assembly-Eingabe → Decompile → Pipeline |
| 04 | [step-04-solution-discovery-und-readme-anzeigename.md](./step-04-solution-discovery-und-readme-anzeigename.md) | `GetSolutionName` + `repositoryFolderName` für `decompile`-Wurzel |
| 05 | [step-05-cli-validierung-und-dokumentation.md](./step-05-cli-validierung-und-dokumentation.md) | `SourceToAiCli` / Usage: DLL/EXE erlauben |
| 06 | [step-06-tests-decompilation.md](./step-06-tests-decompilation.md) | Unit-/Integrationstests, `dotnet test` grün |
| 07 | [step-07-manuelle-smoke-tests.md](./step-07-manuelle-smoke-tests.md) | Manuelle End-to-End-Prüfung (optional aber empfohlen) |

## Projekt-Kontext (für den Agenten)

- **Solution:** `SourceToAI.CLI` (Exe, `net10.0`), Tests: `SourceToAI.Tests` mit `ProjectReference` auf die CLI.
- **Orchestrierung:** `ConsoleOrchestrator` (`SourceToAI.CLI/App/ConsoleOrchestrator.cs`) — Einstieg pro Quelle `RunSingleSourceAsync`.
- **Discovery:** `SolutionDiscoveryService` — erwartet heute ein **existierendes Verzeichnis**; Assembly-Dateien müssen **vor** `GetSolutionName` in ein Decompile-Verzeichnis umgewandelt werden.
- **Export-Pfade:** `MultiViewExportPaths.GetSolutionExportRoot(exportPath, solutionName)` → `{exportPath}/{solutionName}/…` (Views: `complete`, `signatures-only`, …). Decompilat laut Konzept: `{exportPath}/{AssemblyName}/decompile/` (bleibt erhalten).
- **Richtlinien:** `.cursor/rules/sourcetoai-projektrichtlinien.mdc` — u. a. kein DI für triviale Framework-Helfer; **`IAssemblyDecompilerService` ist bewusst legitim** (Orchestrierung, Mocking). Keine Compiler-Warnungen. Dateien ≥500 Zeilen nicht weiter aufblähen.

## NuGet-Versionen

In jedem Step, der Pakete anfasst: **aktuellste stabile** Version per `dotnet add package <Id>` (ohne feste alte Version pinnen), sofern der Step nicht ausdrücklich eine Mindestversion fordert. Anschließend `dotnet restore` / Build prüfen.

## Abhaken (nach Durchsicht / Anpassung dieser Übersicht)

Wenn die Tabelle und Links zu den Steps stimmen und nichts mehr ergänzt werden muss:

- [ ] **Step 00 abgehackt** → `- [X] **Step 00 abgehackt**`
