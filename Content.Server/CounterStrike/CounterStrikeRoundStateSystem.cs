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
        RaiseNetworkEvent(new AutoRoundEndingHudClearEvent());
    }
}
