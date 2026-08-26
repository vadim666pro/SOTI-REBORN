using Content.Server.Chat.Systems;
using Content.Server.CounterStrike.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Popups;
using Content.Shared.CounterStrike.Components;
using Content.Shared.CounterStrike.Events;
using Content.Shared.CounterStrike.Systems;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.CounterStrike.Systems;

/// <summary>
/// Handles Counter-Strike bomb planting, defusing, and explosion.
/// </summary>
public sealed class CsBombSystem : SharedCsBombSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly CsRoundControllerSystem _csRound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CsBombComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<CsBombComponent, CsBombPlantDoAfterEvent>(OnPlantDoAfter);
        SubscribeLocalEvent<CsBombComponent, CsBombDefuseDoAfterEvent>(OnDefuseDoAfter);
        SubscribeLocalEvent<CsBombComponent, TriggerEvent>(OnTriggered);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New != GameRunLevel.InRound)
            return;

        // Reset bomb sites when a new round starts.
        var siteQuery = EntityQueryEnumerator<CsBombSiteComponent>();
        while (siteQuery.MoveNext(out _, out var site))
        {
            site.Occupied = false;
            site.PlantedBomb = null;
        }
    }

    private void OnGetVerbs(EntityUid uid, CsBombComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!comp.Planted)
        {
            if (!IsTerrorist(args.User))
                return;

            if (AnyBombPlanted())
                return;

            if (!TryFindBombSite(args.User, out _, out _))
                return;

            args.Verbs.Add(new Verb
            {
                Text = "Установить бомбу",
                Act = () => StartPlant(uid, args.User, comp),
                Priority = 1,
            });
            return;
        }

        if (!IsCounterTerrorist(args.User))
            return;

        args.Verbs.Add(new Verb
        {
            Text = "Обезвредить",
            Act = () => StartDefuse(uid, args.User, comp),
            Priority = 2,
        });
    }

    private void StartPlant(EntityUid bomb, EntityUid user, CsBombComponent comp)
    {
        if (comp.Planted || !IsTerrorist(user) || AnyBombPlanted())
            return;

        if (!TryFindBombSite(user, out _, out _))
        {
            _popup.PopupEntity("Бомбу можно установить только на площадке.", bomb, user, PopupType.Medium);
            return;
        }

        var ev = new CsBombPlantDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(comp.PlantTime), ev, bomb, target: bomb)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = 1.5f,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void StartDefuse(EntityUid bomb, EntityUid user, CsBombComponent comp)
    {
        if (!comp.Planted || !IsCounterTerrorist(user))
            return;

        var ev = new CsBombDefuseDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(comp.DefuseTime), ev, bomb, target: bomb)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = 2f,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnPlantDoAfter(EntityUid bomb, CsBombComponent comp, CsBombPlantDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Planted)
            return;

        if (!IsTerrorist(args.User) || AnyBombPlanted())
            return;

        if (!TryFindBombSite(args.User, out var siteUid, out var site))
        {
            _popup.PopupEntity("Бомбу можно установить только на площадке.", bomb, args.User, PopupType.Medium);
            return;
        }

        var siteXform = Transform(siteUid);
        var bombXform = Transform(bomb);

        if (_hands.IsHolding(args.User, bomb, out _))
            _hands.TryDrop(args.User, bomb);

        _transform.SetCoordinates(bomb, siteXform.Coordinates);
        _transform.AnchorEntity(bomb, bombXform);

        comp.Planted = true;
        comp.Site = siteUid;
        Dirty(bomb, comp);

        site.Occupied = true;
        site.PlantedBomb = bomb;

        if (TryComp<TimerTriggerComponent>(bomb, out var timer))
            _trigger.ActivateTimerTrigger((bomb, timer));

        _chat.DispatchGlobalAnnouncement("Бомба установлена.", sender: "Мировая арена");
        _popup.PopupEntity("Бомба установлена.", bomb, PopupType.Large);

        RaiseLocalEvent(new CsBombPlantedEvent(bomb, siteUid));

        args.Handled = true;
    }

    private void OnDefuseDoAfter(EntityUid bomb, CsBombComponent comp, CsBombDefuseDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !comp.Planted)
            return;

        if (!IsCounterTerrorist(args.User))
            return;

        DefuseBomb(bomb, comp);
        args.Handled = true;
    }

    private void DefuseBomb(EntityUid bomb, CsBombComponent comp)
    {
        var siteUid = comp.Site;

        RemComp<ActiveTimerTriggerComponent>(bomb);

        if (siteUid is { } site && TryComp(site, out CsBombSiteComponent? siteComp))
        {
            siteComp.Occupied = false;
            siteComp.PlantedBomb = null;
            RaiseLocalEvent(new CsBombDefusedEvent(bomb, site));
        }

        _chat.DispatchGlobalAnnouncement("Бомба обезврежена. Победа CT!", sender: "Мировая арена");
        _csRound.OnBombDefused();

        QueueDel(bomb);
    }

    private void OnTriggered(EntityUid bomb, CsBombComponent comp, ref TriggerEvent args)
    {
        if (!comp.Planted || args.Key != "timer")
            return;

        ExplodeBomb(bomb, comp);
    }

    private void ExplodeBomb(EntityUid bomb, CsBombComponent comp)
    {
        var siteUid = comp.Site;

        if (siteUid is { } site && TryComp(site, out CsBombSiteComponent? siteComp))
        {
            siteComp.Occupied = false;
            siteComp.PlantedBomb = null;
        }

        // Explosion first — kills players while BombPlanted is still true
        _explosion.TriggerExplosive(bomb);
        _chat.DispatchGlobalAnnouncement("Бомба взорвалась! Победа Т!", sender: "Мировая арена");

        // Reset bomb state
        RaiseLocalEvent(new CsBombExplodedEvent(bomb, siteUid));

        // T wins by bomb explosion (CS convention)
        _csRound.OnBombExploded();

        QueueDel(bomb);
    }

    private bool TryFindBombSite(EntityUid user, out EntityUid siteUid, out CsBombSiteComponent site)
    {
        siteUid = EntityUid.Invalid;
        site = default!;

        var userXform = Transform(user);
        if (userXform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = _map.LocalToTile(gridUid, grid, userXform.Coordinates);
        var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (enumerator.MoveNext(out var ent))
        {
            if (ent is not { } siteEnt)
                continue;

            if (!TryComp(siteEnt, out CsBombSiteComponent? siteComp) || siteComp.Occupied)
                continue;

            siteUid = siteEnt;
            site = siteComp;
            return true;
        }

        return false;
    }

    private bool AnyBombPlanted()
    {
        var query = EntityQueryEnumerator<CsBombComponent>();
        while (query.MoveNext(out _, out var bomb))
        {
            if (bomb.Planted)
                return true;
        }

        return false;
    }
}
