using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.StillWatching.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public int InactivityThresholdSeconds { get; set; }
        public bool EnableMessageDisplay { get; set; }

        public PluginConfiguration()
        {
            InactivityThresholdSeconds = 7200; // Default 2 hours (7200 seconds)
            EnableMessageDisplay = true;
        }
    }
}
