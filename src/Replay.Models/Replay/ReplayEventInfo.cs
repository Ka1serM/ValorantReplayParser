namespace Replay.Models.Replay;

/// <summary>An event chunk of the replay file, as <c>FLocalFileEventInfo</c>: the recorder's AddEvent marker.</summary>
public sealed class ReplayEventInfo
{
    public int ChunkIndex { get; init; } = ReplayInfo.NoChunkIndex;
    public string Id { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Metadata { get; init; } = string.Empty;
    public uint Time1 { get; init; }
    public uint Time2 { get; init; }
    public int SizeInBytes { get; init; }
    public long EventDataOffset { get; init; }
}
