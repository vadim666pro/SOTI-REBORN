using Content.Server.Chat.Systems;
using Content.Server.CounterStrike;
using Content.Server.CounterStrike.Systems;
using Content.Server.Roles.Jobs;
using Content.Shared.CounterStrike;
using Content.Shared.CounterStrike.Components;
using Content.Shared.CounterStrike.Events;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Systems;

/// <summary>
/// Ends the round when all players on one Counter-Strike team are eliminated.
/// Routes through CsRoundControllerSystem for sub-round lifecycle management.
/// </summary>
public sealed class CounterStrikeTeamEliminationSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly CounterStrikeRoundStateSystem _csRoundState = default!;
    [Dependency] private readonly CsRoundControllerSystem _csRound = default!;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("cs-team-elimination");

    private bool _endingRound;
    private float _checkTimer;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<CsSubRoundEndedEvent>(OnSubRoundEnded);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel != GameRunLevel.InRound || _endingRound)
            return;

        if (!IsInActionPhase())
            return;

        _checkTimer += frameTime;
        if (_checkTimer >= 1f)
        {
            _checkTimer = 0f;
            TryEndRoundOnTeamElimination();
        }
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New != GameRunLevel.InRound)
        {
            _endingRound = false;
            _checkTimer = 0f;
        }
    }

    private void OnSubRoundEnded(CsSubRoundEndedEvent ev)
    {
        _endingRound = false;
        _checkTimer = 0f;
        _csRoundState.ResetBombPlanted();
        Sawmill.Info("[CS Elim] Sub-round ended — resetting elimination state for next round.");
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound || _endingRound)
            return;

        if (!IsInActionPhase())
            return;

        if (args.NewMobState == MobState.Alive)
            return;

        TryEndRoundOnTeamElimination();
    }

    private bool IsInActionPhase()
    {
        var query = EntityQueryEnumerator<CsRoundControllerComponent>();
        while (query.MoveNext(out _, out var controller))
        {
            return controller.CurrentPhase == CsRoundPhase.ActionPhase;
        }
        return false;
    }

    private void TryEndRoundOnTeamElimination()
    {
        var ctAlive = 0;
        var tAlive = 0;
        var ctTotal = 0;
        var tTotal = 0;
        var queried = 0;

        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            queried++;

            if (!_mind.TryGetMind(uid, out var mindId, out _))
                continue;

            if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
                continue;

            var alive = _mobState.IsAlive(uid, mobState);

            if (CounterStrikeTeams.CtJobs.Contains(jobId.Value))
            {
                ctTotal++;
                if (alive) ctAlive++;
            }
            else if (CounterStrikeTeams.TJobs.Contains(jobId.Value))
            {
                tTotal++;
                if (alive) tAlive++;
            }
        }

        Sawmill.Info($"[CS Elim] Check: queried={queried}, CT={ctAlive}/{ctTotal}, T={tAlive}/{tTotal}, bombPlanted={_csRoundState.BombPlanted}");

        if (ctTotal == 0 && tTotal == 0)
            return;

        string? loserTeam = null;

        if (_csRoundState.BombPlanted)
        {
            // Bomb is planted — only CT elimination matters (T wins if all CT die)
            if (ctAlive == 0 && ctTotal > 0)
                loserTeam = "КТ";
        }
        else
        {
            // No bomb — check both teams
            if (ctTotal == 0 && tAlive > 0)
                loserTeam = "КТ";
            else if (tTotal == 0 && ctAlive > 0)
                loserTeam = "Т";
            else if (ctAlive == 0 && tAlive > 0)
                loserTeam = "КТ";
            else if (tAlive == 0 && ctAlive > 0)
                loserTeam = "Т";
        }

        if (loserTeam == null)
            return;

        Sawmill.Info($"[CS Elim] Team wipe detected! Loser: {loserTeam}. Calling OnTeamWiped.");
        _endingRound = true;
        RaiseNetworkEvent(new AutoRoundEndingHudClearEvent());
        _csRound.OnTeamWiped(loserTeam);
    }
}
