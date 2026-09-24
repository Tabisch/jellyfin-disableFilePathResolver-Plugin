using System;
using System.IO;
using Jellyfin.Plugin.NoSeasonParsing.Configuration;

namespace Jellyfin.Plugin.NoSeasonParsing;

internal static class SeasonCalculator
{
    public static int? Get(PluginConfiguration config, DateTime? premiereDate, string? path)
    {
        if (config.SeasonMode == SeasonMode.Fixed)
        {
            return config.FixedSeason;
        }

        if (premiereDate.HasValue)
        {
            return premiereDate.Value.Year;
        }

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            // GetLastWriteTimeUtc does not throw for missing files, it returns 1601-01-01.
            var info = new FileInfo(path);
            return info.Exists ? info.LastWriteTimeUtc.Year : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
