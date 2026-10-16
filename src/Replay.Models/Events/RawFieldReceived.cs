namespace Replay.Models.Events;

public sealed record RawFieldReceived(
    float TimeSeconds,
    int PacketId,
    uint ChannelIndex,
    uint ActorNetGuid,
    uint ObjectNetGuid,
    string? ExportGroupPath,
    int Handle,
    string? Name,
    byte[] Payload,
    int PayloadBits)
    : ReplayEvent(TimeSeconds, PacketId);
