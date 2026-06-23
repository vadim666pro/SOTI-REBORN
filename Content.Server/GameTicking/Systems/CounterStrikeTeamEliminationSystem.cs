using Content.Server.Chat.Systems;
using Content.Server.Roles.Jobs;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Systems;

/// <summary>
/// Ends the round when all players on one Counter-Strike team are eliminated.
/// Teams are determined by job (CT/T uplink jobs configured in starting gear).
/// </summary>
public sealed class CounterStrikeTeamEliminationSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private static readonly HashSet<ProtoId<JobPrototype>> CtJobs =
    [
        "Musician",
        "Janitor",
        "Reporter",
        "Librarian",
    ];

    private static readonly HashSet<ProtoId<JobPrototype>> TJobs =
    [
        "Passenger",
        "Lawyer",
        "ServiceWorker",
        "Mime",
    ];

    private bool _endingRound;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New != GameRunLevel.InRound)
            _endingRound = false;
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound || _endingRound)
            return;

        if (args.NewMobState == MobState.Alive)
            return;

        TryEndRoundOnTeamElimination();
    }

    private void TryEndRoundOnTeamElimination()
    {
        var ctAlive = 0;
        var tAlive = 0;
        var ctTotal = 0;
        var tTotal = 0;

        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            if (!_mind.TryGetMind(uid, out var mindId, out _))
                continue;

            if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
                continue;

            if (CtJobs.Contains(jobId.Value))
            {
                ctTotal++;
                if (_mobState.IsAlive(uid, mobState))
                    ctAlive++;
            }
            else if (TJobs.Contains(jobId.Value))
            {
                tTotal++;
                if (_mobState.IsAlive(uid, mobState))
                    tAlive++;
            }
        }

        // Not a CS round unless both teams have spawned players.
        if (ctTotal == 0 || tTotal == 0)
            return;

        string? endText = null;
        string? announcement = null;

        if (ctAlive == 0 && tAlive > 0)
        {
            endText = "Команда CT уничтожена. Победа T!";
            announcement = endText;
        }
        else if (tAlive == 0 && ctAlive > 0)
        {
            endText = "Команда T уничтожена. Победа CT!";
            announcement = endText;
        }

        if (endText == null)
            return;

        _endingRound = true;
        RaiseNetworkEvent(new AutoRoundEndingHudClearEvent());
        _chat.DispatchGlobalAnnouncement(announcement!, sender: "Мировая арена");
        _gameTicker.EndRound(endText);
    }
}
