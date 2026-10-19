using Replay.Models.Descriptors;
using Replay.Valorant.Descriptors.Agents.Sequoia;

namespace Replay.Valorant.Descriptors;

internal sealed class ValorantPathAliasProvider : IReplayPathAliasProvider
{
    public static ValorantPathAliasProvider Instance { get; } = new();

    private ValorantPathAliasProvider()
    {
    }

    public string? GetAlternatePath(string path)
    {
        const string cacheSuffix = "_ClassNetCache";
        var isCache = path.EndsWith(cacheSuffix, StringComparison.Ordinal);
        var classPath = isCache ? path[..^cacheSuffix.Length] : path;
        // Iso's arena covers use child-actor templates as their replicated archetypes.
        if (classPath is
            "Cover1_GEN_VARIABLE.ChildActor_GEN_VARIABLE_GameObject_Sequoia_X_ArenaCover_C_CAT" or
            "Cover2_GEN_VARIABLE.Cover2_GEN_VARIABLE_GameObject_Sequoia_X_ArenaCover_C_CAT" or
            "Cover3_GEN_VARIABLE.Cover3_GEN_VARIABLE_GameObject_Sequoia_X_ArenaCover_C_CAT")
        {
            return isCache ? SequoiaDescriptors.ArenaCoverPath + cacheSuffix : SequoiaDescriptors.ArenaCoverPath;
        }

        // Swiftplay's game states and player state are Blueprint children of BombGameState and BombPlayerState: they
        // replicate the parent's fields by the same names.
        if (classPath is
            "/Game/GameModes/_Development/Swiftplay_EndOfRoundCredits/Swiftplay_EoRCredits_GameState.Swiftplay_EoRCredits_GameState_C" or
            "/Game/GameModes/_Development/Swiftplay_EndOfRoundCredits/EOR5RoundHalf/Swiftplay_EoRCredits5RH_GameState.Swiftplay_EoRCredits5RH_GameState_C")
        {
            const string bombGameState = "/Game/GameModes/Bomb/BombGameState.BombGameState_C";
            return isCache ? bombGameState + cacheSuffix : bombGameState;
        }
        if (classPath == "/Game/GameModes/_Development/Swiftplay_EndOfRoundCredits/Swiftplay_EoRCredits_PlayerState.Swiftplay_EoRCredits_PlayerState_C")
        {
            const string bombPlayerState = "/Game/GameModes/Bomb/BombPlayerState.BombPlayerState_C";
            return isCache ? bombPlayerState + cacheSuffix : bombPlayerState;
        }

        const string coreSegment = "/_Core/";
        const string charactersRoot = "/Game/Characters/";

        var coreSegmentIndex = path.IndexOf(coreSegment, StringComparison.Ordinal);
        if (coreSegmentIndex >= 0)
        {
            return string.Concat(
                path.AsSpan(0, coreSegmentIndex),
                "/",
                path.AsSpan(coreSegmentIndex + coreSegment.Length));
        }

        if (path.StartsWith(charactersRoot, StringComparison.Ordinal))
        {
            return string.Concat(
                charactersRoot,
                "_Core/",
                path.AsSpan(charactersRoot.Length));
        }

        return null;
    }
}
