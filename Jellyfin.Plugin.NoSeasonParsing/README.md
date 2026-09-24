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
4. Scan the library. Episodes already in the library are corrected by the scan too.

## How it works
- `FlatEpisodeResolver` runs before the built-in resolvers and creates episodes without parsing the filename.
- `SeasonEnforcer` puts the configured season back whenever Jellyfin saves an episode (scans, "Refresh metadata", "Replace all metadata"), then refreshes the show so its seasons are rebuilt.
- `EnforceSeasonTask` runs after each library scan and checks every episode in the configured folders.
- Episode numbers are still read from the filename; only the season is overridden.
