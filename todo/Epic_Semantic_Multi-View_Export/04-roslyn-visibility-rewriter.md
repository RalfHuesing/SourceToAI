# Task 04: Roslyn — `VisibilityRewriter` (Public/Protected-Only)

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken — nicht nur im Chat oder im Commit beschreiben.

## Ziel

- Klasse `VisibilityRewriter : CSharpSyntaxRewriter` (oder klar benannter `PublicApiRewriter`).
- Entferne Member, deren Modifier **`private`** oder **`internal`** enthalten (`SyntaxKind.PrivateKeyword`, `SyntaxKind.InternalKeyword`).
- **Wichtig:** `private`/`internal` **verschachtelte Typen** (private nested class) — konzeptkonform entfernen oder nur Member? **Konzept:** „NUR public/protected Member“ — verschachtelte private Typen entfernen.
- Typen auf Datei-/Namespace-Ebene: wenn die Klasse `internal` ist, ist die ganze Deklaration für einen **öffentlichen API-Export** typischerweise wegzulassen oder zu leeren — **Entscheidung:** Nur `public`/`protected` Top-Level-Typen behalten; `internal` Klasse ganz entfernen (mit Tests).
- Nach Rewrite: kompilierbar wo möglich; unnötige `using`-Direktiven können bleiben (kein Muss für Epic).

## Abhängigkeiten

- `01`, `02`; Kombination mit `03` erst in `06` (Reihenfolge der Rewriter festlegen: z. B. zuerst Visibility, dann Signatures für `public-api` vs. nur Signatures für andere — in `06` dokumentieren).

## Tests (Pflicht)

- Quelltext mit `public void A()`, `private void Secret()`, `internal class X` — im Ergebnis **kein** Vorkommen von `Secret` / `X` (Identifier-Assert vorsichtig: substring oder SyntaxWalker).
- Negativtest: `public` Methoden mit `private` **lokalen** Funktionen — lokale Funktionen sollen nicht als „Member“ durchrutschen, falls sie im Output stören (konzept: public API — lokale Funktionen entfernen oder einkapseln; **testen**).

## Selbstverifikation

- [x] `00`-Checkliste: „private Methoden nicht in public-only“ — dedizierter Testname, grep-freundlich.
- [x] `protected` und `public` bleiben erhalten.
- [x] `dotnet test` grün.

## Nächster Schritt

`05-dto-filter.md`
