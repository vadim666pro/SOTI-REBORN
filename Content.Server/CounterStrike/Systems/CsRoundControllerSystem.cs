using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.CounterStrike;
using Content.Shared.CounterStrike.Components;
using Content.Shared.CounterStrike.Events;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Store;
using Content.Shared.Store.Components;

namespace Content.Server.CounterStrike.Systems;

/// <summary>
/// Drives the Counter-Strike sub-round cycle within a single global SS14 round.
/// Phases: FreezeTime (15s) → ActionPhase (120s) → PostAction (10s) → repeat 6 times.
/// After 6 sub-rounds the global round ends via GameTicker.
/// </summary>
public sealed class CsRoundControllerSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CsRoundEconomySystem _economy = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("cs-round-controller");

    private bool _frozenThisRound;
    private bool _bombPlanted;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CsRoundControllerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CsFrozenComponent, RefreshMovementSpeedModifiersEvent>(OnFrozenRefreshSpeed);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<CsBombPlantedEvent>(OnBombPlanted);
        SubscribeLocalEvent<CsBombDefusedEvent>(OnBombDefused);
        SubscribeLocalEvent<CsBombExplodedEvent>(OnBombExploded);
        SubscribeLocalEvent<CsOpenUplinkEvent>(OnCsOpenUplinkAction);
    }

    private void OnCsOpenUplinkAction(CsOpenUplinkEvent args)
    {
        if (!TryComp<StoreComponent>(args.Performer, out var store))
            return;
        if (!store.Balance.ContainsKey("Telecrystal"))
            return;
        _ui.TryToggleUi(args.Performer, StoreUiKey.Key, args.Performer);
        args.Handled = true;
    }

    private void OnFrozenRefreshSpeed(EntityUid uid, CsFrozenComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(0f);
    }

    private void OnBombPlanted(CsBombPlantedEvent ev)
    {
        _bombPlanted = true;

        var query = EntityQueryEnumerator<CsRoundControllerComponent>();
        while (query.MoveNext(out _, out var controller))
        {
            controller.BombTimer = CsRoundControllerComponent.BombTimerDuration;
        }

        Sawmill.Info("[CS Round] Bomb planted — timer paused.");
    }

    private void OnBombDefused(CsBombDefusedEvent ev)
    {
        _bombPlanted = false;
        ResetBombTimer();
    }

    private void OnBombExploded(CsBombExplodedEvent ev)
    {
        _bombPlanted = false;
        ResetBombTimer();
    }

    private void ResetBombTimer()
    {
        var query = EntityQueryEnumerator<CsRoundControllerComponent>();
        while (query.MoveNext(out _, out var controller))
        {
            controller.BombTimer = 0f;
        }
    }

    private void OnMapInit(EntityUid uid, CsRoundControllerComponent component, MapInitEvent args)
    {
        component.CurrentPhase = CsRoundPhase.FreezeTime;
        component.Timer = CsRoundControllerComponent.FreezeTimeDuration;
        component.TotalRoundsPlayed = 0;
        component.CtWins = 0;
        component.TWins = 0;
        _frozenThisRound = false;
        Sawmill.Info("[CS Round] Controller initialized. Starting FreezeTime.");
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.InRound && !_frozenThisRound)
        {
            // Only freeze if CS controller is active on the map
            var query = EntityQueryEnumerator<CsRoundControllerComponent>();
            if (!query.MoveNext(out _, out _))
                return;

            _frozenThisRound = true;
            _bombPlanted = false;
            FreezeAllPlayers();
            AssignBombToRandomT();
            Sawmill.Info("[CS Round] Round started — freezing all players, bomb assigned.");
        }

        if (ev.New == GameRunLevel.PreRoundLobby)
        {
            _frozenThisRound = false;
            UnfreezeAllPlayers();
            ClearHud();
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CsRoundControllerComponent>();
        while (query.MoveNext(out var uid, out var controller))
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound)
                continue;

            // Don't tick regular timer during ActionPhase if bomb is planted
            if (controller.CurrentPhase != CsRoundPhase.ActionPhase || !_bombPlanted)
                controller.Timer -= frameTime;

            // Tick bomb timer when bomb is planted
            if (_bombPlanted && controller.CurrentPhase == CsRoundPhase.ActionPhase)
                controller.BombTimer -= frameTime;

            // Restore TC for newly spawned players during FreezeTime
            if (controller.CurrentPhase == CsRoundPhase.FreezeTime)
            {
                _economy.RestorePlayerTc();
            }

            switch (controller.CurrentPhase)
            {
                case CsRoundPhase.FreezeTime:
                    UpdateFreezeTime(uid, controller);
                    break;
                case CsRoundPhase.ActionPhase:
                    UpdateActionPhase(uid, controller);
                    break;
                case CsRoundPhase.PostAction:
                    UpdatePostAction(uid, controller);
                    break;
            }

            BroadcastHud(controller);
        }
    }

    #region Phase Updates

    private void UpdateFreezeTime(EntityUid uid, CsRoundControllerComponent controller)
    {
        if (controller.Timer <= 0f)
        {
            Sawmill.Info("[CS Round] FreezeTime ended. Starting ActionPhase.");
            TransitionToPhase(uid, controller, CsRoundPhase.ActionPhase, CsRoundControllerComponent.ActionPhaseDuration);
        }
    }

    private void UpdateActionPhase(EntityUid uid, CsRoundControllerComponent controller)
    {
        if (_bombPlanted)
            return;

        if (controller.Timer <= 0f)
        {
            Sawmill.Info("[CS Round] ActionPhase timer expired. No bomb planted — CT wins by default.");
            EndActionPhase("КТ");
        }
    }

    private void UpdatePostAction(EntityUid uid, CsRoundControllerComponent controller)
    {
        if (controller.Timer <= 0f)
        {
            controller.TotalRoundsPlayed++;
            Sawmill.Info($"[CS Round] PostAction ended. Sub-round {controller.TotalRoundsPlayed} complete. Score: CT {controller.CtWins} — {controller.TWins} T");

            if (controller.CtWins < CsRoundControllerComponent.WinsNeeded && controller.TWins < CsRoundControllerComponent.WinsNeeded)
            {
                ResetRound(uid, controller);
                TransitionToPhase(uid, controller, CsRoundPhase.FreezeTime, CsRoundControllerComponent.FreezeTimeDuration);
            }
            else
            {
                UnfreezeAllPlayers();
                AnnounceMatchWinner(controller);
                ClearHud();
                _gameTicker.EndRound(BuildMatchResultText(controller));
            }
        }
    }

    #endregion

    #region Phase Transitions

    private void TransitionToPhase(
        EntityUid uid,
        CsRoundControllerComponent controller,
        CsRoundPhase newPhase,
        float duration)
    {
        var oldPhase = controller.CurrentPhase;

        OnPhaseExit(uid, controller, oldPhase);

        controller.CurrentPhase = newPhase;
        controller.Timer = duration;

        OnPhaseEnter(uid, controller, newPhase);

        RaiseLocalEvent(uid, new CsRoundPhaseChangedEvent(oldPhase, newPhase));
        Sawmill.Info($"[CS Round] Phase: {oldPhase} -> {newPhase} ({duration}s)");
    }

    private void OnPhaseExit(EntityUid uid, CsRoundControllerComponent controller, CsRoundPhase phase)
    {
        switch (phase)
        {
            case CsRoundPhase.FreezeTime:
                UnfreezeAllPlayers();
                CloseAllUplinks();
                break;
            case CsRoundPhase.PostAction:
                UnfreezeAllPlayers();
                break;
        }
    }

    private void OnPhaseEnter(EntityUid uid, CsRoundControllerComponent controller, CsRoundPhase phase)
    {
        switch (phase)
        {
            case CsRoundPhase.FreezeTime:
                FreezeAllPlayers();
                OpenAllUplinks();
                break;
        }
    }

    #endregion

    #region Freeze / Unfreeze

    private void FreezeAllPlayers()
    {
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            if (!_mobState.IsAlive(uid, mobState))
                continue;

            if (!_mind.TryGetMind(uid, out _, out _))
                continue;

            EnsureComp<CsFrozenComponent>(uid);
            _speedModifier.RefreshMovementSpeedModifiers(uid);
        }
    }

    private void UnfreezeAllPlayers()
    {
        var query = EntityQueryEnumerator<CsFrozenComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemComp<CsFrozenComponent>(uid);
            _speedModifier.RefreshMovementSpeedModifiers(uid);
        }
    }

    private void OpenAllUplinks()
    {
        var query = EntityQueryEnumerator<StoreComponent, HumanoidAppearanceComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var store, out _, out var mindContainer))
        {
            if (!mindContainer.HasMind)
                continue;
            if (!store.Balance.ContainsKey("Telecrystal"))
                continue;
            _ui.TryOpenUi(uid, StoreUiKey.Key, uid);
        }
    }

    private void CloseAllUplinks()
    {
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var store))
        {
            if (!store.Balance.ContainsKey("Telecrystal"))
                continue;
            _ui.CloseUi(uid, StoreUiKey.Key);
        }
    }

    #endregion

    #region ActionPhase Completion

    public void EndActionPhase(string winnerTeam)
    {
        var query = EntityQueryEnumerator<CsRoundControllerComponent>();
        while (query.MoveNext(out var uid, out var controller))
        {
            if (controller.CurrentPhase != CsRoundPhase.ActionPhase)
                continue;

            if (winnerTeam == "КТ" || winnerTeam == "CT")
                controller.CtWins++;
            else
                controller.TWins++;

            var message = $"Раунд завершён! Победа команды {winnerTeam}. Счёт: КТ {controller.CtWins} — {controller.TWins} Т";
            _chat.DispatchGlobalAnnouncement(message, sender: "Мировая арена", playSound: false);

            RaiseLocalEvent(new CsSubRoundEndedEvent(
                winnerTeam,
                controller.CtWins,
                controller.TWins,
                controller.TotalRoundsPlayed + 1));

            TransitionToPhase(uid, controller, CsRoundPhase.PostAction, CsRoundControllerComponent.PostActionDuration);
        }
    }

    #endregion

    #region Round Reset

    private void ResetRound(EntityUid uid, CsRoundControllerComponent controller)
    {
        Sawmill.Info("[CS Round] Resetting for next sub-round.");

        _bombPlanted = false;
        controller.BombTimer = 0f;
        CleanupRoundItems();
        DeleteOldBombs();
        RespawnPlayers();
        AssignBombToRandomT();
    }

    private void DeleteOldBombs()
    {
        var query = EntityQueryEnumerator<CsBombComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }
    }

    private void AssignBombToRandomT()
    {
        var candidates = new List<EntityUid>();

        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent>();
        while (query.MoveNext(out var bodyUid, out _, out var mindContainer))
        {
            if (!mindContainer.HasMind)
                continue;

            var mindId = mindContainer.Mind!.Value;

            if (!TryComp(mindId, out MindComponent? mind))
                continue;

            if (mind.CurrentEntity is not { } player)
                continue;

            if (!TryComp(player, out MobStateComponent? mobState) || !_mobState.IsAlive(player, mobState))
                continue;

            if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
                continue;

            if (CounterStrikeTeams.TJobs.Contains(jobId.Value))
                candidates.Add(player);
        }

        if (candidates.Count == 0)
        {
            Sawmill.Warning("[CS Round] No T players found — bomb not assigned.");
            return;
        }

        var carrier = _random.Pick(candidates);
        var bomb = Spawn("CsBomb", Transform(carrier).Coordinates);
        _hands.TryPickup(carrier, bomb);

        Sawmill.Info($"[CS Round] Gave bomb to {ToPrettyString(carrier)}.");
    }

    private void CleanupRoundItems()
    {
        var toDelete = new List<EntityUid>();

        // Delete dead humanoid bodies (corpses)
        var corpseQuery = EntityQueryEnumerator<HumanoidAppearanceComponent, MobStateComponent>();
        while (corpseQuery.MoveNext(out var uid, out _, out var mobState))
        {
            if (_mobState.IsAlive(uid, mobState))
                continue;

            // Don't delete living players' bodies
            if (_container.IsEntityInContainer(uid))
                continue;

            toDelete.Add(uid);
        }

        // Delete loose items (not in containers, not bombs)
        var itemQuery = EntityQueryEnumerator<ItemComponent>();
        while (itemQuery.MoveNext(out var uid, out _))
        {
            if (HasComp<CsBombComponent>(uid))
                continue;

            if (_container.IsEntityInContainer(uid))
                continue;

            toDelete.Add(uid);
        }

        foreach (var entity in toDelete)
        {
            QueueDel(entity);
        }

        Sawmill.Info($"[CS Round] Cleaned up {toDelete.Count} entities (corpses + items).");
    }

    private void RespawnPlayers()
    {
        var station = _station.GetStations().FirstOrNull();
        if (station == null)
        {
            Sawmill.Warning("[CS Round] No station found for respawn.");
            return;
        }

        var toRespawn = new List<(ICommonSession session, EntityUid oldBody, string jobId)>();

        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent>();
        while (query.MoveNext(out var bodyUid, out _, out var mindContainer))
        {
            if (!mindContainer.HasMind)
                continue;

            var mindId = mindContainer.Mind!.Value;

            if (!TryComp(mindId, out MindComponent? mind))
                continue;

            if (mind.UserId is not { } userId)
                continue;

            if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
                continue;

            if (!CounterStrikeTeams.CtJobs.Contains(jobId.Value)
                && !CounterStrikeTeams.TJobs.Contains(jobId.Value))
                continue;

            if (!_playerManager.TryGetSessionById(userId, out var session))
                continue;

            _economy.SaveTcForRespawn(bodyUid, userId);

            toRespawn.Add((session, bodyUid, jobId.Value));
        }

        Sawmill.Info($"[CS Round] RespawnPlayers: found {toRespawn.Count} players, station={station.Value}");

        foreach (var (session, oldBody, jobId) in toRespawn)
        {
            Sawmill.Info($"[CS Round] Respawning {session.Name} (job={jobId}, body={ToPrettyString(oldBody)})");

            QueueDel(oldBody);
            _gameTicker.MakeJoinGame(session, station.Value, jobId, silent: true);

            Sawmill.Info($"[CS Round] MakeJoinGame returned for {session.Name}");
        }
    }

    #endregion

    #region Win Triggers

    public void OnTeamWiped(string loserTeam)
    {
        string winnerTeam = loserTeam == "КТ" || loserTeam == "CT" ? "Т" : "КТ";
        Sawmill.Info($"[CS Round] Team wipe: {loserTeam} eliminated. {winnerTeam} wins.");
        EndActionPhase(winnerTeam);
    }

    public void OnBombExploded()
    {
        Sawmill.Info("[CS Round] Bomb exploded. T wins.");
        EndActionPhase("Т");
    }

    public void OnBombDefused()
    {
        Sawmill.Info("[CS Round] Bomb defused. CT wins.");
        EndActionPhase("КТ");
    }

    #endregion

    #region Match End

    private void AnnounceMatchWinner(CsRoundControllerComponent controller)
    {
        string result;
        if (controller.CtWins > controller.TWins)
            result = $"КТ побеждают в матче! Финальный счёт: КТ {controller.CtWins} — {controller.TWins} Т";
        else if (controller.TWins > controller.CtWins)
            result = $"Т побеждают в матче! Финальный счёт: КТ {controller.CtWins} — {controller.TWins} Т";
        else
            result = $"Ничья в матче! Финальный счёт: КТ {controller.CtWins} — {controller.TWins} Т";

        _chat.DispatchGlobalAnnouncement(result, sender: "Мировая арена");
    }

    private static string BuildMatchResultText(CsRoundControllerComponent controller)
    {
        return $"CS Match завершён. КТ {controller.CtWins} — {controller.TWins} Т";
    }

    #endregion

    #region HUD

    private void BroadcastHud(CsRoundControllerComponent controller)
    {
        var phaseName = controller.CurrentPhase switch
        {
            CsRoundPhase.FreezeTime => "ЗАКУПКА",
            CsRoundPhase.ActionPhase => "БОЙ",
            CsRoundPhase.PostAction => "ПАУЗА",
            _ => "—"
        };

        RaiseNetworkEvent(new CsRoundHudEvent(
            MathF.Max(0f, controller.Timer),
            phaseName,
            controller.CtWins,
            controller.TWins,
            controller.TotalRoundsPlayed + 1,
            CsRoundControllerComponent.WinsNeeded,
            _bombPlanted,
            MathF.Max(0f, controller.BombTimer)));
    }

    private void ClearHud()
    {
        RaiseNetworkEvent(new CsRoundHudClearEvent());
    }

    #endregion
}
