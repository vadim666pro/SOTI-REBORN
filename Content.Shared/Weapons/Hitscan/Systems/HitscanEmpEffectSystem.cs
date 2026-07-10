using Robust.Shared.GameObjects;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanEmpEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanEmpEffectComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanEmpEffectComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        // TODO: Implement actual EMP pulse effect when EMP system is available
        // For now, this is a placeholder that logs the EMP effect
        var coords = _transform.GetMapCoordinates(args.Data.HitEntity.Value);
        // Future implementation: _emp.EmpPulse(coords, hitscan.Comp.Range, hitscan.Comp.EnergyConsumption, hitscan.Comp.DisableDuration);
    }
}
