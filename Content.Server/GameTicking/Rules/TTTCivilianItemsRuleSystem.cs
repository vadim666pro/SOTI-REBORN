using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// System that handles giving civilian items to players in TTT mode.
/// All players except Assassin and Sheriff (Quartermaster) receive a random civilian item.
/// </summary>
public sealed class TTTCivilianItemsRuleSystem : GameRuleSystem<TTTCivilianItemsRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    protected override void Started(EntityUid uid, TTTCivilianItemsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (component.CivilianItems.Count == 0)
        {
            Log.Warning($"TTTCivilianItemsRule has no civilian items configured!");
            return;
        }

        // Iterate through all players with minds
        var query = EntityQuery<MindComponent>();
        foreach (var mindComp in query)
        {
            var mindId = mindComp.Owner;

            // Skip if the mind doesn't have a current entity
            if (mindComp.CurrentEntity == null)
                continue;

            var playerEntity = mindComp.CurrentEntity.Value;

            // Check if player has Assassin role
            if (_roles.MindHasRole<AssassinRoleComponent>(mindId))
            {
                Log.Debug($"Skipping {ToPrettyString(playerEntity)} - has Assassin role");
                continue;
            }

            // Check if player is Quartermaster (Sheriff)
            if (_roles.MindHasRole<JobRoleComponent>(mindId, out var jobRole))
            {
                var jobProto = jobRole.Value.Comp1.JobPrototype;
                if (jobProto == "Quartermaster")
                {
                    Log.Debug($"Skipping {ToPrettyString(playerEntity)} - is Quartermaster (Sheriff)");
                    continue;
                }
            }

            // Give a random civilian item to the player
            GiveCivilianItem(playerEntity, component.CivilianItems);
        }
    }

    private void GiveCivilianItem(EntityUid playerEntity, List<EntProtoId> civilianItems)
    {
        // Pick a random item from the list
        var randomItem = RobustRandom.Pick(civilianItems);

        // Try to spawn the item in pocket1, if that fails try pocket2
        if (_inventory.SpawnItemInSlot(playerEntity, "pocket1", randomItem))
        {
            Log.Info($"Gave {randomItem} to {ToPrettyString(playerEntity)} in pocket1");
        }
        else if (_inventory.SpawnItemInSlot(playerEntity, "pocket2", randomItem))
        {
            Log.Info($"Gave {randomItem} to {ToPrettyString(playerEntity)} in pocket2");
        }
        else
        {
            Log.Warning($"Failed to give {randomItem} to {ToPrettyString(playerEntity)} - no available pocket slot");
        }
    }
}
