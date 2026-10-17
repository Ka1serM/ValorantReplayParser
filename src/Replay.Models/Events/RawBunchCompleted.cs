namespace Replay.Models.Events;

public sealed record RawBunchCompleted(
    float TimeSeconds,
    int PacketId,
    uint ChannelIndex,
    uint ActorNetGuid)
    : ReplayEvent(TimeSeconds, PacketId);
