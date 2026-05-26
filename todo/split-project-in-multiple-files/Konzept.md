# Problem
Wir haben größere Projekte bei denen die Markdown Datei größe auf über 1.6MB anwächst.
Beispiel: `C:\Daten\Entwicklung\SAN\San.smart.Planner.Platform\San.smart.Planner.Platform\*`

Wenn ich diese Markdown Datei einem Web-LLM gebe hat es arge Mühe den Quellcode zu lesen.
Die reine Dateigröße wird ein Problem sein, aber vermutlich auch den Kontext zu finden.

## Manuelle Selektion
WENN ich manuell den richtigen Kontext VORHER wüsste dann könnte ich natürlich nur das in eine Markdown Datei packen lassen was notwendig ist.
Aber im Grunde weiß ich garnicht was wirklich notwendig ist weil sich das erst durch die Interaktion und den Chat ergibt.
Das ist zumindest meine These.
Das ist aber manuell effektiv nicht machbar

## Projekt besser strukturieren
Das Projekt selbst könnte man auch in sinnvolle Unter-Projekte untergliedern.
Schau dir gerne das genannte Beispielprojekt an.
Was ich aber immer vermeiden will ist das wir eine VS-Solution haben die 20 Projekte und dazu jeweils Test-Projekte hat (also insgesamt über 40 Projekte).
Das überblickt auch keiner.
In wie fern könnte man das Beispiel Projekt besser strukturieren?
Handler extrahieren (Pro Handler ein Projekt = rund 5 Projekte)?
Nur ein Unit-Test Projekt?
Auth, wwwroot (js), andere Dinge Auslagern in eigene Projekte (etwas in Richtung Domain Driven Design aber ohne das der code explodiert)?

## SourceToAi erzeugt mehrere .md-Dateien nach Kontext

In SourceTo hat haben wir den Roslyn Parser, der analysiert den Quellcode.
Unsere Projekte sind mindestens mal nach Namespaces strukturiert.
Meine Idee wäre: 
Man kann irgendwie definieren "pro Solution N-Mardown Dateien" und "maximal größe N-Kilobyte".
SourceToAi könnte dann vorher eine Art Analyse durchführen: "in Namespace Handler sind 50kb" und in "Namespace Auth 10kb" usw. usf. und dann Merge-md-dateien machen oder eine  die nur bestimmte dinge beinhaltet damit die Anzahl der Markdown Dateien der definierten entspricht.
Es könnte beispielsweise  für "...voller.namespace-Handler.md" geben die ist vielleicht 70kb dann eine "..voller.namespace.Auth.md" die ist vielleicht 40kb und alles andere dann in eine "..voller.namespace.rest.md" was dann 90kb wären.
Ziel wäre die markdown dateien fachlich möglichst sinnvoll aufzuteilen.
Ich denke namespaces ist das einzige was wir in SourceToAi tun können da wir von dem anderen code keine fachliche Ahnung haben.
Das ganze wäre optional und per Parameter gesteuert.

## Dein Task

Mache eine Analyse zu meinen Gedanken und erstelle eine Markdown Datei mit deinen Ratschlägen und Vorschlägen in `todo\split-project-in-multiple-files\`.