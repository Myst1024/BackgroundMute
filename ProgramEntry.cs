namespace BackgroundMute;

internal sealed record ProgramEntry(
    int ProcessId,
    string DisplayName,
    string IdentityKey,
    bool IsPlayingAudio);
