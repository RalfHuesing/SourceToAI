# Task 03: Roslyn — `SignaturesRewriter`

## Ziel

- Klasse `SignaturesRewriter : CSharpSyntaxRewriter`.
- Methoden, Konstruktoren, Operatoren, Accessors (`get`/`set`): **Body** entfernen und durch **`SemicolonToken`** ersetzen (klassischer `CSharpSyntaxRewriter`-Stil).
- **Expression-bodied** Members (`=> expr`): ebenfalls zu `;`-Form umformen (kein `=>` im Output).
- Properties: auto-Property kann zu ` { get; set; }` ohne Initializer werden — konsistent und **kompilierbar**.
- Indexer, Finalizer, static constructors — falls im Scope: gleiche Regel.
- Keine Änderung an Namespace-/Using-Struktur außer was nötig ist für Syntaxvalidität.

## Abhängigkeiten

- `01`, `02` (Rewriter kann zunächst standalone getestet werden, Integration in Generator in `06`).

## Tests (Pflicht)

- Theory-/ parametrisierte Tests mit **embedded source strings** (normal body, `=>` method, `=>` property, constructor, event accessors wenn relevant).
- Assert: Output mit `CSharpSyntaxTree.ParseText` erneut parsen — **keine** Parser-Fehler-Diagnostiken vom Schweregrad `Error` (oder explizit erlaubte Ausnahmen dokumentiert).
- Snapshot optional; mindestens string-Contains/NotContains auf Body-Inhalten, die entfernt sein müssen.

## Selbstverifikation

- [ ] Primary Constructors / record-Syntax: mindestens **gelesen** und mit Test abgedeckt oder als „Known limitation“ in `00` nachgetragen (lieber abdecken).
- [ ] `00`-DoD: „signatures syntaktisch valide“ — hier mit Tests vorab absichern.
- [ ] Kein doppeltes File-Read — Rewriter bekommt nur `SyntaxNode`.
- [ ] `dotnet test` grün.

## Nächster Schritt

`04-roslyn-visibility-rewriter.md`
