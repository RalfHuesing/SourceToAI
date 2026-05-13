# Step 01 — NuGet: ICSharpCode.Decompiler

## Ziel

Das Paket **`ICSharpCode.Decompiler`** (ILSpy-Engine) in **`SourceToAI.CLI`** einbinden — **neueste stabile Version** (per CLI holen, nicht willkürlich eine alte Version ins `.csproj` schreiben).

## Kontext

- Projektdatei: `SourceToAI.CLI/SourceToAI.CLI.csproj` (SDK-Style, `net10.0`).
- Nach diesem Step soll `dotnet build` für die Solution weiterhin erfolgreich sein (noch ohne Nutzung des Pakets im Code, falls Step 02 folgt).

## Aufgaben

1. Im Repository-Root (oder im CLI-Projektordner) ausführen:
   - `dotnet add SourceToAI.CLI/SourceToAI.CLI.csproj package ICSharpCode.Decompiler`
   - Dadurch wird die **aktuellste stabile** Version eingetragen.
2. Prüfen, ob transitive Abhängigkeiten mit `net10.0` kompatibel sind; bei Konflikten kurz dokumentieren (Kommentar im PR/Commit-Body reicht) und kompatible Version wählen.
3. **`dotnet build`** auf der Solution ausführen und Warnungen **0** anstreben (Projektrichtlinie).

## Nicht-Ziele

- Keine Implementierung des Decompilers in diesem Step (nur Paketreferenz).
- Keine Änderung an `SourceToAI.Tests`, außer der Build zieht die Referenz transitiv mit — normalerweise nicht nötig.

## Referenz (API-Hinweis für Folge-Steps)

- Klasse `ICSharpCode.Decompiler.CSharp.ProjectDecompiler.WholeProjectDecompiler` mit `DecompileProject(MetadataFile file, string targetDirectory, CancellationToken cancellationToken = default)`.
- Typischer Einstieg für Dateien: `ICSharpCode.Decompiler.Metadata.PEFile` (exakte Factory-/Konstruktor-Signatur an der **installierten** Paketversion orientieren).

## Abhaken (Pflicht am Step-Ende)

Wenn alle Aufgaben erledigt sind, in **dieser** Datei die folgende Zeile von unchecked auf checked ändern:

- [X] **Step 01 abgehackt**
