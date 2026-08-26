namespace Content.Shared.CounterStrike;

/// <summary>
/// Phases of a single Counter-Strike round within the global SS14 round.
/// </summary>
public enum CsRoundPhase : byte
{
    /// <summary>Players are frozen, can buy equipment.</summary>
    FreezeTime,

    /// <summary>Active combat phase.</summary>
    ActionPhase,

    /// <summary>Short pause between rounds.</summary>
    PostAction,
}
