
### b) Was sollte gestrafft / entfernt werden? (Refactoring vor dem Feature)

Um eine solide Basis für den Umbau zu haben, solltest du folgende Dinge straffen, um die Komplexität ("Anti-Bloat" laut deinen Projektrichtlinien) zu senken:

**1. Entferne die redundanten View-Builder-Klassen**
In `MarkdownConcreteProjectViewBuilders.cs` hast du vier fast leere Klassen (`CompleteMarkdownProjectViewBuilder`, `SignaturesOnlyMarkdownProjectViewBuilder`, etc.), die nur den `base`-Konstruktor mit statischen Booleans aufrufen.

* **Straffung:** Lösche diese Klassen. Registriere stattdessen direkt die abstrakte Basisklasse (die du dann konkret machst) über Factory-Delegates in der `IServiceCollection`. Das reduziert Boilerplate-Code drastisch.

**2. Straffe das Error-Handling (`ExtractionResult<T>`)**
Dein `ExtractionResult<T>` ist in Ordnung, führt aber im `ConsoleOrchestrator` und `MultiViewExportService` zu massiven `if (!result.IsSuccess)` Kaskaden.

* **Straffung:** Für kritische Fehler (wie IO-Exceptions auf Root-Ebene) solltest du einfache Exceptions werfen und diese zentral in der `Program.cs` fangen. Das `ExtractionResult` ist nützlich für *Warnings* (wie übersprungene Dateien), aber als reiner Kontrollfluss-Ersatz bläht es den Code künstlich auf.

**3. Entschlacke die `ViewGenerators**`
Deine Generatoren (`DtoOnlyViewGenerator`, `PublicOnlyViewGenerator`, etc.) rufen eigentlich nur eine statische `Rewrite`-Methode auf und verpacken das Ergebnis.

* **Straffung:** Du brauchst nicht für jede Sicht eine eigene Klasse. Ein generischer `RoslynRewriteViewGenerator` mit einem Delegaten `Func<CompilationUnitSyntax, CompilationUnitSyntax> rewriter` genügt vollkommen und spart dir vier Klassen.

**4. Ersetze das harte Argument-Parsing**

* **Straffung:** Nutze ein leichtgewichtiges CLI-Paket (wie `System.CommandLine` oder `Cocona`), anstatt das Array `args` manuell zu verarbeiten. Das macht die Implementierung deines Ziels ("mehrere Dateien/Verzeichnisse übergeben") zu einem Einzeiler (`string[] inputPaths`).

