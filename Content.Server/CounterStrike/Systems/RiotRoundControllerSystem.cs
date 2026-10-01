using System.Linq;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.CounterStrike.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.CounterStrike.Systems;

public sealed class RiotRoundControllerSystem : GameRuleSystem<RiotRoundControllerComponent>
{
    private const string CivilianJobId = "RiotPuzogradCivilian";
    private const string CopJobId = "RiotPuzogradCop";
    private const string SoldierJobId = "RiotPuzogradSoldier";

    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("riot-round");

    protected override void Started(EntityUid uid, RiotRoundControllerComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.TimeRemaining = component.RoundDuration;
        component.TimeElapsed = 0f;
        component.CaptureCheckTimer = 0f;
        component.SwatSpawned = false;
        component.RoundEnded = false;
        component.MissingPointWarned = false;

        var points = EntityQueryEnumerator<RiotControlPointComponent>();
        while (points.MoveNext(out _, out var point))
        {
            point.Owner = RiotTeam.None;
        }

        AssignStartingTeams();
        _chat.DispatchGlobalAnnouncement("RIOT has begun. Hold the control point until the round ends.", sender: "RIOT", playSound: false);
    }

    protected override void ActiveTick(EntityUid uid, RiotRoundControllerComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (component.RoundEnded)
            return;

        component.TimeElapsed += frameTime;
        component.TimeRemaining -= frameTime;

        if (!component.SwatSpawned && component.TimeElapsed >= component.SwatArrivalDelay)
        {
            component.SwatSpawned = true;
            SpawnSwatReinforcements();
        }

        component.CaptureCheckTimer -= frameTime;
        if (component.CaptureCheckTimer <= 0f)
        {
            component.CaptureCheckTimer = 0.5f;
            UpdateControlPoint(component);
        }

        var (civiliansAlive, lawEnforcementAlive) = CountLivingTeams();
        if (civiliansAlive == 0 || lawEnforcementAlive == 0)
        {
            EndByElimination(component, civiliansAlive, lawEnforcementAlive);
            return;
        }

        if (component.TimeRemaining <= 0f)
            EndByControlPoint(component);
    }

    private void AssignStartingTeams()
    {
        var station = _station.GetStations().FirstOrDefault();
        if (station == default)
        {
            Sawmill.Error("[RIOT] No station found; starting team roles were not assigned.");
            return;
        }

        var players = new List<(ICommonSession Session, EntityUid Body)>();
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent>();
        while (query.MoveNext(out var body, out _, out var mindContainer))
        {
            if (!mindContainer.HasMind || mindContainer.Mind is not { } mindId ||
                !TryComp<MindComponent>(mindId, out var mind) || mind.UserId is not { } userId ||
                !_playerManager.TryGetSessionById(userId, out var session))
            {
                continue;
            }

            players.Add((session, body));
        }

        if (players.Count < 2)
        {
            Sawmill.Warning($"[RIOT] Expected at least two players, found {players.Count}.");
            return;
        }

        var balancedPlayers = CounterStrikeTeamBalancer.ShuffleAndSplit(RobustRandom, players, out var civilianCount);
        for (var i = 0; i < balancedPlayers.Count; i++)
        {
            var (session, oldBody) = balancedPlayers[i];
            Del(oldBody);
            GameTicker.MakeJoinGame(session, station, i < civilianCount ? CivilianJobId : CopJobId, silent: true);
        }

        Sawmill.Info($"[RIOT] Assigned {civilianCount} civilians and {players.Count - civilianCount} cops.");
    }

    private void SpawnSwatReinforcements()
    {
        var station = _station.GetStations().FirstOrDefault();
        if (station == default)
        {
            Sawmill.Error("[RIOT] No station found; SWAT reinforcements could not spawn.");
            return;
        }

        var deadPlayers = new List<(ICommonSession Session, EntityUid Body)>();
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent, MobStateComponent>();
        while (query.MoveNext(out var body, out _, out var mindContainer, out var mobState))
        {
            if (_mobState.IsAlive(body, mobState) || !mindContainer.HasMind ||
                mindContainer.Mind is not { } mindId || !TryGetRiotTeam(mindId, out _) ||
                !TryComp<MindComponent>(mindId, out var mind) || mind.UserId is not { } userId ||
                !_playerManager.TryGetSessionById(userId, out var session))
            {
                continue;
            }

            deadPlayers.Add((session, body));
        }

        RobustRandom.Shuffle(deadPlayers);
        var reinforcements = deadPlayers.Count / 2;
        foreach (var (session, oldBody) in deadPlayers.Take(reinforcements))
        {
            Del(oldBody);
            GameTicker.MakeJoinGame(session, station, SoldierJobId, silent: true);
        }

        _chat.DispatchGlobalAnnouncement($"SWAT has arrived. {reinforcements} fallen players have returned as reinforcements.", sender: "RIOT", playSound: false);
    }

