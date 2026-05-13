# Step 07 — Manuelle Smoke-Tests (End-to-End)

## Ziel

Nach abgeschlossenen Steps 01–06 die Funktion **real** gegen eine .NET-Assembly verifizieren (kein Ersatz für `dotnet test`, Ergänzung).

## Voraussetzungen

- `dotnet build` für CLI erfolgreich.
- Beliebige **eigene** kleine `.dll` (z. B. aus einem Hello-World-`dotnet publish` eines separaten Temp-Projekts) oder die gebaute `SourceToAI.CLI.dll` — wichtig ist, dass Referenzen im gleichen Ordner auflösbar sind.

## Aufgaben

1. **CLI ausführen** (Pfade anpassen):

   ```text
   dotnet run --project SourceToAI.CLI/SourceToAI.CLI.csproj -- <Export-Temp> <Pfad-zur-Assembly.dll>
   ```

   bzw. mit `--export` / `--input` analog zu den bestehenden Beispielen in `SourceToAiCli`.

2. **Erwartetes Ergebnis prüfen:**

   - Unter `{Export-Temp}/{ErwarteterSolutionName}/` existieren `complete/`, `signatures-only/`, … und generierte Markdowns.
   - Unter `{Export-Temp}/{AssemblyName}/decompile/` liegen **`.csproj`**, **`.cs`**-Dateien — Ordner wurde **nicht** vom Tool gelöscht (Konzept: Decompilat bleiben).
   - Kein unsinniger Solution-Name `decompile` in Ausgaben/Dateipräfixen (Step 04).

3. **Zweiter Lauf** auf dieselbe Assembly: kein hängender Zustand, kein Datenmüll aus altem Decompile (Zielordner wird vor Decompile geleert).

4. **Regression:** Ein bestehendes **Verzeichnis**-Repo wie zuvor läuft unverändert durch.

## Abhaken (Pflicht am Step-Ende)

- [ ] **Step 07 abgehackt** → `- [X] **Step 07 abgehackt**`
