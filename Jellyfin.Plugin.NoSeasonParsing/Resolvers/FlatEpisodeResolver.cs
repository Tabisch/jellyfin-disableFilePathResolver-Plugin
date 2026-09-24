using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.NoSeasonParsing.Resolvers;

/// <summary>
/// Runs before Jellyfin's built-in resolvers. For video files inside configured folders
/// it creates an Episode directly, so the filename is never parsed for season/episode
/// numbers and files are never grouped as versions of each other.
/// </summary>
public class FlatEpisodeResolver : IItemResolver
{
    private readonly NamingOptions _namingOptions;

    public FlatEpisodeResolver(NamingOptions namingOptions)
    {
        _namingOptions = namingOptions;
    }

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

        // Uses Jellyfin's own list of video extensions; parseName: false keeps the filename untouched.
        var videoInfo = VideoResolver.Resolve(args.Path, false, _namingOptions, parseName: false);
        if (videoInfo is null || videoInfo.IsStub || videoInfo.ExtraType is not null)
        {
            // Not a video, or a trailer/extra -> leave it to Jellyfin.
            return null;
        }

        var parent = args.Parent;
        if (parent is null)
        {
            return null;
        }

        var season = parent as Season ?? parent.GetParents().OfType<Season>().FirstOrDefault();
        var series = parent as Series ?? season?.Series ?? parent.GetParents().OfType<Series>().FirstOrDefault();
        if (series is null)
        {
            // Not inside a show -> leave it to Jellyfin.
            return null;
        }

        var episode = new Episode
        {
            Path = args.Path,
            Name = videoInfo.Name,
            VideoType = string.Equals(videoInfo.Container, "iso", StringComparison.OrdinalIgnoreCase)
                ? VideoType.Iso
                : VideoType.VideoFile,
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
