using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.NoSeasonParsing.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.NoSeasonParsing.Resolvers;

/// <summary>
/// Runs before Jellyfin's built-in resolvers. For video files inside configured folders it
/// creates one Episode per file: the filename is never parsed for season/episode numbers, and
/// files are never grouped as versions of each other or stacked as parts (part1/cd1/...).
/// </summary>
/// <remarks>
/// In TV libraries Jellyfin resolves a show folder's files with a multi-item resolver
/// (the movie resolver, which also does version grouping and stacking) before any per-file
/// resolver runs, so this has to be an <see cref="IMultiItemResolver"/> to take effect.
/// The per-file <see cref="IItemResolver"/> path covers files resolved individually.
/// </remarks>
public class FlatEpisodeResolver : IItemResolver, IMultiItemResolver
{
    private readonly NamingOptions _namingOptions;

    public FlatEpisodeResolver(NamingOptions namingOptions)
    {
        _namingOptions = namingOptions;
    }

    public ResolverPriority Priority => ResolverPriority.Plugin;

    public MultiItemResolverResult ResolveMultiple(
        Folder parent,
        List<FileSystemMetadata> files,
        CollectionType? collectionType,
        IDirectoryService directoryService)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || parent is null || !TryGetShow(parent, out _, out _))
        {
            // Jellyfin treats null as "not handled" (the interface just isn't annotated).
            return null!;
        }

        var result = new MultiItemResolverResult();

        foreach (var file in files)
        {
            var episode = file.IsDirectory ? null : CreateEpisode(config, parent, file.FullName);
            if (episode is null)
            {
                // Folders, non-video files and extras are resolved by Jellyfin as usual.
                result.ExtraFiles.Add(file);
            }
            else
            {
                result.Items.Add(episode);
            }
        }

        // Returning no items lets Jellyfin's own resolvers handle this folder.
        return result.Items.Count > 0 ? result : null!;
    }

    public BaseItem? ResolvePath(ItemResolveArgs args)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || args.IsDirectory || args.Parent is null)
        {
            return null;
        }

        return CreateEpisode(config, args.Parent, args.Path);
    }

    private Episode? CreateEpisode(PluginConfiguration config, Folder parent, string path)
    {
        if (!config.Matches(path) || !TryGetShow(parent, out var series, out var season))
        {
            return null;
        }

        // Uses Jellyfin's own list of video extensions; parseName: false keeps the filename untouched.
        var videoInfo = VideoResolver.Resolve(path, false, _namingOptions, parseName: false);
        if (videoInfo is null || videoInfo.IsStub || videoInfo.ExtraType is not null)
        {
            // Not a video, or a trailer/extra -> leave it to Jellyfin.
            return null;
        }

        var episode = new Episode
        {
            Path = path,
            Name = videoInfo.Name,
            VideoType = string.Equals(videoInfo.Container, "iso", StringComparison.OrdinalIgnoreCase)
                ? VideoType.Iso
                : VideoType.VideoFile,
            ParentIndexNumber = SeasonCalculator.Get(config, null, path),
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

    private static bool TryGetShow(Folder parent, out Series series, out Season? season)
    {
        season = parent as Season ?? parent.GetParents().OfType<Season>().FirstOrDefault();
        series = (parent as Series ?? season?.Series ?? parent.GetParents().OfType<Series>().FirstOrDefault())!;
        return series is not null;
    }
}
