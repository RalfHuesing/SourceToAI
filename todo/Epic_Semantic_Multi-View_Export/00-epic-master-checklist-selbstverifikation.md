# Epic: Semantic Multi-View Export — Master-Checkliste & Selbstverifikation

Diese Datei ist die **Querschnitts-Referenz**. Jede nummerierte Task-Datei (`01-…` bis `09-…`) enthält eigene **Schritt-Selbstverifikation**. Vor Epic-Abschluss hier **alles** abhaken.

## Pflicht für umsetzende Agenten (Cursor o. Ä.)

Wenn eine nummerierte Task (`01-…`–`09-…`) **inhaltlich erledigt** ist: den Fortschritt **in den Markdown-Dateien** nachziehen — nicht nur im Chat. Konkret die **Selbstverifikation**-Checkboxen der betroffenen Task-Datei von `- [ ]` auf `- [x]` setzen. Betrifft die Arbeit auch Checkboxen oder dokumentierte Kriterien **in dieser** Master-Datei, diese dort **ebenfalls** abhaken bzw. den Text anpassen. Ein abgeschlossener Task nur in der Konversation, ohne aktualisierte `.md`, gilt als **unvollständig**.

## Abdeckungs-Matrix (Konzept → Tasks)

| Anforderung aus `konzept.md` | Primär umgesetzt in |
|------------------------------|----------------------|
| Parse Once, Rewrite Multiple | `01` (`ICSharpDocumentLoader`, `ParsedCSharpDocument`, `CSharpDocumentLoader`), `06` |
| `IViewGenerator` / `ICodeProcessor` + Strategy | [x] `02` — `SourceToAI.CLI/Services/Processing/IViewGenerator.cs` |
| [x] `SignaturesRewriter` (Bodies `;`, expression-bodied) | `03` — `SourceToAI.CLI/Services/Processing/Rewriters/SignaturesRewriter.cs` |
| [x] `VisibilityRewriter` (kein private/internal im public-export) | `04` — `SourceToAI.CLI/Services/Processing/Rewriters/VisibilityRewriter.cs` |
| `DtoFilter` (records, enums, property-only-Klassen) | [x] `05` — `SourceToAI.CLI/Services/Processing/Rewriters/DtoRewriter.cs` |
| View-Builder + Markdown (`csharp`-Fences, Pfade) | [x] `06` — `SourceToAI.CLI/Services/Processing/Markdown/` |
| [x] `dependency-graph.md` (csproj) | `07` — `SourceToAI.CLI/Services/Export/CsprojDependencyGraphMarkdownGenerator.cs`, `ConsoleOrchestrator` |
| Orchestrierung, Ordner, `readme.md` | [x] `08` — `ConsoleOrchestrator`, `MultiViewExportService`, `MultiViewReadmeMarkdownGenerator` |
| Output-Struktur exakt wie Konzept | [x] `01`, `08` |
| `output` bei Start sauber / neu | [x] `01`, `08` |
| Tests überall | jeweilige Steps + `09` |
| Performance: Datei nur 1× lesen (für `.cs`) | `01`, Verifikation `09` |

## Ziel-Verzeichnisstruktur (Definition of Done — Dateisystem)

Nach einem Lauf (pro Solution/Export-Root wie in der Architektur festgelegt):

```text
…/                          # z. B. bestehendes exportPath/solutionName oder vereinbarter Root
├── readme.md
├── dependency-graph.md
├── complete/
│   └── full-source.md
├── signatures-only/
│   └── signatures.md
├── public-only/
│   └── public-api.md
└── dto-only/
    └── models.md
```

**Hinweis zur Integration (Stand Task 08):** Unter `{exportPath}/{solutionName}` liegt nur noch der **Konzept-Baum** (`readme.md`, `dependency-graph.md`, `complete/`, …). Flache datierte Projekt-Feeds entfallen; `complete/full-source.md` übernimmt den 1:1-Inhalt. `IFeedGenerator`/`MarkdownFeedGenerator` bleiben für Tests und künftige Nutzung registriert, der Orchestrator ruft sie nicht mehr auf.

## Architektur-Regeln (nicht verhandelbar)

1. Jede `.cs`-Datei: **einmal** `File.ReadAllText` (o. Ä.) + **einmal** `CSharpSyntaxTree.ParseText` → gespeichertes `SyntaxTree`/`SyntaxNode` für alle Rewriter.
2. Kein erneutes Einlesen derselben Datei für andere Views.
3. Rewriter implementieren als `CSharpSyntaxRewriter` (oder dokumentierte gleichwertige Filter-Pipeline), robust für expression-bodied Members.
4. Neue Services: **Interface + DI** in `Program.cs` (Projektrichtlinien).

## Finale Epic-Selbstverifikation (Agent / Mensch)

**Vor Merge / vor „Epic fertig“:**

- [ ] Matrix oben: jede Zeile mit PR-/Commit-Referenz oder Dateipfad belegt.
- [x] Manuell oder per Test: Ordnerbaum wie oben vorhanden.
- [ ] `signatures-only/signatures.md`: Stichprobe mit Roslyn oder `dotnet` — syntaktisch valide C#-Schnittstellen (keine halben Bodies).
- [ ] `public-only/public-api.md`: Stichprobe — **kein** Body von klar `private`/`internal` Methoden; grep nach bekanntem privaten Test-Member negativ.
- [ ] `dto-only/models.md`: enthält keine „vollen“ Service-Klassen mit Logik-Methoden (laut Definition in `05`).
- [x] `dependency-graph.md`: alle `.csproj` des Scans mit Package/Project-Referenzen abgedeckt (oder dokumentierte Ausnahme).
- [x] `readme.md`: Projektname (aus Root), Zeitstempel, **Erklärung jedes Unterordners** für Prompt-Use-Cases.
- [ ] `complete/full-source.md`: entspricht bisherigem „alles 1:1“-Export (inkl. Nicht-`.cs`, sofern im Konzept gefordert).
- [ ] Alle Unit-/Integrationstests grün (`dotnet test`).
- [x] `IPostExportTask`-Hooks: Verhalten unverändert oder bewusst angepasst und getestet.

## Bekannte Fallstricke (bewusst gegenlesen)

- Expression-bodied Properties/Methods (`=>`) in Signatures-View.
- `file`-scoped namespaces, `partial`, `record struct`, Primary Constructors (.NET 8+).
- `internal` in `public`-Klassen: Member müssen verschwinden, nicht nur Klassen.
- DTO-Heuristik: false positives/negatives — Tests mit Gegenbeispielen.
- Markdown-Fencing: Backticks im Quelltext (`MarkdownFenceUtility.CalculateRequiredBackticks`, zentral extrahiert).

---

**Nächster Schritt:** `01-pipeline-parsed-document-output-aufräumen.md`
