# 🚀 SourceToAI - Standalone AI Feed Generator

 <img width="1905" height="1128" alt="image" src="https://github.com/user-attachments/assets/3d85e7d9-36ae-4541-bb38-00cccf4b315e" />

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**SourceToAI** ist ein leichtgewichtiges, eigenständiges .NET 8 CLI-Tool. Es extrahiert Quellcode und Dokumentationen aus lokalen C#-Solutions offline und wandelt sie in KI-optimierte Markdown-Dateien um. 

Entwickelt speziell für Entwickler, die mit Visual Studio Solutions und Web-basierten KIs (wie ChatGPT, Gemini, Claude) arbeiten. Lade deinen Code einfach und perfekt formatiert in den KI-Kontext!

---

## ✨ Features

* **KI-Optimiertes Format (MarkdownFeed):** Generiert Dateien mit YAML-Frontmatter für Metadaten und einer vollständigen Manifest-Tabelle inkl. MD5-Hashes, Dateigrößen und relativen Pfaden.
* **One-File-Per-Project:** Extrahiert den gesamten relevanten Code eines `.csproj`-Projekts in exakt *eine* Markdown-Datei.
* **Intelligente Dokumentations-Erfassung (.Docs):** Bündelt automatisch deine Root `README.md` und eventuelle `.cursor/rules` in einem virtuellen Projekt, damit die KI sofort die Architektur- und Projektregeln versteht.
* **Google Drive Sync:** Automatischer Upload der generierten KI-Feeds in einen Google Drive Ordner nach dem lokalen Export (Konfigurierbar, siehe [Google Drive Setup](SourceToAI.Docs-GoogleDrive.md)).
* **Dynamic Fencing:** Verhindert Formatierungsfehler durch intelligente Backtick-Ermittlung bei Code-Blöcken (zählt die längste Sequenz von Backticks in einer Datei und fügt `n+1` Backticks für den Code-Block hinzu).
* **Filter-Engine:** Ignoriert standardmäßig Build-Artefakte (`bin`, `obj`), Source-Control-Ordner (`.git`) und IDE-Metadaten (`.vs`, `.idea`).

---

## 🤖 KI-Workflow: Best Practices

So nutzt du SourceToAI am besten mit ChatGPT, Gemini und Co.:

1. **Code exportieren:** Lass das Tool über deine Solution laufen.
2. **Dateien hochladen:** Lade die generierte `.Docs`-Datei (für den Gesamtkontext) sowie die relevanten Projekt-Dateien (`ProjektA.md`, `ProjektB.md`) in den Chat deiner Web-KI hoch.
3. **Prompten:** Nutze Prompts, die das Manifest und die Struktur referenzieren. Beispiel:
   > *"Im angehängten KI-Feed findest du die Architektur-Doku und den Code von Projekt X. Bitte analysiere Datei [ID 5] und Datei [ID 12] aus dem Manifest und schreibe mir Unit-Tests dafür. Beachte dabei die in der .Docs-Datei definierten Architekturregeln."*

---

## 📦 Installation & Download

Die Anwendung wird automatisch über GitHub Actions gebaut. Du kannst die fertigen Binaries direkt herunterladen:

1. Gehe zu den [Releases](../../releases) in diesem Repository.
2. Lade die passende `.zip`-Datei für dein Betriebssystem herunter.
3. Entpacke das Archiv in ein Verzeichnis deiner Wahl.

*(Alternativ kannst du das Repository klonen und per `dotnet build` bzw. `dotnet run` selbst kompilieren).*

---

## 🚀 Verwendung

Das Tool wird über die Kommandozeile bedient und benötigt zwingend **zwei Argumente**:
1. `<Export-Pfad>`: Wo sollen die generierten Markdown-Dateien gespeichert werden?
2. `<Pfad-zur-Solution>`: Das Root-Verzeichnis deiner C#-Solution (dort, wo die `.sln`-Datei liegt).

**Beispiel:**
```cmd
SourceToAI.exe C:\Daten\MeineSolution\

```

### Ordner- & Datei-Struktur (Output)

Das System erstellt für jeden Durchlauf einen neuen Unterordner mit dem Solution-Namen.
Existiert der Ordner bereits werden alle darin befindlichen .md-Dateien gelöscht, um sicherzustellen, dass nur die aktuellsten Daten vorhanden sind.

```text
C:\AI_Feeds\Exports\MeineSolution\
    ├── MeineSolution.Docs-20260222.md
    ├── MeineSolution.ProjektA-20260222.md
    └── MeineSolution.ProjektB-20260222.md

```

---

## ⚙️ Konfiguration (`appsettings.json`)

Das Tool verwendet eine `appsettings.json`, die im gleichen Verzeichnis wie die Ausführungsdatei liegen muss. Hier definierst du, welche Verzeichnisse ignoriert und welche Dateiendungen inkludiert werden sollen.

```json
{
    "SourceToAI": {
        "ExcludedDirectories": [ "bin", "obj", ".git", ".vs", ".idea", "node_modules" ],
        "IncludedExtensions": [ ".cs", ".sql", ".json", ".xml", ".xaml", ".md", ".mdc", ".js", ".ts", ".css" ]
    }
}

```

---

## 📄 Lizenz

Dieses Projekt ist unter der **MIT-Lizenz** lizenziert. Weitere Details findest du in der `LICENSE` Datei.
