# Kontext & Ziel
Wir müssen das Tool `SourceToAI` erweitern. Bisher akzeptiert das Tool Verzeichnisse als Eingabe (über Positionsargumente oder `--input`), sucht darin nach `.sln` oder `.csproj` Dateien und generiert einen Markdown-KI-Feed.

**Neues Feature:** 
Das Tool soll nun auch direkt kompilierte .NET Assemblies (`.dll` oder `.exe`) als Eingabe akzeptieren können (auch gemischt mit Verzeichnissen). 
Wenn eine Assembly übergeben wird, soll diese zunächst decompiliert werden. Der decompilierte Code dient dann als Basis (Quellverzeichnis) für die bereits bestehende Pipeline.

# Architektur-Entscheidungen & Vorgaben
1. **Decompiler-Bibliothek:** Nutze das NuGet-Paket `ICSharpCode.Decompiler` (die Engine hinter ILSpy). Es ist der De-facto-Standard und bietet genau die benötigten Funktionen.
2. **WholeProjectDecompiler:** Nutze die Klasse `WholeProjectDecompiler` aus `ICSharpCode.Decompiler`. Sie erstellt nicht nur die `.cs`-Dateien, sondern generiert automatisch eine passende `.csproj`-Datei. Das ist extrem wichtig, damit unser bestehender `FileDiscoveryService` und die `CSharpDocumentLoader`-Pipeline unverändert funktionieren können.
3. **Ordner-Struktur:** Der decompilierte Code soll im Ziel-Export-Pfad unter `<Export-Root>\<AssemblyName>\decompile\` landen. Er darf am Ende **nicht** gelöscht werden. Die generierten Views (complete, public-only etc.) landen regulär daneben (z. B. `<Export-Root>\<AssemblyName>\complete\`).
4. **Eingabe-Unterscheidung:** Der `ConsoleOrchestrator` oder die vorgeschaltete CLI-Logik muss prüfen, ob ein Input-Pfad ein Verzeichnis (`Directory.Exists`) oder eine Datei (`File.Exists` mit Endung `.dll`/`.exe`) ist.

# Schritt-für-Schritt Implementierungsplan

## Schritt 1: NuGet-Paket hinzufügen
Füge das Paket `ICSharpCode.Decompiler` zum Projekt `SourceToAI.CLI` hinzu.

## Schritt 2: Neuen Service für die Decompilierung erstellen
Erstelle ein Interface `IAssemblyDecompilerService` und die Implementierung `AssemblyDecompilerService` im Ordner `Services/Processing` (oder einem neuen Ordner `Services/Decompilation`).
- **Input:** Pfad zur `.dll`/`.exe`, Zielverzeichnis (`.../decompile`).
- **Logik:** 
  - Instanziiere den `UniversalAssemblyResolver` mit dem Verzeichnis der Ziel-Assembly (damit Referenzen gefunden werden).
  - Konfiguriere `DecompilerSettings` sinnvoll (z. B. `RemoveDeadCode = true`, `YieldReturn = true`, `AsyncAwait = true`).
  - Nutze den `WholeProjectDecompiler`, um das Projekt in das Zielverzeichnis zu schreiben.
- **Rückgabe:** Der Pfad zu dem Verzeichnis, in dem die neue `.csproj` liegt.

## Schritt 3: CLI und Orchestrator anpassen
Passe `ConsoleOrchestrator.RunAsync` an. Aktuell iteriert die Methode über `rootPaths`. 
- **Prüfung:** Ist der `rootPath` eine Datei und endet auf `.dll` oder `.exe`?
- **Wenn Datei:** 
  - Bestimme den Assembly-Namen (z.B. `MyLib` aus `MyLib.dll`).
  - Definiere das Decompile-Ziel: `Path.Combine(exportPath, assemblyName, "decompile")`.
  - Rufe den `AssemblyDecompilerService` auf.
  - Setze den `rootPath` für den *Rest der Schleife* auf dieses neue Decompile-Verzeichnis! So läuft die bestehende Pipeline völlig transparent weiter.
- **Wenn Verzeichnis:** Logik bleibt exakt wie bisher.

## Schritt 4: Anpassung SolutionDiscoveryService (Edge Case)
Wenn wir den `rootPath` auf `...\<AssemblyName>\decompile` ändern, wird `SolutionDiscoveryService.GetSolutionName()` mangels einer `.sln` den Ordnernamen (`"decompile"`) als Fallback zurückgeben. Das führt zu unschönen Ausgaben wie `decompile.MyLib.md`.
**Lösung:** Passe `SolutionDiscoveryService.GetSolutionName()` an. Wenn keine `.sln` gefunden wird und der aktuelle Ordnername "decompile" ist, nutze stattdessen den Namen des übergeordneten Verzeichnisses (Parent Directory), also den `<AssemblyName>`.

# Fallstricke & konkrete Lösungsansätze

1. **Abhängigkeiten der Assembly werden nicht gefunden (AssemblyResolutionException):**
   - *Problem:* ILSpy stürzt ab, wenn es beim Decompilieren referenzierte Typen nicht auflösen kann.
   - *Lösung:* Konfiguriere den `UniversalAssemblyResolver` so, dass er das Ursprungsverzeichnis der übergebenen `.dll` als Suchpfad registriert (`resolver.AddSearchDirectory(Path.GetDirectoryName(dllPath))`). Setze ggf. `ThrowOnAssemblyResolveErrors = false` in den `DecompilerSettings`, falls möglich, um robust gegen fehlende GAC-Abhängigkeiten zu sein.

2. **Fehlende `.csproj` Datei:**
   - *Problem:* Ein reiner AST-Visitor exportiert nur `.cs` Dateien, aber unser Tool braucht die `.csproj` für die Discovery-Phase.
   - *Lösung:* Vergewissere dich strikt, dass `WholeProjectDecompiler.DecompileProject()` genutzt wird. Dies generiert eine vollständige Projektstruktur.

3. **Überschreiben bestehender Decompilate:**
   - *Problem:* Wenn das Tool mehrfach auf dieselbe DLL ausgeführt wird, meckert der Decompiler evtl., dass Dateien schon existieren, oder alte Dateien bleiben als Artefakte liegen.
   - *Lösung:* Bevor der Decompiler gestartet wird, prüfe ob der Ordner `...\decompile` existiert. Wenn ja, leere ihn vollständig.

4. **DI-Registrierung:**
   - Vergiss nicht, den neuen `IAssemblyDecompilerService` in der `Program.cs` im ServiceCollection-Container zu registrieren.

# Test-Vorgaben
Bitte aktualisiere die Tests oder füge einen Integrationstest hinzu (z. B. `AssemblyDecompilerServiceTests`), der (sofern mockbar oder mit einer minimalen Dummy-DLL machbar) sicherstellt, dass eine übergebene Datei korrekt als Decompilierungs-Job erkannt und eine `.csproj` generiert wird. Falls eine echte DLL für Tests zu komplex ist, teste die Logik-Weichen im `ConsoleOrchestrator` (Datei vs. Verzeichnis).

Setze dies nun unter strikter Beachtung der bestehenden Projektrichtlinien (Anti-Bloat, keine unötigen Interfaces für reine Framework-Aufrufe, aber `IAssemblyDecompilerService` ist hier als Orchestrierungs-Dienstleistung legitim) um.