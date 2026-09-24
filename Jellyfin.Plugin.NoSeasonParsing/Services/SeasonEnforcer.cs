using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NoSeasonParsing.Services;

/// <summary>
/// Puts the configured season back on episodes in configured folders (and removes episode
/// numbers if enabled). Jellyfin re-parses the
/// season from the path in several places (existing items keep their stored season on rescan,
/// "Replace all metadata" forces a re-parse, embedded mp4 tags, metadata providers), so this is
/// applied whenever an episode is saved and again after every library scan.
/// </summary>
public sealed class SeasonEnforcer : IDisposable
{
    private static readonly TimeSpan RefreshDelay = TimeSpan.FromSeconds(5);

    // Stops a refresh -> re-parse -> fix -> refresh loop if something keeps overriding the season.
    private static readonly TimeSpan RefreshCooldown = TimeSpan.FromMinutes(2);

    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SeasonEnforcer> _logger;
    private readonly ConcurrentDictionary<Guid, byte> _inFlight = new();
    private readonly object _refreshLock = new();
    private readonly HashSet<Guid> _pendingSeries = new();
    private readonly Dictionary<Guid, DateTime> _lastRefresh = new();
    private readonly Timer _refreshTimer;

    public SeasonEnforcer(IProviderManager providerManager, IFileSystem fileSystem, ILogger<SeasonEnforcer> logger)
    {
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _logger = logger;
        _refreshTimer = new Timer(_ => QueueSeriesRefreshes(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Fixes the season of one episode if needed.
    /// </summary>
    /// <returns>True if the episode was changed.</returns>
    public async Task<bool> FixAsync(Episode episode, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null
            || episode.IsVirtualItem
            || episode.IsLocked // Respect items the user locked in the metadata editor.
            || !config.Matches(episode.Path))
        {
            return false;
        }

        var wanted = SeasonCalculator.Get(config, episode.PremiereDate, episode.Path);
        var fixSeason = wanted.HasValue && episode.ParentIndexNumber != wanted;
        var clearEpisodeNumber = config.DisableEpisodeNumbers
            && (episode.IndexNumber.HasValue || episode.IndexNumberEnd.HasValue);
        if (!fixSeason && !clearEpisodeNumber)
        {
            return false;
        }

        // Our own save raises ItemUpdated again; don't process the same episode twice at once.
        if (!_inFlight.TryAdd(episode.Id, 0))
        {
            return false;
        }

        try
        {
            if (clearEpisodeNumber)
            {
                _logger.LogDebug("Episode number {Number} removed: {Path}", episode.IndexNumber, episode.Path);
                episode.IndexNumber = null;
                episode.IndexNumberEnd = null;
            }

            if (fixSeason)
            {
                _logger.LogInformation("Season {Old} -> {New}: {Path}", episode.ParentIndexNumber, wanted, episode.Path);
                episode.ParentIndexNumber = wanted;

                // Link to the matching season if it already exists; otherwise the series refresh creates it.
                episode.SeasonId = episode.FindSeasonId();
                episode.SeasonName = episode.FindSeasonName();
            }

            await episode.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

            if (fixSeason)
            {
                ScheduleSeriesRefresh(episode.SeriesId);
            }

            return true;
        }
        finally
        {
            _inFlight.TryRemove(episode.Id, out _);
        }
    }

    /// <summary>
    /// Refreshes the show shortly after its episodes changed, so Jellyfin rebuilds its seasons.
    /// Batched so a scan touching many episodes causes one refresh per show.
    /// </summary>
    private void ScheduleSeriesRefresh(Guid seriesId)
    {
        if (seriesId.Equals(Guid.Empty))
        {
            return;
        }

        lock (_refreshLock)
        {
            _pendingSeries.Add(seriesId);
            _refreshTimer.Change(RefreshDelay, Timeout.InfiniteTimeSpan);
        }
    }

    private void QueueSeriesRefreshes()
    {
        List<Guid> seriesIds;
        var now = DateTime.UtcNow;

        lock (_refreshLock)
        {
            seriesIds = _pendingSeries
                .Where(id => !_lastRefresh.TryGetValue(id, out var last) || now - last > RefreshCooldown)
                .ToList();
            _pendingSeries.Clear();

            foreach (var id in seriesIds)
            {
                _lastRefresh[id] = now;
            }
        }

        foreach (var seriesId in seriesIds)
        {
            _providerManager.QueueRefresh(
                seriesId,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                RefreshPriority.Normal);
        }
    }

    public void Dispose()
    {
        _refreshTimer.Dispose();
    }
}
