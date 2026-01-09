using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.StillWatching.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public int InactivityThresholdSeconds { get; set; }
        public bool EnableMessageDisplay { get; set; }

        public PluginConfiguration()
        {
            InactivityThresholdSeconds = 60; // Default 1 minute for testing
            EnableMessageDisplay = true;
        }
    }
}
