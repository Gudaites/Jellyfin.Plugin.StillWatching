using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller;
using Jellyfin.Plugin.StillWatching.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.StillWatching
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IPluginServiceRegistrator
    {
        public override string Name => "Still Watching";

        public override Guid Id => Guid.Parse("96C89874-5555-46B4-8A8E-2B0387532050");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; } = null!;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }
        public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
        {
            services.AddHostedService<InactivityWatcher>();
        }
    }
}
