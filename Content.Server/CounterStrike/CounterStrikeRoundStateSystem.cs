using Content.Server.GameTicking;
using Content.Shared.CounterStrike.Events;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using System.Linq;

namespace Content.Server.CounterStrike;

/// <summary>
/// Tracks Counter-Strike round state that changes bomb-plant behaviour
/// (disables round timer and team-elimination wins while bomb is live).
/// </summary>
public sealed class CounterStrikeRoundStateSystem : EntitySystem
{
    private static readonly ISawmill Sawmill = Logger.GetSawmill("cs-round-state");

    /// <summary>
    /// True once a bomb has been planted this round.
    /// While true, automatic round ending and team elimination are suppressed.
    /// </summary>
    public bool BombPlanted { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<CsBombPlantedEvent>(OnBombPlanted);
        SubscribeLocalEvent<CsBombDefusedEvent>(OnBombDefused);
        SubscribeLocalEvent<CsBombExplodedEvent>(OnBombExploded);
    }

    /// <summary>
    /// Returns true when this map is running Counter-Strike game rules.
    /// </summary>
    public bool IsCounterStrikeRound()
    {
        return EntityQuery<AutoRoundEndingRuleComponent, ActiveGameRuleComponent>().Any();
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.InRound)
            BombPlanted = false;
    }

    private void OnBombPlanted(CsBombPlantedEvent ev)
    {
        BombPlanted = true;
        Sawmill.Info($"[CS RoundState] BombPlanted = TRUE (bomb={ev.Bomb})");
        RaiseNetworkEvent(new AutoRoundEndingHudClearEvent());
    }

    private void OnBombDefused(CsBombDefusedEvent ev)
    {
        BombPlanted = false;
        Sawmill.Info($"[CS RoundState] BombPlanted = FALSE (defused)");
    }

    private void OnBombExploded(CsBombExplodedEvent ev)
    {
        BombPlanted = false;
        Sawmill.Info($"[CS RoundState] BombPlanted = FALSE (exploded)");
    }

    /// <summary>
    /// Resets BombPlanted state. Called at the start of each CS sub-round
    /// to prevent stale bomb state from suppressing team elimination checks.
    /// </summary>
    public void ResetBombPlanted()
    {
        if (BombPlanted)
        {
            BombPlanted = false;
            Sawmill.Info("[CS RoundState] BombPlanted = FALSE (sub-round reset)");
        }
    }
}
