namespace Replay.Models.Events;

public sealed record NetFieldExportGroupChanged(
    float TimeSeconds,
    int PacketId,
    string Path,
    uint PathIndex,
    IReadOnlyList<string?> FieldNames)
    : ReplayEvent(TimeSeconds, PacketId);
