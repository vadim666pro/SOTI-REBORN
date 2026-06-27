using Content.Server.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Log;

namespace Content.Server.GameTicking.Systems;

/// <summary>
/// System that ends the round when an Assassin enters critical state for TTT mode.
/// </summary>
public sealed class AssassinRoundEndSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("assassin_round_end");
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        _sawmill.Info($"MobStateChangedEvent: Entity={args.Target}, OldState={args.OldMobState}, NewState={args.NewMobState}");

        // Check if the new state is Critical
        if (args.NewMobState != MobState.Critical)
        {
            _sawmill.Info($"Not critical state, skipping");
            return;
        }

        _sawmill.Info($"Entity is in critical state, checking if Assassin...");

        // Get the mind from the entity
        if (!_mindSystem.TryGetMind(args.Target, out var mindId, out var mindComp))
        {
            _sawmill.Info($"Entity has no mind, skipping");
            return;
        }

        _sawmill.Info($"Entity has mind: {mindId}, checking if antagonist...");

        // Check if the mind is an antagonist
        if (!_roleSystem.MindIsAntagonist(mindId))
        {
            _sawmill.Info($"Mind is not an antagonist, skipping");
            return;
        }

        _sawmill.Info($"Mind is an antagonist, checking for Assassin role...");

        // Check if the mind has the Assassin role specifically
        var roleInfo = _roleSystem.MindGetAllRoleInfo(new Entity<MindComponent?>(mindId, mindComp));
        foreach (var role in roleInfo)
        {
            _sawmill.Info($"Role: {role.Name}, Prototype: {role.Prototype}");
            if (role.Prototype == "Assassin")
            {
                _sawmill.Info($"Assassin found in critical state! Ending round.");
                _gameTicker.EndRound("Assassin has been critically injured! Round ending.");
                return;
            }
        }

        _sawmill.Info($"No Assassin role found on this entity");
    }
}