    private (int Civilians, int LawEnforcement) CountLivingTeams()
    {
        var civilians = 0;
        var lawEnforcement = 0;
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent, MobStateComponent>();
        while (query.MoveNext(out var body, out _, out var mindContainer, out var mobState))
        {
            if (!_mobState.IsAlive(body, mobState) || !mindContainer.HasMind ||
                mindContainer.Mind is not { } mindId || !TryGetRiotTeam(mindId, out var team))
            {
                continue;
            }

            if (team == RiotTeam.Civilians)
                civilians++;
            else if (team == RiotTeam.LawEnforcement)
                lawEnforcement++;
        }

        return (civilians, lawEnforcement);
    }

    private void UpdateControlPoint(RiotRoundControllerComponent component)
    {
        var pointQuery = EntityQueryEnumerator<RiotControlPointComponent, TransformComponent>();
        if (!pointQuery.MoveNext(out var pointUid, out var point, out _))
        {
            if (!component.MissingPointWarned)
            {
                Sawmill.Warning("[RIOT] No RiotControlPoint entity is on this map. Place the RiotControlPoint prototype to enable the hold objective.");
                component.MissingPointWarned = true;
            }

            return;
        }

        var pointCoordinates = _transform.GetMapCoordinates(pointUid);
        var civiliansPresent = false;
        var lawEnforcementPresent = false;
        var playerQuery = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent, MobStateComponent, TransformComponent>();
        while (playerQuery.MoveNext(out var body, out _, out var mindContainer, out var mobState, out _))
        {
            if (!_mobState.IsAlive(body, mobState) || !mindContainer.HasMind ||
                mindContainer.Mind is not { } mindId || !TryGetRiotTeam(mindId, out var team))
            {
                continue;
            }

            var playerCoordinates = _transform.GetMapCoordinates(body);
            if (playerCoordinates.MapId != pointCoordinates.MapId ||
                Vector2.DistanceSquared(playerCoordinates.Position, pointCoordinates.Position) > point.CaptureRadius * point.CaptureRadius)
            {
                continue;
            }

            civiliansPresent |= team == RiotTeam.Civilians;
            lawEnforcementPresent |= team == RiotTeam.LawEnforcement;
        }

        var newOwner = civiliansPresent == lawEnforcementPresent
            ? RiotTeam.None
            : civiliansPresent ? RiotTeam.Civilians : RiotTeam.LawEnforcement;

        if (newOwner == RiotTeam.None || newOwner == point.Owner)
            return;

        point.Owner = newOwner;
        var teamName = newOwner == RiotTeam.Civilians ? "Puzograd civilians" : "Puzograd cops and SWAT";
        _chat.DispatchGlobalAnnouncement($"{teamName} now control the point.", sender: "RIOT", playSound: false);
    }

    private bool TryGetRiotTeam(EntityUid mindId, out RiotTeam team)
    {
        team = RiotTeam.None;
        if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
            return false;

        team = jobId.Value.ToString() switch
        {
            CivilianJobId => RiotTeam.Civilians,
            CopJobId or SoldierJobId => RiotTeam.LawEnforcement,
            _ => RiotTeam.None,
        };

        return team != RiotTeam.None;
    }

    private void EndByElimination(RiotRoundControllerComponent component, int civiliansAlive, int lawEnforcementAlive)
    {
        var result = civiliansAlive == 0 && lawEnforcementAlive == 0
            ? "RIOT round ended in a draw: both teams were eliminated."
            : civiliansAlive == 0
                ? "RIOT round ended: Puzograd cops and SWAT win by elimination."
                : "RIOT round ended: Puzograd civilians win by elimination.";

        EndRound(component, result);
    }

    private void EndByControlPoint(RiotRoundControllerComponent component)
    {
        var pointQuery = EntityQueryEnumerator<RiotControlPointComponent>();
        if (!pointQuery.MoveNext(out _, out var point))
        {
            EndRound(component, "RIOT round ended in a draw: no control point was configured.");
            return;
        }

        var result = point.Owner switch
        {
            RiotTeam.Civilians => "RIOT round ended: Puzograd civilians win by holding the point.",
            RiotTeam.LawEnforcement => "RIOT round ended: Puzograd cops and SWAT win by holding the point.",
            _ => "RIOT round ended in a draw: the control point was never captured.",
        };

        EndRound(component, result);
    }

    private void EndRound(RiotRoundControllerComponent component, string result)
    {
        component.RoundEnded = true;
        Sawmill.Info($"[RIOT] {result}");
        GameTicker.EndRound(result);
    }
}