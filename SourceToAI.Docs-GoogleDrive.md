# Google Drive Sync Integration

SourceToAI bietet die Möglichkeit, nach einem erfolgreichen lokalen Export die generierten Markdown-Feeds automatisch in Google Drive hochzuladen. 

## Funktionsweise
- Der Upload überschreibt ("Clean Slate") bei jedem Lauf den projektspezifischen Ordner, um sicherzustellen, dass keine alten/gelöschten Dateien auf Google Drive übrig bleiben.
- Der verwendete OAuth-Scope (`drive.file`) stellt sicher, dass SourceToAI **nur** auf die von der App selbst erstellten Dateien und Ordner zugreifen kann, was maximale Sicherheit für deine restlichen privaten Daten bietet.

## Setup-Anleitung

1. **Google Cloud Console:** Erstelle ein neues Projekt und aktiviere die "Google Drive API".
2. **OAuth-Zustimmungsbildschirm:** Richte den Zustimmungsbildschirm ein (User Type: External/Internal). Wähle als Scope `.../auth/drive.file`.
3. **Anmeldedaten:** Erstelle "OAuth-Client-ID"-Anmeldedaten für den Anwendungstyp "Desktop-App".
4. **Client Secret speichern:** Lade die JSON-Datei herunter (beginnt meist mit `client_secret_...`) und speichere sie in folgendem Pfad auf deinem lokalen Rechner:
   `%LOCALAPPDATA%\GoogleDrive.CodeSync\`
   (Erstelle den Ordner, falls er noch nicht existiert).

Beim ersten Ausführen von SourceToAI mit aktiviertem Drive-Sync wird sich ein Browser-Fenster öffnen, in dem du die App einmalig autorisieren musst. Danach wird ein `token.json` im selben Verzeichnis abgelegt, das für künftige Uploads genutzt wird.

## Konfiguration

In der `appsettings.json` kannst du die Funktion aktivieren und den Root-Ordnernamen in Drive anpassen:

```json
{
    "GoogleDriveSync": {
        "Enabled": true,
        "TargetFolder": "Code"
    }
}
```
