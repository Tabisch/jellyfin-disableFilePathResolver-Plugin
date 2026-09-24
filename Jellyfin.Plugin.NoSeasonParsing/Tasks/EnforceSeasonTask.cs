using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NoSeasonParsing.Tasks;

/// <summary>
/// Safety net after every library scan: Jellyfin can re-parse the path during metadata
/// refresh (FillMissingEpisodeNumbersFromPath). This task puts the configured season back
/// and re-refreshes the affected shows so their season folders are rebuilt.
/// </summary>
public class EnforceSeasonTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<EnforceSeasonTask> _logger;

    public EnforceSeasonTask(
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILogger<EnforceSeasonTask> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || config.Paths.Length == 0)
        {
            return;
        }

        var episodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                IsVirtualItem = false,
                Recursive = true
            })
            .OfType<Episode>()
            // Respect items the user locked in the metadata editor.
            .Where(e => !e.IsLocked && config.Matches(e.Path))
            .ToList();

        var touchedSeries = new HashSet<Guid>();

        for (var i = 0; i < episodes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ep = episodes[i];
            var wanted = SeasonCalculator.Get(config, ep.PremiereDate, ep.Path);

            if (wanted.HasValue && ep.ParentIndexNumber != wanted)
            {
                _logger.LogInformation("Season {Old} -> {New}: {Path}", ep.ParentIndexNumber, wanted, ep.Path);
                ep.ParentIndexNumber = wanted;
                await ep.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

                if (!ep.SeriesId.Equals(Guid.Empty))
                {
                    touchedSeries.Add(ep.SeriesId);
                }
            }

            progress.Report(100.0 * (i + 1) / Math.Max(episodes.Count, 1));
        }

        foreach (var seriesId in touchedSeries)
        {
            _providerManager.QueueRefresh(
                seriesId,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                RefreshPriority.Normal);
        }

        _logger.LogInformation("No Season Parsing: fixed {Count} show(s)", touchedSeries.Count);
    }
}
