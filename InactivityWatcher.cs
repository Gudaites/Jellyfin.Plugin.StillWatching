using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.StillWatching.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.StillWatching
{
    public class InactivityWatcher : IDisposable
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<InactivityWatcher> _logger;
        private Timer? _timer;
        
        // Maps SessionId -> Last "User Interaction" Time
        private readonly ConcurrentDictionary<string, DateTime> _lastActionTime = new();
        
        // Maps SessionId -> Last known PositionTicks
        private readonly ConcurrentDictionary<string, long> _lastPositionTicks = new();
        
        // Maps SessionId -> Last known "IsPaused" state
        private readonly ConcurrentDictionary<string, bool> _lastPausedState = new();

        public InactivityWatcher(ISessionManager sessionManager, ILogger<InactivityWatcher> logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
            
            Start();
        }

        public void Start()
        {
            _logger.LogInformation("Still Watching Plugin Started");
            _sessionManager.PlaybackStart += OnPlaybackStart;
            _sessionManager.PlaybackStopped += OnPlaybackStopped;
            _sessionManager.PlaybackProgress += OnPlaybackProgress;
            
            // Check every 10 seconds
            _timer = new Timer(CheckInactivity, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
        {
            if (e.Session == null) return;
            _lastActionTime.TryRemove(e.Session.Id, out _);
            _lastPausedState.TryRemove(e.Session.Id, out _);
            _lastPositionTicks.TryRemove(e.Session.Id, out _);
        }

        private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
        {
            if (e.Session == null) return;
            UpdateActivity(e.Session.Id);
            _lastPausedState[e.Session.Id] = false;
        }

        private void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs e)
        {
            // Optional: You could update activity on progress if you wanted to track "active watching" 
            // but we only want to track *interaction*.
            // So we don't update activity here unless we detect a seek, which is handled in the timer loop 
            // or we could handle it here by comparing ticks.
            // For now, leaving the seek logic in the timer loop or implicit.
        }

        private void UpdateActivity(string sessionId)
        {
            _lastActionTime.AddOrUpdate(sessionId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
        }

        private async void CheckInactivity(object? state)
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || config.InactivityThresholdSeconds <= 0) return;

            var threshold = TimeSpan.FromSeconds(config.InactivityThresholdSeconds);
            var now = DateTime.UtcNow;
            var timerIntervalSeconds = 10;

            var activeSessions = _sessionManager.Sessions
                .Where(s => !string.IsNullOrEmpty(s.NowPlayingItem?.Id.ToString()))
                .ToList();

            foreach (var session in activeSessions)
            {
                bool currentPaused = session.PlayState.IsPaused;
                bool knownPaused = _lastPausedState.GetOrAdd(session.Id, currentPaused);
                long currentTicks = session.PlayState.PositionTicks ?? 0;
                
                bool userInteracted = false;

                // 1. Detect Resume
                if (knownPaused && !currentPaused)
                {
                    userInteracted = true;
                    _logger.LogDebug("Session {Id} resumed.", session.Id);
                }
                
                // 2. Detect Seek
                if (!knownPaused && !currentPaused && _lastPositionTicks.TryGetValue(session.Id, out var lastTicks))
                {
                    long timerTicks = timerIntervalSeconds * 10_000_000; 
                    long diff = Math.Abs(currentTicks - lastTicks);
                    long deviation = Math.Abs(diff - timerTicks);
                    
                    // If deviation is huge (seek)
                    if (deviation > (3 * 10_000_000)) 
                    {
                        userInteracted = true;
                        _logger.LogDebug("Session {Id} seek detected.", session.Id);
                    }
                }

                _lastPausedState[session.Id] = currentPaused;
                _lastPositionTicks[session.Id] = currentTicks;

                if (userInteracted)
                {
                    UpdateActivity(session.Id);
                }

                if (!_lastActionTime.TryGetValue(session.Id, out var lastActive))
                {
                    UpdateActivity(session.Id);
                    lastActive = now;
                }

                if (!currentPaused)
                {
                    if (now - lastActive > threshold)
                    {
                        _logger.LogInformation("StillWatching: Session {Id} inactive for {Duration}. Pausing.", session.Id, now - lastActive);

                        await PerformInactivityActions(session, config);
                        
                        UpdateActivity(session.Id);
                    }
                }
            }
        }

        private async Task PerformInactivityActions(SessionInfo session, PluginConfiguration config)
        {
            try 
            {
                _logger.LogInformation("StillWatching: Sending pause command to session {SessionId}", session.Id);
                
                await _sessionManager.SendPlaystateCommand(session.Id, session.Id, new PlaystateRequest
                {
                    Command = PlaystateCommand.Pause,
                    ControllingUserId = session.UserId.ToString()
                }, CancellationToken.None);

                _logger.LogInformation("StillWatching: Pause command sent successfully");

                if (config.EnableMessageDisplay)
                {
                    _logger.LogInformation("StillWatching: Sending message to session {SessionId}", session.Id);
                    
                    await _sessionManager.SendMessageCommand(session.Id, session.Id, new MessageCommand
                    {
                        Header = "Still Watching?",
                        Text = "Paused due to inactivity. Press Play to continue.",
                        TimeoutMs = 15000 
                    }, CancellationToken.None);
                    
                    _logger.LogInformation("StillWatching: Message sent successfully");
                }
                else
                {
                    _logger.LogInformation("StillWatching: Message display is disabled in configuration");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing inactivity actions for session {SessionId}", session.Id);
            }
        }

        public void Dispose()
        {
            _sessionManager.PlaybackStart -= OnPlaybackStart;
            _sessionManager.PlaybackStopped -= OnPlaybackStopped;
            _sessionManager.PlaybackProgress -= OnPlaybackProgress;
            _timer?.Dispose();
        }
    }
}
