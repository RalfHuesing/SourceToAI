# Schritt 01: View-Generatoren zusammenführen

## Ziel (laut Konzept)

Statt vier eigener Klassen (`CompleteViewGenerator`, `SignaturesOnlyViewGenerator`, `PublicOnlyViewGenerator`, `DtoOnlyViewGenerator`) die Roslyn-Sichten mit **weniger Typen** abbilden — ideal: ein wiederverwendbarer Generator + DI-Registrierung mit unterschiedlicher Logik.

## Ist-Analyse

### Dateien (alle unter `SourceToAI.CLI/Services/Processing/ViewGenerators/`)

| Klasse | `ViewKey` | Logik |
|--------|-----------|--------|
| `CompleteViewGenerator` | `complete` | Wenn `context.OriginalSourceText != null` → diesen Text zurück; sonst `root.ToFullString()`. `HasExportableSurface: true`. |
| `SignaturesOnlyViewGenerator` | `signatures-only` | `SignaturesRewriter.Rewrite(root)` → `ToFullString()`, `HasExportableSurface` via `CSharpCompilationUnitExportSurface.HasExportableSurface(rewritten)`. |
| `PublicOnlyViewGenerator` | `public-only` | `VisibilityRewriter.Rewrite(root)` — gleiches Muster wie signatures-only. |
| `DtoOnlyViewGenerator` | `dto-only` | `DtoRewriter.Rewrite(root)` — gleiches Muster. |

**Wichtig:** Die drei Rewriter-Views sind strukturell identisch (Rewriter → Text → Surface-Check). **Complete** ist **kein** einfaches `Func<CompilationUnitSyntax, CompilationUnitSyntax>` — er braucht `ViewGeneratorContext.OriginalSourceText`.

### DI-Registrierung

`SourceToAI.CLI/Infrastructure/ViewGeneratorServiceCollectionExtensions.cs`:

- `AddKeyedTransient<IViewGenerator, CompleteViewGenerator>(MarkdownViewKeys.Complete)` usw.
- Schlüsselkonstanten: `SourceToAI.CLI/Infrastructure/MarkdownViewKeys.cs` (`complete`, `signatures-only`, `public-only`, `dto-only`).

### Vertrag

`SourceToAI.CLI/Services/Processing/IViewGenerator.cs`: `Generate` liefert weiter `ExtractionResult<ViewGenerationResult>` (Schritt 03 kann das später anfassen — hier **unverändert** lassen, außer ihr führt 03 zuerst aus).

### XML-Doku

`MarkdownProjectViewBuilderBase.cs` (Remarks-Liste) nennt noch die konkreten Generator-Klassennamen — nach Umbau auf **neue Typnamen** anpassen.

### Tests

`SourceToAI.Tests/Processing/ViewGeneratorDiTests.cs`:

- `AddViewGenerators_keyed_only_no_unkeyed_collection` / `AddViewGenerators_registers_keyed_services_matching_view_keys` — bleiben sinnvoll, nur keine festen Typnamen `CompleteViewGenerator` nötig.
- `View_generators_empty_compilation_unit_succeeds_with_whitespace_only_or_empty` — instanziiert aktuell die vier konkreten Klassen; auf neue Fabrik/Implementierung umstellen.
- `Complete_view_prefers_original_source_text_when_context_supplies_it` — Verhalten **1:1** erhalten.

## Umsetzungsvorschläge (wählbar)

### Option A (nah am Konzept „ein Generator + Delegate“)

1. Neue Klasse z. B. `RoslynRewriteViewGenerator` (`sealed`) mit Konstruktor-Parametern:
   - `string viewKey`
   - `Func<CompilationUnitSyntax, CompilationUnitSyntax> rewrite`
2. Registrierung für die drei Rewriter-Views mit jeweils passendem Lambda: `root => SignaturesRewriter.Rewrite(root)` etc.
3. **Complete** separat: kleine Klasse `CompleteViewGenerator` **behalten** (minimal) **oder** zweite Implementierung `OriginalOrFullStringViewGenerator` mit nur Complete-Logik — weiterhin 2 Typen statt 5.

### Option B (ein Typ für alle vier)

Ein `ConfigurableViewGenerator` mit Delegate `Func<CompilationUnitSyntax, ViewGeneratorContext, ViewGenerationResult>` und vier statischen Factory-Methoden / vier keyed `AddKeyedTransient`-Factory-Lambdas. Ein Typ, aber explizite Konfiguration pro Key.

**Empfehlung:** Option A — klar getrennt: „Complete“ vs. „Rewriter-Pipeline“, wenig Magie.

## Konkrete Aufgabenliste

1. Neue Generator-Datei(en) anlegen; alte vier Dateien entfernen (oder nur drei entfernen, Complete minimal behalten — je nach Option).
2. `ViewGeneratorServiceCollectionExtensions.AddViewGenerators` auf neue Typen/`AddKeyedTransient` mit Factory umstellen (falls Generatoren nicht parameterloser `new()` sind: `AddKeyedTransient<IViewGenerator>((sp, key) => …)` oder äquivalente Überladung nutzen).
3. `ViewGeneratorDiTests` anpassen; ggf. `using`-Aufräumen.
4. XML in `MarkdownProjectViewBuilderBase.cs` aktualisieren (Rewriter-Namen `SignaturesRewriter` etc. **beibehalten**, nur Generator-Klassennamen ersetzen).
5. `dotnet build` + `dotnet test` im Repo-Root.

## Nicht tun (Scope)

- `ExtractionResult` hier nicht refaktorisieren (Schritt 03).
- `MarkdownConcreteProjectViewBuilders` nicht anfassen (Schritt 02).

## Erfolgskriterien

- Alle vier `ViewKey`-Werte unverändert; keyed DI wie bisher.
- Gleiche Ausgabetexte und `HasExportableSurface`-Semantik für leere Compilation Units und Complete mit `OriginalSourceText`.
- Keine neuen Warnungen.
