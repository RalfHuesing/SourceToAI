# Step 04 — SolutionDiscovery: Name „decompile“ + Konsistenz

## Ziel

Wenn die Quellwurzel ein Ordner **`decompile`** ist (typisch: `{exportPath}/{AssemblyName}/decompile` nach Assembly-Import), soll der **Solution-Name** nicht `decompile` sein, sondern der **Name des übergeordneten Verzeichnisses** (Assembly-Name). Damit bleiben `MultiViewExportPaths`, Dateipräfixe und Konsolen-Ausgabe konsistent mit dem Konzept in [konzept.md](./konzept.md).

## Kontext

- Datei: `SourceToAI.CLI/Services/Discovery/SolutionDiscoveryService.cs`, Methode `GetSolutionName`.
- Bestehende Tests: `SourceToAI.Tests/Discovery/SolutionDiscoveryServiceTests.cs` — **erweitern**.

## Aufgaben

1. **Logik in `GetSolutionName`:**
   - Unverändert: Wenn `*.sln` im `rootPath` (top-level), weiterhin erster Treffer ohne Endung.
   - Fallback bisher: `new DirectoryInfo(rootPath).Name`.
   - **Neu:** Wenn **keine** `.sln` und der Verzeichnisname (letztes Segment) **case-sensitive oder invariant?** — Konzept sagt wörtlich `"decompile"`: praktisch **`StringComparer.OrdinalIgnoreCase`** für Ordnernamen `decompile` verwenden, damit Windows-/Tool-Unterschiede robust sind.
   - Wenn Ordnername = `decompile` **und** `Directory.GetParent(rootPath)` nicht null: Fallback-Name = **Parent.Name**.
   - Edge Case: `decompile` ganz oben ohne Parent → weiterhin `decompile` oder explizite Fehlermeldung — dokumentieren und testen.
2. **`FindProjects`:** unverändert lassen, solange `.csproj` unterhalb von `decompile` gefunden werden (WholeProjectDecompiler erzeugt Projektstruktur).
3. **Tests** (mindestens Unit):
   - Temporäres Verzeichnislayout `…/MyAssembly/decompile/` ohne `.sln` → `GetSolutionName` liefert `MyAssembly`.
   - Normales Repo ohne `decompile` → unverändertes Verhalten bleibt abgedeckt (Regression).

## Abgleich mit Step 03

- `ConsoleOrchestrator` sollte für Readme/Anzeige bereits den Parent-Namen nutzen, wenn Basis `decompile` ist; **`GetSolutionName`** muss dieselbe Semantik für **Export-Pfade und Dateinamen** liefern — nach diesem Step identisch.

## Abhaken (Pflicht am Step-Ende)

- [X] **Step 04 abgehackt**
