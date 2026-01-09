# Jellyfin "Are You Still Watching?" Plugin

A server-side plugin for Jellyfin that pauses playback after a configurable period of inactivity, similar to Netflix's "Are you still watching?" feature.

## Features

- **Activity Tracking**: Monitors active playback sessions.
- **Auto-Pause**: Automatically pauses playback if no user interaction (pause/resume/seek) occurs for a set duration (default: 2 hours).
- **User Notification**: Displays a "Still Watching?" message on supported clients when pausing.
- **Smart Resume**: Detects when the user resumes playback (unpauses) and resets the timer.

## Configuration

You can configure the plugin from the Jellyfin Dashboard > Plugins > Still Watching.

- **Inactivity Timeout**: The duration in seconds to wait before pausing (e.g., 3600 for 1 hour).
- **Show Message**: Toggle the display of the warning message.

## Building

1. Ensure you have the .NET 8.0 SDK installed.
2. Run the build command:
   ```bash
   dotnet publish -c Release
   ```
3. The compiled DLL will be in `bin/Release/net8.0/publish/`.

## Installation

1. Create a folder named `StillWatching` in your Jellyfin server's `plugins` directory.
2. Copy `Jellyfin.Plugin.StillWatching.dll` (and any dependencies if not already present, though usually just the main DLL is needed) into that folder.
3. Restart Jellyfin.
