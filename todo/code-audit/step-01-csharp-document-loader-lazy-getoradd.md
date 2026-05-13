# Schritt 01: `CSharpDocumentLoader` — echtes „parse once“ unter Parallelität

## Ziel

`ReadAndParse` / `File.ReadAllText` pro absoluter `.cs`-Pfad höchstens **einmal** materialisieren, wenn mehrere Threads gleichzeitig dieselbe Datei über `LoadParsedDocuments` anfordern — im Sinne von [`konzept.md`](konzept.md) Abschnitt **A** und der Projektregel „kein blockierendes I/O in Locks“ (hier: ohne grobes `lock` auf den gesamten Loader).

## Ausgangslage (relevante Dateien)

- `SourceToAI.CLI/Services/Processing/CSharpDocumentLoader.cs` — aktuell `TryGetValue` + bei Miss **sofort** `ReadAndParse`, danach vorbereitetes `Lazy(() => parsed)` in die `ConcurrentDictionary` einhängen. Zwei Threads können zwischen `TryGetValue` und `TryAdd` beide `ReadAndParse` ausführen.
- `SourceToAI.CLI/Services/Processing/ICSharpDocumentLoader.cs` — Vertrag unverändert lassen, sofern keine API-Änderung nötig.
- `SourceToAI.CLI/Services/Export/MultiViewExportService.cs` — prüfen, ob parallele Aufrufe den Loader stressen (Kontext für Tests).
- `SourceToAI.Tests/Processing/CSharpDocumentLoaderTests.cs` — bestehende Tests müssen grün bleiben.

## Aufgaben

1. **Cache-Materialisierung** auf das Muster aus dem Konzept umbauen: `_parseCache.GetOrAdd(fullPath, fp => new Lazy<CachedCSharpParse>(() => ReadAndParse(fp), LazyThreadSafetyMode.ExecutionAndPublication))` (oder äquivalent: genau eine Factory pro Key, die I/O + Parse ausführt).
2. **Skippable I/O-Fehler** (`SkippableLocalFileIoExceptions`): Heute wird bei Miss geparst und bei Catch nicht gecacht. Mit `Lazy` darf ein werfender Factory-Call die `Lazy`-Instanz nicht „dauerhaft vergiften“, wenn ihr später erneut lesen wollt — Strategie festlegen und dokumentieren (z. B. bei skippable Exception `TryRemove` des Keys **bevor** andere Threads eine poisoned `Lazy` sehen, oder Ergebnis-Typ im Lazy, der Fehler von Erfolg trennt). Randbedingung: weiterhin **Warnung** + Segment überspringen wie bisher.
3. **Reihenfolge / Dedup** innerhalb einer Invocation (`seenInThisInvocation`) beibehalten.
4. Neuen **Test** ergänzen: viele parallele `LoadParsedDocuments`-Aufrufe (oder ein Aufruf mit interner Parallelität, falls ihr die API dafür erweitert — nur wenn nötig) auf **dieselbe** Datei; assert: **eine** Syntaxbaum-Identität bzw. einmaliges Lesen (z. B. über gemeinsame `SyntaxTree`-Referenz oder Hilfs-Counter nur im Test-Build — pragmatisch wählen).

## Akzeptanzkriterien

- Kein globales `lock` um den gesamten Ladevorgang nur für dieses Problem.
- Kein doppeltes `ReadAndParse` für denselben `fullPath` bei Race zweier Threads (nachweisbar durch Test).
- Alle bestehenden `CSharpDocumentLoaderTests` grün; `dotnet test` ohne neue Warnungen.

## Nicht-Ziele

- Kein Umbau von `ICSharpDocumentLoader`-Nutzern außerhalb des nötigen Minimums.
- Kein Ändern der öffentlichen Semantik von `Clear()` (Cache leeren zwischen Läufen).

## Anschluss

Weiter mit [`step-02-exportpfad-parse-once-verifikation.md`](step-02-exportpfad-parse-once-verifikation.md).
