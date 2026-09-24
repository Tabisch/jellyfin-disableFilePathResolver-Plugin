# Jellyfin.Plugin.NoSeasonParsing

Standalone plugin: bypasses Jellyfin's filename-based season/episode detection for selected folders.

## Requirements
- Jellyfin 12.0 or newer
- .NET 10 SDK (to build)

## Build
    dotnet publish -c Release -o out

## Install from repository
1. Dashboard -> Plugins -> Repositories -> add
   `https://github.com/Tabisch/jellyfin-disableFilePathResolver-Plugin/releases/latest/download/manifest.json`
2. Catalog -> No Season Parsing -> Install, then restart Jellyfin

To publish a new version, run the Release workflow (Actions -> Release -> Run workflow) with a version like `1.0.1`, or push a tag `v1.0.1`. It builds the plugin and updates the manifest.

## Install manually
1. Copy `out/Jellyfin.Plugin.NoSeasonParsing.dll` to `<jellyfin-config>/plugins/NoSeasonParsing_1.0.0.0/`
2. Restart Jellyfin
3. Dashboard -> Plugins -> No Season Parsing: add your folder(s)
4. Scan the library (for existing items: "Replace all metadata" on the show, or remove/re-add it)

## How it works
- `FlatEpisodeResolver` runs before the built-in resolvers and creates episodes without parsing the filename.
- `EnforceSeasonTask` runs after each scan and restores the configured season if Jellyfin re-parsed the path.
