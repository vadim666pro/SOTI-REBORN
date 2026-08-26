using Content.Shared.Actions;
using Content.Shared.CounterStrike;

namespace Content.Shared.CounterStrike.Events;

/// <summary>
/// Raised directed on the controller entity when the CS round phase changes.
/// </summary>
public sealed class CsRoundPhaseChangedEvent : EntityEventArgs
{
    public CsRoundPhase OldPhase;
    public CsRoundPhase NewPhase;

    public CsRoundPhaseChangedEvent(CsRoundPhase oldPhase, CsRoundPhase newPhase)
    {
        OldPhase = oldPhase;
        NewPhase = newPhase;
    }
}

/// <summary>
/// Raised broadcast when a CS sub-round ends with a winner.
/// </summary>
public sealed class CsSubRoundEndedEvent : EntityEventArgs
{
    public string WinnerTeam;
    public int CtWins;
    public int TWins;
    public int RoundNumber;

    public CsSubRoundEndedEvent(string winnerTeam, int ctWins, int tWins, int roundNumber)
    {
        WinnerTeam = winnerTeam;
        CtWins = ctWins;
        TWins = tWins;
        RoundNumber = roundNumber;
    }
}

/// <summary>
/// Raised when the player activates the CS uplink action in the hotbar.
/// </summary>
public sealed partial class CsOpenUplinkEvent : InstantActionEvent;
