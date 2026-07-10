using Content.Server.Temperature.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.GameObjects;

namespace Content.Server.Weapons.Hitscan.Systems;

public sealed class HitscanIgniteEffectSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanIgniteEffectComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanIgniteEffectComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        _temperature.ChangeHeat(args.Data.HitEntity.Value, hitscan.Comp.Temperature);
    }
}
