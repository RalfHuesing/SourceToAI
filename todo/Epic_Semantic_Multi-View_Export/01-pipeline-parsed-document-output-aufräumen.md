# Task 01: Pipeline — einmaliges Einlesen/Parsen + Output-Basis

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken — nicht nur im Chat oder im Commit beschreiben.

## Ziel

- Trennung: **Einlesen + Parsen** (und Halten im Speicher) vs. **Schreiben** der Markdown-Ausgaben (kommt in späteren Tasks).
- Zentraler Service (oder klar abgegrenzte Komponente), der für ein `ProjectDefinition` alle relevanten **`.cs`**-Dateien sammelt, **genau einmal** einliest und mit `CSharpSyntaxTree.ParseText` parst.
- Ergebnis-Typ z. B. `ParsedCSharpDocument` / `IReadOnlyList<ParsedCSharpDocument>` mit mindestens: relativer Pfad, `SyntaxTree` oder `CompilationUnitSyntax` Root, optional Originaltext für `complete`-View ohne erneutes Lesen.
- **NuGet:** `Microsoft.CodeAnalysis.CSharp` im CLI-Projekt referenzieren (Version konsistent mit Target-Framework `net10.0`).
- **Output-Verzeichnis:** Konzept verlangt Unterordner `complete/`, `signatures-only/`, usw. In diesem Task: **Schnittstelle oder Hilfsmethode**, die den Ziel-Root für den Multi-View-Export definiert und dokumentiert, wie ein „Clean“ vor jedem Lauf aussieht (z. B. gesamten Multi-View-Unterbaum löschen/neu anlegen — konsistent mit `08`, hier nur vorbereiten/Grundstein).

## Nicht-Ziel (bewusst später)

- Keine fertigen Rewriter/View-Builder (Tasks `03`–`06`).
- **Complete/full-source:** Enthält laut Konzept **alle** Dateien 1:1 — für `.cs` kann der gespeicherte Originalstring genutzt werden; für andere Extensions weiterhin einmalig einlesen und mitführen (Design in diesem Task festlegen, Implementierung Text-Zusammenbau in `06`).

## Abhängigkeiten

- Keine (erster Implementierungs-Task nach Master-Checkliste).
- Orientierung: `SourceToAI.CLI/Services/Processing/MarkdownFeedGenerator.cs` (aktuell mixed read+write), `FileDiscoveryService`, `ProjectDefinition`.

## Tests (Pflicht)

- Unit-Tests mit temporärem Verzeichnis (`TempWorkspace` o. Ä.): zwei `.cs`-Dateien anlegen, Service aufrufen, sicherstellen:
  - Anzahl `ParsedCSharpDocument` == 2.
  - **Mock/Spy nicht nötig** — stattdessen: gleiche Instanz/Root-Node wird für zwei fiktive „Consumer“ übergeben, ohne dass `File.ReadAllText` zweimal pro Pfad aufgerufen wird (z. B. Zähler-Wrapper um `IFileSystem` oder statistisch: nur wenn ihr echtes FS nutzt, einmal lesen und dokumentieren; besser: `IFileReader`-Abstraktion nur wenn schon Projekt-Pattern — sonst Integrationstest mit echtem Temp-Dir und Assert auf Parse-Ergebnis `ToString()`/`Members.Any()`).
- Mindestens ein Test mit **ungültigem C#**: erwarte definiertes Verhalten (Diagnostik sammeln, Datei überspringen oder Fehlerobjekt — **explizit dokumentieren und testen**).

## Selbstverifikation (nach Umsetzung — nicht überspringen)

- [x] `dotnet build` und `dotnet test` grün.
- [x] Keine `.cs`-Datei wird in diesem Task für mehrere Parse-Zwecke zweimal von der Platte gelesen (Code-Review eigenes `foreach`).
- [x] `Microsoft.CodeAnalysis.CSharp` erscheint in `SourceToAI.CLI.csproj`.
- [x] `00-epic-master-checklist-selbstverifikation.md`: Zeile „Parse Once“ mit diesem Task verknüpft.
- [x] Kurz im Code-Kommentar oder XML-Doc: wo der Multi-View-Root relativ zu `exportPath`/`solutionName` liegt (für `08`).

## Nächster Schritt

`02-strategy-viewgenerator-di.md`
