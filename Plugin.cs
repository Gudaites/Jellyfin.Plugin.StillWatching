using System;
using System.Collections.Generic;
using Jellyfin.Plugin.StillWatching.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.StillWatching
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILoggerFactory _loggerFactory;
        private InactivityWatcher? _watcher;

        public override string Name => "Still Watching";

        public override Guid Id => Guid.Parse("96C89874-5555-46B4-8A8E-2B0387532050");

        public Plugin(
            IApplicationPaths applicationPaths, 
            IXmlSerializer xmlSerializer,
            ISessionManager sessionManager,
            ILoggerFactory loggerFactory)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _sessionManager = sessionManager;
            _loggerFactory = loggerFactory;
            
            // Initialize the watcher
            _watcher = new InactivityWatcher(
                _sessionManager, 
                _loggerFactory.CreateLogger<InactivityWatcher>()
            );
        }

        public static Plugin? Instance { get; private set; }

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
    }
}
