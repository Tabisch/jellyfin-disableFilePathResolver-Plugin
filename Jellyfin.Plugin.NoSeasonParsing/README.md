# Jellyfin.Plugin.NoSeasonParsing

Standalone plugin: bypasses Jellyfin's filename-based season/episode detection for selected folders.

## Build
    dotnet publish -c Release -o out

Set `TargetFramework` and `JellyfinVersion` in the .csproj to match your server first.

## Install
1. Copy `out/Jellyfin.Plugin.NoSeasonParsing.dll` to `<jellyfin-config>/plugins/NoSeasonParsing_1.0.0.0/`
2. Restart Jellyfin
3. Dashboard -> Plugins -> No Season Parsing: add your folder(s)
4. Scan the library (for existing items: "Replace all metadata" on the show, or remove/re-add it)

## How it works
- `FlatEpisodeResolver` runs before the built-in resolvers and creates episodes without parsing the filename.
- `EnforceSeasonTask` runs after each scan and restores the configured season if Jellyfin re-parsed the path.
