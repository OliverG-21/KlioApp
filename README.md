# KlioApp

Desktop-Client für [Klio](https://github.com/OliverG-21/Klio) — eine selbst gehostete Foto- und Video-Galerie-App.

KlioApp läuft lokal auf Windows und macOS und verbindet sich über die REST API mit einer Klio-Instanz (z.B. auf dem Raspberry Pi). Bilder können lokal durchsucht, optional via Ollama automatisch getaggt und dann direkt in Klio hochgeladen werden.

## Features

- **Verbindung** zu Klio-Server via HTTP + API-Key
- **Ordner/Album-Baum** aus Klio laden, neue Ordner und Alben anlegen
- **Lokaler Datei-Browser** — Ordner öffnen, Bilder auswählen (JPG, PNG, HEIC, HEIF, GIF, WebP, MP4, MOV)
- **Upload-Warteschlange** — Dateien einem Album zuweisen und hochladen
- **Auto-Tagging via Ollama** — lokale Ollama-Instanz (llava-phi3 o.ä.) taggt Bilder vor dem Upload

## Voraussetzungen

- Laufende [Klio](https://github.com/OliverG-21/Klio) Instanz (v1.0+)
- API-Key in Klio unter `/admin/settings` gesetzt
- Optional: [Ollama](https://ollama.com) lokal installiert für Auto-Tagging

## Installation

### macOS (Apple Silicon)

1. `KlioApp.app` herunterladen
2. In den Ordner `Programme` ziehen
3. Beim ersten Start: Rechtsklick → Öffnen → Öffnen bestätigen

### Windows

1. `KlioApp-Windows.zip` herunterladen und entpacken
2. `KlioApp.UI.exe` starten

## Einrichtung

1. App starten
2. Oben rechts **Einstellungen** öffnen
3. **Server-URL** eintragen (z.B. `http://raspberrypi:5005`)
4. **API-Key** eintragen (aus Klio `/admin/settings`)
5. Optional: **Ollama-URL** und **Modell** eintragen (Standard: `http://localhost:11434`, `llava-phi3`)
6. **Testen & Speichern** klicken

## Workflow

1. Album im linken Baum auswählen
2. Rechts lokalen Ordner öffnen und Bilder auswählen
3. **+ Zur Warteschlange** klicken
4. Optional: **Auto-Tag** — taggt alle wartenden Bilder via Ollama
5. **Upload starten**

## Selbst kompilieren

Voraussetzung: [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/OliverG-21/KlioApp
cd KlioApp

# Starten (Entwicklung)
dotnet run --project src/KlioApp.UI

# Release macOS (Apple Silicon)
dotnet publish src/KlioApp.UI \
  -r osx-arm64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -c Release -o ./publish/macos

# Release Windows
dotnet publish src/KlioApp.UI \
  -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -c Release -o ./publish/windows
```

## Projektstruktur

```
KlioApp/
├── src/
│   ├── KlioApp.Core/      # Models + Interfaces
│   ├── KlioApp.Client/    # Klio REST API Client (HttpClient)
│   ├── KlioApp.Ollama/    # Ollama Client (lokales Auto-Tagging)
│   └── KlioApp.UI/        # Avalonia UI + ViewModels
```

## Lizenz

MIT
