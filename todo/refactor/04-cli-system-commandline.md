# Schritt 04: CLI mit System.CommandLine (statt manuellem `args[]`)

## Ziel (laut Konzept)

Argumente nicht mehr per `args.Length < 2` und Indexzugriff parsen, sondern mit einem **leichtgewichtigen CLI-Framework**. Vorbereitung für **mehrere Eingabe-Pfade** (`string[] inputPaths` o. ä.) als nahezu declaratives Binding.

## Ist-Analyse

### Einstieg

`SourceToAI.CLI/Program.cs` (top-level statements):

- Prüfung: `args.Length < 2` oder leere Strings → Usage-Text, `return`.
- `exportPath = args[0]`, `solutionPath = args[1]`.
- Danach Konfiguration, `ServiceCollection`, `await orchestrator.RunAsync(solutionPath, exportPath)`.

### Projekt

`SourceToAI.CLI/SourceToAI.CLI.csproj` — `net10.0`, derzeit **kein** `System.CommandLine`-Package.

### Tests

Tests instantiieren `ConsoleOrchestrator` direkt — **nicht** den `Program.cs`-Einstieg. CLI-Umstellung betrifft primär **manuelle Ausführung** und ggf. neue **CLI-Integrationstests** (optional).

## Umsetzung

1. **Package:** `System.CommandLine` (Version passend zu net10.0 / NuGet — aktuelle stabile 2.x prüfen) als `PackageReference` in `SourceToAI.CLI.csproj`.
2. **Root command** mit mindestens zwei **Arguments** oder **Options**:
   - Konzept spricht von „Einzeiler“ für mehrere Pfade: sinnvoll `--export <path>` und `--input <path>` **oder** Positional: `<export> <solution>` beibehalten für Rückwärtskompatibilität.
   - Für **mehrere** Solution-/Input-Pfade: `Argument<string[]>` oder wiederholbare Option `--input` — genaue UX im Issue/README der Anwendung festlegen.
3. **Handler** async: nach erfolgreichem Parse dieselbe Pipeline wie heute (`ConfigurationBuilder`, DI, `RunAsync`).
4. **Fehlerausgabe:** `ParseResult` / Validator-Messages auf `Console.Error` oder bestehende Konvention; Exit-Code `1` bei Parse-Fehler (Standard bei `System.CommandLine`).
5. **Program.cs-Struktur:** Entweder `await root.InvokeAsync(args);` als letzte Zeile oder explizite `Parser`-API — teamüblich halten.
6. **Dokumentation der Usage-Strings** im Code an eine Stelle (lokale Konstante), damit Tests/Copy nicht divergieren.

## Abgrenzung

- **Orchestrator-Signatur** `RunAsync(string rootPath, string exportPath)` muss erst geändert werden, wenn **wirklich** mehrere Roots in einem Lauf unterstützt werden — Schritt 04 kann zunächst nur **ein** Solution-Argument binden und trotzdem `System.CommandLine` nutzen.
- Schritt 03 (Exceptions): Wenn bereits umgesetzt, im Handler `try/catch` aus `Program.cs` mit Exit-Code verzahnen.

## Erfolgskriterien

- Gleiche funktionale CLI für den Fall „zwei Argumente Export + Solution“ (oder explizite Optionen mit gleichem Ergebnis).
- Keine Warnungen; `dotnet build` erfolgreich.
- Optional: kleiner Test, der `Parser`/`RootCommand` mit Beispiel-Args füttert (ohne vollen Export).

## Nicht tun

- Keine große Umstellung von `AppSettings` oder Config-Pfad — nur CLI-Argumente.
