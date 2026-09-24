using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NoSeasonParsing.Services;

/// <summary>
/// Applies <see cref="SeasonEnforcer"/> whenever Jellyfin adds or saves an episode, so the season
/// is corrected right away (e.g. after "Refresh metadata"), not only after a full library scan.
/// </summary>
public sealed class SeasonEnforcerHostedService : IHostedService
{
    private readonly ILibraryManager _libraryManager;
    private readonly SeasonEnforcer _enforcer;
    private readonly ILogger<SeasonEnforcerHostedService> _logger;

    public SeasonEnforcerHostedService(
        ILibraryManager libraryManager,
        SeasonEnforcer enforcer,
        ILogger<SeasonEnforcerHostedService> logger)
    {
        _libraryManager = libraryManager;
        _enforcer = enforcer;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnItemChanged;
        _libraryManager.ItemUpdated += OnItemChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemChanged;
        _libraryManager.ItemUpdated -= OnItemChanged;
        return Task.CompletedTask;
    }

    private void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        if (e.Item is not Episode episode)
        {
            return;
        }

        // Run outside Jellyfin's save call.
        _ = Task.Run(async () =>
        {
            try
            {
                await _enforcer.FixAsync(episode, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fix season for {Path}", episode.Path);
            }
        });
    }
}
