using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Hitscan.Components;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanCyberneticDisruptionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanCyberneticDisruptionComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanCyberneticDisruptionComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        // Apply cybernetic disruption effect (simplified - full system not ported)
        if (_random.NextFloat() <= hitscan.Comp.DisableChance)
        {
            // Cybernetic disruption applied - full implementation would require
            // SharedCyberneticDisruptionSystem from Starlight
        }
    }
}
