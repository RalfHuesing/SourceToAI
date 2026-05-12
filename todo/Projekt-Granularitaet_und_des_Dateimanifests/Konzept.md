# Korrektur: Wiederherstellung der Projekt-Granularität und des Dateimanifests

## 1. Problembeschreibung
Die aktuelle Implementierung des Multi-View-Exports weist zwei kritische Architektur-Fehler auf, die die Nutzbarkeit der Exporte für LLMs massiv verschlechtern:
1. **Fehlende Projekt-Granularität:** Aktuell wird der gesamte Code der Solution in eine einzige Datei pro View exportiert (z. B. `complete\full-source.md`). Das flutet das Context Window und verursacht das "Lost in the Middle"-Phänomen.
2. **Fehlendes Manifest:** Die generierten Markdown-Dateien enthalten kein Inhaltsverzeichnis (Manifest) und keine Metadaten mehr. Das LLM verliert dadurch den architektonischen Überblick.

## 2. Zielzustand: Verzeichnisstruktur (Sichten × Projekte)
Der Export muss zwingend nach **Sichten (Views)** UND **Projekten (C# .csproj)** aufgeteilt werden. 

Das Output-Verzeichnis muss nach der Generierung exakt dieses Muster aufweisen:

```text
output/
├── readme.md                           # Dynamische Erklärung der Views
├── complete/                           # View-Ordner
│   ├── SolutionName.ProjektA.md        # 1 Datei pro Projekt in dieser View
│   └── SolutionName.ProjektB.md
├── signatures-only/                    # View-Ordner
│   ├── SolutionName.ProjektA.md
│   └── SolutionName.ProjektB.md
└── public-only/                        # View-Ordner
    ├── SolutionName.ProjektA.md
    └── SolutionName.ProjektB.md
```

## 3. Zielzustand: Innerer Aufbau der Markdown-Dateien
JEDE generierte Markdown-Datei (z. B. `output/complete/SolutionName.ProjektA.md`) muss zwingend nach folgendem strukturellen Schema aufgebaut sein:

### A) YAML Frontmatter (Metadaten)
Ganz am Anfang der Datei.
```yaml
---
feed_type: source_export
project: SolutionName (ProjektName)
session_id: <Neu_generierte_Guid>
generated: <ISO_8601_Timestamp>
file_count: <Anzahl_der_Dateien_in_diesem_Dokument>
---
```

### B) Header & Instruction
```markdown
# AI FEED: SolutionName (ProjektName)

## INSTRUCTION
SYSTEM-KONTEXT: Dies ist ein Snapshot eines Software-Projekts. Das Format ist Markdown mit Fencing. Dies ist Projekt: 'ProjektName'. Analysiere den Code im Kontext der Architektur.
```

### C) Manifest-Tabelle
Eine Markdown-Tabelle aller Dateien, die in **genau diesem** Dokument enthalten sind. Die ID muss fortlaufend sein. Der Pfad ist relativ zum Root-Verzeichnis des jeweiligen Projekts.
```markdown
## MANIFEST
| ID | Type | Hash | Size | Path |
|---:|:---|:---|---:|:---|
| [1](#1) | Code | <8-Char-MD5> | <Bytes> | Verzeichnis\Datei1.cs |
| [2](#2) | Doc | <8-Char-MD5> | <Bytes> | Verzeichnis\Datei2.md |
```
*Hinweis: Wenn eine Datei durch einen View-Filter (z. B. public-only) komplett geleert wird, darf sie nicht im Manifest und nicht im Content auftauchen.*

### D) Content-Bereich
Hier folgen die durch Roslyn veränderten/gefilterten AST-Ausgaben, getrennt durch Trennlinien und referenziert durch die ID aus dem Manifest. Das Fencing (Anzahl der Backticks) muss dynamisch berechnet werden, um Konflikte zu vermeiden (mindestens 4 Backticks).
```markdown
## CONTENT

---
### [1] Verzeichnis\Datei1.cs
````csharp
// Modifizierter oder kompletter Code hier

```

---

### [2] Verzeichnis\Datei2.md

```markdown
# Doku

```

```

## 4. Implementierungsvorgaben für den Agenten
1. **Behalte das Roslyn-Parsing bei:** Die AST-Manipulation (Parse Once, Rewrite Multiple Times) ist korrekt.
2. **Schleifen-Anpassung:** Die Orchestrierung muss iterieren über: `Projekte -> Views`. Für jedes Projekt wird der zugehörige Code an alle definierten Views übergeben, diese schreiben dann die Datei `Solution.Project.md` in ihren jeweiligen Unterordner.
3. **Builder-Pattern:** Implementiere einen zentralen Builder oder Generator für das Markdown-Dokument, der den YAML-Header, das Manifest und den Content strikt zusammensetzt, sodass diese Logik nicht in jedem View einzeln dupliziert werden muss.

Bitte analysiere den aktuellen Code und passe die Pipeline so an, dass sie exakt diesen Zielzustand erfüllt. Erstelle einen Umsetzungsplan und warte auf Bestätigung, bevor du große Refactorings beginnst.
````</Bytes></Bytes></Anzahl_der_Dateien_in_diesem_Dokument></ISO_8601_Timestamp></Neu_generierte_Guid>
