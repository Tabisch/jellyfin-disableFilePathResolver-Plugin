using System;
using System.IO;
using System.Linq;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NoSeasonParsing.Configuration;

public enum SeasonMode
{
    /// <summary>Season = premiere year (from any metadata source), falling back to the file's modification year.</summary>
    Year,

    /// <summary>All episodes go into one fixed season.</summary>
    Fixed
}

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Folders (as Jellyfin sees them) where season parsing is bypassed.</summary>
    public string[] Paths { get; set; } = Array.Empty<string>();

    public SeasonMode SeasonMode { get; set; } = SeasonMode.Year;

    public int FixedSeason { get; set; } = 1;

    public bool Matches(string? path)
    {
        if (string.IsNullOrEmpty(path) || Paths.Length == 0)
        {
            return false;
        }

        return Paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().TrimEnd('/', '\\'))
            .Any(p => path.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                      || path.StartsWith(p + "/", StringComparison.Ordinal));
    }
}
