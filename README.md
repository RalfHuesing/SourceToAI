Hier ist die vollumfängliche README für dein neues Projekt **SourceToAI**. Sie dient als Architekturdokumentation und Spezifikation für unseren Nachbau.

---

# 📖 SourceToAI - Standalone AI Feed Generator

**SourceToAI** ist ein leichtgewichtiges, eigenständiges .NET 8 CLI-Tool. Es repliziert die Kernfunktionalität der `San.Development.Tools.Core` Feed-Engine, um Quellcode aus lokalen C#-Solutions offline zu extrahieren und in KI-optimierte Markdown-Dateien (SanMarkdownFeed-Format) umzuwandeln.

## ✨ Features

* **Headless CLI:** Einfache Ausführung über die Kommandozeile ohne UI-Overhead.
* **Auto-Discovery:** Identifiziert automatisch die `.sln`-Datei für den Lösungsnamen und alle darin enthaltenen `.csproj`-Projekte.
* **SanMarkdownFeed-Protokoll:**
* Generierung von YAML-Frontmatter für Metadaten.
* Erstellung einer vollständigen Manifest-Tabelle inkl. MD5-Hashes, Dateigrößen und relativen Pfaden.
* Dynamic Fencing (Vermeidung von Formatierungsfehlern durch intelligente Backtick-Ermittlung).


* **Filter-Engine:** Ignoriert automatisch Build-Artefakte (`bin`, `obj`), Source-Control-Ordner (`.git`) und IDE-Metadaten (`.vs`).
* **One-File-Per-Project:** Extrahiert den gesamten relevanten Code eines Projekts in exakt *eine* Datei.

---

## 🚀 Verwendung

Das Tool erwartet als einziges Argument den Pfad zum Root-Verzeichnis der Solution (dort wo die `.sln` liegt).

```cmd
SourceToAI.exe C:\Daten\MeineSolution\

```

### Ordner- & Datei-Struktur (Output)

Das System generiert bei jedem Aufruf eine neue UUID, um alte Exporte nicht zu überschreiben.

```text
[Ausgabeverzeichnis lt. appsettings]\MeineSolution-8f7a6b5c-4d3e-2f1a-0b9c-8d7e6f5a4b3c\
    ├── MeineSolution.ProjektA-20260221.md
    ├── MeineSolution.ProjektB-20260221.md
    └── MeineSolution.ProjektC-20260221.md

```

---

## ⚙️ Konfiguration (`appsettings.json`)

Die Konfigurationsdatei muss im gleichen Verzeichnis wie die `.exe` liegen.

```json
{
  "SourceToAI": {
    "OutputRootDirectory": "C:\\AI_Feeds\\Exports",
    "ExcludedDirectories": [ "bin", "obj", ".git", ".vs", ".idea", "node_modules" ],
    "IncludedExtensions": [ ".cs", ".sql", ".json", ".xml", ".xaml", ".md", ".js", ".ts", ".css" ]
  }
}

```

---

## 📦 Abhängigkeiten (NuGet Packages)

Um das Projekt aufzusetzen, benötigst du folgende Standard-Microsoft-Pakete für Dependency Injection und Konfiguration:

* `Microsoft.Extensions.Configuration.Json`
* `Microsoft.Extensions.Configuration.Binder`
* `Microsoft.Extensions.DependencyInjection`
* `Microsoft.Extensions.Logging.Console` *(Optional, falls strukturiertes Console-Logging gewünscht ist)*

*(Hinweis: Kryptographie für MD5 ist nativ in `System.Security.Cryptography` im .NET 8 BCL enthalten).*

---

## 🏗️ Architektur & Klassenstruktur

Das Projekt wird strikt nach Clean Code / SOLID Prinzipien in saubere Namespaces unterteilt. Du kannst diese Struktur direkt in deiner IDE scaffolden.

### 1. `SourceToAI.Bootstrapper`

Zuständig für das Setup der Applikation.

* `Program.cs` - Einstiegspunkt. Parst `args[0]`, baut den `ServiceCollection`-Container und lädt die `appsettings.json`.

### 2. `SourceToAI.Configuration`

* `AppSettings.cs` - Typisierte Repräsentation der `appsettings.json`.

### 3. `SourceToAI.Models`

Datenstrukturen für den Kontrollfluss und das Feed-Protokoll.

* `ProjectDefinition.cs` - Hält den Namen des Projekts und den Root-Pfad der `.csproj`.
* `FileManifestEntry.cs` - Modell für die Manifest-Tabelle (ID, Typ, Hash, Size, RelativePath).
* `FileContent.cs` - Repräsentiert eine eingelesene Datei inkl. Inhalt und Syntax-Sprache.
* `ExtractionResult.cs` - Result-Pattern-Klasse für sauberes Error-Handling ohne Exceptions.

### 4. `SourceToAI.Services.Discovery`

Verantwortlich für das Finden der Dateien auf der Festplatte.

* `ISolutionDiscoveryService` / `SolutionDiscoveryService.cs`
* Findet die `.sln` (für den Namen).
* Sucht rekursiv nach `.csproj` Dateien.


* `IFileDiscoveryService` / `FileDiscoveryService.cs`
* Sucht alle Dateien innerhalb eines Projekt-Ordners.
* Wendet die Ignore-Listen (`ExcludedDirectories`, `IncludedExtensions`) an.



### 5. `SourceToAI.Services.Processing`

Die Kern-Logik (Der Nachbau der Core-Engine).

* `IFileTypeService` / `FileTypeService.cs`
* Ermittelt anhand der Extension den "Type" (Code, Doc, Config).
* Liefert den passenden Markdown-Language-Tag (z.B. `csharp`, `xml`).


* `IHashService` / `HashService.cs`
* Kapselt `MD5.HashData()` zur Generierung der 8-stelligen Hex-Hashes für das Manifest.


* `IFeedGenerator` / `MarkdownFeedGenerator.cs`
* **Das Herzstück:** Baut den tatsächlichen Markdown-String zusammen.
* Generiert YAML-Frontmatter (inkl. neuer Session-UUID).
* Baut die Markdown-Tabelle (`## MANIFEST`).
* Fügt die Dateiinhalte zusammen und berechnet das **Dynamic Fencing** (zählt die längste Sequenz von Backticks in einer Datei und fügt n+1 Backticks für den Code-Block hinzu).



### 6. `SourceToAI.App`

* `ConsoleOrchestrator.cs`
* Wird vom `Program.cs` aufgerufen.
* Steuert den Workflow: `Lade Solution` -> `Für jedes Projekt: Lade Dateien -> Generiere Feed -> Speichere auf Disk`.
