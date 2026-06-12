using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace BackgroundMute;

internal sealed class AudioSessionService
{
    private readonly Dictionary<string, bool> _originalMuteBySessionId = new(StringComparer.Ordinal);

    public IReadOnlyList<ProgramEntry> GetProgramEntries()
    {
        var sessions = EnumerateAudioSessions();
        var byIdentity = new Dictionary<string, ProgramEntry>(StringComparer.OrdinalIgnoreCase);
        var sessionProcessIds = new HashSet<int>();

        foreach (var session in sessions)
        {
            if (session.ProcessId <= 0)
            {
                continue;
            }

            sessionProcessIds.Add(session.ProcessId);

            if (!TryGetProcessDisplayInfo(session.ProcessId, out var displayName, out var identityKey))
            {
                continue;
            }

            if (byIdentity.TryGetValue(identityKey, out var existing))
            {
                byIdentity[identityKey] = existing with
                {
                    IsPlayingAudio = existing.IsPlayingAudio || session.IsPlayingAudio
                };
            }
            else
            {
                byIdentity[identityKey] = new ProgramEntry(
                    session.ProcessId,
                    displayName,
                    identityKey,
                    session.IsPlayingAudio);
            }
        }

        foreach (var process in EnumerateForegroundAppProcesses())
        {
            using (process)
            {
                if (!TryGetProcessDisplayInfo(process.Id, out var displayName, out var identityKey))
                {
                    continue;
                }

                if (byIdentity.ContainsKey(identityKey))
                {
                    continue;
                }

                byIdentity[identityKey] = new ProgramEntry(
                    process.Id,
                    displayName,
                    identityKey,
                    sessionProcessIds.Contains(process.Id));
            }
        }

        return byIdentity.Values
            .OrderByDescending(static x => x.IsPlayingAudio)
            .ThenBy(static x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void ApplyMutePolicy(HashSet<string> selectedProgramKeys, string? foregroundIdentityKey)
    {
        var sessions = EnumerateAudioSessions();
        var activeSessionIds = new HashSet<string>(sessions.Select(static x => x.SessionId), StringComparer.Ordinal);

        foreach (var session in sessions)
        {
            var shouldManage = selectedProgramKeys.Contains(session.IdentityKey);
            var shouldMute = shouldManage
                && !string.Equals(session.IdentityKey, foregroundIdentityKey, StringComparison.OrdinalIgnoreCase);

            if (shouldMute)
            {
                if (!_originalMuteBySessionId.ContainsKey(session.SessionId))
                {
                    _originalMuteBySessionId[session.SessionId] = session.CurrentMute;
                }

                if (!session.CurrentMute)
                {
                    session.Volume.Mute = true;
                }
            }
            else if (_originalMuteBySessionId.TryGetValue(session.SessionId, out var originalMute))
            {
                if (session.CurrentMute != originalMute)
                {
                    session.Volume.Mute = originalMute;
                }

                _originalMuteBySessionId.Remove(session.SessionId);
            }
        }

        var deadSessions = _originalMuteBySessionId.Keys
            .Where(key => !activeSessionIds.Contains(key))
            .ToArray();

        foreach (var deadSession in deadSessions)
        {
            _originalMuteBySessionId.Remove(deadSession);
        }
    }

    public void RestoreAllMutedSessions()
    {
        if (_originalMuteBySessionId.Count == 0)
        {
            return;
        }

        try
        {
            var sessions = EnumerateAudioSessions();
            var sessionsBySessionId = new Dictionary<string, AudioSessionInfo>(sessions.Count, StringComparer.Ordinal);

            foreach (var session in sessions)
            {
                sessionsBySessionId[session.SessionId] = session;
            }

            foreach (var kvp in _originalMuteBySessionId)
            {
                var sessionId = kvp.Key;
                var originalMute = kvp.Value;

                if (sessionsBySessionId.TryGetValue(sessionId, out var session))
                {
                    if (session.CurrentMute != originalMute)
                    {
                        session.Volume.Mute = originalMute;
                    }
                }
            }

            _originalMuteBySessionId.Clear();
        }
        catch
        {
            // Silently ignore errors during cleanup to ensure app exit is not blocked
        }
    }

    public string? TryGetIdentityKeyForProcess(int processId)
    {
        if (!TryGetProcessDisplayInfo(processId, out _, out var identityKey))
        {
            return null;
        }

        return identityKey;
    }

    private static bool TryGetProcessDisplayInfo(int processId, out string displayName, out string identityKey)
    {
        displayName = string.Empty;
        identityKey = string.Empty;

        try
        {
            using var process = Process.GetProcessById(processId);
            var name = process.ProcessName;
            var title = process.MainWindowTitle;

            displayName = string.IsNullOrWhiteSpace(title)
                ? name
                : $"{name} ({title})";

            identityKey = name.ToLowerInvariant();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<Process> EnumerateForegroundAppProcesses()
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch
        {
            yield break;
        }

        foreach (var process in processes)
        {
            Process? keep = null;

            try
            {
                if (process.HasExited)
                {
                    process.Dispose();
                    continue;
                }

                if (process.Id <= 0 || process.MainWindowHandle == IntPtr.Zero)
                {
                    process.Dispose();
                    continue;
                }

                keep = process;
            }
            catch
            {
                process.Dispose();
                continue;
            }

            yield return keep;
        }
    }

    private static List<AudioSessionInfo> EnumerateAudioSessions()
    {
        var results = new List<AudioSessionInfo>();

        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var sessions = device.AudioSessionManager.Sessions;

        for (var i = 0; i < sessions.Count; i++)
        {
            using var session = sessions[i];
            var processId = (int)session.GetProcessID;
            if (processId <= 0)
            {
                continue;
            }

            var identityKey = string.Empty;
            _ = TryGetProcessDisplayInfo(processId, out _, out identityKey);

            var sessionId = session.GetSessionInstanceIdentifier;
            var currentMute = session.SimpleAudioVolume.Mute;
            var isPlayingAudio = session.AudioMeterInformation.MasterPeakValue > 0.001f;

            results.Add(new AudioSessionInfo(
                processId,
                identityKey,
                sessionId,
                currentMute,
                isPlayingAudio,
                session.SimpleAudioVolume));
        }

        return results;
    }

    private sealed record AudioSessionInfo(
        int ProcessId,
        string IdentityKey,
        string SessionId,
        bool CurrentMute,
        bool IsPlayingAudio,
        SimpleAudioVolume Volume);
}
