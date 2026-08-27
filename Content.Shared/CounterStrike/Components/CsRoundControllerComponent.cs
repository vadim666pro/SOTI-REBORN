using Content.Shared.CounterStrike;
using Robust.Shared.GameStates;

namespace Content.Shared.CounterStrike.Components;

/// <summary>
/// Core state for the Counter-Strike round controller. Placed on a map entity.
/// Drives phase transitions, tracks score across sub-rounds, and manages the
/// 6-round cycle within a single global SS14 round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CsRoundControllerComponent : Component
{
    /// <summary>
    /// Duration of each phase in seconds.
    /// </summary>
    public const float FreezeTimeDuration = 15f;
    public const float ActionPhaseDuration = 120f;
    public const float PostActionDuration = 10f;

    /// <summary>
    /// Seconds until bomb explodes after planting.
    /// </summary>
    public const float BombTimerDuration = 40f;

    /// <summary>
    /// Wins needed by one team to end the match.
    /// </summary>
    public const int WinsNeeded = 5;

    /// <summary>
    /// Starting Telecrystals for each player.
    /// </summary>
    public const int StartingTC = 19;

    /// <summary>
    /// Maximum Telecrystals a player can hold.
    /// </summary>
    public const int MaxTC = 100;

    /// <summary>
    /// TC reward for winning a sub-round.
    /// </summary>
    public const int WinBonusTC = 25;

    /// <summary>
    /// TC reward for losing a sub-round.
    /// </summary>
    public const int LossBonusTC = 15;

    /// <summary>
    /// Current phase of the CS round cycle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CsRoundPhase CurrentPhase = CsRoundPhase.FreezeTime;

    /// <summary>
    /// Timer tracking time remaining in the current phase (seconds).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Timer;

    /// <summary>
    /// Total sub-rounds completed so far.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TotalRoundsPlayed;

    /// <summary>
    /// Counter-Terrorist wins across all sub-rounds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CtWins;

    /// <summary>
    /// Terrorist wins across all sub-rounds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TWins;

    /// <summary>
    /// Countdown timer for bomb explosion (seconds remaining).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BombTimer;
}
