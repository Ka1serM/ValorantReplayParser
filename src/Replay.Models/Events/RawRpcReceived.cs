namespace Replay.Models.Events;

public sealed record RawRpcReceived(
    float TimeSeconds,
    int PacketId,
    uint ChannelIndex,
    uint ActorNetGuid,
    uint ObjectNetGuid,
    string? ClassPath,
    int Handle,
    string? Name,
    byte[] Payload,
    int PayloadBits)
    : ReplayEvent(TimeSeconds, PacketId);
