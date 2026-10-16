namespace Replay.Models.Events;

public sealed record RawUnframedPayloadReceived(
    float TimeSeconds,
    int PacketId,
    uint ChannelIndex,
    uint ActorNetGuid,
    uint ObjectNetGuid,
    string? ExportGroupPath,
    bool HasRepLayout,
    string Reason,
    byte[] Payload,
    int PayloadBits)
    : ReplayEvent(TimeSeconds, PacketId);
