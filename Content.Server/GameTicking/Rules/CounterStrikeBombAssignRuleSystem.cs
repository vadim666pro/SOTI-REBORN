using Content.Server.GameTicking.Rules.Components;
using Content.Shared.CounterStrike;
using Content.Shared.CounterStrike.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Jobs;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Game rule that gives the C4 bomb to a random Terrorist player at round start.
/// </summary>
public sealed class CounterStrikeBombAssignRuleSystem : GameRuleSystem<CounterStrikeBombAssignRuleComponent>
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    protected override void Started(
        EntityUid uid,
        CounterStrikeBombAssignRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        if (EntityQuery<CsBombComponent>().Any())
        {
            Log.Info("[CS Bomb Assign] Skipping — a bomb already exists on the map.");
            return;
        }

        var candidates = new List<EntityUid>();

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out _, out var mind))
        {
            if (mind.CurrentEntity is not { } player)
                continue;

            if (!TryComp(player, out MobStateComponent? mobState) || !_mobState.IsAlive(player, mobState))
                continue;

            if (!_mind.TryGetMind(player, out var mindId, out _))
                continue;

            if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
                continue;

            if (CounterStrikeTeams.TJobs.Contains(jobId.Value))
                candidates.Add(player);
        }

        if (candidates.Count == 0)
        {
            Log.Warning("[CS Bomb Assign] No Terrorist players found — bomb not assigned.");
            return;
        }

        var carrier = RobustRandom.Pick(candidates);
        var bomb = Spawn(component.BombPrototype, Transform(carrier).Coordinates);

        if (_hands.TryPickupAnyHand(carrier, bomb)
            || _inventory.TryEquip(carrier, bomb, "back", silent: true, force: true))
        {
            Log.Info($"[CS Bomb Assign] Gave bomb to {ToPrettyString(carrier)}.");
            return;
        }

        Log.Warning($"[CS Bomb Assign] Gave bomb to {ToPrettyString(carrier)} but could not place it in hands or on back.");
    }
}
