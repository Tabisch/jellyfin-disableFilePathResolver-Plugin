using Jellyfin.Plugin.NoSeasonParsing.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.NoSeasonParsing;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<SeasonEnforcer>();
        serviceCollection.AddHostedService<SeasonEnforcerHostedService>();
    }
}
