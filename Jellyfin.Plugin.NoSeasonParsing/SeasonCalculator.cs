using System;
using System.IO;
using Jellyfin.Plugin.NoSeasonParsing.Configuration;

namespace Jellyfin.Plugin.NoSeasonParsing;

internal static class SeasonCalculator
{
    public static int? Get(PluginConfiguration config, DateTime? premiereDate, string path)
    {
        if (config.SeasonMode == SeasonMode.Fixed)
        {
            return config.FixedSeason;
        }

        if (premiereDate.HasValue)
        {
            return premiereDate.Value.Year;
        }

        try
        {
            return File.GetLastWriteTimeUtc(path).Year;
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
