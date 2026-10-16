namespace Replay.Models.Events;

public sealed record RawContentBlockReceived(
    float TimeSeconds,
    int PacketId,
    uint ChannelIndex,
    uint ActorNetGuid,
    uint ObjectNetGuid,
    uint ClassNetGuid,
    uint OuterNetGuid,
    bool IsActor,
    bool HasRepLayout,
    bool IsDeleted,
    byte DeleteFlags,
    string? ExportGroupPath)
    : ReplayEvent(TimeSeconds, PacketId);
