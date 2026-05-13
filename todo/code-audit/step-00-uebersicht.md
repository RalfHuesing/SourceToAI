# Code-Audit: Umsetzungsreihenfolge (Übersicht)

Bezug: [`konzept.md`](konzept.md)

Diese Schritte sind so gewählt, dass jeder Schritt in einem **frischen Chat** umsetzbar ist: klar abgegrenzter Scope, konkrete Dateipfade, Akzeptanzkriterien.

## Abgleich Stand (Stand Repo zum Erstellungszeitpunkt)

| Konzept-Thema | Grober Ist-Stand im Repo |
|---------------|------------------------|
| **A** Lock-freies / thread-sicheres Einmal-Parsen | `ConcurrentDictionary` + `Lazy` ist angelegt, aber `ReadAndParse` läuft bei Cache-Miss **außerhalb** einer `GetOrAdd`-Factory — parallele Threads können dieselbe Datei **mehrfach** lesen/parsen. → **step-01** |
| **B** Kein zweites `ParseText` im Export für Exportierbarkeit | `ViewGenerationResult`, `AiFeedSegmentExportability` mit Flag auf Segment — wirkt umgesetzt. → **step-02** (Verifikation, Lücken, Tests) |
| **C** Weniger DI-/Interface-Bloat | Kein `IFileReader`/`IHashService`; View-Generator keyed-only (**step-03**); `IFileTypeService` entfernt zugunsten `static FileTypeService` (**step-04**). |

## Empfohlene Reihenfolge

1. [`step-01-csharp-document-loader-lazy-getoradd.md`](step-01-csharp-document-loader-lazy-getoradd.md) — Thread-sicheres „parse exactly once“ pro Pfad
2. [`step-02-exportpfad-parse-once-verifikation.md`](step-02-exportpfad-parse-once-verifikation.md) — Produktionspfad ohne redundantes Parsen
3. [`step-03-di-viewgeneratoren-straffen.md`](step-03-di-viewgeneratoren-straffen.md) — Keyed-only oder klarere Fabrik
4. [`step-04-interfaces-yagni-audit.md`](step-04-interfaces-yagni-audit.md) — Restliche Abstraktionen vs. Projektrichtlinien (**erledigt**, siehe Audit-Tabelle dort)

**Status-Spalte (Schritt 04):** Abgeschlossen — `IFileTypeService` zugunsten `static class FileTypeService` entfernt; übrige `I…`-Schnittstellen im Audit als sinnvoll dokumentiert.

Nach Abschluss aller Schritte: `dotnet test` und bei Bedarf ein kurzer Performance-Sanity-Check (paralleler Export mit vielen `.cs`).
