using Robust.Shared.Serialization;

namespace Content.Shared.CounterStrike.Events;

/// <summary>
/// Sent to all clients every tick with current CS round state for HUD display.
/// </summary>
[Serializable, NetSerializable]
public sealed class CsRoundHudEvent : EntityEventArgs
{
    /// <summary>Seconds remaining in the current phase.</summary>
    public float TimerRemaining;

    /// <summary>Current phase name.</summary>
    public string Phase = string.Empty;

    /// <summary>CT wins across all sub-rounds.</summary>
    public int CtWins;

    /// <summary>T wins across all sub-rounds.</summary>
    public int TWins;

    /// <summary>Current sub-round number (1-based).</summary>
    public int RoundNumber;

    /// <summary>Total sub-rounds in the match.</summary>
    public int MaxRounds;

    /// <summary>Whether a bomb is currently planted.</summary>
    public bool BombPlanted;

    /// <summary>Seconds until bomb explodes (only valid when BombPlanted is true).</summary>
    public float BombTimerRemaining;

    public CsRoundHudEvent() { }

    public CsRoundHudEvent(float timerRemaining, string phase, int ctWins, int tWins, int roundNumber, int maxRounds, bool bombPlanted, float bombTimerRemaining)
    {
        TimerRemaining = timerRemaining;
        Phase = phase;
        CtWins = ctWins;
        TWins = tWins;
        RoundNumber = roundNumber;
        MaxRounds = maxRounds;
        BombPlanted = bombPlanted;
        BombTimerRemaining = bombTimerRemaining;
    }
}

/// <summary>
/// Sent to all clients to hide the CS round HUD.
/// </summary>
[Serializable, NetSerializable]
public sealed class CsRoundHudClearEvent : EntityEventArgs
{
}
