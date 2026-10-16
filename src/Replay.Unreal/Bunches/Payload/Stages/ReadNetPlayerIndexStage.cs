using Replay.Models.Descriptors;
using Replay.Unreal.Channels;
using Replay.Unreal.Parsing;

namespace Replay.Unreal.Bunches.Payload.Stages;

internal sealed class ReadNetPlayerIndexStage : IBunchPayloadStage
{
    public BunchStageResult Process(ref BunchPayloadContext context)
    {
        if (!context.OpenedDynamicActor || context.Payload.AtEnd)
        {
            return BunchStageResult.Continue;
        }

        if (context.Channel is not null && IsPlayerController(context.Channel,
            context.ReaderContext.ExportBindingRegistry, context.ReaderContext.PlayerControllerPaths))
        {
            _ = context.Payload.ReadByte();
        }

        return BunchStageResult.Continue;
    }

    private static bool IsPlayerController(ActorChannelState channel, ExportBindingRegistry bindingRegistry,
        IReadOnlySet<string>? playerControllerPaths)
    {
        return IsPlayerControllerPath(channel.ReplicationClassPath, bindingRegistry, playerControllerPaths)
            || IsPlayerControllerPath(channel.ArchetypePath, bindingRegistry, playerControllerPaths)
            || IsPlayerControllerPath(channel.ActorPath, bindingRegistry, playerControllerPaths);
    }

    private static bool IsPlayerControllerPath(string? path, ExportBindingRegistry bindingRegistry,
        IReadOnlySet<string>? playerControllerPaths)
    {
        return path is not null && (playerControllerPaths?.Contains(path) == true ||
            bindingRegistry.GetExportGroupKind(path) == ExportGroupKind.PlayerController);
    }
}
