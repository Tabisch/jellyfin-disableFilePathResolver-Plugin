using System;
using System.Collections.Generic;
using Jellyfin.Plugin.NoSeasonParsing.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.NoSeasonParsing;

/// <summary>
/// Standalone plugin that bypasses Jellyfin's name-based season/episode parsing
/// for configured folders.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "No Season Parsing";

    public override Guid Id => Guid.Parse("b6f3c2a4-7d1e-4f8a-9c3b-2e5d8a1f4c70");

    public override string Description =>
        "Disables filename-based season detection for selected folders and assigns seasons by year or a fixed number.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
        };
    }
}
