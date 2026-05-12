# Epic: Semantic Multi-View Export

## 1. Zielsetzung (Objective)
Der aktuelle `MarkdownFeedGenerator` exportiert den gesamten Quellcode in eine einzige Markdown-Datei. Dies erzeugt bei großen Repositories zu viel Rauschen (Tokens) für LLMs. 
Das Ziel ist es, den Export in verschiedene, spezialisierte Sichten ("Views" / Context-Densities) aufzuteilen. Alle Views sollen bei jedem Durchlauf standardmäßig (ohne Extra-Parameter) generiert werden. Die Transformation des Codes erfolgt AST-basiert via **Roslyn (Microsoft.CodeAnalysis.CSharp)**.

## 2. Ziel-Verzeichnisstruktur
Das Output-Verzeichnis soll nach dem Durchlauf exakt so aussehen:

```text
output/
├── readme.md                       # Inhaltsverzeichnis, Meta-Daten, Erklärung der Ordner
├── dependency-graph.md             # Auflistung aller .csproj Referenzen & NuGet Packages (ohne Code)
├── complete/
│   └── full-source.md              # Der bisherige Export: Alle Dateien 1:1 unverändert
├── signatures-only/
│   └── signatures.md               # Nur Namespaces, Klassen, Interfaces & Member-Signaturen (keine Bodies)
├── public-only/
│   └── public-api.md               # Code-Export, der NUR public/protected Member enthält (inklusive Bodies)
└── dto-only/
    └── models.md                   # Enthält NUR records, enums, DTOs (Klassen, die nur Properties haben)
```

## 3. Architektonische Vorgaben (CRITICAL für Cursor)
Bitte beachte bei der Implementierung zwingend folgende Architektur-Regeln:

1. **Parse Once, Rewrite Multiple Times (Performance):**
   - Die `.cs` Dateien auf der Festplatte dürfen **nur ein einziges Mal** eingelesen und in einen Roslyn `SyntaxTree` geparst werden.
   - Dieser *eine* Syntaxbaum (bzw. die Root-Node) wird dann an verschiedene `CSharpSyntaxRewriter` (oder Filter-Logiken) übergeben, um die unterschiedlichen Views zu generieren.
2. **Strategy Pattern für Views:**
   - Erstelle ein Interface `IViewGenerator` oder `ICodeProcessor`, das eine Methode wie `string Process(SyntaxNode rootNode)` anbietet.
   - Jede View (`complete`, `signatures-only`, etc.) wird als eigene Klasse implementiert, die dieses Interface erfüllt.
3. **Robustes Rewriting (Roslyn):**
   - **Signatures-Only:** Ersetze `BlockSyntax` von Methoden, Konstruktoren, etc. durch ein `SemicolonToken`. Achtung bei Expression-bodied Members (`=>`), diese müssen ebenfalls entfernt und durch ein Semikolon ersetzt werden.
   - **Public-Only:** Filtere Member heraus, die als `private` oder `internal` markiert sind.
   - **DTO-Only:** Behalte nur `enum`, `record` und Klassen, die keine komplexen Methoden enthalten (Klassen, die nur Properties/Felder haben).
4. **Erweiterbarkeit:**
   - Der `ConsoleOrchestrator` (oder die Main-Pipeline) iteriert über eine Liste von registrierten View-Generatoren und schreibt deren Ergebnisse in die entsprechenden Unterordner.

## 4. Implementierungs-Schritte (Step-by-Step Plan)

**Step 1: Refactoring der File-Reading Pipeline & Basis-Setup**
- Trenne das Einlesen der Dateien vom Schreiben der Markdown-Datei.
- Baue einen zentralen Service, der alle `.cs` Dateien sammelt, einliest und per `CSharpSyntaxTree.ParseText()` in den AST überführt. Die Ergebnisse (Pfad + AST) werden im Speicher gehalten (z.B. `IEnumerable<ParsedDocument>`).
- Stelle sicher, dass das `output` Verzeichnis bei jedem Start aufgeräumt/neu angelegt wird.

**Step 2: Implementierung der Roslyn Rewriter (Die Kern-Logik)**
Implementiere die verschiedenen Logiken, die den AST modifizieren:
- `SignaturesRewriter`: Erbt von `CSharpSyntaxRewriter`. Besucht Methoden/Properties und entfernt die Bodies.
- `VisibilityRewriter`: Erbt von `CSharpSyntaxRewriter`. Entfernt Nodes, die `SyntaxKind.PrivateKeyword` oder `SyntaxKind.InternalKeyword` in den Modifiern haben.
- `DtoFilter`: Prüft eine Klasse/Datei, ob sie in die Kategorie "Data-Transfer-Object / Model" fällt.

**Step 3: Implementierung der "View Builder" (Markdown Generierung)**
Erstelle Klassen für jede View, die die manipulierten ASTs entgegennehmen und den entsprechenden Markdown-String (mit ` ```csharp ` Blöcken und Dateipfaden als Überschriften) zusammenbauen.
- `CompleteViewBuilder` (nutzt Original-Code)
- `SignaturesOnlyViewBuilder` (nutzt `SignaturesRewriter`)
- `PublicOnlyViewBuilder` (nutzt `VisibilityRewriter`)
- `DtoOnlyViewBuilder` (nutzt den `DtoFilter`)

**Step 4: Implementierung der Non-Code Views**
- Erstelle einen Parser für `.csproj` Dateien.
- Generiere die `dependency-graph.md`: Extrahiere `<PackageReference>` und `<ProjectReference>` aus allen C# Projekten und stelle sie als saubere Markdown-Listen oder Markdown-Tabellen dar.

**Step 5: Orchestrierung und Readme.md (Finalisierung)**
- Verbinde alles in der Hauptlogik.
- Iteriere durch alle View-Builder, generiere den Text, erstelle die Unterordner und speichere die Dateien ab.
- Generiere dynamisch die `output/readme.md`. Diese muss Folgendes enthalten:
  1. Den Namen des Projekts (abgeleitet aus dem Root-Ordner).
  2. Einen Zeitstempel der Generierung.
  3. Eine Erklärung der generierten Unterordner, damit der Entwickler (oder die KI) sofort weiß, welche Datei für welchen Use-Case (Prompts) am besten geeignet ist.

## 5. Definition of Done
- Wenn ich das Tool starte, wird der `output` Ordner mit allen Unterverzeichnissen und Dateien generiert.
- Cursor/KI kann die `signatures-only` Datei fehlerfrei als C#-Code lesen (syntaktisch valide Schnittstellen).
- Die Performance ist weiterhin hoch (Dateien werden nur 1x eingelesen).
- Code aus `private` Methoden taucht im `public-only` Export **nicht** auf.