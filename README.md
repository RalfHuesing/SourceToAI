# SourceToAI – KI-Feed aus .NET-Quellen

<img width="1905" height="1128" alt="SourceToAI Übersicht" src="https://github.com/user-attachments/assets/3d85e7d9-36ae-4541-bb38-00cccf4b315e" />

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**SourceToAI** ist ein eigenständiges .NET-10-CLI-Tool. Es liest lokale C#-Solutions (oder entpackt kompilierte .NET-Assemblies) und erzeugt daraus **Markdown-KI-Feeds** mit Metadaten, Manifest und mehreren „Views“ (vollständiger Code, Signaturen, öffentliche API, DTO-Fokus). Alles läuft offline auf deiner Maschine.

---

## Was es macht

- **Multi-View-Export:** In den Ordnern `complete/`, `signatures-only/`, `public-only/` und `dto-only/` liegt pro Projekt jeweils **eine** Markdown-Datei. Dateinamen tragen das View-Suffix: `<Solution>.<Projekt>-<view>.md` (z. B. `MySol.Proj-complete.md`). Virtuelle Solution-Doku (Root-`README`, `.cursor/rules` usw.) erscheint in `complete/` als `<Solution>..Docs-complete.md`.
- **Mehrere Quellen in einem Lauf:** Du gibst **ein** globales Export-Ziel und **eine oder mehrere** Quellen an. Jede Quelle wird nacheinander verarbeitet. Workspace-weite AI-Kontextdateien fließen in den gemeinsamen Baum `Merged/` ein, isolierte projektbezogene Dateien landen in `Isolated/<SolutionName>/`.
- **Quellen:** Verzeichnis mit `.sln`/`.csproj` (typisch Repository- oder Solution-Stamm) **oder** eine **.dll/.exe**-Assembly. Bei Assemblies wird der Code zuerst per Decompiler in ein temporäres `decompile/`-Projekt unter `Isolated/<AssemblyName>/decompile/` gelegt und von dort wie gewohnt exportiert.
- **Robustheit:** Build-Artefakte und übliche Tool-Ordner werden standardmäßig ignoriert; Lesefehler in Unterzweigen führen zu Warnungen, nicht zum kompletten Abbruch.

---

## Nutzung mit Web-KIs (Kurz)

1. Export ausführen (siehe unten).
2. Aus dem gemeinsamen Ordner `Merged/complete/` die passenden `<Solution>..Docs-complete.md`- und Projekt-MD-Dateien (z. B. `...-complete.md`) in den Chat laden.
3. Im Prompt auf Manifest-Einträge und Views verweisen (Details stehen in der generierten `readme.md` im jeweiligen Solution-Exportordner).

---

## Installation

1. Unter [Releases](../../releases) die passende ZIP für dein Betriebssystem laden und entpacken, **oder**
2. Repository klonen und mit [.NET 10 SDK](https://dotnet.microsoft.com/download) bauen: `dotnet build` / `dotnet run --project SourceToAI.CLI`.

Für ein einzelnes, portables Binary: im CLI-Projekt z. B. `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true` (siehe Kommentar in `SourceToAI.CLI.csproj`).

---

## Kommandozeile

**Syntax (eine Variante wählen – nicht mischen):**

- **Positionsargumente:** `SourceToAI <Export-Pfad> <Quelle> [<Quelle> …]`
- **Optionen:** `SourceToAI --export <Export-Pfad> --input <Quelle> [--input <Quelle> …]` (Kurzform: `-i`)

**Quelle** ist jeweils ein existierendes **Verzeichnis** (Solution/Repo mit `.sln` oder `.csproj`) oder eine **.dll**-/.**exe**-Assembly.

**Beispiele:**

```cmd
SourceToAI C:\AI_Feeds\Exports C:\Daten\RepoA\ C:\Daten\RepoB\
```

```cmd
SourceToAI C:\AI_Feeds\Exports C:\Apps\MyLib\bin\Debug\net10.0\MyLib.dll
```

```cmd
SourceToAI --export ./exports -i C:\Daten\RepoA\ -i C:\Daten\RepoB\
```

---

## Ausgabeordner und Sicherheit

Unter dem globalen `<Export-Pfad>` bereitet das Tool einen Verzeichnisbaum vor:
- Ein globales `readme.md` mit Nutzungshinweisen.
- Ein Verzeichnis `Merged/`, in dem alle Projekte aller exportierten Solutions vereint und nach Views gruppiert liegen.
- Ein Verzeichnis `Isolated/`, in dem jede Solution (z. B. `<SolutionName>`) ihren eigenen Unterordner behält. Dort finden sich z. B. der lösungsspezifische `dependency-graph.md` und bei Assembly-Quellen zusätzlich `decompile/` mit dem erzeugten C#-Baum.

**Wichtig:** Bevor ein bestehender globaler Exportordner überschrieben wird, muss direkt im `<Export-Pfad>` die von SourceToAI angelegte Markerdatei **`.sta-marker`** liegen. Fehlt sie (z. B. Ordner war nie ein SourceToAI-Export oder wurde manuell angelegt), bricht die CLI ab, um **versehentlichen Datenverlust** zu vermeiden. Zum erneuten Export: Ordner leeren oder bewusst `.sta-marker` anlegen – siehe Konsolenmeldung.

---

## Konfiguration (`appsettings.json`)

Die Datei muss **neben der ausführbaren Datei** liegen (wird mit ausgeliefert). Dort werden u. a. ignorierte Verzeichnisse und erlaubte Dateiendungen festgelegt.

```json
{
    "SourceToAI": {
        "ExcludedDirectories": [ "bin", "obj", ".git", ".vs", ".idea", "node_modules" ],
        "IncludedExtensions": [ ".cs", ".sql", ".json", ".xml", ".xaml", ".md", ".mdc", ".js", ".ts", ".css" ]
    }
}
```

---

## Lizenz

MIT – siehe [LICENSE](LICENSE).
