using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.NoSeasonParsing.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NoSeasonParsing.Tasks;

/// <summary>
/// Safety net after every library scan: checks all episodes in configured folders and puts the
/// configured season back, including episodes that existed before the plugin was installed.
/// </summary>
public class EnforceSeasonTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly SeasonEnforcer _enforcer;
    private readonly ILogger<EnforceSeasonTask> _logger;

    public EnforceSeasonTask(
        ILibraryManager libraryManager,
        SeasonEnforcer enforcer,
        ILogger<EnforceSeasonTask> logger)
    {
        _libraryManager = libraryManager;
        _enforcer = enforcer;
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
            .Where(e => config.Matches(e.Path))
            .ToList();

        var fixedCount = 0;

        for (var i = 0; i < episodes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await _enforcer.FixAsync(episodes[i], cancellationToken).ConfigureAwait(false))
            {
                fixedCount++;
            }

            progress.Report(100.0 * (i + 1) / episodes.Count);
        }

        _logger.LogInformation("No Season Parsing: checked {Checked} episode(s), fixed {Fixed}", episodes.Count, fixedCount);
    }
}
