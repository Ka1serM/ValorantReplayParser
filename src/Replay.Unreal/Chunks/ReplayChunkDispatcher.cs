using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Replay.Encoding.Archives;
using Replay.Encoding.Compression;
using Replay.Models.Errors;
using Replay.Models.Replay;
using Replay.Unreal.Header;
using Replay.Unreal.Info;
using Replay.Unreal.Readers;

namespace Replay.Unreal.Chunks;

internal sealed class ReplayChunkDispatcher
{
    private readonly ReplayDataChunkPayloadReader _replayDataChunkPayloadReader;
    private readonly IReplayDataChunkHandler _replayDataChunkHandler;

    public ReplayChunkDispatcher(
        IOodleDecompressor? oodleDecompressor = null,
        IReplayDataChunkHandler? replayDataChunkHandler = null)
    {
        _replayDataChunkPayloadReader = new ReplayDataChunkPayloadReader(oodleDecompressor);
        _replayDataChunkHandler = replayDataChunkHandler ?? new PlaybackPacketReplayDataChunkHandler();
    }

    public ReplayPreambleReadResult ReadPreamble(FBinaryArchive archive)
    {
        try
        {
            return ReadPreambleCore(archive);
        }
        catch (ArchiveReadException exception)
        {
            throw new InvalidReplayInfoException(
                $"Error while parsing replay chunk: {exception.Message}", exception);
        }
        catch (OverflowException exception)
        {
            throw new InvalidReplayInfoException(
                $"Error while parsing replay chunk: {exception.Message}", exception);
        }
    }

    public void DispatchRemaining(ReplayReaderContext context)
    {
        var logger = context.LoggerFactory?.CreateLogger<ReplayChunkDispatcher>()
            ?? NullLogger<ReplayChunkDispatcher>.Instance;

        while (!context.Archive.AtEnd)
        {
            DispatchNext(context, logger);
        }
    }

    private static ReplayPreambleReadResult ReadPreambleCore(FBinaryArchive archive)
    {
        var replayInfoResult = new ReplayInfoReader(archive).Read();
        var replayInfo = replayInfoResult.Info;

        while (!archive.AtEnd)
        {
            var typeOffset = archive.Position;
            var chunkType = (ReplayChunkType)archive.ReadUInt32();
            var chunk = new ReplayChunkInfo
            {
                ChunkType = chunkType,
                SizeInBytes = archive.ReadInt32(),
                TypeOffset = typeOffset,
                DataOffset = archive.Position,
            };

            var chunkIndex = replayInfo.Chunks.Count;
            replayInfo.Chunks.Add(chunk);

            switch (chunkType)
            {
                case ReplayChunkType.Header:
                    if (replayInfo.HeaderChunkIndex != ReplayInfo.NoChunkIndex)
                    {
                        throw new InvalidReplayInfoException("Replay info contains multiple header chunks.");
                    }

                    var headerArchive = new FBinaryArchive(archive.ReadBytes(chunk.SizeInBytes));
                    var headerResult = new ReplayHeaderReader(headerArchive).Read();
                    replayInfo.HeaderChunkIndex = chunkIndex;
                    replayInfo.IsValid = true;
                    return new ReplayPreambleReadResult(
                        replayInfo,
                        replayInfoResult.SerializationMetadata,
                        headerResult.Header,
                        headerResult.ReplayVersion,
                        headerResult.UEVersion);
                case ReplayChunkType.ReplayData:
                    throw new InvalidReplayInfoException("Replay data was encountered before the replay header.");
                default:
                    archive.Skip(chunk.SizeInBytes);
                    break;
            }
        }

        throw new InvalidReplayInfoException("Replay info does not contain a valid header chunk.");
    }

    private void DispatchNext(ReplayReaderContext context, ILogger<ReplayChunkDispatcher> logger)
    {
        try
        {
            DispatchNextCore(context, logger);
        }
        catch (ArchiveReadException exception)
        {
            throw new InvalidReplayDataException(
                $"Error while parsing replay data chunk: {exception.Message}", exception);
        }
        catch (OverflowException exception)
        {
            throw new InvalidReplayDataException(
                $"Error while parsing replay data chunk: {exception.Message}", exception);
        }
    }

