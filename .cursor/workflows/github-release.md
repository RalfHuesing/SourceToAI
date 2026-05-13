# Workflow: GitHub-Release (SourceToAI)

Wenn der Nutzer diese Datei referenziert und ein Release wünscht, **diesen Ablauf vollständig und selbstständig ausführen** (inkl. Shell-Befehle). Arbeitsverzeichnis: Repository-Root.

## Voraussetzungen

- Branch typischerweise `master` (an `origin/HEAD` anpassen, falls abweichend).
- Versionsquelle: **nur** `SourceToAI.CLI/SourceToAI.CLI.csproj` → `<Version>x.y.z</Version>`.
- **PowerShell** auf Windows: keine `&&`-Ketten; Befehle mit `;` trennen oder `Set-Location` einmal setzen.

## Nicht zuverlässig (vorher gescheitert)

- `gh release create` **ohne** vorherige Anmeldung: `gh` meldet dann, dass `gh auth login` oder `GH_TOKEN` nötig ist. Nicht nur darauf verlassen.

## Empfohlener Weg (Reihenfolge)

### 1) Version bestimmen und in der csproj setzen

- Aktuelle `<Version>` aus `SourceToAI.CLI.csproj` lesen.
- Patch-Version um 1 erhöhen (z. B. `1.0.11` → `1.0.12`), **es sei denn**, der Nutzer nennt explizit eine andere Version.
- Nur die eine `<Version>`-Zeile ändern.

### 2) Qualitätssicherung

```powershell
Set-Location "<REPO-ROOT>"
dotnet test "SourceToAI.sln" -c Release
```

Bei Fehlern: nicht taggen/pushen; dem Nutzer die Testausgabe nennen.

### 3) Git: Commit, Push, annotierter Tag

Vor dem Tag: `git ls-remote --tags origin "refs/tags/vx.y.z"` — wenn der Tag **schon** existiert, nicht erneut pushen; mit dem Nutzer klären (neue Patch-Version oder anderer Tag).

Commit-Betreff (Conventional Commits, deutsch ok):

- `chore(release): Version x.y.z`

```powershell
Set-Location "<REPO-ROOT>"
git add "SourceToAI.CLI/SourceToAI.CLI.csproj"
git status
git commit -m "chore(release): Version x.y.z"
git push origin master
git tag -a "vx.y.z" -m "Release vx.y.z"
git push origin "vx.y.z"
```

`master` durch den tatsächlichen Release-Branch ersetzen, falls nötig. Tag-Name **immer** `v` + semver (z. B. `v1.0.12`).

### 4) GitHub-Release anlegen

**Zuerst** (wenn `gh` installiert und nutzbar):

```powershell
& "${env:ProgramFiles}\GitHub CLI\gh.exe" auth status
```

Wenn angemeldet:

```powershell
& "${env:ProgramFiles}\GitHub CLI\gh.exe" release create "vx.y.z" --repo "RalfHuesing/SourceToAI" --title "vx.y.z" --generate-notes
```

**Fallback** (hat bei Git Credential Manager + HTTPS-Push funktioniert): REST-API `POST /repos/RalfHuesing/SourceToAI/releases` mit Bearer-Token aus `git credential fill` — **Token niemals in Logs, Chat oder Dateien ausgeben**.

Ablauf nur beschreibend (der Agent setzt das in PowerShell um, ohne Secrets zu leaken):

1. Per `git credential fill` mit `protocol=https`, `host=github.com` und passendem `path` (z. B. `RalfHuesing/SourceToAI.git`) die Zeile `password=` auslesen.
2. `Invoke-RestMethod` gegen `https://api.github.com/repos/RalfHuesing/SourceToAI/releases` mit Header `Authorization: Bearer <password>`, `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2022-11-28`.
3. JSON-Body mindestens: `tag_name`, `name`, optional `body`, `generate_release_notes: true`.

Wenn weder `gh` (angemeldet) noch Credential-Fallback klappt: dem Nutzer klar sagen, er soll **`gh auth login`** einmalig ausführen oder **`GH_TOKEN`** setzen (Repo-Berechtigung `contents: write` bzw. klassisch: Release/Releases).

### 5) Abschluss dem Nutzer mitteilen

- Link: `https://github.com/RalfHuesing/SourceToAI/releases/tag/vx.y.z`
- Ob Binaries angehängt wurden: standardmäßig **nein**; nur wenn der Nutzer das explizit will (dann z. B. `dotnet publish` + `gh release upload` dokumentieren).

## Kurz-Checkliste für den Agenten

1. Version in csproj erhöht?
2. `dotnet test` Release grün?
3. Commit + Push + Tag `vx.y.z` gepusht?
4. GitHub-Release existiert (gh oder API-Fallback)?
5. Keine Geheimnisse ausgegeben?
