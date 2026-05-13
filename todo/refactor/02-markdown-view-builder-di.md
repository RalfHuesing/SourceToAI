# Schritt 02: Markdown-View-Builder — Boilerplate-Wrapper entfernen

## Ziel (laut Konzept)

Die vier fast leeren Klassen in `MarkdownConcreteProjectViewBuilders.cs` entfernen, die nur `MarkdownProjectViewBuilderBase` mit statischen Booleans verkabeln. Stattdessen **vier gleichwertige `IMarkdownProjectViewBuilder`-Registrierungen** über Factory/Delegate in der `IServiceCollection`.

## Ist-Analyse

### Wrapper-Datei

`SourceToAI.CLI/Services/Processing/Markdown/MarkdownConcreteProjectViewBuilders.cs` — vier `sealed class`:

| Klasse | `includeNonCSharpFiles` | `passOriginalSourceTextForCSharp` | Keyed `IViewGenerator` |
|--------|-------------------------|-----------------------------------|-------------------------|
| `CompleteMarkdownProjectViewBuilder` | `true` | `true` | `MarkdownViewKeys.Complete` |
| `SignaturesOnlyMarkdownProjectViewBuilder` | `false` | `false` | `MarkdownViewKeys.SignaturesOnly` |
| `PublicOnlyMarkdownProjectViewBuilder` | `false` | `false` | `MarkdownViewKeys.PublicOnly` |
| `DtoOnlyMarkdownProjectViewBuilder` | `false` | `false` | `MarkdownViewKeys.DtoOnly` |

Konstruktorparameter: `ICSharpDocumentLoader` + `[FromKeyedServices(...)] IViewGenerator`.

### Basisklasse

`SourceToAI.CLI/Services/Processing/Markdown/MarkdownProjectViewBuilderBase.cs`:

- Ist als `public abstract class … : IMarkdownProjectViewBuilder` deklariert, implementiert aber **keine** abstrakten Member — die `abstract`-Markierung dient nur der Verhinderung direkter Instanziierung.
- Enthält die gesamte `BuildContentSegments`-Logik.

### DI-Registrierung

`SourceToAI.CLI/Infrastructure/MarkdownViewBuilderServiceCollectionExtensions.cs`:

```csharp
services.AddTransient<IMarkdownProjectViewBuilder, CompleteMarkdownProjectViewBuilder>();
// … drei weitere analog
```

### Interface

`IMarkdownProjectViewBuilder` in `IMarkdownProjectViewBuilder.cs` — unverändert lassen (Schritt 03 kann Rückgabetyp später ändern).

### Tests / Hosts

Suche nach `CompleteMarkdownProjectViewBuilder` / `MarkdownConcreteProjectViewBuilders` — typischerweise **nur** `AddMarkdownProjectViewBuilders()` in:

- `SourceToAI.Tests/Processing/MarkdownProjectViewBuilderTests.cs`
- `SourceToAI.Tests/App/ConsoleOrchestratorTests.cs`
- `SourceToAI.Tests/App/MultiViewExportTestHost.cs`

Erwartung: Tests nutzen die Extension-Methode; **keine** direkten Typreferenzen auf die Wrapper nötig.

## Umsetzung

1. **`MarkdownProjectViewBuilderBase` konkret machen:** `abstract` entfernen (Klassenname kann `MarkdownProjectViewBuilder` bleiben oder umbenannt werden — bei Umbenennung alle `using`/Verweise prüfen). `sealed` ist optional (meist nicht nötig).
2. **`MarkdownViewBuilderServiceCollectionExtensions`:** Vier mal `AddTransient<IMarkdownProjectViewBuilder>(_ => new MarkdownProjectViewBuilderBase(...))` **oder** benannte lokale Factory, die auflöst:
   - `sp.GetRequiredService<ICSharpDocumentLoader>()`
   - `sp.GetRequiredKeyedService<IViewGenerator>(MarkdownViewKeys.*)`
   - die beiden Booleans wie in der Tabelle oben.
3. **Datei `MarkdownConcreteProjectViewBuilders.cs` löschen** (oder leer lassen — **löschen** ist sauberer).
4. **Csproj:** Falls die `.cs` explizit eingetragen war (unüblich bei SDK-Style) — prüfen.
5. **`dotnet test`** — insbesondere `MarkdownProjectViewBuilderTests` (wählt Builder per `ViewKey` aus der enumerierten Collection).

## Hinweise

- `[FromKeyedServices]` wird in den Factory-Registrierungen **nicht** benötigt; nur noch normales `GetRequiredKeyedService`.
- `Program.cs` ruft nur `AddMarkdownProjectViewBuilders()` — meist **keine** Änderung nötig.

## Nicht tun

- Keine Änderung an `MultiViewExportService` / `ViewKeyOrder` (außer falls ihr in Schritt 01/03 Keys zentralisiert — optional).
- Schritt 01 (ViewGenerators) nicht zwingend voraussetzen; dieser Schritt ist DI-rein für Markdown-Builder.

## Erfolgskriterien

- Weiterhin **genau vier** `IMarkdownProjectViewBuilder` in `GetServices<IMarkdownProjectViewBuilder>()`, gleiche `ViewKey`s.
- Kein toter Code / keine leeren Subklassen-Datei mehr.