    private void DispatchNextCore(ReplayReaderContext context, ILogger<ReplayChunkDispatcher> logger)
    {
        var typeOffset = context.Archive.Position;
        var chunkType = (ReplayChunkType)context.Archive.ReadUInt32();
        var chunk = new ReplayChunkInfo
        {
            ChunkType = chunkType,
            SizeInBytes = context.Archive.ReadInt32(),
            TypeOffset = typeOffset,
            DataOffset = context.Archive.Position,
        };

        context.ReplayInfo.Chunks.Add(chunk);
        var chunkIndex = context.ReplayInfo.Chunks.Count - 1;
        var chunkBytes = context.Archive.ReadBytes(chunk.SizeInBytes);
        var chunkArchive = new FBinaryArchive(chunkBytes);
        var chunkEndOffset = checked(chunk.DataOffset + chunk.SizeInBytes);

// Logs are cheap and only get called a few hundred times
#pragma warning disable CA1873
        logger.LogDebug("Dispatching replay chunk {ChunkIndex} of type {ChunkType}.", chunkIndex, chunkType);

        switch (chunkType)
        {
            case ReplayChunkType.Header:
                DispatchHeader(context, chunkIndex, chunkArchive);
                break;
            case ReplayChunkType.Checkpoint:
                logger.LogDebug("Skipping checkpoint chunk {ChunkIndex}.", chunkIndex);
                break;
            case ReplayChunkType.Event:
                DispatchEvent(context, chunkIndex, chunkArchive, chunk.DataOffset);
                break;
            case ReplayChunkType.ReplayData:
                DispatchReplayData(context, chunkIndex, chunkArchive, chunk.DataOffset);
                break;
            case ReplayChunkType.Unknown:
            default:
                logger.LogDebug("Skipping unknown replay chunk {ChunkIndex} of type {ChunkType}.", chunkIndex,
                    chunkType);
                break;
        }
#pragma warning restore CA1873
        context.Archive.Seek(chunkEndOffset);
    }

    private static void DispatchHeader(ReplayReaderContext context, int chunkIndex, FBinaryArchive chunkArchive)
    {
        if (context.ReplayInfo.HeaderChunkIndex != ReplayInfo.NoChunkIndex)
        {
            throw new InvalidReplayInfoException("Replay info contains multiple header chunks.");
        }

        context.ReplayInfo.HeaderChunkIndex = chunkIndex;
        var result = new ReplayHeaderReader(chunkArchive).Read();
        context.ReplayHeader = result.Header;
        context.ReplayVersion = result.ReplayVersion;
        context.UEVersion = result.UEVersion;
    }

    // FLocalFileNetworkReplayStreamer::ReadReplayInfo, ELocalFileChunkType::Event.
    private static void DispatchEvent(ReplayReaderContext context, int chunkIndex, FBinaryArchive chunkArchive,
        long chunkDataOffset)
    {
        var id = chunkArchive.ReadFString();
        var group = chunkArchive.ReadFString();
        var metadata = chunkArchive.ReadFString();
        var time1 = chunkArchive.ReadUInt32();
        var time2 = chunkArchive.ReadUInt32();
        var size = chunkArchive.ReadInt32();
        if (size < 0 || chunkArchive.Position + size > chunkArchive.Length)
        {
            throw new InvalidReplayDataException($"Event chunk {chunkIndex} declares an invalid data size {size}.");
        }

        context.ReplayInfo.Events.Add(new ReplayEventInfo
        {
            ChunkIndex = chunkIndex,
            Id = id,
            Group = group,
            Metadata = metadata,
            Time1 = time1,
            Time2 = time2,
            SizeInBytes = size,
            EventDataOffset = chunkDataOffset + chunkArchive.Position,
        });
    }

    private void DispatchReplayData(
        ReplayReaderContext context,
        int chunkIndex,
        FBinaryArchive chunkArchive,
        long chunkDataOffset)
    {
        var dataChunk = new ReplayDataChunkInfo
        {
            ChunkIndex = chunkIndex,
            StreamOffset = context.ReplayInfo.TotalDataSizeInBytes,
            Time1 = chunkArchive.ReadUInt32(),
            Time2 = chunkArchive.ReadUInt32(),
            SizeInBytes = chunkArchive.ReadInt32(),
            MemorySizeInBytes = chunkArchive.ReadInt32(),
            ReplayDataOffset = chunkDataOffset + chunkArchive.Position,
        };

        if (dataChunk.MemorySizeInBytes < 0)
        {
            throw new InvalidReplayDataException(
                $"Replay-data memory size {dataChunk.MemorySizeInBytes} is negative.");
        }

        context.ReplayInfo.TotalDataSizeInBytes =
            checked(context.ReplayInfo.TotalDataSizeInBytes + dataChunk.MemorySizeInBytes);
        context.ReplayInfo.DataChunks.Add(dataChunk);

        var replayDataArchive = _replayDataChunkPayloadReader.ReadPayload(context.ReplayInfo, dataChunk, chunkArchive);
        context.ReplayDataStream = replayDataArchive;
        _replayDataChunkHandler.Handle(context, replayDataArchive);
    }
}
