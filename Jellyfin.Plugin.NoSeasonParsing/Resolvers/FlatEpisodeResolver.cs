using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;

namespace Jellyfin.Plugin.NoSeasonParsing.Resolvers;

/// <summary>
/// Runs before Jellyfin's built-in resolvers. For video files inside configured folders
/// it creates an Episode directly, so the filename is never parsed for season/episode
/// numbers and files are never grouped as versions of each other.
/// </summary>
public class FlatEpisodeResolver : IItemResolver
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".webm", ".m4v", ".mov", ".avi"
    };

    public ResolverPriority Priority => ResolverPriority.Plugin;

    public BaseItem? ResolvePath(ItemResolveArgs args)
    {
        if (args.IsDirectory)
        {
            return null;
        }

        var config = Plugin.Instance?.Configuration;
        if (config is null || !config.Matches(args.Path))
        {
            return null;
        }

        if (!VideoExtensions.Contains(Path.GetExtension(args.Path)))
        {
            return null;
        }

        var parent = args.Parent;
        var season = parent as Season;
        var series = parent as Series ?? season?.Series ?? parent?.GetParents().OfType<Series>().FirstOrDefault();
        if (series is null)
        {
            // Not inside a show -> leave it to Jellyfin.
            return null;
        }

        var episode = new Episode
        {
            Path = args.Path,
            Name = Path.GetFileNameWithoutExtension(args.Path),
            ParentIndexNumber = SeasonCalculator.Get(config, null, args.Path),
            SeriesId = series.Id,
            SeriesName = series.Name
        };

        if (season is not null)
        {
            episode.SeasonId = season.Id;
            episode.SeasonName = season.Name;
        }

        return episode;
    }
}
