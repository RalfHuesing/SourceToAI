# Schritt 04: YAGNI-/Interface-Audit (Rest-Bloat)

## Ziel

Restliche Stellen finden, die dem Geist von [`konzept.md`](konzept.md) Abschnitt **C** und den **Projektrichtlinien** (`.cursor/rules/sourcetoai-projektrichtlinien.mdc`) widersprechen: unnötige `I…`-Schichten um zustandslose Framework-APIs, künstliche Services ohne echte Austauschbarkeit.

Hinweis: Die im Konzept genannten `IFileReader` / `IHashService` sind im aktuellen Repo **nicht** vorhanden — dieser Schritt ist ein **Audit + gezielte Bereinigung**, kein blindes Umbenennen.

## Aufgaben

1. **Inventar**: Alle `interface I*` im `SourceToAI.CLI`-Projekt auflisten; pro Interface kurz bewerten:
   - Zustand / Orchestrierung / Mocking nötig → **behalten**
   - reiner Wrapper um `File.*`, `HashData`, `Path.*` ohne Zustand → **candidate für Entfernung** (direkte Aufrufe oder `static class` Helper)
2. Kandidaten **einzeln** refactoren (ein Chat kann mehrere kleine Interfaces bündeln, wenn unabhängig — sonst aufteilen).
3. DI-Registrierungen in `Program.cs` bereinigen.
4. Tests anpassen; keine aufblasenden neuen Abstraktionen einführen.

## Akzeptanzkriterien

- Kurzes Audit-Ergebnis (Tabelle: Interface → Verbleib/Begründung oder entfernt).
- `dotnet test` grün; Warnungsregel aus Projektrichtlinien eingehalten.

## Nicht-Ziele

- `ICSharpDocumentLoader` nicht „nur aus Prinzip“ entfernen: Der Loader hat **Zustand** (Parse-Cache) und ist ein sinnvoller DI-Singleton — nur anfassen, wenn ihr eine klarere Modellierung wollt (optionaler Folgeschritt, nicht Pflicht dieses Audits).

## Abschluss

[`step-00-uebersicht.md`](step-00-uebersicht.md) aktualisieren (Status-Spalte), wenn alle Schritte erledigt sind.
